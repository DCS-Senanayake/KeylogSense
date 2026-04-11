namespace KeyloggerDetection.Tests;

/// <summary>
/// Placeholder test to verify the test project builds and runs.
/// Real tests will be added alongside each implementation phase.
/// </summary>
public class SanityTests
{
    [Fact]
    public void Solution_Builds_And_Tests_Run()
    {
        // This test exists solely to verify the test infrastructure works.
        Assert.True(true);
    }

    [Fact]
    public void DetectionConfig_Defaults_Match_Proposal_Table1()
    {
        // Verify that the default scoring weights match proposal Table 1.
        var config = new Core.Configuration.DetectionConfig();

        Assert.Equal(4, config.SuspiciousLocationScore);   // Table 1: +4
        Assert.Equal(3, config.UntrustedPublisherScore);   // Table 1: +3
        Assert.Equal(2, config.FrequentSmallWritesScore);   // Table 1: +2
        Assert.Equal(2, config.RepeatedSameFileWritesScore); // Table 1: +2
        Assert.Equal(2, config.OutboundNetworkScore);       // Table 1: +2
        Assert.Equal(2, config.FileNetworkCorrelationScore); // Table 1: +2
        Assert.Equal(5, config.PersistenceDetectedScore);   // Table 1: +5
    }

    [Fact]
    public void RiskAssessment_Uses_StrictGreaterThan()
    {
        // Proposal § 2.1.3: "If score > threshold then alert"
        // Score equal to threshold must NOT trigger.
        var process = new Core.Models.ProcessInfo { Pid = 1, Name = "test" };

        var atThreshold = new Core.Models.RiskAssessment
        {
            Process = process,
            TotalScore = 7,
            Threshold = 7
        };
        Assert.False(atThreshold.IsFlagged, "Score == threshold should NOT flag (strict >).");

        var aboveThreshold = new Core.Models.RiskAssessment
        {
            Process = process,
            TotalScore = 8,
            Threshold = 7
        };
        Assert.True(aboveThreshold.IsFlagged, "Score > threshold should flag.");

        var belowThreshold = new Core.Models.RiskAssessment
        {
            Process = process,
            TotalScore = 6,
            Threshold = 7
        };
        Assert.False(belowThreshold.IsFlagged, "Score < threshold should NOT flag.");
    }
}
