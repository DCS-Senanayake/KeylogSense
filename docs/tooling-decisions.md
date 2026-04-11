# Tooling and Architecture Decisions

This document records the main engineering decisions that shape the current
implementation.

## Decision 1. User-Mode Only

Decision:
- the system remains entirely user-mode
- no kernel drivers
- no hypervisor components
- no ring-0 code

Proposal linkage:
- this is a hard constraint from the project title and scope

Implications:
- telemetry must come from user-mode accessible Windows APIs
- some visibility limitations are expected and must be documented honestly

## Decision 2. C# On .NET 10

Decision:
- the repository targets C# on .NET 10

Proposal linkage:
- the proposal specifies C# and modern .NET, but not a fixed SDK version

Rationale:
- .NET 10 is the SDK currently used in this repository

## Decision 3. WinForms Tray Application

Decision:
- the system tray UI is implemented with WinForms

Proposal linkage:
- the proposal calls for a tray application with menu actions and alert
  notifications

Rationale:
- WinForms provides a direct and lightweight `NotifyIcon`-based implementation

## Decision 4. Telemetry Sources Are Now Committed

Decision:
- the project now has concrete telemetry implementations rather than abstract
  placeholders

Current telemetry stack:

| Signal | Current Source | Notes |
|---|---|---|
| Process context | `System.Diagnostics.Process` plus helper classes | PID, name, path, start time, location, trust |
| File behaviour | ETW kernel file events through `Microsoft.Diagnostics.Tracing.TraceEvent` | Requires Administrator privileges |
| Network behaviour | `GetExtendedTcpTable` snapshotting via `Win32TcpTable` | Tracks outbound TCP activity by PID |
| Persistence | Registry and Startup-folder polling | Covers Run, RunOnce, and Startup folders |
| Digital signature trust | `X509CertificateLoader`-based certificate loading | Best-effort user-mode trust check |

Implications:
- the project now reflects a specific implementation choice and should be
  documented as such
- telemetry gaps are reported as limitations instead of hidden behind vague
  abstractions

## Decision 5. Require Administrator For Full Telemetry Coverage

Decision:
- the tray app requests elevation through an application manifest

Manifest:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

Rationale:
- the ETW kernel file trace session used for file behaviour monitoring needs
  Administrator privileges

Operational effect:
- Windows shows a UAC prompt each time the tray app starts
- declining the prompt prevents startup

Important clarification:
- this does not make the project kernel-mode
- it remains a user-mode process that requests elevation

## Decision 6. Proposal Scoring Fidelity

Decision:
- the scoring engine follows the proposal's additive scoring table as the
  default model
- the alert rule is strictly `score > threshold`

Current default threshold:
- `AlertThreshold = 7`

Rationale:
- preserves proposal fidelity while remaining configurable for P9 tuning

## Decision 7. Version 1 Focuses On Four Core Signal Groups

Decision:
- version 1 focuses on:
  - process context
  - file behaviour
  - network behaviour
  - persistence

Rationale:
- these are the concrete proposal signal groups and the basis of the scoring
  model

Implication:
- keyboard hook scanning is not the foundation of the current implementation

## Decision 8. Detect And Alert, Do Not Remediate

Decision:
- the system detects and alerts only
- it does not kill processes, quarantine files, or perform automated cleanup

Proposal linkage:
- remediation is outside the proposal scope

## Decision 9. Local Host Scope Only

Decision:
- the system monitors the local machine only

Proposal linkage:
- the proposal describes a local system tray application, not a network-wide
  IDS

## Decision 10. Evaluation Must Stay Academically Defensible

Decision:
- the evaluation workflow is simulator-first
- optional approved-sample testing is VM-only and manifest-driven
- measured limitations are documented rather than suppressed

Implication:
- the project should report measured results conservatively and avoid blanket
  effectiveness claims
