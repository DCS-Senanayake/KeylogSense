# Scoring Plan

This document describes the current scoring model implemented in KeylogSense
and how it maps back to the proposal.

## 1. Scoring Model

The detection model is additive and explainable. Each triggered indicator adds
points to a per-process risk score.

| Rule | Logic | Score |
|---|---|---:|
| R1 | Process runs from suspicious location | +4 |
| R2 | Untrusted or unsigned publisher | +3 |
| R3 | Frequent small file writes | +2 |
| R4 | Repeated writes to same file in a short window | +2 |
| R5 | Outbound network connections | +2 |
| R6 | File and network activity close in time | +2 |
| R7 | Persistence detected | +5 |

Maximum possible score: `20`

## 2. Decision Rule

The proposal requires a strict greater-than alert condition:

```text
alert when score > threshold
```

Current default:
- `AlertThreshold = 7`

Important:
- score `7` is not an alert
- score `8` is an alert

## 3. Current Default Configuration

The active defaults in `DetectionConfig` are:

| Setting | Default |
|---|---:|
| `AlertThreshold` | 7 |
| `MonitoringIntervalMs` | 5000 |
| `SmallWriteCountThreshold` | 10 |
| `SmallWriteMaxBytes` | 1024 |
| `RepeatedSameFileWriteThreshold` | 5 |
| `RepeatedWriteWindowSeconds` | 60 |
| `NetworkPollingIntervalMs` | 2000 |
| `FileNetworkCorrelationWindowSeconds` | 30 |
| `PersistencePollingIntervalMs` | 15000 |

These are engineering defaults, not proposal-mandated constants.

## 4. Rule Semantics

### R1. Suspicious Location

Checks whether the executable path is in user-writable locations such as:
- AppData
- LocalAppData
- Temp
- Downloads

This is intended to capture the proposal's suspicious-location rule, not to
claim that all software in those locations is malicious.

### R2. Untrusted Publisher

Checks digital signature trust where feasible in user mode. The intent is to
add points when the executable is observed as unsigned or untrusted, not to
penalize every process whose signature cannot be inspected.

### R3. Frequent Small File Writes

Current default interpretation:
- each counted write is at most `1024` bytes
- the behaviour becomes suspicious when at least `10` such writes are observed

This captures log-like repeated small writes as described in the proposal.

### R4. Repeated Writes To The Same File

Current default interpretation:
- at least `5` writes
- to the same file
- within `60` seconds

### R5. Outbound Network Connections

Adds score when the process is observed initiating outbound TCP activity.

### R6. File And Network Correlation

Adds score when file-write behaviour and outbound network activity are both
observed for the same PID within the current `30` second correlation window.

### R7. Persistence Detected

Adds score when the process is associated with detected persistence changes in:
- Run keys
- RunOnce keys
- Startup folders

## 5. Score Accumulation Examples

| Scenario | Score | Alert? |
|---|---:|---:|
| Suspicious location only | 4 | No |
| Suspicious location + untrusted publisher | 7 | No |
| Suspicious location + untrusted publisher + outbound network | 9 | Yes |
| Suspicious location + persistence | 9 | Yes |
| File writes + network + correlation | 6 | No |
| Suspicious location + file writes + network + correlation | 10 | Yes |

These examples show why strict `>` matters. A borderline score of `7` should
not be treated as suspicious by default.

## 6. Explainability Requirement

Every alert should be explainable in terms of:
- the total score
- the triggered rules
- short human-readable reasons

This is a core academic requirement because the project is intentionally
rule-based rather than opaque.

## 7. Tuning Guidance

P9 tuning should be based on measured results, especially:
- true positives on safe simulator scenarios
- false positives on benign applications
- detection latency
- CPU and RAM overhead

If any threshold or weight is changed from the current defaults:
- record the exact new value
- record why it was changed
- avoid claiming the proposal defined that tuned value
