using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.Placeholders;

/// <summary>
/// Placeholder implementation of <see cref="INetworkActivityMonitor"/>.
/// Reports no network activity. Will be replaced with real
/// implementation in Phase P5.
/// </summary>
public sealed class PlaceholderNetworkActivityMonitor : INetworkActivityMonitor
{
    public void StartMonitoring()
    {
        // TODO: Implement in Phase P5
    }

    public void StopMonitoring()
    {
        // TODO: Implement in Phase P5
    }

    public NetworkActivityInfo? GetActivityForProcess(int pid) => null;

    public void Dispose()
    {
        // Nothing to dispose yet
    }
}
