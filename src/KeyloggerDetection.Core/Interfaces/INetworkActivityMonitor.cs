using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Monitors network activity per process to detect outbound connections.
/// Proposal reference: § 2.1.2, signal group 3 — network behaviour.
/// Implementation target: Phase P5.
/// </summary>
public interface INetworkActivityMonitor : IDisposable
{
    /// <summary>
    /// Starts network activity monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops network activity monitoring.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets the current network activity summary for a given process.
    /// Returns null if no activity has been observed.
    /// </summary>
    NetworkActivityInfo? GetActivityForProcess(int pid);
}
