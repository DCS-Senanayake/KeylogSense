using System.Text.RegularExpressions;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;
using Microsoft.Win32;

namespace KeyloggerDetection.Monitoring.Persistence;

/// <summary>
/// Periodically polls explicitly designated persistence locations.
/// Implements proposal requirement: read-only polling of Run/RunOnce keys and Startup folders.
/// </summary>
public sealed class PersistenceCollector : ICollector
{
    private readonly IAppLogger _logger;
    private readonly DetectionConfig _config;
    private readonly IClock _clock;

    // Track active keys to fire new events on diff
    // Key format: "Type|Name -> NormalizedPath"
    private Dictionary<string, string> _baseline = new();

    public PersistenceCollector(IAppLogger logger, DetectionConfig config, IClock clock)
    {
        _logger = logger;
        _config = config;
        _clock = clock;
    }

    public async Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        _logger.LogInfo("PersistenceCollector started.");

        try
        {
            // Initial Baseline (don't alert on existing configurations assuming a clean start,
            // or the aggregator can do historical back-checks. Proposal implies "when new/changed entries are observed")
            _baseline = TakeSnapshot();

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_config.PersistencePollingIntervalMs, cancellationToken);
                
                var newSnapshot = TakeSnapshot();
                EvaluateDiff(newSnapshot, pipeline);
                _baseline = newSnapshot;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("PersistenceCollector shutdown requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in PersistenceCollector loop.", ex);
        }
    }

    /// <summary>
    /// Parses the raw command string in registry to extract the naked executable path.
    /// Proposal explicitly requested normalization logic tests.
    /// </summary>
    public static string NormalizeCommandString(string rawCommandString)
    {
        if (string.IsNullOrWhiteSpace(rawCommandString)) return string.Empty;

        // E.g. "C:\Program Files\Test\app.exe" -arg1
        // Quote wrapped paths
        var match = Regex.Match(rawCommandString, "^\"([^\"]+)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Unquoted paths: split by space and take first argument. 
        // Note: this breaks if path contains space but no quotes, which is technically invalid Windows syntax 
        // but often tolerated. Assuming space-split is the most acceptable best-effort normalize.
        var spaceSplit = rawCommandString.Split(' ', 2);
        return spaceSplit[0];
    }

    private Dictionary<string, string> TakeSnapshot()
    {
        var snapshot = new Dictionary<string, string>();

        void ProcessRegistry(RegistryKey baseKey, string rootName)
        {
            var targets = new[]
            {
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            foreach (var subKeyPath in targets)
            {
                try
                {
                    using var key = baseKey.OpenSubKey(subKeyPath, writable: false);
                    if (key == null) continue;

                    foreach (var valueName in key.GetValueNames())
                    {
                        var data = key.GetValue(valueName)?.ToString();
                        if (string.IsNullOrWhiteSpace(data)) continue;

                        var typeId = $"{rootName}\\{subKeyPath}\\{valueName}";
                        snapshot[typeId] = NormalizeCommandString(data);
                    }
                }
                catch (System.Security.SecurityException) { /* Access Denied to Run key */ }
                catch (Exception) { /* Unhandled parse error */ }
            }
        }

        // Windows-only API calls (guarded implicitly by target frameworks and OS design)
#pragma warning disable CA1416 // Validate platform compatibility
        ProcessRegistry(Registry.CurrentUser, "HKCU");
        ProcessRegistry(Registry.LocalMachine, "HKLM");
#pragma warning restore CA1416

        // Startup Folders
        void ProcessFolder(Environment.SpecialFolder folder, string rootName)
        {
            try
            {
                var folderPath = Environment.GetFolderPath(folder);
                if (Directory.Exists(folderPath))
                {
                    foreach (var file in Directory.EnumerateFiles(folderPath))
                    {
                        var typeId = $"{rootName}\\{Path.GetFileName(file)}";
                        snapshot[typeId] = file;
                    }
                }
            }
            catch { /* Edge case folder rights lost */ }
        }

        ProcessFolder(Environment.SpecialFolder.Startup, "UserStartup");
        ProcessFolder(Environment.SpecialFolder.CommonStartup, "CommonStartup");

        return snapshot;
    }

    private void EvaluateDiff(Dictionary<string, string> currentSnapshot, ITelemetryPipeline pipeline)
    {
        foreach (var kvp in currentSnapshot)
        {
            var keyStr = kvp.Key;
            var executablePath = kvp.Value;

            // If it's completely new, or the value (the executable path) was changed.
            if (!_baseline.TryGetValue(keyStr, out var oldVal) || !string.Equals(oldVal, executablePath, StringComparison.OrdinalIgnoreCase))
            {
                // We don't natively know the active PID that set this purely from a registry poll.
                // We emit PID=-1, allowing the scoring aggregator to map this path to previously seen ProcessContextEvents.
                var telemetryEvent = new PersistenceEvent(
                    Pid: -1, 
                    Timestamp: _clock.UtcNow,
                    RegistryOrFilePath: keyStr,
                    PersistenceType: executablePath
                );

                pipeline.Publish(telemetryEvent);
            }
        }
    }

    public void Dispose()
    {
    }
}
