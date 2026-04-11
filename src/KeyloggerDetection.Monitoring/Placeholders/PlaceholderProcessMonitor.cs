using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.Placeholders;

/// <summary>
/// Placeholder implementation of <see cref="IProcessMonitor"/>.
/// Returns an empty process list. Will be replaced with real
/// implementation in Phase P3.
/// </summary>
public sealed class PlaceholderProcessMonitor : IProcessMonitor
{
    public IReadOnlyList<ProcessInfo> GetRunningProcesses() => [];

    public void StartMonitoring()
    {
        // TODO: Implement in Phase P3
    }

    public void StopMonitoring()
    {
        // TODO: Implement in Phase P3
    }

    public void Dispose()
    {
        // Nothing to dispose yet
    }
}
