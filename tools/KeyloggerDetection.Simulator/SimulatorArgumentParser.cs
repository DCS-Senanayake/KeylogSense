namespace KeyloggerDetection.Simulator;

internal static class SimulatorArgumentParser
{
    public static SimulatorParseResult Parse(string[] args)
    {
        var options = SimulatorOptions.Default;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;
                case "--cleanup":
                    options = options with { CleanupOnly = true };
                    break;
                case "--enable-persistence":
                    options = options with { EnablePersistence = true };
                    break;
                case "--artifact-path":
                    options = options with { ArtifactPath = RequireValue(args, ref index, argument) };
                    break;
                case "--file-write-iterations":
                    options = options with { FileWriteIterations = ParsePositiveInt(RequireValue(args, ref index, argument), argument) };
                    break;
                case "--file-write-interval-ms":
                    options = options with { FileWriteIntervalMs = ParsePositiveInt(RequireValue(args, ref index, argument), argument) };
                    break;
                case "--network-bursts":
                    options = options with { NetworkBurstCount = ParsePositiveInt(RequireValue(args, ref index, argument), argument) };
                    break;
                case "--network-hold-ms":
                    options = options with { NetworkHoldMs = ParsePositiveInt(RequireValue(args, ref index, argument), argument) };
                    break;
                case "--network-pause-ms":
                    options = options with { NetworkPauseMs = ParsePositiveInt(RequireValue(args, ref index, argument), argument) };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {argument}");
            }
        }

        return new SimulatorParseResult(options, showHelp);
    }

    private static string RequireValue(string[] args, ref int index, string argumentName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {argumentName}");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string argumentName)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{argumentName} expects a positive integer.");
        }

        return parsed;
    }
}
