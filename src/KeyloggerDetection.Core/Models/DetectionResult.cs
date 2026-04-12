namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Captures the complete outcome of a scoring evaluation.
/// Proposal linkage: Explainable Risk Scoring output container.
/// </summary>
public sealed class DetectionResult
{
    /// <summary>Basic process identity (PID, Name, Path).</summary>
    public required ProcessInfo ProcessIdentity { get; init; }

    /// <summary>The exact snapshot of features that triggered the evaluation.</summary>
    public required FeatureVector Features { get; init; }

    /// <summary>Total calculated score across all rules.</summary>
    public required int TotalScore { get; init; }

    /// <summary>The threshold at the time of calculation.</summary>
    public required int Threshold { get; init; }

    /// <summary>Whether the event exceeded the threshold (TotalScore > Threshold).</summary>
    public bool IsSuspicious => TotalScore > Threshold;

    /// <summary>
    /// Whether the score-above-threshold result also satisfies the user-facing
    /// alert guardrail requiring a stronger behavioural signal.
    /// </summary>
    public bool MeetsAlertGuardrail { get; init; } = true;

    /// <summary>Explains why a score-above-threshold result was suppressed.</summary>
    public string? AlertGuardrailReason { get; init; }

    /// <summary>True only when the strict score rule and the alert guardrail both pass.</summary>
    public bool ShouldRaiseAlert => IsSuspicious && MeetsAlertGuardrail;

    /// <summary>Clear human-readable reasons bridging indicators to the score.</summary>
    public required IReadOnlyList<TriggeredRule> RuleHits { get; init; }

    /// <summary>When the decision was made.</summary>
    public DateTime EvaluationTime { get; init; } = DateTime.UtcNow;
}
