using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Detects whether a process has persistence entries (e.g., Run-key registry entries).
/// Proposal reference: § 2.1.2, signal group 4 — basic persistence indicator.
/// Implementation target: Phase P6.
/// </summary>
public interface IPersistenceDetector
{
    /// <summary>
    /// Checks whether the given process has any persistence entries
    /// (e.g., entries in HKCU/HKLM Run or RunOnce registry keys).
    /// </summary>
    bool HasPersistenceEntry(ProcessInfo process);
}
