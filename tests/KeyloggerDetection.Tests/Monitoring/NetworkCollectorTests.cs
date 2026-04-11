using System.Net;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.NetworkBehaviour;
using KeyloggerDetection.Tests.Mocks;
using System.Net.NetworkInformation;

namespace KeyloggerDetection.Tests.Monitoring;

public class NetworkCollectorTests
{
    [Fact]
    public void TakeSnapshot_NewConnections_EmitsEvents()
    {
        var clock = new MockClock();
        var pipeline = new MockTelemetryPipeline();
        var config = new DetectionConfig { ExcludeLoopbackTraffic = false };
        
        var mockConnections = new List<Win32TcpTable.TcpConnectionRecord>
        {
            new Win32TcpTable.TcpConnectionRecord(1234, IPAddress.Parse("192.168.1.5"), 5000, IPAddress.Parse("8.8.8.8"), 443, TcpState.Established)
        };

        using var collector = new NetworkCollector(new MockAppLogger(), config, clock, () => mockConnections);
        
        // Single snapshot
        collector.TakeSnapshot(pipeline);

        Assert.Single(pipeline.PublishedEvents);
        
        var evt = (NetworkConnectionEvent)pipeline.PublishedEvents.First();
        Assert.Equal(1234, evt.Pid);
        Assert.Equal("192.168.1.5:5000", evt.LocalEndpoint);
        Assert.Equal("8.8.8.8:443", evt.RemoteEndpoint);
    }

    [Fact]
    public void TakeSnapshot_ExistingConnections_AreSuppressed()
    {
        var clock = new MockClock();
        var pipeline = new MockTelemetryPipeline();
        var config = new DetectionConfig { ExcludeLoopbackTraffic = false };
        
        var mockConnections = new List<Win32TcpTable.TcpConnectionRecord>
        {
            new Win32TcpTable.TcpConnectionRecord(1234, IPAddress.Parse("192.168.1.5"), 5000, IPAddress.Parse("8.8.8.8"), 443, TcpState.Established)
        };

        using var collector = new NetworkCollector(new MockAppLogger(), config, clock, () => mockConnections);
        
        collector.TakeSnapshot(pipeline); // Hit 1 (Published)
        collector.TakeSnapshot(pipeline); // Hit 2 (Suppressed duplicate)

        Assert.Single(pipeline.PublishedEvents); // Still only 1
    }

    [Fact]
    public void TakeSnapshot_LoopbackFiltering_AppliesWhenConfigured()
    {
        var clock = new MockClock();
        var pipeline = new MockTelemetryPipeline();
        var config = new DetectionConfig { ExcludeLoopbackTraffic = true };
        
        var mockConnections = new List<Win32TcpTable.TcpConnectionRecord>
        {
            new Win32TcpTable.TcpConnectionRecord(1234, IPAddress.Parse("127.0.0.1"), 5000, IPAddress.Parse("127.0.0.1"), 8080, TcpState.Established),
            new Win32TcpTable.TcpConnectionRecord(1234, IPAddress.Parse("192.168.1.5"), 5001, IPAddress.Parse("9.9.9.9"), 443, TcpState.Established)
        };

        using var collector = new NetworkCollector(new MockAppLogger(), config, clock, () => mockConnections);
        
        collector.TakeSnapshot(pipeline);

        Assert.Single(pipeline.PublishedEvents); // Only the 9.9.9.9 survived
        var evt = (NetworkConnectionEvent)pipeline.PublishedEvents.First();
        Assert.Equal("9.9.9.9:443", evt.RemoteEndpoint);
    }
}
