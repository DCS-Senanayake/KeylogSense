# Test Scenarios

This document describes the **current** scenario set used during Phase P9.
It replaces earlier informal expectations that assumed all simulator modes
would alert automatically.

## 1  Important Scoring Reminder

The project proposal requires:

```text
Alert only when score > threshold
```

With the current default threshold of `7`:
- score `7` is **not** an alert
- score `9` is an alert

This matters when interpreting simulator outcomes.

## 2  Automated Scenario Set

The repeatable runner is:

```powershell
dotnet run --project tools/KeyloggerDetection.Evaluation
```

The runner currently uses the following scenarios.

### 2.1  Positive Controls

| Scenario | Why It Exists | Expected Outcome |
|---|---|---|
| `simulator-temp` | Stages the safe simulator under `%TEMP%` so suspicious location can combine with repeated file writes and outbound network activity from one realistic flow | Alert expected when full telemetry is available |
| `simulator-temp-persistence` | Uses the same staged simulator flow with explicitly enabled inert persistence simulation | Alert expected |

### 2.2  Benign Controls

| Scenario | Why It Exists | Expected Outcome |
|---|---|---|
| `notepad` | Signed system text editor | No alert expected |
| `calc` | Signed system UI process | No alert expected |
| `cmd-timeout` | Short-lived system console process | No alert expected |
| `powershell-sleep` | Signed PowerShell host kept idle briefly | No alert expected |

## 3  Why `%TEMP%` Staging Is Used

The safe simulator normally runs from the repository build output, which is
not one of the proposal's suspicious locations. For P9 we need at least some
safe positive controls that can cross the proposal's threshold honestly.

Staging a copy of the simulator under `%TEMP%` gives:
- suspicious location `+4`
- unsigned / untrusted publisher `+3`
- plus any additional observed behaviour

This allows the system to demonstrate alerting without introducing real
malware behaviour.

## 4  Known Current Limitations

### 4.1  File Telemetry

The current live file-behaviour collector uses ETW kernel file events and
therefore needs administrator privileges. When the app is not elevated:

- file behaviour monitoring is offline
- `simulator-temp` cannot fully exercise file-write rules end-to-end
- the runner must report this honestly instead of counting it as a failure

### 4.2  Persistence Safety Model

When persistence is enabled, the simulator writes an **inert** Run-key value
pointing to `notepad.exe` for safety. This avoids creating a self-launch loop
at logon while still exercising the persistence collector defensively.

## 5  Output Interpretation

Use these files together:
- `evaluation/results.csv`
- `evaluation/summary.md`
- `evaluation/artifacts/<run-id>/evaluation-app.log`

Interpret them as follows:
- `results.csv` gives the per-scenario facts
- `summary.md` gives the aggregated metrics and written limitations
- `evaluation-app.log` shows capability warnings such as file telemetry being offline

## 6  Final Report Guidance

For the dissertation:
- treat the automated runner as the repeatable baseline
- use the isolated VM run as the authoritative measurement pass
- do not describe capability-limited scenarios as true failures
- do not claim persistence end-to-end coverage until attribution is improved
