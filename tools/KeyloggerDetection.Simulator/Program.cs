using System.Net.Sockets;
using Microsoft.Win32;

namespace KeyloggerDetection.Simulator;

/// <summary>
/// Safe behaviour simulator harness reproducing behaviour profiles without 
/// acting maliciously or capturing inputs.
/// </summary>
internal class Program
{
    private const string RegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string SimulatorRunValueFriendlyName = "KeylogSenseSimulatorTest";

    private static string TempLogFile => Path.Combine(Path.GetTempPath(), "keylogsense_sim_test.txt");

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== KeylogSense Behaviour Simulator ===");
        Console.WriteLine("ETHICAL SAFEGUARD: This simulator does not hook keyboards,");
        Console.WriteLine("it does not capture actual keystrokes, and it does not hide itself.");
        Console.WriteLine();

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: KeyloggerDetection.Simulator.exe [mode]");
            Console.WriteLine("Modes:");
            Console.WriteLine("  file-only        — Repeatedly appends small chunks to a temp file");
            Console.WriteLine("  network-only     — Generates outbound TCP activity");
            Console.WriteLine("  persistence-only — Creates a temporary Run key marker");
            Console.WriteLine("  combined         — Triggers multiple behaviours");
            Console.WriteLine("  cleanup          — Removes temp file and registry markers");
            return;
        }

        string mode = args[0].ToLowerInvariant();
        try
        {
            switch (mode)
            {
                case "file-only":
                    await SimulateFileBehaviourAsync();
                    break;
                case "network-only":
                    await SimulateNetworkBehaviourAsync();
                    break;
                case "persistence-only":
                    SimulatePersistence();
                    break;
                case "combined":
                    SimulatePersistence();
                    var fTask = SimulateFileBehaviourAsync();
                    var nTask = SimulateNetworkBehaviourAsync();
                    await Task.WhenAll(fTask, nTask);
                    break;
                case "cleanup":
                    Cleanup();
                    break;
                default:
                    Console.WriteLine($"Unknown mode: {mode}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static async Task SimulateFileBehaviourAsync()
    {
        Console.WriteLine("[*] Starting File-Only Simulation...");
        Console.WriteLine($"[*] Target: {TempLogFile}");
        
        for (int i = 0; i < 15; i++)
        {
            string payload = $"[Simulated Input {DateTime.Now:O}]\r\n";
            File.AppendAllText(TempLogFile, payload);
            Console.WriteLine($"    [File] Appended {payload.Length} bytes.");
            await Task.Delay(1000); // 1 second intervals
        }
        
        Console.WriteLine("[+] File simulation complete.");
    }

    private static async Task SimulateNetworkBehaviourAsync()
    {
        Console.WriteLine("[*] Starting Network-Only Simulation...");
        // 1.1.1.1 is Cloudflare DNS, safe public target to resolve routing. Use port 80.
        string host = "1.1.1.1";
        int port = 80;

        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var client = new TcpClient();
                Console.WriteLine($"    [Network] Establishing outbound connection to {host}:{port}...");
                await client.ConnectAsync(host, port);
                Console.WriteLine("    [Network] Connection successful. Simulating data transmission...");
                // Just hold the connection open briefly to be captured by snapshot polls
                await Task.Delay(3000); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [Network Error] {ex.Message}");
            }

            if (i < 2) await Task.Delay(5000); 
        }

        Console.WriteLine("[+] Network simulation complete.");
    }

    private static void SimulatePersistence()
    {
        Console.WriteLine("[*] Starting Persistence-Only Simulation...");
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
            if (key != null)
            {
                // Write a dummy string instead of the simulator's own path to avoid accidentally running loops at boot
                string dummyPayload = @"C:\Windows\System32\notepad.exe";
                key.SetValue(SimulatorRunValueFriendlyName, dummyPayload);
                Console.WriteLine($"    [Persistence] Test marker '{SimulatorRunValueFriendlyName}' added to HKCU Run key.");
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("    [Persistence Error] Access Denied. Administrator rights might be required.");
        }
        Console.WriteLine("[+] Persistence simulation complete.");
    }

    private static void Cleanup()
    {
        Console.WriteLine("[*] Starting Cleanup...");

        // File Cleanup
        if (File.Exists(TempLogFile))
        {
            File.Delete(TempLogFile);
            Console.WriteLine($"    [Cleanup] Deleted {TempLogFile}.");
        }
        else
        {
            Console.WriteLine("    [Cleanup] No temp file found.");
        }

        // Registry Cleanup
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, writable: true);
            if (key != null && key.GetValue(SimulatorRunValueFriendlyName) != null)
            {
                key.DeleteValue(SimulatorRunValueFriendlyName);
                Console.WriteLine($"    [Cleanup] Eliminated registry marker '{SimulatorRunValueFriendlyName}'.");
            }
            else
            {
                Console.WriteLine("    [Cleanup] No registry marker found.");
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine($"    [Cleanup Reg Error] {ex.Message}");
        }

        Console.WriteLine("[+] Cleanup complete.");
    }
}
