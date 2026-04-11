using System.Diagnostics;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.ProcessContext;

/// <summary>
/// Periodically enumerates running processes and emits ProcessContextEvents.
/// Inherits ICollector and participates in the telemetry pipeline.
/// </summary>
public sealed class ProcessCollector : ICollector
{
    private readonly IAppLogger _logger;
    private readonly DetectionConfig _config;
    private readonly LocationClassifier _locationClassifier;
    private readonly SignatureVerifier _signatureVerifier;
    private readonly IClock _clock;
    
    // Track known PIDs to reduce redundant logging and signature checks
    private readonly HashSet<int> _knownPids = new();

    public ProcessCollector(IAppLogger logger, DetectionConfig config, IClock clock)
    {
        _logger = logger;
        _config = config;
        _clock = clock;
        _locationClassifier = new LocationClassifier();
        _signatureVerifier = new SignatureVerifier();
    }

    public async Task StartAsync(ITelemetryPipeline pipeline, CancellationToken cancellationToken)
    {
        _logger.LogInfo("ProcessCollector started.");

        try
        {
            // Loop until cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                EnumerateProcesses(pipeline);

                // Delay according to config (e.g. 5000ms)
                await Task.Delay(_config.MonitoringIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo("ProcessCollector shutdown requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError("ProcessCollector loop encountered a fatal error.", ex);
        }
    }

    private void EnumerateProcesses(ITelemetryPipeline pipeline)
    {
        var currentPids = new HashSet<int>();
        var processes = Process.GetProcesses();

        foreach (var proc in processes)
        {
            try
            {
                currentPids.Add(proc.Id);

                // Check if we've already processed this exact PID
                // (In a real system, PID reuse could happen, but for simplicity we rely on PID presence)
                if (_knownPids.Contains(proc.Id))
                {
                    continue;
                }

                // New process discovered
                ProcessNewProcess(proc, pipeline);
            }
            // Catch AccessDenied (Win32Exception) safely without crashing the loop
            catch (System.ComponentModel.Win32Exception)
            {
                // We don't have rights to query this process (likely System/Elevated).
                // Do not automatically flag as malicious.
            }
            catch (InvalidOperationException)
            {
                // Process terminated before we could inspect it
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Unexpected error inspecting PID {proc.Id}: {ex.Message}");
            }
            finally
            {
                proc.Dispose();
            }
        }

        // Clean up known PIDs that are no longer running
        _knownPids.IntersectWith(currentPids);
    }

    private void ProcessNewProcess(Process proc, ITelemetryPipeline pipeline)
    {
        int pid = proc.Id;
        string processName = proc.ProcessName;
        
        // Safely extract path and start time
        string? executablePath = null;
        DateTime? startTime = null;
        try
        {
            // MainModule can throw AccessDenied
            executablePath = proc.MainModule?.FileName;
            startTime = proc.StartTime;
        }
        catch
        {
            // If AccessDenied or exited, leave as null. 
            // "Unknown or inaccessible data must not be treated as automatically malicious."
        }

        var locationClass = _locationClassifier.Classify(executablePath);
        var trustState = _signatureVerifier.CheckTrust(executablePath);

        var telemetryEvent = new ProcessContextEvent(
            pid,
            _clock.UtcNow,
            processName,
            executablePath,
            startTime,
            locationClass,
            trustState,
            null // Publisher extraction optionally added if Authenticode API used
        );

        pipeline.Publish(telemetryEvent);
        _knownPids.Add(pid);
    }

    public void Dispose()
    {
        // No unmanaged resources
    }
}
