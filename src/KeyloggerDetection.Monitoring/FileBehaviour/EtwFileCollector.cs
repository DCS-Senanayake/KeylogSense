using System.ComponentModel;
using System.Security.Principal;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

namespace KeyloggerDetection.Monitoring.FileBehaviour;

/// <summary>
/// User-mode telemetry provider using Event Tracing for Windows (ETW) 
/// to monitor actual file writes per-process.
/// Proposal linkage: Event Log / ETW consumers (P4).
/// </summary>
public sealed class EtwFileCollector : ICollector
{
    private readonly IAppLogger _logger;
    private readonly DetectionConfig _config;
    private readonly IClock _clock;
    private TraceEventSession? _session;

    public EtwFileCollector(IAppLogger logger, DetectionConfig config, IClock clock)
    {
        _logger = logger;
        _config = config;
        _clock = clock;
    }

    public Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        var isElevated = IsAdministrator();
        _logger.LogInfo($"ETW File Collector start requested. Elevated={isElevated}.");

        // ETW Kernel traces require Administrator privileges safely verified here.
        if (!isElevated)
        {
            _logger.LogWarning("CAPABILITY LIMITATION: ETW file tracing requires Administrator privileges.");
            _logger.LogWarning("Real file behaviour monitoring is currently offline. The application will continue running with reduced telemetry coverage.");
            _logger.LogWarning("Launch the tray application elevated to restore ETW-backed file telemetry.");
            return Task.CompletedTask;
        }

        return Task.Run(() => RunEtwSession(pipeline, cancellationToken), cancellationToken);
    }

    private void RunEtwSession(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        var sessionCreated = false;
        var providerEnabled = false;
        _logger.LogInfo("ETW File Collector creating kernel session...");

        try
        {
            // Name must be unique for generic sessions, or "NT Kernel Logger" for the kernel session.
            _session = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
            sessionCreated = true;
            _logger.LogInfo($"ETW kernel session created successfully: {KernelTraceEventParser.KernelSessionName}.");

            // Enable file IO tracing
            _session.EnableKernelProvider(KernelTraceEventParser.Keywords.FileIOInit);
            providerEnabled = true;
            _logger.LogInfo("ETW FileIOInit provider enabled. File telemetry session is active.");

            _session.Source.Kernel.FileIOWrite += data =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _session.Stop();
                    return;
                }

                // ETW drops the file name occasionally based on system load. We parse what is feasible.
                var fileName = data.FileName;
                if (string.IsNullOrWhiteSpace(fileName)) return;

                // Create the telemetry event and push to pipeline
                var telemetryEvent = new FileWriteEvent(
                    data.ProcessID,
                    _clock.UtcNow, // Use our clock for consistency with analyzers
                    fileName,
                    data.IoSize
                );

                pipeline.Publish(telemetryEvent);
            };

            // Hook cancellation to safely kill the blocking Process loop
            using var reg = cancellationToken.Register(() => _session.Stop());

            // This is a blocking call until stopped.
            _logger.LogInfo("ETW File Collector processing kernel events.");
            _session.Source.Process();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
        {
            _logger.LogError("Access denied creating ETW session despite Admin check. Has ETW been disabled by policy?", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("Fatal error in ETW file collector.", ex);
        }
        finally
        {
            _logger.LogInfo($"ETW File Collector stopped. SessionCreated={sessionCreated}; ProviderEnabled={providerEnabled}.");
            _session?.Dispose();
        }
    }

    /// <summary>
    /// Safely checks if the current process is elevated.
    /// </summary>
    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
