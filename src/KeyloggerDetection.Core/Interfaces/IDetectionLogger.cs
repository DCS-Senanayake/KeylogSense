using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Logs detection events to CSV/text files.
/// Proposal reference: § 2.1.6 — log files containing timestamp, process details,
/// feature values, risk score, and triggered rules.
/// Implementation target: Phase P2 (logging infrastructure).
/// </summary>
public interface IDetectionLogger : IDisposable
{
    /// <summary>
    /// Logs a detection event to the configured log file.
    /// </summary>
    void LogDetection(DetectionEvent detectionEvent);

    /// <summary>
    /// Gets the directory where log files are stored.
    /// </summary>
    string LogDirectory { get; }
}
