using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Tests.Mocks;

/// <summary>
/// Mock provider generating deterministic file write telemetry.
/// Fulfills proposal requirement for safe simulators avoiding real hooks.
/// </summary>
public sealed class MockFileTelemetryProvider : ICollector
{
    private readonly List<FileWriteEvent> _scriptedEvents;

    public MockFileTelemetryProvider(IEnumerable<FileWriteEvent> scriptedEvents)
    {
        _scriptedEvents = scriptedEvents.ToList();
    }

    public Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        // Deterministically pump all recorded events directly into the pipeline
        foreach (var e in _scriptedEvents)
        {
            if (cancellationToken.IsCancellationRequested) break;
            pipeline.Publish(e);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
