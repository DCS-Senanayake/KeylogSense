using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Scoring;

namespace KeyloggerDetection.Tests.Monitoring;

public class AllowlistManagerTests
{
    [Fact]
    public void IsTrusted_PublisherMatches_ReturnsTrue()
    {
        var config = new DetectionConfig();
        config.Allowlist.TrustedPublishers = ["ValidCorp Inc."];
        var manager = new AllowlistManager(config);

        var result = manager.IsTrusted(@"C:\App.exe", "App", TrustState.Trusted, "ValidCorp Inc.");
        
        Assert.True(result);
    }

    [Fact]
    public void IsTrusted_UntrustedStateWithValidName_ReturnsFalse()
    {
        var config = new DetectionConfig();
        config.Allowlist.TrustedPublishers = ["Microsoft Corporation"];
        var manager = new AllowlistManager(config);

        // Name matches, but certificate throws invalid
        var result = manager.IsTrusted(@"C:\App.exe", "App", TrustState.Untrusted, "Microsoft Corporation");
        
        Assert.False(result);
    }

    [Fact]
    public void IsTrusted_PathMatches_ReturnsTrue()
    {
        var config = new DetectionConfig();
        config.Allowlist.TrustedExecutablePaths = [@"C:\Static\SafeApp.exe"];
        var manager = new AllowlistManager(config);

        // Normalize matching
        var result = manager.IsTrusted(@"c:\static\safeapp.exe", null, TrustState.Unknown, null);
        
        Assert.True(result);
    }
    
    [Fact]
    public void IsTrusted_NameMatchesWithOrWithoutExe_ReturnsTrue()
    {
        var config = new DetectionConfig();
        config.Allowlist.TrustedProcessNames = ["notepad"];
        var manager = new AllowlistManager(config);

        var result1 = manager.IsTrusted(@"C:\notepad.exe", "notepad.exe", TrustState.Unknown, null);
        var result2 = manager.IsTrusted(@"C:\notepad.exe", "notepad", TrustState.Unknown, null);
        
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public void IsHashTrusted_Matches_ReturnsTrue()
    {
        var config = new DetectionConfig();
        config.Allowlist.TrustedHashes = ["a1b2c3d4"];
        var manager = new AllowlistManager(config);

        Assert.True(manager.IsHashTrusted("A1B2C3D4")); // Case insensitive verify
        Assert.False(manager.IsHashTrusted("F9E8"));
    }
}
