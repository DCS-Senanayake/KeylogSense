using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using KeyloggerDetection.App;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Infrastructure.Logging;
using KeyloggerDetection.Infrastructure.Pipeline;
using KeyloggerDetection.Monitoring.FileBehaviour;
using KeyloggerDetection.Monitoring.NetworkBehaviour;
using KeyloggerDetection.Monitoring.Persistence;
using KeyloggerDetection.Monitoring.ProcessContext;
using KeyloggerDetection.Scoring;

namespace KeyloggerDetection.Evaluation;

internal sealed class EvaluationOrchestrator
{
    private readonly EvaluationOptions _options;

    public EvaluationOrchestrator(EvaluationOptions options)
    {
        _options = options;
    }

    public async Task RunAsync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputRoot = Path.GetFullPath(Path.Combine(repositoryRoot, _options.OutputRoot));
        var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var artifactsRoot = Path.Combine(outputRoot, "artifacts", runId);
        var detectorLogRoot = Path.Combine(artifactsRoot, "detector-logs");
        var evaluationLogPath = Path.Combine(artifactsRoot, "evaluation-app.log");

        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(artifactsRoot);
        Directory.CreateDirectory(detectorLogRoot);

        using var logger = new EvaluationAppLogger(evaluationLogPath);
        logger.LogInfo("Starting KeylogSense P9 evaluation workflow.");

        var config = BuildConfig(detectorLogRoot);
        var results = new List<EvaluationResultRow>();

        var baselineMetrics = await SampleCurrentProcessMetricsAsync(_options.IdleSampleSeconds);
        results.Add(EvaluationResultRow.CreateOverheadRow(
            name: "baseline-self",
            description: "Current evaluation process overhead before monitoring starts.",
            metrics: baselineMetrics,
            notes: "Used as a lightweight baseline before the detection pipeline is started."));

        IClock clock = new SystemClock();
        ITelemetryPipeline pipeline = new TelemetryPipeline(logger);
        var tracker = new DetectionTracker();
        var scoringEngine = new ObservingScoringEngine(new RiskScoringEngine(config, new AllowlistManager(config), clock));
        scoringEngine.Evaluated += tracker.RecordEvaluation;

        using var detectionLogger = new DetectionLogFileService(config);
        var fileWriteAnalyzer = new FileWriteAnalyzer(config, clock);
        var aggregator = new FeatureAggregator(logger, pipeline, scoringEngine, detectionLogger, fileWriteAnalyzer);
        aggregator.OnSuspiciousAlert += tracker.RecordAlert;

        using var coordinator = new MonitoringCoordinator(
            logger,
            [
                new ProcessCollector(logger, config, clock),
                new EtwFileCollector(logger, config, clock),
                new NetworkCollector(logger, config, clock),
                new PersistenceCollector(logger, config, clock)
            ],
            aggregator,
            pipeline);

        coordinator.Start();
        logger.LogInfo($"Monitoring started. Warm-up period: {_options.WarmupSeconds} seconds.");
        await Task.Delay(TimeSpan.FromSeconds(_options.WarmupSeconds));

        var capabilities = CapabilityProfile.FromLogEntries(logger.Entries);
        var idleMonitoringMetrics = await SampleCurrentProcessMetricsAsync(_options.IdleSampleSeconds);
        results.Add(EvaluationResultRow.CreateOverheadRow(
            name: "monitoring-idle",
            description: "Current evaluation process overhead while KeylogSense monitoring is active with no scenarios running.",
            metrics: idleMonitoringMetrics,
            notes: capabilities.FileTelemetryAvailable
                ? "File telemetry remained available during the warm-up period."
                : "File telemetry was unavailable during this run; see limitations in summary.md."));

