# System Architecture

**Project:** Design and Implementation of a User-Mode, Real-Time Keylogger
Detection System Based on Process Behaviour Analysis

## 1. Runtime Summary

The current implementation is a pipeline-based Windows tray application:

1. telemetry collectors gather process, file, network, and persistence data
2. telemetry events are published into an in-memory pipeline
3. the feature aggregator updates per-process state
4. the risk scoring engine evaluates the feature vector
5. suspicious detections are logged and surfaced as tray notifications

The system remains fully user-mode. It does not use kernel drivers or
hypervisor components. The tray process requests Administrator privileges so
it can start the ETW kernel file trace session needed for file telemetry.

## 2. Main Components

### 2.1 Tray Application

Location: `src/KeyloggerDetection.App/`

Key classes:
- `Program.cs`
- `TrayApplicationContext.cs`
- `MonitoringCoordinator.cs`
- `FeatureAggregator.cs`

Responsibilities:
- bootstraps configuration and logging
- manually composes the runtime services
- starts and stops monitoring from the tray menu
- shows balloon notifications for suspicious detections

### 2.2 Telemetry Collectors

Location: `src/KeyloggerDetection.Monitoring/`

Implemented collectors:
- `ProcessContext/ProcessCollector.cs`
- `FileBehaviour/EtwFileCollector.cs`
- `NetworkBehaviour/NetworkCollector.cs`
- `Persistence/PersistenceCollector.cs`

Important support classes:
- `ProcessContext/LocationClassifier.cs`
- `ProcessContext/SignatureVerifier.cs`
- `FileBehaviour/FileWriteAnalyzer.cs`
- `NetworkBehaviour/Win32TcpTable.cs`

### 2.3 Telemetry Pipeline

Location: `src/KeyloggerDetection.Infrastructure/Pipeline/TelemetryPipeline.cs`

The pipeline is an in-memory bounded channel used to decouple collectors from
the feature aggregation and scoring stages.

### 2.4 Feature Aggregation

Location: `src/KeyloggerDetection.App/FeatureAggregator.cs`

The feature aggregator keeps state per PID and updates:
- process name and executable path
- suspicious location flag
- trust and publisher metadata
- small-write and repeated-write counters
- outbound network activity state
- persistence state
- last file and network activity timestamps for correlation

### 2.5 Scoring

Location: `src/KeyloggerDetection.Scoring/`

Key classes:
- `RiskScoringEngine.cs`
- `AllowlistManager.cs`

Key behaviour:
- applies the proposal's additive scoring model
- checks the allowlist before final scoring
- treats a process as suspicious only when `score > threshold`
- returns triggered rules and short human-readable reasons

### 2.6 Logging

Locations:
- `src/KeyloggerDetection.Infrastructure/Logging/TextAppLogger.cs`
- `src/KeyloggerDetection.Infrastructure/Logging/DetectionLogFileService.cs`

Current behaviour:
- application events are written to text logs
- suspicious detections are written to structured output files
- the detector's structured log contains alerts, not every telemetry event

### 2.7 Evaluation Tooling

Location: `tools/KeyloggerDetection.Evaluation/`

The evaluation runner starts the detection stack headlessly and executes:
- safe simulator scenarios
- benign control scenarios
- optional approved-sample scenarios from a manifest

It writes:
- `evaluation/results.csv`
- `evaluation/summary.md`
- `evaluation/artifacts/<run-id>/...`

## 3. Data Flow

```text
Tray App
  -> MonitoringCoordinator
  -> Collectors
     - ProcessCollector
     - EtwFileCollector
     - NetworkCollector
     - PersistenceCollector
  -> TelemetryPipeline
  -> FeatureAggregator
  -> RiskScoringEngine + AllowlistManager
  -> DetectionLogFileService
  -> Tray balloon notification
```

## 4. Collector Notes

### 4.1 Process Context

`ProcessCollector` periodically enumerates processes and records:
- PID
- process name
- executable path when accessible
- start time when accessible
- suspicious location classification
- digital signature trust state

### 4.2 File Behaviour

`EtwFileCollector` uses ETW kernel file events and maps them into
`FileWriteEvent` records. `FileWriteAnalyzer` then evaluates these events for:
- frequent small file writes
- repeated writes to the same file

Important limitation:
- ETW kernel file tracing requires Administrator privileges
- when ETW cannot start, the project logs the limitation honestly instead of
  pretending file telemetry is available

### 4.3 Network Behaviour

`NetworkCollector` uses TCP table snapshots to observe newly seen outbound
connections by PID.

### 4.4 Persistence

`PersistenceCollector` polls:
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
- `HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce`
- user and common Startup folders

It uses snapshot differencing to detect new or changed entries.

## 5. Elevation Model

The tray application includes an application manifest with:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

Operational effect:
- Windows shows a UAC prompt each time the tray app starts
- approving the prompt launches the app elevated
- declining the prompt prevents startup

This remains a user-mode application. Elevation is required for telemetry
coverage, not because the project includes a kernel-mode component.

## 6. Current Implementation Status

Implemented:
- end-to-end telemetry pipeline
- rule-based scoring with explainable reasons
- tray notifications
- structured detection logging
- safe simulator
- repeatable P9 evaluation workflow

Known transitional areas:
- `PlaceholderAlertService.cs` still exists as legacy infrastructure, while the
  active user-facing alerts are delivered through the tray context
- file telemetry coverage depends on ETW availability and elevation
- the simulator's optional persistence flag is intentionally safety-limited to
  an inert Run-key marker for `notepad.exe`
- final dissertation packaging is still pending

## 7. Proposal Alignment

| Proposal Requirement | Current Implementation |
|---|---|
| Process context signals | `ProcessCollector`, `LocationClassifier`, `SignatureVerifier` |
| File logging behaviour | `EtwFileCollector`, `FileWriteAnalyzer` |
| Network behaviour | `NetworkCollector`, `Win32TcpTable` |
| Persistence indicator | `PersistenceCollector` |
| Explainable scoring | `RiskScoringEngine`, triggered rule output |
| Strict `score > threshold` logic | `RiskScoringEngine` and `DetectionConfig` |
| Tray UI | `TrayApplicationContext` |
| Alert notification | tray balloon notifications |
| Logging and reporting | `DetectionLogFileService`, `TextAppLogger` |
| Evaluation metrics | evaluation runner plus generated artifacts |
