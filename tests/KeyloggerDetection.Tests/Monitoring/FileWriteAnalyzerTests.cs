using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.FileBehaviour;
using KeyloggerDetection.Tests.Mocks;

namespace KeyloggerDetection.Tests.Monitoring;

public class FileWriteAnalyzerTests
{
    [Fact]
    public void ProcessEvent_SmallWrites_CalculatesCorrectly()
    {
        var config = new DetectionConfig { SmallWriteMaxBytes = 100 };
        var clock = new MockClock();
        var analyzer = new FileWriteAnalyzer(config, clock);

        var pid = 1001;
        
        // 3 small writes, 1 large write
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\temp\1.log", 50));
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\temp\2.log", 100)); // Inclusive
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\temp\3.log", 150)); // Large
        var finalResult = analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\temp\4.log", 10));

        Assert.Equal(3, finalResult.SmallWriteCount);
    }

    [Fact]
    public void ProcessEvent_RepeatedSameFile_CountsMaxOccurrences()
    {
        var config = new DetectionConfig();
        var clock = new MockClock();
        var analyzer = new FileWriteAnalyzer(config, clock);
        
        var pid = 2002;
        var targetFile = @"C:\Users\Public\keylog.txt";

        // Hit same file 4 times
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, targetFile, 50));
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, targetFile, 50));
        var intermediate = analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\other\random.txt", 100));
        
        Assert.Equal(2, intermediate.RepeatedSameFileWriteCount);

        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, targetFile, 50));
        var finalResult = analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, targetFile, 50));

        Assert.Equal(4, finalResult.RepeatedSameFileWriteCount);
    }

    [Fact]
    public void ProcessEvent_OutsideWindow_ArePruned()
    {
        var config = new DetectionConfig { RepeatedWriteWindowSeconds = 10 };
        var clock = new MockClock { FixedTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
        var analyzer = new FileWriteAnalyzer(config, clock);
        
        var pid = 3003;
        var targetFile = @"C:\Temp\log.txt";

        // Event from exactly window limit (should be excluded due to strict < check in analyzer or included depending on logic)
        // Analyzer uses: < windowStart meaning inclusive of window Start boundary. Let's make it older.
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.FixedTime.AddSeconds(-20), targetFile, 50));
        
        // This event triggers the pruning evaluation inside the analyzer at current `FixedTime`
        var result = analyzer.ProcessEvent(new FileWriteEvent(pid, clock.FixedTime, targetFile, 50));

        // The older event is pruned, leaving only 1 active.
        Assert.Equal(1, result.RepeatedSameFileWriteCount);
        Assert.Equal(1, result.SmallWriteCount); // Older one pruned!
    }

    [Fact]
    public void ProcessEvent_FilterMonitoredRoots_ExcludesOthers()
    {
        var config = new DetectionConfig 
        { 
            MonitoredFileRoots = new[] { @"C:\Users\" } 
        };
        var clock = new MockClock();
        var analyzer = new FileWriteAnalyzer(config, clock);
        var pid = 4004;

        // Ignore System volume writes
        analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\Windows\System32\config.sys", 10));
        
        // Track user directory writes
        var result = analyzer.ProcessEvent(new FileWriteEvent(pid, clock.UtcNow, @"C:\Users\Admin\secrets.txt", 10));

        // Only the second event is processed
        Assert.Equal(1, result.SmallWriteCount);
    }
}
