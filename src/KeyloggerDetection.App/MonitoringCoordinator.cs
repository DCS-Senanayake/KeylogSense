using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Monitoring.ProcessContext;
using KeyloggerDetection.Monitoring.FileBehaviour;
using KeyloggerDetection.Monitoring.NetworkBehaviour;
using KeyloggerDetection.Monitoring.Persistence;
using KeyloggerDetection.Scoring;

namespace KeyloggerDetection.App;

/// <summary>
/// Stitches together process, file, network, and persistence monitors, feeding the aggregator.
/// </summary>
public sealed class MonitoringCoordinator : IMonitoringCoordinator
{
    private readonly IAppLogger _logger;
    private readonly IEnumerable<ICollector> _collectors;
    private readonly FeatureAggregator _aggregator;
    private readonly ITelemetryPipeline _pipeline;
    
    private CancellationTokenSource? _cts;
    
    public event Action<DetectionResult, string>? OnAlert;

    public MonitoringCoordinator(
        IAppLogger logger, 
        IEnumerable<ICollector> collectors,
        FeatureAggregator aggregator,
        ITelemetryPipeline pipeline)
    {
        _logger = logger;
        _collectors = collectors;
        _aggregator = aggregator;
        _pipeline = pipeline;

        // Pass-through the event hook
        _aggregator.OnSuspiciousAlert += (res, rs) => OnAlert?.Invoke(res, rs);
    }

    public void Start()
    {
        if (_cts != null) return;
        
        _logger.LogInfo("Monitoring Coordinator starting all modules...");
        _cts = new CancellationTokenSource();

        // 1. Start pipeline consumer (Aggregator)
        _ = Task.Run(() => _aggregator.StartProcessingAsync(_cts.Token));

        // 2. Start all discrete telemetry collectors
        foreach (var collector in _collectors)
        {
            _ = Task.Run(() => collector.StartAsync(_pipeline, _cts.Token));
        }
    }

    public void Stop()
    {
        if (_cts == null) return;

        _logger.LogInfo("Monitoring Coordinator stopping...");
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    public void Dispose()
    {
        Stop();
        foreach (var collector in _collectors)
        {
            collector.Dispose();
        }
    }
}
