using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Tests.Mocks;

public class MockClock : IClock
{
    public DateTime FixedTime { get; set; } = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    public DateTime UtcNow => FixedTime;
}

public class MockTelemetryPipeline : ITelemetryPipeline
{
    public List<TelemetryEvent> PublishedEvents { get; } = new();

    public void Publish(TelemetryEvent telemetryEvent)
    {
        PublishedEvents.Add(telemetryEvent);
    }

    public IAsyncEnumerable<TelemetryEvent> ConsumeAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public class MockAppLogger : IAppLogger
{
    public void Log(LogLevel level, string message, Exception? exception = null) { }
    public void Dispose() { }
}
