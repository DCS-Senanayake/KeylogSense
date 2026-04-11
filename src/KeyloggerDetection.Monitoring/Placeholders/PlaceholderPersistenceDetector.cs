using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.Placeholders;

/// <summary>
/// Placeholder implementation of <see cref="IPersistenceDetector"/>.
/// Always returns false. Will be replaced with real
/// implementation in Phase P6.
/// </summary>
public sealed class PlaceholderPersistenceDetector : IPersistenceDetector
{
    public bool HasPersistenceEntry(ProcessInfo process) => false;
}
