using System.Collections.Concurrent;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Monitoring.FileBehaviour;
using KeyloggerDetection.Scoring;

namespace KeyloggerDetection.App;

/// <summary>
/// Consumes raw telemetry, builds stateful Process Feature Vectors, and pipes them 
/// into the RiskScoringEngine. Emits actionable alerts on threshold breach.
/// </summary>
public sealed class FeatureAggregator
{
    private readonly DetectionConfig _config;
    private readonly IAppLogger _logger;
    private readonly ITelemetryPipeline _pipeline;
    private readonly IRiskScoringEngine _scoringEngine;
    private readonly IDetectionLogger _detectionLogger;
    private readonly FileWriteAnalyzer _fileWriteAnalyzer;

    // Stateful representation of active processes matching the indicators
    private readonly ConcurrentDictionary<int, FeatureVector> _vectors = new();
    
    // Alert debouncing (PID -> Last Alert Time)
    private readonly ConcurrentDictionary<int, DateTime> _lastAlerts = new();
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(2); // Suppress spam for 2 mins

    public event Action<DetectionResult, string>? OnSuspiciousAlert;

    public FeatureAggregator(
        DetectionConfig config,
        IAppLogger logger, 
        ITelemetryPipeline pipeline, 
        IRiskScoringEngine scoringEngine,
        IDetectionLogger detectionLogger,
        FileWriteAnalyzer fileWriteAnalyzer)
    {
        _config = config;
        _logger = logger;
        _pipeline = pipeline;
        _scoringEngine = scoringEngine;
        _detectionLogger = detectionLogger;
        _fileWriteAnalyzer = fileWriteAnalyzer;
    }

    public async Task StartProcessingAsync(CancellationToken token)
    {
        _logger.LogInfo("FeatureAggregator started consuming telemetry.");
        try
        {
            await foreach (var evt in _pipeline.ConsumeAsync(token))
            {
                var vector = GetOrAddVector(evt.Pid);
                bool updated = MapEventToVector(evt, vector);

                if (updated)
                {
                    EvaluateAndAlert(vector);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("FeatureAggregator shutdown requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Fatal error in FeatureAggregator processing loop.", ex);
        }
    }

    private FeatureVector GetOrAddVector(int pid)
    {
        return _vectors.GetOrAdd(pid, _ => new FeatureVector { Pid = pid });
    }

    private bool MapEventToVector(TelemetryEvent evt, FeatureVector vector)
    {
        bool updated = false;

        switch (evt)
        {
            case ProcessContextEvent pce:
                vector.ProcessName = pce.ProcessName;
                vector.ExecutablePath = pce.ExecutablePath;
                vector.LocationClassification = pce.LocationClassification;
                vector.Trust = pce.Trust;
                vector.PublisherName = pce.PublisherName;
                updated = true;
                break;

            case FileWriteEvent fwe:
                var fae = _fileWriteAnalyzer.ProcessEvent(fwe);
                if (fae != null && (fae.SmallWriteCount > 0 || fae.RepeatedSameFileWriteCount > 0))
                {
                    vector.SmallWriteCount = fae.SmallWriteCount;
                    vector.RepeatedSameFileWriteCount = fae.RepeatedSameFileWriteCount;
                    vector.LastFileWriteTime = fae.LastActivityTime;
                    updated = true;
                }
                break;

            case NetworkConnectionEvent nce:
                vector.HasOutboundConnections = true;
                vector.OutboundConnectionCount++;
                vector.LastNetworkActivityTime = nce.Timestamp;
                updated = true;
                break;

            case PersistenceEvent pe:
                vector.PersistenceDetected = true;
                
                // If the PID is unknown (-1) but we know the path, map it to an active vector if already cached
                if (vector.Pid == -1 && !string.IsNullOrWhiteSpace(pe.PersistenceType))
                {
                    var match = _vectors.Values.FirstOrDefault(v => string.Equals(v.ExecutablePath, pe.PersistenceType, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        match.PersistenceDetected = true;
                        updated = false; // We updated match instead
                        EvaluateAndAlert(match);
                    }
                }
                else
                {
                    updated = true;
                }
                break;
        }

        return updated;
    }

    private void EvaluateAndAlert(FeatureVector vector)
    {
        if (vector.Pid == -1) return; // Unmapped isolated events don't score reliably alone without PID context.

        var result = _scoringEngine.Evaluate(vector);

        if (result.ShouldRaiseAlert)
        {
            // Debounce check
            var now = DateTime.UtcNow;
            if (_lastAlerts.TryGetValue(vector.Pid, out var lastTime) && (now - lastTime) < AlertCooldown)
            {
                return; // Suppressed
            }

            _lastAlerts[vector.Pid] = now;

            // Generate rules string safely
            var shortReasons = string.Join("; ", result.RuleHits.Select(r => r.RuleName));

            // Log securely to persistent storage
            var logEvent = new DetectionEvent
            {
                Timestamp = result.EvaluationTime,
                ProcessName = vector.ProcessName ?? "Unknown",
                Pid = vector.Pid,
                ExecutablePath = vector.ExecutablePath,
                SuspiciousLocation = vector.LocationClassification != SuspiciousLocationClassification.Safe,
                UntrustedPublisher = vector.Trust == TrustState.Untrusted || vector.Trust == TrustState.InvalidSignature,
                FrequentSmallWrites = vector.SmallWriteCount >= _config.SmallWriteCountThreshold,
                RepeatedSameFileWrites = vector.RepeatedSameFileWriteCount >= _config.RepeatedSameFileWriteThreshold,
                OutboundNetwork = vector.OutboundConnectionCount >= _config.OutboundConnectionCountThreshold,
                FileNetworkCorrelation = result.RuleHits.Any(r => r.RuleName.Contains("Simultaneous Network")),
                PersistenceDetected = vector.PersistenceDetected,
                RiskScore = result.TotalScore,
                TriggeredRules = shortReasons
            };

            _detectionLogger.LogDetection(logEvent);

            // Notify UI Context
            OnSuspiciousAlert?.Invoke(result, shortReasons);
        }
    }
}
