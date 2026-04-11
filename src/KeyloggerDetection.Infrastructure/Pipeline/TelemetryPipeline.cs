using System.Threading.Channels;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Infrastructure.Pipeline;

/// <summary>
/// A high-performance, in-memory event bus using System.Threading.Channels.
/// Decouples collectors (producers) from the aggregation/scoring engine (consumers).
/// </summary>
public sealed class TelemetryPipeline : ITelemetryPipeline
{
    private readonly Channel<TelemetryEvent> _channel;
    private readonly IAppLogger _logger;

    public TelemetryPipeline(IAppLogger logger, int capacity = 10000)
    {
        _logger = logger;
        
        // Engineering Assumption: Using a bounded channel with DropOldest.
        // We want to avoid explosive memory usage if consumers back up.
        // Real-time security tools must fail gracefully under heavy load (e.g. log spam).
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true, // We will have one aggregator loop consuming
            SingleWriter = false // Multiple collectors will write
        };

        _channel = Channel.CreateBounded<TelemetryEvent>(options);
    }

    public void Publish(TelemetryEvent telemetryEvent)
    {
        if (!_channel.Writer.TryWrite(telemetryEvent))
        {
            // Note: with DropOldest, TryWrite should typically succeed unless the channel completes.
            _logger.LogWarning($"Failed to publish telemetry event {telemetryEvent.GetType().Name} for PID {telemetryEvent.Pid}");
        }
    }

    public IAsyncEnumerable<TelemetryEvent> ConsumeAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
