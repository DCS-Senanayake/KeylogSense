namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents the aggregated features of a process over time,
/// serving as the inputs to the risk scoring engine.
/// </summary>
public sealed class FeatureVector
{
    public int Pid { get; init; }
    public string ProcessName { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }
    
    // Signal Group 1: Process Context
    public SuspiciousLocationClassification LocationClassification { get; set; } = SuspiciousLocationClassification.Safe;
    public TrustState Trust { get; set; } = TrustState.Unknown;
    public string? PublisherName { get; set; }

    // Signal Group 2: File Logging
    public int SmallWriteCount { get; set; }
    public int RepeatedSameFileWriteCount { get; set; }
    public DateTime? LastFileWriteTime { get; set; }
    
    // Signal Group 3: Network Behaviour
    public bool HasOutboundConnections { get; set; }
    public int OutboundConnectionCount { get; set; }
    public DateTime? LastNetworkActivityTime { get; set; }

    // Signal Group 4: Persistence
    public bool PersistenceDetected { get; set; }

    // Timing tracking for correlation
    public DateTime CaptureWindowStart { get; init; }
    public DateTime CaptureWindowEnd { get; set; }
}
