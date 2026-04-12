using System.Collections.Concurrent;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.FileBehaviour;

/// <summary>
/// Stateful analyzer that processes raw FileWriteEvents to evaluate
/// log-like behaviour signals (frequent small writes, repeated same-file writes).
/// Proposal linkage: Component of P4 file logging behaviour monitoring.
/// </summary>
public sealed class FileWriteAnalyzer
{
    private readonly DetectionConfig _config;
    private readonly IClock _clock;
    
    // Tracks individual writes per PID to calculate windowed correlations.
    // PID -> List of writes
    private readonly ConcurrentDictionary<int, List<FileWriteRecord>> _history = new();

    private sealed record FileWriteRecord(string FilePath, long Size, DateTime Timestamp);

    public FileWriteAnalyzer(DetectionConfig config, IClock clock)
    {
        _config = config;
        _clock = clock;
    }

    /// <summary>
    /// Processes a new file write event and updates the features for the related process.
    /// Returns the updated FileActivityInfo summary.
    /// </summary>
    public FileActivityInfo ProcessEvent(FileWriteEvent writeEvent)
    {
        if (ShouldIgnorePath(writeEvent.FilePath))
        {
            return GetCurrentActivityOrEmpty(writeEvent.Pid);
        }

        var record = new FileWriteRecord(
            FilePath: writeEvent.FilePath,
            Size: writeEvent.WriteSizeBytes,
            Timestamp: writeEvent.Timestamp
        );

        var processHistory = _history.AddOrUpdate(
            writeEvent.Pid,
            // add new
            _ => [record],
            // update existing
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(record);
                    return existing;
                }
            });

        return EvaluatePid(writeEvent.Pid, processHistory);
    }

    private FileActivityInfo EvaluatePid(int pid, List<FileWriteRecord> writes)
    {
        var now = _clock.UtcNow;
        var windowStart = now.AddSeconds(-_config.RepeatedWriteWindowSeconds);

        var smallWrites = 0;
        var sameFileCountMetrics = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        DateTime? lastWrite = null;

        // Take a snapshot and prune old records to prevent unbounded memory growth
        List<FileWriteRecord> activeWrites;
        lock (writes)
        {
            writes.RemoveAll(x => x.Timestamp < windowStart);
            activeWrites = writes.ToList();
        }

        foreach (var w in activeWrites)
        {
            if (lastWrite == null || w.Timestamp > lastWrite)
            {
                lastWrite = w.Timestamp;
            }

            if (w.Size <= _config.SmallWriteMaxBytes)
            {
                smallWrites++;
            }

            if (!sameFileCountMetrics.TryAdd(w.FilePath, 1))
            {
                sameFileCountMetrics[w.FilePath]++;
            }
        }

        // Find the maximum repetition hitting the same file in this time window
        var maxRepeats = sameFileCountMetrics.Values.Count > 0 ? sameFileCountMetrics.Values.Max() : 0;

        return new FileActivityInfo
        {
            Pid = pid,
            SmallWriteCount = smallWrites,
            RepeatedSameFileWriteCount = maxRepeats,
            LastActivityTime = lastWrite ?? now
        };
    }

    private bool ShouldIgnorePath(string filePath)
    {
        if (_config.BenignFilePathExclusions.Any(p => filePath.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (_config.MonitoredFileRoots.Length == 0) return false;

        foreach (var root in _config.MonitoredFileRoots)
        {
            // If path starts with a monitored root, we DON'T ignore it (return false)
            if (filePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        
        // If roots are configured but this doesn't match any, ignore it
        return true;
    }

    private FileActivityInfo GetCurrentActivityOrEmpty(int pid)
    {
        if (_history.TryGetValue(pid, out var existing))
        {
            return EvaluatePid(pid, existing);
        }

        return new FileActivityInfo { Pid = pid };
    }
}
