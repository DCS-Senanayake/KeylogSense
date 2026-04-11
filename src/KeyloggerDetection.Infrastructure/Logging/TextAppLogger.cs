using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Infrastructure.Logging;

/// <summary>
/// Text-based application logger.
/// Writes general application events to an app.log file in the configured log directory.
/// </summary>
public sealed class TextAppLogger : IAppLogger
{
    private readonly string _logDirectory;
    private readonly LogLevel _minimumLevel;
    private readonly object _writeLock = new();

    public TextAppLogger(string logDirectory, LogLevel minimumLevel)
    {
        _logDirectory = logDirectory;
        _minimumLevel = minimumLevel;

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null)
    {
        if (level < _minimumLevel) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}] [{level}] {message}";
        if (exception != null)
        {
            line += Environment.NewLine + exception.ToString();
        }

        var filePath = Path.Combine(_logDirectory, "app.log");

        lock (_writeLock)
        {
            try
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
            catch
            {
                // In a robust system we might fall back to EventLog, but for the minimal proposal,
                // failing to write a log shouldn't crash the detector.
            }
        }
    }

    public void Dispose()
    {
        // Nothing to dispose.
    }
}
