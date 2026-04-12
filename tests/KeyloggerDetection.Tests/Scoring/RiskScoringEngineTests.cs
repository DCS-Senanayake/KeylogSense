using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Scoring;
using KeyloggerDetection.Tests.Mocks;

namespace KeyloggerDetection.Tests.Scoring;

public class RiskScoringEngineTests
{
    private DetectionConfig GetConfig() => new DetectionConfig();
    private AllowlistManager GetAllowlist(DetectionConfig config) => new AllowlistManager(config);

    [Fact]
    public void Evaluate_AllFeaturesClear_ScoresZero()
    {
        var config = GetConfig();
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        var vector = new FeatureVector { Pid = 123, ProcessName = "clean.exe" };
        
        var result = engine.Evaluate(vector);
        
        Assert.Equal(0, result.TotalScore);
        Assert.False(result.IsSuspicious);
        Assert.Empty(result.RuleHits);
    }

    [Fact]
    public void Evaluate_UnknownTrust_DoesNotScore()
    {
        var config = GetConfig();
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        // Important Edge Case: Missing certs on common tools doesn't mean malicious directly.
        var vector = new FeatureVector { Trust = TrustState.Unknown };
        
        var result = engine.Evaluate(vector);
        
        // No points added for unknown
        Assert.Equal(0, result.TotalScore);
    }

    [Fact]
    public void Evaluate_ExceedsThreshold_MarksSuspicious()
    {
        var config = GetConfig();
        config.AlertThreshold = 7; // As defined by ENGINEERING ASSUMPTION
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        // Combination: AppData suspicious location(+3) + Persistence(+5) = 8
        var vector = new FeatureVector 
        { 
            LocationClassification = SuspiciousLocationClassification.AppData,
            PersistenceDetected = true 
        };
        
        var result = engine.Evaluate(vector);
        
        Assert.Equal(8, result.TotalScore);
        Assert.True(result.IsSuspicious); // 9 > 7
        Assert.True(result.ShouldRaiseAlert);
        Assert.Equal(2, result.RuleHits.Count);
        
        // Assert human readable explanation populated
        Assert.Contains("Persistence", result.RuleHits.Last().RuleName);
    }

    [Fact]
    public void Evaluate_ExactlyAtThreshold_IsNotSuspicious()
    {
        var config = GetConfig();
        config.AlertThreshold = 4;
        
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        // Only Suspicious Location (+4)
        var vector = new FeatureVector 
        { 
            LocationClassification = SuspiciousLocationClassification.Temp
        };
        
        var result = engine.Evaluate(vector);
        
        Assert.Equal(4, result.TotalScore);
        
        // CRITICAL PROPOSAL RULE CHECK: > threshold, NOT >=
        Assert.False(result.IsSuspicious); 
    }

    [Fact]
    public void Evaluate_TimingCorrelation_RequiresTightWindowAndStrongFileSignal()
    {
        var config = GetConfig();
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0);
        
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        // Strong file signal is present, but the events are outside the tighter 10s burst window.
        var vector = new FeatureVector 
        { 
            HasOutboundConnections = true,
            OutboundConnectionCount = 2,
            SmallWriteCount = config.SmallWriteCountThreshold,
            LastFileWriteTime = baseTime,
            LastNetworkActivityTime = baseTime.AddSeconds(15) // +15 sec diff
        };
        
        var result = engine.Evaluate(vector);
        
        // 2 points for outbound burst + 2 points for frequent small writes = 4
        Assert.Equal(4, result.TotalScore);
        Assert.DoesNotContain(result.RuleHits, r => r.RuleName == "Simultaneous Network and File Activity");
    }

    [Fact]
    public void Evaluate_TimingCorrelation_AwardsBonusWithinBurstWindow()
    {
        var config = GetConfig();
        var baseTime = new DateTime(2025, 1, 1, 12, 0, 0);

        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        var vector = new FeatureVector
        {
            HasOutboundConnections = true,
            OutboundConnectionCount = 2,
            SmallWriteCount = config.SmallWriteCountThreshold,
            LastFileWriteTime = baseTime,
            LastNetworkActivityTime = baseTime.AddSeconds(5)
        };

        var result = engine.Evaluate(vector);

        Assert.Equal(6, result.TotalScore);
        Assert.Contains(result.RuleHits, r => r.RuleName == "Simultaneous Network and File Activity");
    }

    [Fact]
    public void Evaluate_WhitelistedPublisher_ScoresZeroDespiteMetrics()
    {
        var config = GetConfig();
        config.Allowlist.TrustedPublishers = ["SafeCorp"];
        
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        // Simulate horrific behaviour but from an explicit manually trusted source installer
        var vector = new FeatureVector 
        { 
            Trust = TrustState.Trusted,
            PublisherName = "SafeCorp",
            PersistenceDetected = true,
            LocationClassification = SuspiciousLocationClassification.Temp,
            SmallWriteCount = 50
        };
        
        var result = engine.Evaluate(vector);
        
        // Assert clean bypass
        Assert.Equal(0, result.TotalScore);
        Assert.False(result.IsSuspicious);
    }

    [Fact]
    public void Evaluate_WeakSignalsOnly_AreSuppressedByAlertGuardrail()
    {
        var config = GetConfig();
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        var vector = new FeatureVector
        {
            ProcessName = "chrome.exe",
            ExecutablePath = @"C:\Users\Test\AppData\Local\Google\Chrome\Application\chrome.exe",
            LocationClassification = SuspiciousLocationClassification.LocalAppData,
            Trust = TrustState.InvalidSignature,
            HasOutboundConnections = true,
            OutboundConnectionCount = config.OutboundConnectionCountThreshold
        };

        var result = engine.Evaluate(vector);

        Assert.False(result.IsSuspicious);
        Assert.False(result.ShouldRaiseAlert);
    }

    [Fact]
    public void Evaluate_ScoreAboveThresholdWithoutStrongBehaviour_DoesNotRaiseAlert()
    {
        var config = GetConfig();
        var engine = new RiskScoringEngine(config, GetAllowlist(config), new MockClock());

        var vector = new FeatureVector
        {
            ProcessName = "downloads-run.exe",
            ExecutablePath = @"C:\Users\Test\Downloads\downloads-run.exe",
            LocationClassification = SuspiciousLocationClassification.Downloads,
            Trust = TrustState.InvalidSignature,
            HasOutboundConnections = true,
            OutboundConnectionCount = config.OutboundConnectionCountThreshold
        };

        var result = engine.Evaluate(vector);

        Assert.True(result.IsSuspicious);
        Assert.False(result.ShouldRaiseAlert);
        Assert.False(result.MeetsAlertGuardrail);
        Assert.NotNull(result.AlertGuardrailReason);
    }
}
