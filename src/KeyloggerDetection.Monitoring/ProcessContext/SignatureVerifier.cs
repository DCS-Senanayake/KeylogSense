using System.Security.Cryptography.X509Certificates;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.ProcessContext;

/// <summary>
/// Basic Authenticode / Signature verifier.
/// Proposal reference: "trusted or signed publisher metadata where feasible."
/// </summary>
public sealed class SignatureVerifier
{
    /// <summary>
    /// Attempts to read the digital signature of the file.
    /// Note: Full verification of the chain requires WinVerifyTrust via P/Invoke. 
    /// For this version, checking if a valid cert exists is a best-effort "feasible" check.
    /// </summary>
    public TrustState CheckTrust(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return TrustState.Unknown;
        }

        try
        {
            // .NET provides a basic way to read embedded Authenticode signatures.
            // .NET 10 explicitly prefers X509CertificateLoader over the obsolete CreateFromSignedFile
            using var cert = X509CertificateLoader.LoadCertificateFromFile(executablePath);
            
            // We can elevate this to X509Certificate2 to check the chain if needed:
            // using var cert2 = new X509Certificate2(cert);
            // var chain = new X509Chain();
            // bool isValid = chain.Build(cert2); // Requires online Revocation check by default
            
            // For now, if it creates successfully, the file has a signature.
            return TrustState.Trusted;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Thrown when the file has no signature
            return TrustState.InvalidSignature;
        }
        catch (Exception)
        {
            // Access denied or other read error
            return TrustState.Unknown;
        }
    }
}
