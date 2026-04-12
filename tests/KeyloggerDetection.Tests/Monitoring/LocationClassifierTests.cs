using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.ProcessContext;

namespace KeyloggerDetection.Tests.Monitoring;

public class LocationClassifierTests
{
    private readonly LocationClassifier _classifier;

    public LocationClassifierTests()
    {
        _classifier = new LocationClassifier();
    }

    [Fact]
    public void Classify_NullPath_ReturnsSafe()
    {
        // Requirement: Unknown or inaccessible data must not be treated as automatically malicious.
        var result = _classifier.Classify(null);
        Assert.Equal(SuspiciousLocationClassification.Safe, result);
    }

    [Fact]
    public void Classify_EmptyPath_ReturnsSafe()
    {
        var result = _classifier.Classify("   ");
        Assert.Equal(SuspiciousLocationClassification.Safe, result);
    }

    [Fact]
    public void Classify_System32Path_ReturnsSafe()
    {
        var path = @"C:\Windows\System32\cmd.exe";
        var result = _classifier.Classify(path);
        Assert.Equal(SuspiciousLocationClassification.Safe, result);
    }

    [Fact]
    public void Classify_TempPath_ReturnsTemp()
    {
        var tempFolder = Path.GetTempPath();
        var malwarePath = Path.Combine(tempFolder, "malware.exe");
        
        var result = _classifier.Classify(malwarePath);
        
        Assert.Equal(SuspiciousLocationClassification.Temp, result);
    }

    [Fact]
    public void Classify_AppDataPath_ReturnsAppData()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var malwarePath = Path.Combine(appData, "hidden", "logger.exe");
        
        var result = _classifier.Classify(malwarePath);
        
        Assert.Equal(SuspiciousLocationClassification.AppData, result);
    }

    [Fact]
    public void Classify_LocalAppDataPath_ReturnsLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appPath = Path.Combine(localAppData, "Vendor", "tool.exe");

        var result = _classifier.Classify(appPath);

        Assert.Equal(SuspiciousLocationClassification.LocalAppData, result);
    }

    [Fact]
    public void Classify_LocalAppDataProgramsPath_ReturnsSafe()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appPath = Path.Combine(localAppData, "Programs", "Vendor", "tool.exe");

        var result = _classifier.Classify(appPath);

        Assert.Equal(SuspiciousLocationClassification.Safe, result);
    }
    
    [Fact]
    public void Classify_DownloadsPath_ReturnsDownloads()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloadsFolder = Path.Combine(userProfile, "Downloads");
        var malwarePath = Path.Combine(downloadsFolder, "installer.exe");
        
        var result = _classifier.Classify(malwarePath);
        
        Assert.Equal(SuspiciousLocationClassification.Downloads, result);
    }

    [Fact]
    public void Classify_MalformedPath_ReturnsSafe_Gracefully()
    {
        var result = _classifier.Classify("C:\\Invalid::\\<><>?Path.exe");
        // Path.GetFullPath will throw ArgumentException 
        Assert.Equal(SuspiciousLocationClassification.Safe, result);
    }
}
