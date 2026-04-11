using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Evaluates a process against all scoring rules and produces a risk assessment.
/// Proposal reference: § 2.1.3 — detection method (explainable risk scoring).
/// Implementation target: Phase P7.
/// </summary>
public interface IRiskScoringEngine
{
    /// <summary>
    /// Evaluates the aggregated feature vector representing process behaviour
    /// against all configured scoring rules.
    /// Returns a <see cref="DetectionResult"/> containing the total score,
    /// triggered rules, and whether the process is flagged (score > threshold).
    /// </summary>
    DetectionResult Evaluate(FeatureVector vector);
}