        var simulatorExecutable = FindProjectArtifact(
            repositoryRoot,
            Path.Combine("tools", "KeyloggerDetection.Simulator"),
            "KeyloggerDetection.Simulator.exe");
        foreach (var scenario in BuildSimulatorScenarios(simulatorExecutable, capabilities))
        {
            var collectMetrics = string.Equals(scenario.Name, "combined-temp", StringComparison.OrdinalIgnoreCase);
            results.Add(await RunProcessScenarioAsync(scenario, tracker, logger, collectMetrics ? _options.ActiveSampleSeconds : null));
        }

        foreach (var scenario in BuildBenignScenarios())
        {
            results.Add(await RunProcessScenarioAsync(scenario, tracker, logger, null));
        }

        if (!string.IsNullOrWhiteSpace(_options.ApprovedSamplesManifestPath))
        {
            foreach (var scenario in BuildApprovedSampleScenarios())
            {
                results.Add(await RunProcessScenarioAsync(scenario, tracker, logger, null));
            }
        }

        coordinator.Stop();

        var resultsPath = Path.Combine(outputRoot, "results.csv");
        var summaryPath = Path.Combine(outputRoot, "summary.md");

        WriteResultsCsv(resultsPath, results);
        WriteSummary(summaryPath, results, runId, artifactsRoot, capabilities, logger.Entries, config);

