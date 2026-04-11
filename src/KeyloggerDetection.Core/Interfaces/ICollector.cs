namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Represents a module that passively or actively collects specific telemetry events
/// (e.g., ETW consumer, API polling loop, registry scanner) and pushes them to the pipeline.
/// </summary>
public interface ICollector : IDisposable
{
    /// <summary>
    /// Starts collecting events and publishing them to the provided pipeline.
    /// </summary>
    Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken);
}
