using System.Text;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Infrastructure.Logging;

/// <summary>
/// Writes detection events to a CSV file as mandated by Phase P8.
/// </summary>
public sealed class DetectionLogFileService : IDetectionLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new object();

    public string LogDirectory { get; }

    public DetectionLogFileService(DetectionConfig config)
    {
        LogDirectory = string.IsNullOrWhiteSpace(config.LogDirectory) 
            ? Path.Combine(AppContext.BaseDirectory, "Logs") 
            : config.LogDirectory;

        if (!Directory.Exists(LogDirectory))
        {
            Directory.CreateDirectory(LogDirectory);
        }

        _logFilePath = Path.Combine(LogDirectory, $"detections_{DateTime.UtcNow:yyyyMMdd}.csv");
        EnsureHeader();
    }

    private void EnsureHeader()
    {
        lock (_lock)
        {
            if (!File.Exists(_logFilePath))
            {
                var header = "Timestamp,PID,ProcessName,ExecutablePath,RiskScore,SuspiciousLocation,UntrustedPublisher,FrequentSmallWrites,RepeatedSameFileWrites,OutboundNetwork,FileNetworkCorrelation,PersistenceDetected,TriggeredRules" + Environment.NewLine;
                File.WriteAllText(_logFilePath, header, Encoding.UTF8);
            }
        }
    }

    public void LogDetection(DetectionEvent detectionEvent)
    {
        lock (_lock)
        {
            var line = string.Join(",", 
                detectionEvent.Timestamp.ToString("O"),
                detectionEvent.Pid,
                EscapeCsv(detectionEvent.ProcessName),
                EscapeCsv(detectionEvent.ExecutablePath),
                detectionEvent.RiskScore,
                detectionEvent.SuspiciousLocation,
                detectionEvent.UntrustedPublisher,
                detectionEvent.FrequentSmallWrites,
                detectionEvent.RepeatedSameFileWrites,
                detectionEvent.OutboundNetwork,
                detectionEvent.FileNetworkCorrelation,
                detectionEvent.PersistenceDetected,
                EscapeCsv(detectionEvent.TriggeredRules)
            );

            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static string EscapeCsv(string? field)
    {
        if (field == null) return string.Empty;
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public void Dispose()
    {
    }
}
