using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// General application logger for system events (startup, shutdown, monitoring status, errors).
/// This is distinct from IDetectionLogger which logs keylogger detection events to CSV.
/// </summary>
public interface IAppLogger : IDisposable
{
    void Log(LogLevel level, string message, Exception? exception = null);
    void LogInfo(string message) => Log(LogLevel.Info, message);
    void LogError(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    void LogWarning(string message) => Log(LogLevel.Warning, message);
}
