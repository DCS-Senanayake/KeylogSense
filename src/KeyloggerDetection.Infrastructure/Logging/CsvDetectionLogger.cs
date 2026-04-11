using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Infrastructure.Logging;

/// <summary>
/// Placeholder implementation of <see cref="IDetectionLogger"/>.
/// Writes detection events to CSV files.
/// Proposal reference: § 2.1.6 — CSV/text logs with timestamp, process details,
/// feature values, risk score, and triggered rules.
///
/// This is a minimal placeholder. Full implementation will be completed
/// alongside the tray app in Phase P2 continuation.
/// </summary>
public sealed class CsvDetectionLogger : IDetectionLogger
{
    private readonly string _logDirectory;
    private readonly string _logFilePrefix;
    private readonly object _writeLock = new();

    public CsvDetectionLogger(string logDirectory, string logFilePrefix = "KeylogSense")
    {
        _logDirectory = logDirectory;
        _logFilePrefix = logFilePrefix;

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public string LogDirectory => _logDirectory;

    public void LogDetection(DetectionEvent detectionEvent)
    {
        var filePath = GetLogFilePath();

        lock (_writeLock)
        {
            var fileExists = File.Exists(filePath);

            using var writer = new StreamWriter(filePath, append: true);

            // Write CSV header if this is a new file
            if (!fileExists)
            {
                writer.WriteLine(string.Join(",",
                    "Timestamp",
                    "ProcessName",
                    "PID",
                    "ExecutablePath",
                    "SuspiciousLocation",
                    "UntrustedPublisher",
                    "FrequentSmallWrites",
                    "RepeatedSameFileWrites",
                    "OutboundNetwork",
                    "FileNetworkCorrelation",
                    "PersistenceDetected",
                    "RiskScore",
                    "TriggeredRules"));
            }

            writer.WriteLine(string.Join(",",
                EscapeCsv(detectionEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(detectionEvent.ProcessName),
                detectionEvent.Pid,
                EscapeCsv(detectionEvent.ExecutablePath ?? ""),
                detectionEvent.SuspiciousLocation,
                detectionEvent.UntrustedPublisher,
                detectionEvent.FrequentSmallWrites,
                detectionEvent.RepeatedSameFileWrites,
                detectionEvent.OutboundNetwork,
                detectionEvent.FileNetworkCorrelation,
                detectionEvent.PersistenceDetected,
                detectionEvent.RiskScore,
                EscapeCsv(detectionEvent.TriggeredRules)));
        }
    }

    public void Dispose()
    {
        // No long-lived resources to dispose — we open/close per write.
    }

    private string GetLogFilePath()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(_logDirectory, $"{_logFilePrefix}_{date}.csv");
    }

    /// <summary>
    /// Escapes a value for safe inclusion in a CSV field.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
