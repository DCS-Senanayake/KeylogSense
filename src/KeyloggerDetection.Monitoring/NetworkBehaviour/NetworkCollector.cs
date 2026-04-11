using System.Net;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.NetworkBehaviour;

/// <summary>
/// Periodically snapshots outbound network sockets.
/// Incorporates specific loopback filtering and semantic tracking to ensure
/// events fired indicate fresh connections strictly. 
/// </summary>
public sealed class NetworkCollector : ICollector
{
    private readonly IAppLogger _logger;
    private readonly DetectionConfig _config;
    private readonly IClock _clock;
    
    // Hash string combining PID:LocalEndpoint:RemoteEndpoint
    // Used to suppress duplicates (Snapshot diff logic)
    private HashSet<string> _activeConnections = new();
    
    // Injectable provder for unit tests
    private readonly Func<IEnumerable<Win32TcpTable.TcpConnectionRecord>> _tcpTableProvider;

    public NetworkCollector(IAppLogger logger, DetectionConfig config, IClock clock)
        : this(logger, config, clock, Win32TcpTable.GetAllTcpConnections)
    {
    }

    // Internal for testability
    internal NetworkCollector(IAppLogger logger, DetectionConfig config, IClock clock, Func<IEnumerable<Win32TcpTable.TcpConnectionRecord>> tcpTableProvider)
    {
        _logger = logger;
        _config = config;
        _clock = clock;
        _tcpTableProvider = tcpTableProvider;
    }

    public async Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        _logger.LogInfo("NetworkCollector started.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TakeSnapshot(pipeline);
                await Task.Delay(_config.NetworkPollingIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("NetworkCollector shutdown requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError("NetworkCollector encountered a fatal error.", ex);
        }
    }

    public void TakeSnapshot(ITelemetryPipeline pipeline)
    {
        var rawConnections = _tcpTableProvider();
        var currentSnapshot = new HashSet<string>();

        foreach (var conn in rawConnections)
        {
            // Fulfill config limitations
            if (_config.ExcludeLoopbackTraffic && IPAddress.IsLoopback(conn.RemoteAddress))
            {
                continue;
            }

            // Exclude completely local 0.0.0.0 binds since they are listeners, not active outbound sessions to internet targets
            if (conn.RemoteAddress.Equals(IPAddress.Any) || conn.RemoteAddress.Equals(IPAddress.None))
            {
                continue;
            }

            var localEpStr = $"{conn.LocalAddress}:{conn.LocalPort}";
            var remoteEpStr = $"{conn.RemoteAddress}:{conn.RemotePort}";
            
            // Uniquely identify the session bound to a process.
            var sessionKey = $"{conn.Pid}|{localEpStr}|{remoteEpStr}";
            currentSnapshot.Add(sessionKey);

            // Snapshot Diff Logic - only emit on New Session discovery
            if (!_activeConnections.Contains(sessionKey))
            {
                var telemetryEvent = new NetworkConnectionEvent(
                    conn.Pid,
                    _clock.UtcNow,
                    localEpStr,
                    remoteEpStr,
                    "TCP"
                );

                pipeline.Publish(telemetryEvent);
            }
        }

        // Swap state: next cycle evaluates against the current reality.
        // Old connections die silently without generating events.
        _activeConnections = currentSnapshot;
    }

    public void Dispose()
    {
        // No unmanaged resources inside collector
    }
}
