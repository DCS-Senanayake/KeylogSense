using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.ProcessContext;
using KeyloggerDetection.Tests.Mocks;

namespace KeyloggerDetection.Tests.Monitoring;

public class ProcessCollectorTests
{
    [Fact]
    public async Task ProcessCollector_StartAsync_EmitsEventsForRunningProcesses()
    {
        // Setup
        var logger = new MockAppLogger();
        var config = new DetectionConfig { MonitoringIntervalMs = 100 };
        var clock = new MockClock();
        var pipeline = new MockTelemetryPipeline();
        
        using var collector = new ProcessCollector(logger, config, clock);
        using var cts = new CancellationTokenSource();
        
        // Execute - Start collection in background
        var runTask = collector.StartAsync(pipeline, cts.Token);
        
        // Wait for at least one sweep
        await Task.Delay(500); 
        
        // Cancel to stop the loop
        cts.Cancel();
        try 
        {
            await runTask;
        }
        catch (OperationCanceledException) { }

        // Assert
        Assert.NotEmpty(pipeline.PublishedEvents);
        
        var processEvents = pipeline.PublishedEvents.OfType<ProcessContextEvent>().ToList();
        Assert.NotEmpty(processEvents);

        // Verify that current test process was captured
        var currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        var myProcEvent = processEvents.FirstOrDefault(e => e.Pid == currentPid);
        
        Assert.NotNull(myProcEvent);
        // Using "testhost" or "dotnet" process name depending on test runner
        Assert.False(string.IsNullOrWhiteSpace(myProcEvent.ProcessName));
        
        // Path should be found for our own process
        Assert.NotNull(myProcEvent.ExecutablePath);
        
        // Location should probably be Safe for standard test runners, but could vary.
        // It shouldn't crash.
    }
}
