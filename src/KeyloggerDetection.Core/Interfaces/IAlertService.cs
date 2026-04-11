using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Raises user-visible alerts when a process is flagged.
/// Proposal reference: § 2.1.5 — tray/toast notification with process name,
/// PID, risk score, and short reasons.
/// Implementation target: Phase P8.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Raises an alert for the given risk assessment.
    /// </summary>
    void RaiseAlert(RiskAssessment assessment);
}
