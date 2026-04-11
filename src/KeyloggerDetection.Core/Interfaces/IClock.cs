namespace KeyloggerDetection.Core.Interfaces;

/// <summary>
/// Simple clock interface to improve testability of time-dependent logic
/// (e.g., calculating correlation windows and logging intervals).
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}

/// <summary>
/// A default implementation of IClock that uses system time.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
