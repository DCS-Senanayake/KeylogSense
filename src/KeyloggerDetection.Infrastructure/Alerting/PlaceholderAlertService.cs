using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Infrastructure.Alerting;

/// <summary>
/// Placeholder implementation of <see cref="IAlertService"/>.
/// Logs alerts to the console. The real implementation (tray/toast
/// notifications) will be connected in Phase P8.
/// </summary>
public sealed class PlaceholderAlertService : IAlertService
{
    public void RaiseAlert(RiskAssessment assessment)
    {
        // TODO: Connect to tray/toast notifications in Phase P8.
        // For now, write to console for development visibility.
        Console.WriteLine($"[ALERT] {assessment.Process} — Score: {assessment.TotalScore} (threshold: {assessment.Threshold})");
        foreach (var rule in assessment.TriggeredRules)
        {
            Console.WriteLine($"  {rule}");
        }
    }
}
