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
- safe for academic testing with simulator-first validation

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

Important current limitations:
- Full ETW-backed file telemetry requires the tray process to be elevated
- The built tray application embeds a `requireAdministrator` manifest
- The simulator's optional persistence flag writes an inert HKCU Run-key
  marker for `notepad.exe`; it is a safe indicator simulation, not a real
  persistence payload
- Final dissertation-grade measurements should still be performed in an
  isolated Windows VM, because host-machine runs are only development checks

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
- suspicious-score condition is strictly `score > threshold`
- user-facing tray alerts require both `score > threshold` and the alert
  guardrail to pass

That means:
- score `7` is not suspicious
- score `8` is suspicious
- score `8` only raises a tray alert when at least one stronger behavioural
  signal is present, such as file-write indicators, file/network correlation,
  or persistence

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
    KeyloggerDetection.Simulator/       Safe combined behaviour simulator
  tests/
    KeyloggerDetection.Tests/           Unit tests
  docs/
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
- the built app embeds a `requireAdministrator` manifest
- full ETW-backed file telemetry is only available when the tray process is elevated
- on a system with UAC enabled, launching the built `.exe` from a non-elevated context should trigger the normal elevation prompt
- `dotnet run` for this project uses the generated apphost `.exe`, so it does not intentionally bypass the manifest
- if no prompt appears, check whether the shell is already elevated or whether UAC policy is disabled on that machine

Run the safe simulator:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator
dotnet run --project tools\KeyloggerDetection.Simulator -- --enable-persistence
dotnet run --project tools\KeyloggerDetection.Simulator -- --cleanup
```

## Documentation

Core design:
- [Technical Specification](docs/technical-spec.md)
- [System Architecture](docs/architecture.md)
- [Scoring Plan](docs/scoring-plan.md)
- [Tooling Decisions](docs/tooling-decisions.md)
- [Proposal Assumptions Register](docs/proposal-assumptions.md)

Running and safety:
- [Development Environment Setup](docs/setup-dev-environment.md)
- [Simulator Guide](docs/simulator.md)
- [Safe Testing Lab](docs/safe-testing-lab.md)
- [File Behaviour Monitoring](docs/file-behaviour-monitoring.md)
- [Non-Goals](docs/non-goals.md)

## Phase Status Snapshot

| Phase | Description | Status |
|---|---|---|
| P1 | Requirements, architecture, scoring plan, evaluation approach | Complete |
| P2 | Setup, tray app skeleton, logging infrastructure | Complete |
| P3 | Process context monitoring | Implemented |
| P4 | File behaviour monitoring | Implemented, ETW elevation-dependent |
| P5 | Network behaviour monitoring | Implemented |
| P6 | Persistence indicator, allowlist, configuration | Implemented |
| P7 | Risk scoring engine | Implemented |
| P8 | Alerts, tray notifications, log outputs | Implemented baseline |
| P9 | Testing, tuning, overhead measurement, results analysis | Manual validation remains |
| P10 | Final dissertation packaging and submission updates | In progress |

## Ethical Notice

This tool is for academic research and defensive evaluation only.

- Do not use it to capture keystroke content
- Do not distribute malware
- Do not overclaim effectiveness beyond measured results
- Use isolated Windows VMs for suspicious or approved-sample testing
