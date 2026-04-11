using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Monitors running processes and provides process context information.
/// Proposal reference: § 2.1.2, signal group 1 — process context.
/// Implementation target: Phase P3.
/// </summary>
public interface IProcessMonitor : IDisposable
{
    /// <summary>
    /// Gets a snapshot of currently running processes with context information.
    /// </summary>
    IReadOnlyList<ProcessInfo> GetRunningProcesses();

    /// <summary>
    /// Starts continuous process monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops continuous process monitoring.
    /// </summary>
    void StopMonitoring();
}
