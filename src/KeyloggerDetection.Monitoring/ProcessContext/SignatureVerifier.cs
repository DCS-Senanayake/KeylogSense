using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.ProcessContext;

/// <summary>
/// Best-effort Authenticode inspection.
/// Signed files are treated as trusted unless Windows exposes an explicit
/// signature distrust condition. Inspection failures remain Unknown.
/// </summary>
public sealed class SignatureVerifier
{
    public SignatureInspectionResult Inspect(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return new SignatureInspectionResult(TrustState.Unknown, null);
        }

        try
        {
#pragma warning disable SYSLIB0057
            var rawCertificate = X509Certificate.CreateFromSignedFile(executablePath);
#pragma warning restore SYSLIB0057
            using var certificate = new X509Certificate2(rawCertificate);

            var publisherName = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
            var trustState = DetermineTrustState(certificate);

            return new SignatureInspectionResult(trustState, string.IsNullOrWhiteSpace(publisherName) ? null : publisherName);
        }
        catch (CryptographicException ex) when (LooksLikeMissingSignature(ex))
        {
            return new SignatureInspectionResult(TrustState.InvalidSignature, null);
        }
        catch (UnauthorizedAccessException)
        {
            return new SignatureInspectionResult(TrustState.Unknown, null);
        }
        catch (IOException)
        {
            return new SignatureInspectionResult(TrustState.Unknown, null);
        }
        catch (Exception)
        {
            return new SignatureInspectionResult(TrustState.Unknown, null);
        }
    }

    private static TrustState DetermineTrustState(X509Certificate2 certificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags =
            X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown |
            X509VerificationFlags.IgnoreCtlSignerRevocationUnknown |
            X509VerificationFlags.IgnoreEndRevocationUnknown |
            X509VerificationFlags.IgnoreRootRevocationUnknown;

        _ = chain.Build(certificate);

        foreach (var status in chain.ChainStatus)
        {
            if (status.Status is X509ChainStatusFlags.ExplicitDistrust
                or X509ChainStatusFlags.NotSignatureValid)
            {
                return TrustState.Untrusted;
            }
        }

        return TrustState.Trusted;
    }

    private static bool LooksLikeMissingSignature(CryptographicException ex)
    {
        return ex.HResult == unchecked((int)0x80092009)
            || ex.HResult == unchecked((int)0x8007000B)
            || ex.Message.Contains("signed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("object", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record SignatureInspectionResult(TrustState TrustState, string? PublisherName);
