using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.Placeholders;

/// <summary>
/// Placeholder implementation of <see cref="IFileActivityMonitor"/>.
/// Reports no file activity. Will be replaced with real
/// implementation in Phase P4.
/// </summary>
public sealed class PlaceholderFileActivityMonitor : IFileActivityMonitor
{
    public void StartMonitoring()
    {
        // TODO: Implement in Phase P4
    }

    public void StopMonitoring()
    {
        // TODO: Implement in Phase P4
    }

    public FileActivityInfo? GetActivityForProcess(int pid) => null;

    public void Dispose()
    {
        // Nothing to dispose yet
    }
}
