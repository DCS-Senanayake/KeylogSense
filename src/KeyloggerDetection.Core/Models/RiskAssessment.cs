namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents the result of evaluating a process against the risk scoring engine.
/// Proposal reference: § 2.1.3 — risk score computation and threshold comparison.
/// </summary>
public sealed class RiskAssessment
{
    /// <summary>The process that was evaluated.</summary>
    public required ProcessInfo Process { get; init; }

    /// <summary>Total risk score (sum of all triggered rule scores).</summary>
    public int TotalScore { get; init; }

    /// <summary>The threshold that was used for comparison.</summary>
    public int Threshold { get; init; }

    /// <summary>
    /// Whether the process is flagged as suspicious.
    /// True when TotalScore > Threshold (strict greater-than).
    /// Proposal reference: § 2.1.3 — "If score > threshold then alert."
    /// </summary>
    public bool IsFlagged => TotalScore > Threshold;

    /// <summary>List of rules that triggered and contributed to the score.</summary>
    public IReadOnlyList<TriggeredRule> TriggeredRules { get; init; } = [];

    /// <summary>Timestamp when the assessment was performed.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
