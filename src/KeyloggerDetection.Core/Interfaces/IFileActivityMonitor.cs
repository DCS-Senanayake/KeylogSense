using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Monitors file system activity per process to detect log-like writing patterns.
/// Proposal reference: § 2.1.2, signal group 2 — file logging behaviour.
/// Implementation target: Phase P4.
/// </summary>
public interface IFileActivityMonitor : IDisposable
{
    /// <summary>
    /// Starts file activity monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops file activity monitoring.
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Gets the current file activity summary for a given process.
    /// Returns null if no activity has been observed.
    /// </summary>
    FileActivityInfo? GetActivityForProcess(int pid);
}
