namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents file activity observed for a process.
/// Proposal reference: § 2.1.2, signal group 2 — file logging behaviour.
/// </summary>
public sealed class FileActivityInfo
{
    /// <summary>PID of the process that performed the writes.</summary>
    public int Pid { get; init; }

    /// <summary>Number of small file writes detected in the monitoring window.</summary>
    public int SmallWriteCount { get; set; }

    /// <summary>Number of repeated writes to the same file in a short time window.</summary>
    public int RepeatedSameFileWriteCount { get; set; }

    /// <summary>Timestamp of the most recent file activity.</summary>
    public DateTime LastActivityTime { get; set; }
}
