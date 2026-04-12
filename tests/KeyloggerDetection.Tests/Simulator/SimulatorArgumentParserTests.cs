using KeyloggerDetection.Simulator;

namespace KeyloggerDetection.Tests.Simulator;

public sealed class SimulatorArgumentParserTests
{
    [Fact]
    public void Parse_UsesSingleCombinedFlowDefaults()
    {
        var result = SimulatorArgumentParser.Parse([]);

        Assert.False(result.ShowHelp);
        Assert.False(result.Options.CleanupOnly);
        Assert.False(result.Options.EnablePersistence);
        Assert.Equal(15, result.Options.FileWriteIterations);
        Assert.Equal(3, result.Options.NetworkBurstCount);
        Assert.EndsWith("keylogger-behaviour-sim.log", result.Options.ArtifactPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_AcceptsSafeConfigurationFlags()
    {
        var result = SimulatorArgumentParser.Parse(
        [
            "--enable-persistence",
            "--artifact-path", @"C:\Temp\demo.log",
            "--file-write-iterations", "8",
            "--network-bursts", "2"
        ]);

        Assert.True(result.Options.EnablePersistence);
        Assert.Equal(@"C:\Temp\demo.log", result.Options.ArtifactPath);
        Assert.Equal(8, result.Options.FileWriteIterations);
        Assert.Equal(2, result.Options.NetworkBurstCount);
    }

    [Fact]
    public void Parse_RecognizesCleanupFlag()
    {
        var result = SimulatorArgumentParser.Parse(["--cleanup"]);

        Assert.True(result.Options.CleanupOnly);
    }

    [Fact]
    public void Parse_RejectsUnknownArguments()
    {
        var ex = Assert.Throws<ArgumentException>(() => SimulatorArgumentParser.Parse(["combined"]));

        Assert.Contains("Unknown argument", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_RejectsNonPositiveNumericValues()
    {
        var ex = Assert.Throws<ArgumentException>(() => SimulatorArgumentParser.Parse(["--network-bursts", "0"]));

        Assert.Contains("--network-bursts", ex.Message, StringComparison.Ordinal);
    }
}
