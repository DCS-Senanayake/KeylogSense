namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents network activity observed for a process.
/// Proposal reference: § 2.1.2, signal group 3 — network behaviour.
/// </summary>
public sealed class NetworkActivityInfo
{
    /// <summary>PID of the process that initiated the connection.</summary>
    public int Pid { get; init; }

    /// <summary>Whether the process has any outbound connections.</summary>
    public bool HasOutboundConnections { get; set; }

    /// <summary>Number of outbound connections detected.</summary>
    public int OutboundConnectionCount { get; set; }

    /// <summary>Timestamp of the most recent network activity.</summary>
    public DateTime LastActivityTime { get; set; }
}
