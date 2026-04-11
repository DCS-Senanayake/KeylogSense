namespace KeyloggerDetection.Evaluation;

internal sealed class EvaluationOptions
{
    public static string UsageText =>
        """
        Usage:
          dotnet run --project tools/KeyloggerDetection.Evaluation -- [options]

        Options:
          --output-root <path>                 Output directory. Default: evaluation
          --warmup-seconds <int>               Warm-up time after monitoring starts. Default: 6
          --idle-sample-seconds <int>          Idle overhead sampling duration. Default: 15
          --active-sample-seconds <int>        Active overhead sampling duration. Default: 20
          --scenario-timeout-seconds <int>     Default benign scenario timeout. Default: 12
          --alert-threshold <int>              Override alert threshold for tuning experiments
          --monitoring-interval-ms <int>       Override process monitoring interval
          --approved-samples-manifest <path>   JSON manifest for approved non-destructive samples
          --acknowledge-isolated-vm            Required before approved sample execution
        """;

    public string OutputRoot { get; init; } = "evaluation";
    public int WarmupSeconds { get; init; } = 6;
    public int IdleSampleSeconds { get; init; } = 15;
    public int ActiveSampleSeconds { get; init; } = 20;
    public int ScenarioTimeoutSeconds { get; init; } = 12;
    public int? AlertThresholdOverride { get; init; }
    public int? MonitoringIntervalMsOverride { get; init; }
    public string? ApprovedSamplesManifestPath { get; init; }
    public bool AcknowledgeIsolatedVm { get; init; }

    public static EvaluationOptions Parse(string[] args)
    {
        var outputRoot = "evaluation";
        var warmupSeconds = 6;
        var idleSampleSeconds = 15;
        var activeSampleSeconds = 20;
        var scenarioTimeoutSeconds = 12;
        int? alertThresholdOverride = null;
        int? monitoringIntervalMsOverride = null;
        string? approvedSamplesManifestPath = null;
        var acknowledgeIsolatedVm = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output-root":
                    outputRoot = ReadString(args, ref i);
                    break;
                case "--warmup-seconds":
                    warmupSeconds = ReadPositiveInt(args, ref i, "--warmup-seconds");
                    break;
                case "--idle-sample-seconds":
                    idleSampleSeconds = ReadPositiveInt(args, ref i, "--idle-sample-seconds");
                    break;
                case "--active-sample-seconds":
                    activeSampleSeconds = ReadPositiveInt(args, ref i, "--active-sample-seconds");
                    break;
                case "--scenario-timeout-seconds":
                    scenarioTimeoutSeconds = ReadPositiveInt(args, ref i, "--scenario-timeout-seconds");
                    break;
                case "--alert-threshold":
                    alertThresholdOverride = ReadPositiveInt(args, ref i, "--alert-threshold");
                    break;
                case "--monitoring-interval-ms":
                    monitoringIntervalMsOverride = ReadPositiveInt(args, ref i, "--monitoring-interval-ms");
                    break;
                case "--approved-samples-manifest":
                    approvedSamplesManifestPath = ReadString(args, ref i);
                    break;
                case "--acknowledge-isolated-vm":
                    acknowledgeIsolatedVm = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }

        if (approvedSamplesManifestPath is not null && !acknowledgeIsolatedVm)
        {
            throw new ArgumentException(
                "Approved sample execution requires --acknowledge-isolated-vm so the run stays academically defensible.");
        }

        return new EvaluationOptions
        {
            OutputRoot = outputRoot,
            WarmupSeconds = warmupSeconds,
            IdleSampleSeconds = idleSampleSeconds,
            ActiveSampleSeconds = activeSampleSeconds,
            ScenarioTimeoutSeconds = scenarioTimeoutSeconds,
            AlertThresholdOverride = alertThresholdOverride,
            MonitoringIntervalMsOverride = monitoringIntervalMsOverride,
            ApprovedSamplesManifestPath = approvedSamplesManifestPath,
            AcknowledgeIsolatedVm = acknowledgeIsolatedVm
        };
    }

    private static string ReadString(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value after {args[index]}");
        }

        index++;
        return args[index];
    }

    private static int ReadPositiveInt(string[] args, ref int index, string optionName)
    {
        var value = ReadString(args, ref index);

        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{optionName} expects a positive integer.");
        }

        return parsed;
    }
}
