using System.Collections.Concurrent;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Evaluation;

internal sealed class ObservingScoringEngine : IRiskScoringEngine
{
    private readonly IRiskScoringEngine _inner;

    public ObservingScoringEngine(IRiskScoringEngine inner)
    {
        _inner = inner;
    }

    public event Action<DetectionResult>? Evaluated;

    public DetectionResult Evaluate(FeatureVector vector)
    {
        var result = _inner.Evaluate(vector);
        Evaluated?.Invoke(result);
        return result;
    }
}

internal sealed class DetectionTracker
{
    private readonly ConcurrentDictionary<int, ProcessObservation> _observations = new();

    public void BeginScenario(int pid, DateTime startedAtUtc)
    {
        _observations.TryAdd(pid, new ProcessObservation(startedAtUtc));
    }

    public void RecordEvaluation(DetectionResult result)
    {
        if (result.ProcessIdentity.Pid < 0)
        {
            return;
        }

        var observation = _observations.GetOrAdd(result.ProcessIdentity.Pid, _ => new ProcessObservation(result.EvaluationTime));
        observation.RecordEvaluation(result);
    }

    public void RecordAlert(DetectionResult result, string shortReasons)
    {
        if (result.ProcessIdentity.Pid < 0)
        {
            return;
        }

        var observation = _observations.GetOrAdd(result.ProcessIdentity.Pid, _ => new ProcessObservation(result.EvaluationTime));
        observation.RecordAlert(result, shortReasons);
    }

    public ProcessObservation? GetObservation(int pid)
    {
        return _observations.TryGetValue(pid, out var observation) ? observation : null;
    }
}

internal sealed class ProcessObservation
{
    private readonly object _lock = new();

    public ProcessObservation(DateTime firstSeenUtc)
    {
        FirstSeenUtc = firstSeenUtc;
    }

    public DateTime FirstSeenUtc { get; }
    public bool AlertRaised { get; private set; }
    public DateTime? FirstAlertUtc { get; private set; }
    public int HighestScore { get; private set; }
    public string? HighestScoreRules { get; private set; }
    public string? Notes { get; private set; }

    public void RecordEvaluation(DetectionResult result)
    {
        lock (_lock)
        {
            if (result.TotalScore >= HighestScore)
            {
                HighestScore = result.TotalScore;
                HighestScoreRules = string.Join("; ", result.RuleHits.Select(rule => rule.RuleName));
            }
        }
    }

    public void RecordAlert(DetectionResult result, string shortReasons)
    {
        lock (_lock)
        {
            AlertRaised = true;
            FirstAlertUtc ??= result.EvaluationTime;

            if (result.TotalScore >= HighestScore)
            {
                HighestScore = result.TotalScore;
                HighestScoreRules = shortReasons;
            }
        }
    }

    public double? FirstAlertLatencySeconds(DateTime startedAtUtc)
    {
        if (!FirstAlertUtc.HasValue)
        {
            return null;
        }

        return Math.Max(0, (FirstAlertUtc.Value - startedAtUtc).TotalSeconds);
    }
}

internal sealed class CapabilityProfile
{
    public bool FileTelemetryAvailable { get; init; }

    public static CapabilityProfile FromLogEntries(IReadOnlyList<LogEntry> entries)
    {
        var fileTelemetryUnavailable = entries.Any(entry =>
            entry.Message.Contains("ETW file tracing requires Administrator privileges", StringComparison.OrdinalIgnoreCase));

        return new CapabilityProfile
        {
            FileTelemetryAvailable = !fileTelemetryUnavailable
        };
    }
}
