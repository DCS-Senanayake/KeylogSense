namespace KeyloggerDetection.Core.Models;

/// <summary>
/// Base type for all internal telemetry events flowing through the monitoring pipeline.
/// </summary>
public abstract record TelemetryEvent(int Pid, DateTime Timestamp);

/// <summary>
/// Emitted when new process context is discovered or updated.
/// Proposal: Process context (signal group 1)
/// </summary>
public sealed record ProcessContextEvent(
    int Pid,
    DateTime Timestamp,
    string ProcessName,
    string? ExecutablePath,
    DateTime? StartTime,
    SuspiciousLocationClassification LocationClassification,
    TrustState Trust,
    string? PublisherName) : TelemetryEvent(Pid, Timestamp);

/// <summary>
/// Emitted when a process writes to a file.
/// Proposal: File logging behaviour (signal group 2)
/// </summary>
public sealed record FileWriteEvent(
    int Pid,
    DateTime Timestamp,
    string FilePath,
    long WriteSizeBytes) : TelemetryEvent(Pid, Timestamp);

/// <summary>
/// Emitted when a process creates an outbound network connection.
/// Proposal: Network behaviour (signal group 3)
/// </summary>
public sealed record NetworkConnectionEvent(
    int Pid,
    DateTime Timestamp,
    string LocalEndpoint,
    string RemoteEndpoint,
    string Protocol) : TelemetryEvent(Pid, Timestamp);

/// <summary>
/// Emitted when a persistence mechanism (e.g. Run key) is detected for a process.
/// Proposal: Persistence indicator (signal group 4)
/// </summary>
public sealed record PersistenceEvent(
    int Pid,
    DateTime Timestamp,
    string RegistryOrFilePath,
    string PersistenceType) : TelemetryEvent(Pid, Timestamp);
