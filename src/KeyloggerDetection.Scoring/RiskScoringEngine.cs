using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Scoring;

/// <summary>
/// Analyzes aggregated Process feature vectors, applying the discrete weights mapped in the proposal.
/// Maintains complete deterministic evaluation strings to enable explainability.
/// </summary>
public sealed class RiskScoringEngine : IRiskScoringEngine /* Assuming interface exists or matches */
{
    private readonly DetectionConfig _config;
    private readonly AllowlistManager _allowlist;
    private readonly IClock _clock;

    public RiskScoringEngine(DetectionConfig config, AllowlistManager allowlist, IClock clock)
    {
        _config = config;
        _allowlist = allowlist;
        _clock = clock;
    }

    /// <summary>
    /// Evaluates a feature vector into a resulting Detection decision.
    /// Strict threshold enforcement: Suspicious = Score > Threshold
    /// </summary>
    public DetectionResult Evaluate(FeatureVector vector)
    {
        var rules = new List<TriggeredRule>();
        int score = 0;

        // 1. Contextual Allowlist check first - significantly reduces false positives.
        if (_allowlist.IsTrusted(vector.ExecutablePath, vector.ProcessName, vector.Trust, vector.PublisherName))
        {
            return BuildResult(vector, score, rules); // 0 score, cleanly bypassed
        }

        // 2. Assess Suspicious Location (+4)
        if (vector.LocationClassification != SuspiciousLocationClassification.Safe)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-01",
                RuleName = "Suspicious Location",
                Score = _config.SuspiciousLocationScore,
                Reason = $"Process is running from a commonly abused user-writable location: {vector.LocationClassification}"
            });
            score += _config.SuspiciousLocationScore;
        }

        // 3. Assess Untrusted Publisher (+3)
        // Unknown is implicitly ignored as per requirements (Not malicious just because we couldn't read the cert).
        if (vector.Trust == TrustState.Untrusted || vector.Trust == TrustState.InvalidSignature)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-02",
                RuleName = "Untrusted Publisher",
                Score = _config.UntrustedPublisherScore,
                Reason = $"Executable lacks a valid, trusted digital signature ({vector.Trust})"
            });
            score += _config.UntrustedPublisherScore;
        }

        // 4. Repeated Small Writes (+2)
        if (vector.SmallWriteCount >= _config.SmallWriteCountThreshold)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-03",
                RuleName = "Frequent Small File Writes",
                Score = _config.FrequentSmallWritesScore,
                Reason = $"Detected {vector.SmallWriteCount} consecutive small file writes indicative of potential keystroke caching."
            });
            score += _config.FrequentSmallWritesScore;
        }

        // 5. Repeated Same-File Writes (+2)
        if (vector.RepeatedSameFileWriteCount >= _config.RepeatedSameFileWriteThreshold)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-04",
                RuleName = "Repeated Writes to Same File",
                Score = _config.RepeatedSameFileWritesScore,
                Reason = $"Process updated the same target file {vector.RepeatedSameFileWriteCount} times within the short-term window."
            });
            score += _config.RepeatedSameFileWritesScore;
        }

        // 6. Outbound Connection (+2)
        if (vector.HasOutboundConnections)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-05",
                RuleName = "Outbound Network Activity",
                Score = _config.OutboundNetworkScore,
                Reason = "Process actively established new outbound network connections to remote targets."
            });
            score += _config.OutboundNetworkScore;
        }

        // 7. Time Correlation: Network & File Logging proximity (+2)
        if (vector.HasOutboundConnections && vector.LastFileWriteTime.HasValue && vector.LastNetworkActivityTime.HasValue)
        {
            var diff = Math.Abs((vector.LastFileWriteTime.Value - vector.LastNetworkActivityTime.Value).TotalSeconds);
            if (diff <= _config.FileNetworkCorrelationWindowSeconds)
            {
                rules.Add(new TriggeredRule
                {
                    RuleId = "RL-06",
                    RuleName = "Simultaneous Network and File Activity",
                    Score = _config.FileNetworkCorrelationScore,
                    Reason = $"File write occurred {diff:F1} seconds apart from network connection. Suggests synchronized local cache and exfiltration."
                });
                score += _config.FileNetworkCorrelationScore;
            }
        }

        // 8. Persistence Indicator (+5)
        if (vector.PersistenceDetected)
        {
            rules.Add(new TriggeredRule
            {
                RuleId = "RL-07",
                RuleName = "Persistence Detected",
                Score = _config.PersistenceDetectedScore,
                Reason = "Process utilizes a recognized Registry Run key or Startup folder mechanism to survive reboots."
            });
            score += _config.PersistenceDetectedScore;
        }

        return BuildResult(vector, score, rules);
    }

    private DetectionResult BuildResult(FeatureVector vector, int score, List<TriggeredRule> rules)
    {
        return new DetectionResult
        {
            ProcessIdentity = new ProcessInfo 
            { 
                Pid = vector.Pid, 
                Name = vector.ProcessName, 
                ExecutablePath = vector.ExecutablePath 
            },
            Features = vector,
            TotalScore = score,
            Threshold = _config.AlertThreshold,
            RuleHits = rules,
            EvaluationTime = _clock.UtcNow
        };
    }
}
