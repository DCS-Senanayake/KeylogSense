using System.Net.Sockets;
using Microsoft.Win32;

namespace KeyloggerDetection.Simulator;

internal static class SimulatorApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("=== KeylogSense Safe Keylogger-Behaviour Simulator ===");
        Console.WriteLine("This tool simulates suspicious indicators for defensive testing only.");
        Console.WriteLine("It does not capture real keystrokes, install stealth, or evade detection.");
        Console.WriteLine();

        SimulatorParseResult parseResult;
        try
        {
            parseResult = SimulatorArgumentParser.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            PrintUsage();
            return 1;
        }

        if (parseResult.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        if (parseResult.Options.CleanupOnly)
        {
            Cleanup(parseResult.Options.ArtifactPath);
            return 0;
        }

        try
        {
            await RunSimulationAsync(parseResult.Options);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Simulation failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task RunSimulationAsync(SimulatorOptions options)
    {
        Console.WriteLine("[*] Starting combined behaviour flow...");
        Console.WriteLine($"    File artifact: {options.ArtifactPath}");
        Console.WriteLine($"    File write iterations: {options.FileWriteIterations}");
        Console.WriteLine($"    File write interval: {options.FileWriteIntervalMs} ms");
        Console.WriteLine($"    Network bursts: {options.NetworkBurstCount}");
        Console.WriteLine($"    Persistence enabled: {options.EnablePersistence}");
        Console.WriteLine();

        if (options.EnablePersistence)
        {
            SimulatePersistence();
        }

        var fileTask = SimulateFileBehaviourAsync(options);
        var networkTask = SimulateNetworkBehaviourAsync(options);

        await Task.WhenAll(fileTask, networkTask);

        Console.WriteLine();
        Console.WriteLine("[+] Simulation complete.");
        Console.WriteLine("    Use '--cleanup' to remove the test artifact and optional registry marker.");
    }

    private static async Task SimulateFileBehaviourAsync(SimulatorOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.ArtifactPath)!);

        for (var index = 0; index < options.FileWriteIterations; index++)
        {
            var payload = $"[Simulated Input {DateTime.UtcNow:O}] chunk-{index:D2}{Environment.NewLine}";
            await File.AppendAllTextAsync(options.ArtifactPath, payload);
            Console.WriteLine($"    [File] Appended {payload.Length} bytes to the same artifact.");
            await Task.Delay(options.FileWriteIntervalMs);
        }

        Console.WriteLine("    [File] Repeated small-write sequence finished.");
    }

    private static async Task SimulateNetworkBehaviourAsync(SimulatorOptions options)
    {
        for (var index = 0; index < options.NetworkBurstCount; index++)
        {
            try
            {
                using var client = new TcpClient();
                Console.WriteLine($"    [Network] Connecting to {options.NetworkHost}:{options.NetworkPort}...");
                await client.ConnectAsync(options.NetworkHost, options.NetworkPort);
                Console.WriteLine("    [Network] Outbound connection established for detector observation.");
                await Task.Delay(options.NetworkHoldMs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [Network Error] {ex.Message}");
            }

            if (index < options.NetworkBurstCount - 1)
            {
                await Task.Delay(options.NetworkPauseMs);
            }
        }

        Console.WriteLine("    [Network] Outbound activity sequence finished.");
    }

    private static void SimulatePersistence()
    {
        Console.WriteLine("    [Persistence] Writing an inert HKCU Run marker for safe testing.");

        using var key = Registry.CurrentUser.OpenSubKey(SimulatorConstants.RegistryKeyPath, writable: true);
        if (key is null)
        {
            Console.WriteLine("    [Persistence Error] HKCU Run key could not be opened for writing.");
            return;
        }

        var dummyPayload = @"C:\Windows\System32\notepad.exe";
        key.SetValue(SimulatorConstants.RegistryValueName, dummyPayload);
        Console.WriteLine($"    [Persistence] Marker '{SimulatorConstants.RegistryValueName}' added.");
    }

    private static void Cleanup(string artifactPath)
    {
        Console.WriteLine("[*] Starting simulator cleanup...");

        if (File.Exists(artifactPath))
        {
            File.Delete(artifactPath);
            Console.WriteLine($"    [Cleanup] Deleted {artifactPath}");
        }
        else
        {
            Console.WriteLine("    [Cleanup] No file artifact found.");
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SimulatorConstants.RegistryKeyPath, writable: true);
            if (key?.GetValue(SimulatorConstants.RegistryValueName) is not null)
            {
                key.DeleteValue(SimulatorConstants.RegistryValueName);
                Console.WriteLine($"    [Cleanup] Removed registry marker '{SimulatorConstants.RegistryValueName}'.");
            }
            else
            {
                Console.WriteLine("    [Cleanup] No registry marker found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [Cleanup Error] {ex.Message}");
        }

        Console.WriteLine("[+] Cleanup complete.");
    }

    private static void PrintUsage()
    {
        var defaults = SimulatorOptions.Default;

        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tools\\KeyloggerDetection.Simulator -- [options]");
        Console.WriteLine();
        Console.WriteLine("Default behaviour:");
        Console.WriteLine("  Runs one safe combined flow with repeated small writes to the same file");
        Console.WriteLine("  plus outbound network activity. Persistence is disabled unless enabled.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --enable-persistence        Add an inert HKCU Run-key marker.");
        Console.WriteLine("  --cleanup                   Remove simulator file and registry artifacts, then exit.");
        Console.WriteLine("  --artifact-path <path>      Override the file artifact path.");
        Console.WriteLine($"  --file-write-iterations <n> Default: {defaults.FileWriteIterations}");
        Console.WriteLine($"  --file-write-interval-ms <n> Default: {defaults.FileWriteIntervalMs}");
        Console.WriteLine($"  --network-bursts <n>        Default: {defaults.NetworkBurstCount}");
        Console.WriteLine($"  --network-hold-ms <n>       Default: {defaults.NetworkHoldMs}");
        Console.WriteLine($"  --network-pause-ms <n>      Default: {defaults.NetworkPauseMs}");
        Console.WriteLine("  --help                      Show this help.");
    }
}
