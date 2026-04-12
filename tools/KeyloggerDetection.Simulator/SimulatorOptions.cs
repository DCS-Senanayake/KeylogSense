namespace KeyloggerDetection.Simulator;

internal sealed record SimulatorOptions(
    bool CleanupOnly,
    bool EnablePersistence,
    string ArtifactPath,
    int FileWriteIterations,
    int FileWriteIntervalMs,
    int NetworkBurstCount,
    int NetworkHoldMs,
    int NetworkPauseMs,
    string NetworkHost,
    int NetworkPort)
{
    public static SimulatorOptions Default { get; } = new(
        CleanupOnly: false,
        EnablePersistence: false,
        ArtifactPath: Path.Combine(Path.GetTempPath(), "KeylogSense", "keylogger-behaviour-sim.log"),
        FileWriteIterations: 15,
        FileWriteIntervalMs: 1000,
        NetworkBurstCount: 3,
        NetworkHoldMs: 3000,
        NetworkPauseMs: 5000,
        NetworkHost: "1.1.1.1",
        NetworkPort: 80);
}
