namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Represents a single scoring rule that contributed to a risk assessment.
/// Proposal reference: § 2.1.3 — each suspicious indicator contributes points.
/// </summary>
public sealed class TriggeredRule
{
    /// <summary>Unique identifier for the rule (e.g., "R1", "R2").</summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>Human-readable name of the rule.</summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>Points contributed by this rule.</summary>
    public int Score { get; init; }

    /// <summary>
    /// Short human-readable reason explaining why the rule triggered.
    /// Proposal reference: § 2.1.5 — alerts must include "short reasons."
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    public override string ToString() => $"[+{Score}] {RuleName}: {Reason}";
}
