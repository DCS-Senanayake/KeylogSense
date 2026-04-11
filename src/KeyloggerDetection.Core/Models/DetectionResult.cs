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

    /// <summary>Clear human-readable reasons bridging indicators to the score.</summary>
    public required IReadOnlyList<TriggeredRule> RuleHits { get; init; }

    /// <summary>When the decision was made.</summary>
    public DateTime EvaluationTime { get; init; } = DateTime.UtcNow;
}
