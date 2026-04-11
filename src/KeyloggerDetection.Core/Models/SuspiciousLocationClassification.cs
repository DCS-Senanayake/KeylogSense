namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Categorises the disk location from which an executable is running.
/// Proposal reference: § 2.1.2 — "suspicious user-writable location checks."
/// </summary>
public enum SuspiciousLocationClassification
{
    /// <summary>Standard, protected system location (e.g., Program Files, Windows).</summary>
    Safe,

    /// <summary>Roaming or Local AppData directories.</summary>
    AppData,

    /// <summary>User temp directories.</summary>
    Temp,

    /// <summary>User downloads folder.</summary>
    Downloads,

    /// <summary>Other commonly abused writable locations (e.g., public desktop, documents).</summary>
    OtherSuspicious
}
