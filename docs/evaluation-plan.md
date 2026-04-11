# Evaluation Plan

This document defines the current evaluation methodology for Phase P9 and
aligns it with the implemented evaluation runner.

## 1. Required Metrics

The proposal requires measuring:

| Metric | Meaning |
|---|---|
| Detection capability | Whether suspicious scenarios are detected |
| False positives | Whether benign applications are incorrectly flagged |
| Detection latency | How quickly an alert appears after suspicious behaviour starts |
| CPU and RAM overhead | Resource impact during monitoring |

## 2. Current Evaluation Workflow

The implemented runner is:

```powershell
dotnet run --project tools\KeyloggerDetection.Evaluation
```

It produces:
- `evaluation/results.csv`
- `evaluation/summary.md`
- `evaluation/artifacts/<run-id>/...`

This workflow is the current authoritative baseline for P9 inside the repo.

## 3. Test Categories

### 3.1 Safe Simulator Scenarios

Purpose:
- measure detection capability without using real malware

Current automated scenarios:

| Scenario | Role | Expected Outcome |
|---|---|---|
| `network-only-temp` | Positive control | Alert expected |
| `combined-temp` | Positive control | Alert expected |
| `file-only-temp` | Capability probe | Alert only if file telemetry is available |
| `persistence-only-temp` | Safety-limited probe | No PID-attributed alert currently expected |

### 3.2 Benign Application Scenarios

Purpose:
- estimate false positives conservatively using common signed Windows tools

Current automated scenarios:
- `notepad`
- `calc`
- `cmd-timeout`
- `powershell-sleep`

Suggested extended benign set for final reporting:
- Edge, Chrome, Firefox
- VS Code, Notepad++
- Teams, Slack, Discord
- Word, Excel, PowerPoint
- Visual Studio, Windows Terminal, Git Bash
- updater and security tools present in the VM

### 3.3 Optional Approved Sample Scenarios

Purpose:
- optional additional validation using approved, non-destructive samples

Conditions:
- isolated Windows VM only
- academic approval only
- manifest-driven execution only
- results must be reported conservatively

## 4. Metric Definitions

### 4.1 Detection Capability

Use positive-control scenarios where an alert is genuinely expected.

Formula:

```text
Detection rate = true positives / eligible positive controls
```

Important:
- capability-limited scenarios should not be counted as false negatives

### 4.2 False Positives

Use benign scenarios that should not alert.

Formula:

```text
False-positive rate = false positives / benign scenarios tested
```

### 4.3 Detection Latency

Measure:
- scenario start time
- time of first alert for the tracked PID

Formula:

```text
Latency = first alert time - scenario start time
```

Current latency is influenced by:
- `MonitoringIntervalMs = 5000`
- `NetworkPollingIntervalMs = 2000`
- event accumulation needed to cross the score threshold

### 4.4 CPU And RAM Overhead

Current workflow measures process-level overhead in three phases:
- before monitoring starts
- during monitoring idle
- during an active combined scenario

The evaluation runner reports:
- average CPU
- peak CPU
- average working set
- peak working set

Important limitation:
- these are process-level measurements for the current evaluation host
- final dissertation-grade measurements should still be repeated in an
  isolated Windows VM

## 5. Safety Rules

- Use safe simulators first
- Do not use malware as a default test input
- Use approved samples only in an isolated VM
- Do not present capability-limited observations as proof of failure
- Do not generalize one machine's results into universal effectiveness claims

See [safe-testing-lab.md](safe-testing-lab.md) for the full lab rules.

## 6. Results Interpretation

Use `evaluation/results.csv` for per-scenario facts:
- expected alert state
- actual alert state
- highest observed score
- triggered rules
- latency
- CPU and RAM values where collected

Use `evaluation/summary.md` for:
- aggregated rates
- latency summary
- overhead summary
- written limitations and warnings

## 7. Honest Limitations To Record

Every evaluation write-up should explicitly note:
- whether file telemetry was available
- whether the app was elevated
- whether the run happened on the host or in a VM
- whether a scenario was safety-limited by design
- whether overhead numbers are process-level or fuller VM/system measurements

## 8. Status

P9 is now partially closed at the repository level:
- the repeatable workflow exists
- baseline outputs exist
- documentation exists

What remains is final repetition, tuning only if justified, and dissertation
report packaging.
