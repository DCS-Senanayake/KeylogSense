# Technical Specification

This document translates the proposal into the current implementation-facing
specification for KeylogSense.

## 1. System Definition

KeylogSense is a Windows 11, user-mode, real-time keylogger detection tool
implemented in C#. It runs as a tray application and correlates process
context, file behaviour, network behaviour, and persistence indicators using
an explainable rule-based scoring model.

## 2. Functional Requirements

### FR-1. Process Context Monitoring

The system must collect, where accessible:
- process name
- PID
- executable path
- start time
- suspicious-location classification
- trust or publisher metadata

Implementation status:
- implemented

### FR-2. File Behaviour Monitoring

The system must detect:
- repeated small file writes
- repeated writes to the same file within a short time window
- file activity that can later be correlated with network behaviour

Implementation status:
- implemented through ETW plus `FileWriteAnalyzer`

Important limitation:
- live ETW file telemetry requires Administrator privileges

### FR-3. Network Behaviour Monitoring

The system must detect:
- outbound connections initiated by the process
- network activity close in time to suspicious file behaviour

Implementation status:
- implemented

### FR-4. Persistence Indicator

The system must detect simple persistence attempts such as:
- Run-key entries
- RunOnce entries
- Startup-folder additions

Implementation status:
- implemented through polling and snapshot differencing

### FR-5. Risk Scoring Engine

The system must:
- use an additive rule-based score
- implement the proposal's score weights as defaults
- flag only when `score > threshold`
- keep the threshold configurable
- return triggered rules and reasons

Implementation status:
- implemented

### FR-6. Tray User Interface

The system must provide:
- tray icon presence
- Start Monitoring
- Stop Monitoring
- Open Logs
- Exit

Implementation status:
- implemented

### FR-7. Alerts

When a process is flagged, the system must provide:
- process name
- PID
- score
- short reasons

Implementation status:
- implemented through tray balloon notifications and structured logs

### FR-8. Logging And Reporting

The system must produce:
- application logs
- suspicious detection logs

Implementation status:
- implemented

## 3. Non-Functional Requirements

| Requirement | Status |
|---|---|
| User-mode only | Met |
| Lightweight, monitor-oriented design | Met in architecture; formal measurement remains pending |
| Windows 11 target | Met |
| C# and modern .NET | Met |
| Privacy-preserving, no keystroke capture | Met |
| Safe and ethical evaluation | Met in docs and workflow |

## 4. Current Deliverables

The repository currently contains:

1. A tray-based Windows detection application
2. A scoring engine with explainable rules
3. Structured logging for alerts and runtime activity
4. A safe simulator tool
5. Documentation for setup, scoring, safety, and architecture

## 5. Constraints

The project does not:
- capture or reconstruct keystroke content
- use kernel drivers
- act as a network-wide IDS
- automatically kill or quarantine processes
- fake telemetry or fabricate evaluation results
- provide offensive or stealth keylogging behaviour

## 6. Phase Status

| Phase | Status |
|---|---|
| P1 | Complete |
| P2 | Complete |
| P3 | Implemented |
| P4 | Implemented with ETW elevation dependency |
| P5 | Implemented |
| P6 | Implemented |
| P7 | Implemented |
| P8 | Implemented baseline |
| P9 | Validation and measurement still to be finalized |
| P10 | In progress |

## 7. Remaining Work

The main remaining work is not foundational architecture. It is project
completion work:
- perform focused P9 measurements in an isolated VM
- tune thresholds only if the evidence justifies it
- refine persistence attribution if needed
- finish final submission packaging and reporting
