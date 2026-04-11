namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Holds information about a monitored process.
/// Proposal reference: § 2.1.2, signal group 1 — process context.
/// </summary>
public sealed class ProcessInfo
{
    /// <summary>Process ID.</summary>
    public int Pid { get; init; }

    /// <summary>Process name (without extension).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Full path to the executable, if accessible.</summary>
    public string? ExecutablePath { get; init; }

    /// <summary>Process start time, if accessible.</summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// Whether the executable is located in a suspicious user-writable
    /// location (AppData, LocalAppData, Temp, Downloads).
    /// Proposal reference: § 2.1.2 — "unusual user-writable locations."
    /// </summary>
    public bool IsInSuspiciousLocation { get; set; }

    /// <summary>
    /// Whether the executable has a valid digital signature from a trusted publisher.
    /// Null if the check could not be performed (e.g., access denied).
    /// Proposal reference: § 2.1.2 — "signed/known publisher where feasible."
    /// </summary>
    public bool? IsTrustedPublisher { get; set; }

    public override string ToString() => $"{Name} (PID: {Pid})";
}
