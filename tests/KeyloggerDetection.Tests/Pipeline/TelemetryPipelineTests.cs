using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Infrastructure.Pipeline;

namespace KeyloggerDetection.Tests.Pipeline;

public class TelemetryPipelineTests
{
    private class DummyLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null) { }
        public void Dispose() { }
    }

    [Fact]
    public async Task Publish_ValidEvent_CanBeConsumed()
    {
        var pipeline = new TelemetryPipeline(new DummyLogger(), 10);
        var expectedEvent = new ProcessContextEvent(
            1234, 
            DateTime.UtcNow, 
            "test_proc", 
            "C:\\test.exe", 
            null,
            SuspiciousLocationClassification.Safe, 
            TrustState.Unknown,
            null);

        pipeline.Publish(expectedEvent);

        // Basic asynchronous check for test 
        var enumerator = pipeline.ConsumeAsync(CancellationToken.None).GetAsyncEnumerator();
        
        // Start a task to grab the first item
        var hasNext = await enumerator.MoveNextAsync();
        Assert.True(hasNext);
        
        Assert.Equal(expectedEvent, enumerator.Current);
    }

    [Fact]
    public async Task ConsumeAsync_Cancels_WhenTokenThrows()
    {
        var pipeline = new TelemetryPipeline(new DummyLogger(), 10);
        var cts = new CancellationTokenSource();
        
        var consumeTask = Task.Run(async () =>
        {
            await foreach (var _ in pipeline.ConsumeAsync(cts.Token))
            {
            }
        });

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await consumeTask);
    }
}
