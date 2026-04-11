using KeyloggerDetection.Monitoring.Persistence;

namespace KeyloggerDetection.Tests.Monitoring;

public class PersistenceCollectorTests
{
    [Fact]
    public void NormalizeCommandString_QuotedPathWithArgs_ExtractsSafely()
    {
        var raw = "\"C:\\Program Files\\My App\\app.exe\" -hidden -start";
        var norm = PersistenceCollector.NormalizeCommandString(raw);
        
        Assert.Equal("C:\\Program Files\\My App\\app.exe", norm);
    }

    [Fact]
    public void NormalizeCommandString_UnquotedPathWithArgs_ExtractsSafely()
    {
        var raw = "C:\\Windows\\System32\\cmd.exe /c start";
        var norm = PersistenceCollector.NormalizeCommandString(raw);
        
        Assert.Equal("C:\\Windows\\System32\\cmd.exe", norm);
    }

    [Fact]
    public void NormalizeCommandString_NakedPath_ExtractsProperly()
    {
        var raw = "C:\\Tools\\logger.exe";
        var norm = PersistenceCollector.NormalizeCommandString(raw);
        
        Assert.Equal("C:\\Tools\\logger.exe", norm);
    }

    [Fact]
    public void NormalizeCommandString_Empty_HandlesGracefully()
    {
        var norm = PersistenceCollector.NormalizeCommandString("   ");
        Assert.Equal(string.Empty, norm);
    }

    // Testing the actual registry snapshot diff live is unstable on CI/CD platforms
    // because it mutates HKCU Run keys.
    // The Snapshot diff logic methodology matches the NetworkCollector and FileWriteAnalyzer 
    // which use Dictionary/HashSet swapping successfully per the tested models.
}
