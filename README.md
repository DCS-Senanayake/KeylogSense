# KeylogSense

**Design and Implementation of a User-Mode, Real-Time Keylogger Detection System Based on Process Behaviour Analysis**

## Project Overview

KeylogSense is a Windows 11, user-mode, real-time keylogger detection project
implemented in C# on .NET 10. It runs as a WinForms tray application and
detects keylogger-like behaviour by correlating explainable process, file,
network, and persistence signals.

The project is intentionally:
- rule-based rather than machine-learning-based
- user-mode rather than kernel-mode
- detection-focused rather than remediation-focused
- safe for academic evaluation with simulator-first testing

## Current Status

The repository is functional and no longer a skeleton.

Implemented:
- WinForms tray application with Start Monitoring, Stop Monitoring, Open Logs,
  and Exit
- Process context monitoring
- ETW-based file behaviour collection and file-write analysis
- Network behaviour monitoring using TCP table snapshots
- Persistence polling for Run, RunOnce, and Startup-folder changes
- Rule-based scoring with explainable triggered reasons
- Allowlist support
- CSV and text logging
- Safe simulator tool
- Phase P9 evaluation runner with baseline outputs

Important current limitations:
- Live ETW file telemetry requires Administrator privileges
- The tray app requests elevation on every start through an application
  manifest
- The persistence-only simulator is intentionally safety-limited and does not
  yet provide a full end-to-end PID-attributed positive control
- P9 baseline evaluation exists, but final dissertation-grade measurements
  should still be repeated in an isolated Windows VM

## Detection Model

The proposal's additive scoring model is implemented with these default
weights:

| Rule | Score |
|---|---:|
| Suspicious location | +4 |
| Untrusted publisher | +3 |
| Frequent small file writes | +2 |
| Repeated writes to same file | +2 |
| Outbound network activity | +2 |
| File and network correlation | +2 |
| Persistence detected | +5 |

Default threshold:
- `AlertThreshold = 7`
- alert condition is strictly `score > threshold`

That means:
- score `7` is not an alert
- score `8` is an alert

## Solution Structure

```text
KeylogSense/
  src/
    KeyloggerDetection.App/             WinForms tray application
    KeyloggerDetection.Core/            Shared models, interfaces, config
    KeyloggerDetection.Infrastructure/  Logging and telemetry pipeline
    KeyloggerDetection.Monitoring/      Process, file, network, persistence collectors
    KeyloggerDetection.Scoring/         Scoring and allowlist logic
  tools/
    KeyloggerDetection.Simulator/       Safe simulator scenarios
    KeyloggerDetection.Evaluation/      Phase P9 evaluation runner
  tests/
    KeyloggerDetection.Tests/           Unit tests
  docs/
  evaluation/
  KeyloggerDetection.slnx
  Project proposal.pdf
```

## Build And Run

Open PowerShell in the repository root:

```powershell
dotnet build KeyloggerDetection.slnx
dotnet test KeyloggerDetection.slnx
```

Run the tray app:

```powershell
dotnet run --project src\KeyloggerDetection.App
```

Important:
- the app manifest uses `requireAdministrator`
- Windows will request elevation every time the tray app starts
- if you launch from a non-elevated terminal, `dotnet run` may fail with
  "The requested operation requires elevation"
- in that case, open PowerShell as Administrator or run the built `.exe`
  manually and accept the UAC prompt

Run the safe simulator:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator -- network-only
dotnet run --project tools\KeyloggerDetection.Simulator -- combined
dotnet run --project tools\KeyloggerDetection.Simulator -- cleanup
```

Run the evaluation workflow:

```powershell
dotnet run --project tools\KeyloggerDetection.Evaluation
```

Outputs:
- `evaluation/results.csv`
- `evaluation/summary.md`
- `evaluation/artifacts/<run-id>/...`

## Evaluation Snapshot

The repository now includes a repeatable P9 workflow covering:
- safe simulator scenarios
- benign application scenarios
- optional approved non-destructive sample scenarios through a manifest
- detection capability
- false positives
- detection latency
- CPU and RAM overhead

The current baseline artifacts are already checked into `evaluation/`.
Interpret them as measured observations for this environment, not universal
effectiveness claims.

## Documentation

Core design:
- [Technical Specification](docs/technical-spec.md)
- [System Architecture](docs/architecture.md)
- [Scoring Plan](docs/scoring-plan.md)
- [Tooling Decisions](docs/tooling-decisions.md)
- [Proposal Assumptions Register](docs/proposal-assumptions.md)

Running and safety:
- [Development Environment Setup](docs/setup-dev-environment.md)
- [Safe Testing Lab](docs/safe-testing-lab.md)
- [File Behaviour Monitoring](docs/file-behaviour-monitoring.md)
- [Non-Goals](docs/non-goals.md)

Testing and evaluation:
- [Evaluation Plan](docs/evaluation-plan.md)
- [Evaluation Workflow](docs/evaluation-workflow.md)
- [Test Scenarios](docs/test-scenarios.md)

## Phase Status Snapshot

| Phase | Description | Status |
|---|---|---|
| P1 | Requirements, architecture, scoring plan, evaluation plan | Complete |
| P2 | Setup, tray app skeleton, logging infrastructure | Complete |
| P3 | Process context monitoring | Implemented |
| P4 | File behaviour monitoring | Implemented, ETW elevation-dependent |
| P5 | Network behaviour monitoring | Implemented |
| P6 | Persistence indicator, allowlist, configuration | Implemented |
| P7 | Risk scoring engine | Implemented |
| P8 | Alerts, tray notifications, log outputs | Implemented baseline |
| P9 | Testing, tuning, overhead measurement, results analysis | Implemented baseline workflow and outputs |
| P10 | Final dissertation packaging and submission updates | In progress |

## Ethical Notice

This tool is for academic research and defensive evaluation only.

- Do not use it to capture keystroke content
- Do not distribute malware
- Do not overclaim effectiveness beyond measured results
- Use isolated Windows VMs for suspicious or approved-sample testing
