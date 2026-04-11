namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Coordinates the starting and stopping of monitoring modules.
/// </summary>
public interface IMonitoringCoordinator : IDisposable
{
    /// <summary>
    /// Starts the monitoring process.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the monitoring process.
    /// </summary>
    void Stop();
}