        logger.LogInfo($"Evaluation results written to {resultsPath}");
        logger.LogInfo($"Evaluation summary written to {summaryPath}");
    }

    private DetectionConfig BuildConfig(string detectorLogRoot)
    {
        var config = new DetectionConfig
        {
            LogDirectory = detectorLogRoot
        };

        if (_options.AlertThresholdOverride.HasValue)
        {
            config.AlertThreshold = _options.AlertThresholdOverride.Value;
        }

        if (_options.MonitoringIntervalMsOverride.HasValue)
        {
            config.MonitoringIntervalMs = _options.MonitoringIntervalMsOverride.Value;
        }

        return config;
    }

    private List<ProcessLaunchScenario> BuildSimulatorScenarios(string simulatorExecutable, CapabilityProfile capabilities)
    {
        return new List<ProcessLaunchScenario>
        {
            new ProcessLaunchScenario
            {
                ScenarioType = "simulator",
                Name = "network-only-temp",
                Description = "Positive control: staged simulator launched from %TEMP% to exercise suspicious location + unsigned publisher + outbound network.",
                SourceExecutablePath = simulatorExecutable,
                Arguments = "network-only",
                ExpectedAlert = true,
                ExpectedBehaviorLabel = "Positive control",
                StageInSuspiciousTempLocation = true,
                TimeoutSeconds = 24
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "simulator",
                Name = "combined-temp",
                Description = "Positive control: staged simulator launched from %TEMP% with combined network/file/persistence behaviour. Should still alert even if file telemetry is unavailable.",
                SourceExecutablePath = simulatorExecutable,
                Arguments = "combined",
                ExpectedAlert = true,
                ExpectedBehaviorLabel = "Positive control",
                StageInSuspiciousTempLocation = true,
                TimeoutSeconds = 24,
                CleanupAfterScenario = true
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "simulator",
                Name = "file-only-temp",
                Description = "File logging probe from %TEMP%. Included to measure file telemetry coverage honestly.",
                SourceExecutablePath = simulatorExecutable,
                Arguments = "file-only",
                ExpectedAlert = capabilities.FileTelemetryAvailable ? true : null,
                ExpectedBehaviorLabel = capabilities.FileTelemetryAvailable ? "File telemetry positive control" : "Capability-limited observation",
                StageInSuspiciousTempLocation = true,
                TimeoutSeconds = 20,
                CapabilityLimited = !capabilities.FileTelemetryAvailable,
                CapabilityReason = capabilities.FileTelemetryAvailable
                    ? null
                    : "ETW file tracing is unavailable in this session, so file-write rules cannot be measured end-to-end."
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "simulator",
                Name = "persistence-only-temp",
                Description = "Safety-focused persistence probe. The current simulator writes an inert Run-key entry pointing to notepad.exe, so attribution to the simulator PID is not expected.",
                SourceExecutablePath = simulatorExecutable,
                Arguments = "persistence-only",
                ExpectedAlert = false,
                ExpectedBehaviorLabel = "Safety limitation control",
                StageInSuspiciousTempLocation = true,
                TimeoutSeconds = 18,
                CleanupAfterScenario = true,
                Notes = "This scenario is retained to document the current attribution limitation instead of overclaiming persistence coverage."
            }
        };
    }

    private List<ProcessLaunchScenario> BuildBenignScenarios()
    {
        var system32 = Environment.SystemDirectory;
        var powerShell = Path.Combine(system32, @"WindowsPowerShell\v1.0\powershell.exe");

        return new List<ProcessLaunchScenario>
        {
            new ProcessLaunchScenario
            {
                ScenarioType = "benign",
                Name = "notepad",
                Description = "Built-in text editor from System32 used as a signed benign baseline.",
                SourceExecutablePath = Path.Combine(system32, "notepad.exe"),
                ExpectedAlert = false,
                ExpectedBehaviorLabel = "Benign control",
                TimeoutSeconds = _options.ScenarioTimeoutSeconds
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "benign",
                Name = "calc",
                Description = "Built-in calculator used as a benign UI process baseline.",
                SourceExecutablePath = Path.Combine(system32, "calc.exe"),
                ExpectedAlert = false,
                ExpectedBehaviorLabel = "Benign control",
                TimeoutSeconds = _options.ScenarioTimeoutSeconds
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "benign",
                Name = "cmd-timeout",
                Description = "Short-lived console process running from System32.",
                SourceExecutablePath = Path.Combine(system32, "cmd.exe"),
                Arguments = "/c ping 127.0.0.1 -n 6 > nul",
                ExpectedAlert = false,
                ExpectedBehaviorLabel = "Benign control",
                TimeoutSeconds = _options.ScenarioTimeoutSeconds
            },
            new ProcessLaunchScenario
            {
                ScenarioType = "benign",
                Name = "powershell-sleep",
                Description = "Signed PowerShell host kept idle for a short period.",
                SourceExecutablePath = powerShell,
                Arguments = "-NoProfile -Command Start-Sleep -Seconds 8",
                ExpectedAlert = false,
                ExpectedBehaviorLabel = "Benign control",
                TimeoutSeconds = _options.ScenarioTimeoutSeconds
            }
        }
        .Where(s => File.Exists(s.SourceExecutablePath))
        .ToList();
    }

    private IEnumerable<ProcessLaunchScenario> BuildApprovedSampleScenarios()
    {
        var manifestPath = Path.GetFullPath(_options.ApprovedSamplesManifestPath!, Environment.CurrentDirectory);
        using var stream = File.OpenRead(manifestPath);
        var manifest = JsonSerializer.Deserialize<ApprovedSampleManifest>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ApprovedSampleManifest();

        foreach (var sample in manifest.Samples)
        {
            yield return new ProcessLaunchScenario
            {
                ScenarioType = "approved-sample",
                Name = sample.Label,
                Description = sample.Notes ?? "Approved non-destructive sample from external manifest.",
                SourceExecutablePath = sample.Path,
                Arguments = sample.Arguments ?? string.Empty,
                ExpectedAlert = true,
                ExpectedBehaviorLabel = "Approved sample",
                TimeoutSeconds = Math.Max(_options.ScenarioTimeoutSeconds, 20),
                Notes = $"Academic approval reference: {sample.AcademicApprovalReference ?? "Not supplied"}"
            };
        }
    }

    private async Task<EvaluationResultRow> RunProcessScenarioAsync(
        ProcessLaunchScenario scenario,
        DetectionTracker tracker,
        EvaluationAppLogger logger,
        int? metricSeconds)
    {
        if (!File.Exists(scenario.SourceExecutablePath))
        {
            return EvaluationResultRow.CreateSkipped(scenario, $"Executable not found: {scenario.SourceExecutablePath}");
        }

        logger.LogInfo($"Running scenario: {scenario.Name}");

        var executablePath = scenario.SourceExecutablePath;
        var workingDirectory = Path.GetDirectoryName(executablePath)!;

        if (scenario.StageInSuspiciousTempLocation)
        {
            executablePath = StageExecutableDirectoryInTemp(executablePath, scenario.Name);
            workingDirectory = Path.GetDirectoryName(executablePath)!;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = scenario.Arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return EvaluationResultRow.CreateSkipped(scenario, "Process.Start returned null.");
        }

        var startedAt = DateTime.UtcNow;
        tracker.BeginScenario(process.Id, startedAt);

        Task<MetricSample>? metricTask = metricSeconds.HasValue
            ? SampleCurrentProcessMetricsAsync(metricSeconds.Value)
            : null;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(scenario.TimeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            TryCloseOrKill(process);
        }

        if (scenario.CleanupAfterScenario)
        {
            await RunCleanupAsync(executablePath);
        }

        var metrics = metricTask is null ? null : await metricTask;
        await Task.Delay(TimeSpan.FromSeconds(2));

        var observation = tracker.GetObservation(process.Id);

        return new EvaluationResultRow
        {
            ScenarioType = scenario.ScenarioType,
            ScenarioName = scenario.Name,
            Description = scenario.Description,
            Executed = true,
            ExpectedAlert = scenario.ExpectedAlert,
            ExpectedBehaviorLabel = scenario.ExpectedBehaviorLabel,
            CapabilityLimited = scenario.CapabilityLimited,
            CapabilityReason = scenario.CapabilityReason,
            ProcessId = process.Id,
            ExecutablePath = executablePath,
            StartedAtUtc = startedAt,
            EndedAtUtc = DateTime.UtcNow,
            AlertRaised = observation?.AlertRaised ?? false,
            FalsePositive = scenario.ScenarioType == "benign" && (observation?.AlertRaised ?? false),
            HighestScore = observation?.HighestScore,
            TriggeredRules = observation?.HighestScoreRules,
            DetectionLatencySeconds = observation?.FirstAlertLatencySeconds(startedAt),
            AverageCpuPercent = metrics?.AverageCpuPercent,
            PeakCpuPercent = metrics?.PeakCpuPercent,
            AverageWorkingSetMb = metrics?.AverageWorkingSetMb,
            PeakWorkingSetMb = metrics?.PeakWorkingSetMb,
            Notes = CombineNotes(scenario.Notes, observation?.Notes)
        };
    }

    private static async Task<MetricSample> SampleCurrentProcessMetricsAsync(int seconds)
    {
        var cpuSamples = new List<double>();
        var memorySamples = new List<double>();
        var process = Process.GetCurrentProcess();
        var previousCpuTime = process.TotalProcessorTime;
        var previousTimestamp = DateTime.UtcNow;

        for (var i = 0; i < seconds; i++)
        {
            await Task.Delay(1000);
            process.Refresh();

            var currentCpuTime = process.TotalProcessorTime;
            var currentTimestamp = DateTime.UtcNow;
            var elapsedMs = (currentTimestamp - previousTimestamp).TotalMilliseconds;
            var cpuMs = (currentCpuTime - previousCpuTime).TotalMilliseconds;
            var cpuPercent = elapsedMs <= 0
                ? 0
                : (cpuMs / (elapsedMs * Environment.ProcessorCount)) * 100d;

            cpuSamples.Add(cpuPercent);
            memorySamples.Add(process.WorkingSet64 / 1024d / 1024d);

            previousCpuTime = currentCpuTime;
            previousTimestamp = currentTimestamp;
        }

        return new MetricSample(
            AverageCpuPercent: cpuSamples.Count == 0 ? 0 : cpuSamples.Average(),
            PeakCpuPercent: cpuSamples.Count == 0 ? 0 : cpuSamples.Max(),
            AverageWorkingSetMb: memorySamples.Count == 0 ? 0 : memorySamples.Average(),
            PeakWorkingSetMb: memorySamples.Count == 0 ? 0 : memorySamples.Max());
    }

    private static async Task RunCleanupAsync(string stagedExecutablePath)
    {
        var cleanupStartInfo = new ProcessStartInfo
        {
            FileName = stagedExecutablePath,
            Arguments = "cleanup",
            WorkingDirectory = Path.GetDirectoryName(stagedExecutablePath)!,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var cleanupProcess = Process.Start(cleanupStartInfo);
        if (cleanupProcess is not null)
        {
            await cleanupProcess.WaitForExitAsync();
        }
    }

    private static string StageExecutableDirectoryInTemp(string sourceExecutablePath, string scenarioName)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceExecutablePath)!;
        var destinationDirectory = Path.Combine(
            Path.GetTempPath(),
            "KeylogSense.Evaluation",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture),
            scenarioName);

        Directory.CreateDirectory(destinationDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory))
        {
            var destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, overwrite: true);
        }

        return Path.Combine(destinationDirectory, Path.GetFileName(sourceExecutablePath));
    }

    private static void TryCloseOrKill(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            if (process.CloseMainWindow() && process.WaitForExit(3000))
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit(3000);
        }
        catch
        {
        }
    }

    private static string? CombineNotes(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left)) return right;
        if (string.IsNullOrWhiteSpace(right)) return left;
        return left + " | " + right;
    }

    private static void WriteResultsCsv(string resultsPath, IEnumerable<EvaluationResultRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",",
            "ScenarioType",
            "ScenarioName",
            "Description",
            "Executed",
            "ExpectedAlert",
            "ExpectedBehaviorLabel",
            "CapabilityLimited",
            "CapabilityReason",
            "ProcessId",
            "ExecutablePath",
            "StartedAtUtc",
            "EndedAtUtc",
            "AlertRaised",
            "FalsePositive",
            "HighestScore",
            "TriggeredRules",
            "DetectionLatencySeconds",
            "AverageCpuPercent",
            "PeakCpuPercent",
            "AverageWorkingSetMb",
            "PeakWorkingSetMb",
            "Notes"));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",",
                EvaluationFormatting.EscapeCsv(row.ScenarioType),
                EvaluationFormatting.EscapeCsv(row.ScenarioName),
                EvaluationFormatting.EscapeCsv(row.Description),
                row.Executed,
                row.ExpectedAlert?.ToString() ?? string.Empty,
                EvaluationFormatting.EscapeCsv(row.ExpectedBehaviorLabel),
                row.CapabilityLimited,
                EvaluationFormatting.EscapeCsv(row.CapabilityReason),
                row.ProcessId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EvaluationFormatting.EscapeCsv(row.ExecutablePath),
                row.StartedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                row.EndedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty,
                row.AlertRaised?.ToString() ?? string.Empty,
                row.FalsePositive?.ToString() ?? string.Empty,
                row.HighestScore?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EvaluationFormatting.EscapeCsv(row.TriggeredRules),
                row.DetectionLatencySeconds?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                row.AverageCpuPercent?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                row.PeakCpuPercent?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                row.AverageWorkingSetMb?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                row.PeakWorkingSetMb?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                EvaluationFormatting.EscapeCsv(row.Notes)));
        }

        File.WriteAllText(resultsPath, builder.ToString(), Encoding.UTF8);
    }

    private static void WriteSummary(
        string summaryPath,
        IReadOnlyList<EvaluationResultRow> rows,
        string runId,
        string artifactsRoot,
        CapabilityProfile capabilities,
        IReadOnlyList<LogEntry> logEntries,
        DetectionConfig config)
    {
        var simulatorPositiveControls = rows
            .Where(r => r.ScenarioType is "simulator" or "approved-sample")
            .Where(r => r.ExpectedAlert == true && !r.CapabilityLimited && r.Executed)
            .ToList();
        var benignRows = rows.Where(r => r.ScenarioType == "benign" && r.Executed).ToList();
        var truePositives = simulatorPositiveControls.Count(r => r.AlertRaised == true);
        var falseNegatives = simulatorPositiveControls.Count(r => r.AlertRaised != true);
        var detectionRate = simulatorPositiveControls.Count == 0
            ? "N/A"
            : (truePositives * 100d / simulatorPositiveControls.Count).ToString("F1", CultureInfo.InvariantCulture) + "%";
        var falsePositives = benignRows.Count(r => r.FalsePositive == true);
        var fpRate = benignRows.Count == 0
            ? "N/A"
            : (falsePositives * 100d / benignRows.Count).ToString("F1", CultureInfo.InvariantCulture) + "%";
        var latencyRows = simulatorPositiveControls
            .Where(r => r.AlertRaised == true && r.DetectionLatencySeconds.HasValue)
            .Select(r => r.DetectionLatencySeconds!.Value)
            .ToList();
        var warningMessages = logEntries
            .Where(entry => entry.Level >= KeyloggerDetection.Core.Models.LogLevel.Warning)
            .Select(entry => entry.Message)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var baselineRow = rows.FirstOrDefault(r => r.ScenarioName == "baseline-self");
        var idleRow = rows.FirstOrDefault(r => r.ScenarioName == "monitoring-idle");
        var activeRow = rows.FirstOrDefault(r => r.ScenarioName == "combined-temp");

        var builder = new StringBuilder();
        builder.AppendLine("# P9 Evaluation Summary");
        builder.AppendLine();
        builder.AppendLine($"- Run ID: `{runId}`");
        builder.AppendLine($"- Generated: `{DateTime.UtcNow:O}`");
        builder.AppendLine($"- Detector alert threshold: `{config.AlertThreshold}`");
        builder.AppendLine($"- Monitoring interval: `{config.MonitoringIntervalMs} ms`");
        builder.AppendLine($"- Detector log artifacts: `{artifactsRoot}`");
        builder.AppendLine();
        builder.AppendLine("## Detection Capability");
        builder.AppendLine();
        builder.AppendLine($"- Eligible positive-control scenarios: `{simulatorPositiveControls.Count}`");
        builder.AppendLine($"- True positives: `{truePositives}`");
        builder.AppendLine($"- False negatives: `{falseNegatives}`");
        builder.AppendLine($"- Detection rate: `{detectionRate}`");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Expected | Alert Raised | Highest Score | Latency (s) | Notes |");
        builder.AppendLine("|---|---:|---:|---:|---:|---|");
        foreach (var row in rows.Where(r => r.ScenarioType is "simulator" or "approved-sample"))
        {
            builder.AppendLine($"| {EvaluationFormatting.EscapeMarkdown(row.ScenarioName)} | {EvaluationFormatting.FormatMaybeBool(row.ExpectedAlert)} | {EvaluationFormatting.FormatMaybeBool(row.AlertRaised)} | {EvaluationFormatting.FormatMaybeInt(row.HighestScore)} | {EvaluationFormatting.FormatMaybeDouble(row.DetectionLatencySeconds)} | {EvaluationFormatting.EscapeMarkdown(row.Notes ?? string.Empty)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## False Positives");
        builder.AppendLine();
        builder.AppendLine($"- Benign scenarios executed: `{benignRows.Count}`");
        builder.AppendLine($"- False positives: `{falsePositives}`");
        builder.AppendLine($"- False-positive rate: `{fpRate}`");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Alert Raised | Highest Score | Notes |");
        builder.AppendLine("|---|---:|---:|---|");
        foreach (var row in benignRows)
        {
            builder.AppendLine($"| {EvaluationFormatting.EscapeMarkdown(row.ScenarioName)} | {EvaluationFormatting.FormatMaybeBool(row.AlertRaised)} | {EvaluationFormatting.FormatMaybeInt(row.HighestScore)} | {EvaluationFormatting.EscapeMarkdown(row.Notes ?? string.Empty)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Detection Latency");
        builder.AppendLine();
        if (latencyRows.Count == 0)
        {
            builder.AppendLine("- No alert latencies were available from positive-control scenarios in this run.");
        }
        else
        {
            builder.AppendLine($"- Min latency: `{latencyRows.Min().ToString("F2", CultureInfo.InvariantCulture)} s`");
            builder.AppendLine($"- Max latency: `{latencyRows.Max().ToString("F2", CultureInfo.InvariantCulture)} s`");
            builder.AppendLine($"- Mean latency: `{latencyRows.Average().ToString("F2", CultureInfo.InvariantCulture)} s`");
        }

        builder.AppendLine();
        builder.AppendLine("## CPU and RAM Overhead");
        builder.AppendLine();
        builder.AppendLine("| Phase | Avg CPU % | Peak CPU % | Avg RAM MB | Peak RAM MB |");
        builder.AppendLine("|---|---:|---:|---:|---:|");
        AppendMetricRow(builder, "Baseline (before monitoring)", baselineRow);
        AppendMetricRow(builder, "Monitoring idle", idleRow);
        AppendMetricRow(builder, "Monitoring during combined-temp", activeRow);

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("- `results.csv` records each executed scenario, whether an alert was expected, whether it actually occurred, the highest observed score for the tracked PID, and any measured latency.");
        builder.AppendLine("- The detector's built-in CSV log only contains suspicious detections. A missing alert row does not imply that every telemetry source was active; consult the limitations section and `evaluation-app.log` when a scenario is capability-limited.");
        builder.AppendLine("- Positive-control simulator scenarios are staged under `%TEMP%` so the existing suspicious-location rule can be exercised safely without modifying the simulator's code or introducing real malicious behavior.");

        builder.AppendLine();
        builder.AppendLine("## Limitations");
        builder.AppendLine();
        builder.AppendLine($"- File telemetry available in this run: `{capabilities.FileTelemetryAvailable}`");
        if (warningMessages.Count == 0)
        {
            builder.AppendLine("- No runtime warnings were captured by the evaluation logger.");
        }
        else
        {
            foreach (var warning in warningMessages)
            {
                builder.AppendLine($"- {warning}");
            }
        }
        builder.AppendLine("- The persistence-only simulator remains intentionally safety-limited: it writes an inert Run-key entry for `notepad.exe`, so end-to-end attribution back to the simulator PID is not currently expected.");
        builder.AppendLine("- CPU and RAM figures in this workflow are process-level measurements for the evaluation host itself. Re-run inside an isolated Windows VM for dissertation-grade overhead figures and a cleaner baseline.");
        builder.AppendLine("- The tool cannot prove whether the current host is an isolated VM. Treat any approved-sample run as valid only when you have independently enforced the VM checklist in `docs/safe-testing-lab.md`.");

        File.WriteAllText(summaryPath, builder.ToString(), Encoding.UTF8);
    }

    private static void AppendMetricRow(StringBuilder builder, string label, EvaluationResultRow? row)
    {
        builder.AppendLine($"| {label} | {EvaluationFormatting.FormatMaybeDouble(row?.AverageCpuPercent)} | {EvaluationFormatting.FormatMaybeDouble(row?.PeakCpuPercent)} | {EvaluationFormatting.FormatMaybeDouble(row?.AverageWorkingSetMb)} | {EvaluationFormatting.FormatMaybeDouble(row?.PeakWorkingSetMb)} |");
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "KeyloggerDetection.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the evaluation tool base directory.");
    }

    private static string FindProjectArtifact(string repositoryRoot, string projectRelativePath, string fileName)
    {
        var projectRoot = Path.Combine(repositoryRoot, projectRelativePath);
        var matches = Directory
            .EnumerateFiles(projectRoot, fileName, SearchOption.AllDirectories)
            .Where(path => path.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            throw new FileNotFoundException($"Unable to locate built artifact: {fileName}");
        }

        return matches
            .OrderByDescending(path => File.GetLastWriteTimeUtc(path))
            .First();
    }
}
