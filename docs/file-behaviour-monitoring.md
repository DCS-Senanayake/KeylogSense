# File Behaviour Monitoring (Phase P4)

## Architecture Overview

The file logging behaviour module tracks two primary signals defined in the KeylogSense proposal (Table 1):
1. **Frequent small file writes** (+2 points)
2. **Repeated writes to the same file within a short time window** (+2 points)

To accomplish this without introducing vulnerable custom drivers, the architecture splits the problem into two parts:
- **Event Abstraction (`FileWriteAnalyzer`)**: Statefully processes discrete `FileWriteEvent` streams per-PID over a sliding time window (configurable via `RepeatedWriteWindowSeconds`).
- **Telemetry Provider (`EtwFileCollector`)**: Listens to the OS and maps raw events to `FileWriteEvent`.

## Telemetry Source: ETW (Event Tracing for Windows)

We selected **ETW** via the `Microsoft.Diagnostics.Tracing.TraceEvent` library for the telemetry source.

### Why ETW?
- It is a **native, user-mode accessible** Windows capability.
- It supplies the critical missing link in generic file APIs (`FileSystemWatcher`): the **Process Identifier (PID)** that performed the write. Without the PID, user-mode correlation to process signals would be impossible.
- It requires no kernel-mode development or filter drivers, honouring the proposal's scope constraints completely.

## Limitations & Feasibility

While ETW is powerful, it carries specific feasibility constraints within a desktop environment:

1. **Elevation Required**: ETW Kernel trace providers (like `FileIOInit`) fundamentally require **Administrator privileges**. The proposal states KeylogSense is a user-mode application; this is true, but it must be run *Elevated* to hook these specific OS traces.
2. **Path Obfuscation**: The OS may occasionally drop the string representations of file paths during high I/O load to preserve trace performance, yielding empty names.
3. **Session Conflicts**: Only one agent can conventionally control the "NT Kernel Logger" ETW session. If another security tool or performance profiler is running, the collector may fail to bind.

## Startup Behaviour

KeylogSense now ships with an application manifest that sets:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

This means:
- Windows shows a **UAC prompt every time the tray app starts**
- if the user approves, the app runs elevated and ETW file telemetry can start
- if the user declines, Windows does not start the app

This does **not** make the project a kernel-mode tool. It remains a
user-mode application that requests elevation because the chosen ETW kernel
trace provider requires it.

## Graceful Fallback Strategy

KeylogSense actively evaluates these capabilities. If the agent notices that Administrator rights are missing, it:
1. Does **not** fake a working state or fail violently.
2. Logs a clear capability limitation warning into the internal application log.
3. Keeps the background application running (so process scanning and other non-elevated sensors stay online).
4. Relies fully on the pipeline architecture allowing engineers to construct and hook up the `MockFileTelemetryProvider` during safety evaluations without needing real malware.

See `docs/safe-testing-lab.md` for information on testing file heuristics securely with the mock provider.
