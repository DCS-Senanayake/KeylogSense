using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Represents the central nervous system of the monitoring architecture.
/// Collectors push events here, and aggregators consume them.
/// </summary>
public interface ITelemetryPipeline
{
    /// <summary>
    /// Publishes a new telemetry event into the pipeline.
    /// </summary>
    void Publish(TelemetryEvent telemetryEvent);

    /// <summary>
    /// Asynchronously streams events from the pipeline until cancellation.
    /// </summary>
    IAsyncEnumerable<TelemetryEvent> ConsumeAsync(CancellationToken cancellationToken);
}
