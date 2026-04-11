namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents a detection event to be written to the CSV/text log.
/// Proposal reference: § 2.1.6 — logs must contain timestamp, process details,
/// feature values, risk score, and triggered rules.
/// </summary>
public sealed class DetectionEvent
{
    /// <summary>When the detection occurred.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Process name.</summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>Process ID.</summary>
    public int Pid { get; init; }

    /// <summary>Executable path.</summary>
    public string? ExecutablePath { get; init; }

    // --- Feature values (which indicators were observed) ---

    /// <summary>Whether the process runs from a suspicious location.</summary>
    public bool SuspiciousLocation { get; init; }

    /// <summary>Whether the process publisher is untrusted/unsigned.</summary>
    public bool UntrustedPublisher { get; init; }

    /// <summary>Whether frequent small file writes were detected.</summary>
    public bool FrequentSmallWrites { get; init; }

    /// <summary>Whether repeated writes to the same file were detected.</summary>
    public bool RepeatedSameFileWrites { get; init; }

    /// <summary>Whether outbound network connections were detected.</summary>
    public bool OutboundNetwork { get; init; }

    /// <summary>Whether file logging and network activity were correlated in time.</summary>
    public bool FileNetworkCorrelation { get; init; }

    /// <summary>Whether persistence (Run key) was detected.</summary>
    public bool PersistenceDetected { get; init; }

    /// <summary>Total risk score.</summary>
    public int RiskScore { get; init; }

    /// <summary>Semicolon-separated list of triggered rule descriptions.</summary>
    public string TriggeredRules { get; init; } = string.Empty;
}
