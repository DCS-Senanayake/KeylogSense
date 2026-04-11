namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents the trust classification of an executable publisher.
/// Proposal reference: § 2.1.2 — "trusted/signed publisher status where feasible."
/// </summary>
public enum TrustState
{
    /// <summary>Status is unknown (e.g., could not be verified or hasn't been checked).</summary>
    Unknown,

    /// <summary>Executable has a valid signature from a trusted publisher.</summary>
    Trusted,

    /// <summary>Executable is explicitly untrusted (e.g., known bad, or obviously suspect).</summary>
    Untrusted,

    /// <summary>Executable is unsigned or has an invalid/broken signature.</summary>
    InvalidSignature
}
