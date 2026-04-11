namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents the current state of the monitoring system.
/// Proposal reference: § 2.1.5 — tray icon shows monitoring status (ON/OFF).
/// </summary>
public enum MonitoringState
{
    /// <summary>Monitoring is stopped.</summary>
    Stopped,

    /// <summary>Monitoring is actively running.</summary>
    Running
}
