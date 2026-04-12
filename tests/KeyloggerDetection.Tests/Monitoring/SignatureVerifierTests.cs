using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.ProcessContext;

namespace KeyloggerDetection.Tests.Monitoring;

public class SignatureVerifierTests
{
    [Fact]
    public void Inspect_MissingFile_ReturnsUnknown()
    {
        var verifier = new SignatureVerifier();

        var result = verifier.Inspect(@"C:\does-not-exist\missing.exe");

        Assert.Equal(TrustState.Unknown, result.TrustState);
        Assert.Null(result.PublisherName);
    }

    [Fact]
    public void Inspect_UnsignedFile_ReturnsInvalidSignature()
    {
        var verifier = new SignatureVerifier();
        var unsignedPePath = typeof(SignatureVerifierTests).Assembly.Location;

        var result = verifier.Inspect(unsignedPePath);

        Assert.Equal(TrustState.InvalidSignature, result.TrustState);
    }
}
