using System.Collections.ObjectModel;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Evaluation;

internal sealed class EvaluationAppLogger : IAppLogger
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly List<LogEntry> _entries = [];

    public EvaluationAppLogger(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public ReadOnlyCollection<LogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList().AsReadOnly();
            }
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        var timestamp = DateTime.UtcNow;
        var entry = new LogEntry(timestamp, level, message, exception?.ToString());
        var line = $"[{timestamp:O}] [{level}] {message}";

        lock (_lock)
        {
            _entries.Add(entry);
            File.AppendAllText(_filePath, line + Environment.NewLine);

            if (exception is not null)
            {
                File.AppendAllText(_filePath, exception + Environment.NewLine);
            }
        }

        Console.WriteLine(line);
    }

    public void LogInfo(string message) => Log(LogLevel.Info, message);
    public void LogWarning(string message) => Log(LogLevel.Warning, message);
    public void LogError(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);

    public void Dispose()
    {
    }
}

internal sealed record LogEntry(DateTime TimestampUtc, LogLevel Level, string Message, string? ExceptionText);
