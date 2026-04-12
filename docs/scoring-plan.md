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

## 1A. P9 False-Positive Tuning Note

The detector remains rule-based and explainable, but the implementation was
tuned to reduce false positives on legitimate applications.

What changed:
- Trust inspection failures now map to `Unknown`, not to the untrusted rule.
- Only explicitly `InvalidSignature` or explicitly `Untrusted` executables
  contribute the untrusted-publisher score.
- Publisher allowlisting now works end to end because publisher metadata is
  populated from the executable's Authenticode certificate when available.
- `LocalAppData` is no longer treated as aggressively as `Temp` or `Downloads`.
- `LocalAppData\\Programs\\...` is treated as `Safe` to avoid penalizing common
  per-user application installs.
- Outbound-network scoring now requires at least `2` distinct outbound
  connections in the active process window.
- File/network correlation now requires a stronger file-write signal and a
  tighter timing burst, not just any file write near any network event.
- Benign browser/app cache roots under `LocalAppData` are excluded from file
  scoring by default.
- A user-facing alert guardrail was added:
  score above threshold is still required, but score-above-threshold alone is
  no longer enough when the score is made up only of weak/common signals.

Old vs new tuning:

| Item | Old | New |
|---|---|---|
| Trust inspection failure | Could fall through as invalid/no signature in practice | `Unknown` |
| Untrusted-publisher scoring | Any invalid/misread signature path could add `+3` | Only explicit `InvalidSignature` or explicit `Untrusted` adds `+3` |
| `LocalAppData` handling | Treated like broader AppData suspicious-location score | Separate lower-risk classification |
| `LocalAppData\\Programs` | No special handling | Classified as `Safe` |
| Suspicious-location score | Flat `+4` for all non-safe locations | `AppData = +3`, `LocalAppData = +2`, `Temp/Downloads/OtherSuspicious = +4` |
| Small write threshold | `10` writes, `<= 1024` bytes | `12` writes, `<= 512` bytes |
| Repeated same-file threshold | `5` writes in `60s` | `8` writes in `30s` |
| Outbound network rule | Any outbound connection | At least `2` distinct outbound connections |
| File/network correlation | Any file-write timestamp near network within `30s` | Strong file signal plus outbound burst within `10s` |
| Alerting | `score > threshold` immediately raised alert | `score > threshold` still defines suspicious score, but user-facing alert also requires at least one stronger behavioural signal |

Why it changed:
- CSV evidence showed Chrome and other legitimate applications being flagged by
  the combination of location + untrusted publisher + outbound network, even
  without any stronger behavioural signal.
- The trust check was overclassifying signed applications because signature
  inspection was not using executable Authenticode semantics correctly.
- Common per-user installs under `LocalAppData` and ordinary outbound network
  activity are too common to justify the same alert behaviour as `Temp`,
  `Downloads`, persistence, or bursty file/network patterns.

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

User-facing alerting now adds one extra guardrail on top of the strict score
rule:
- the score must still be `> threshold`
- and at least one stronger behavioural signal must be present, such as:
  frequent small file writes, repeated same-file writes, file/network
  correlation, or persistence

## 3. Current Default Configuration

The active defaults in `DetectionConfig` are:

| Setting | Default |
|---|---:|
| `AlertThreshold` | 7 |
| `MonitoringIntervalMs` | 5000 |
| `SmallWriteCountThreshold` | 12 |
| `SmallWriteMaxBytes` | 512 |
| `RepeatedSameFileWriteThreshold` | 8 |
| `RepeatedWriteWindowSeconds` | 30 |
| `NetworkPollingIntervalMs` | 2000 |
| `OutboundConnectionCountThreshold` | 2 |
| `FileNetworkCorrelationWindowSeconds` | 10 |
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

Current tuning:
- `Temp`, `Downloads`, and other clearly risky writable locations retain the
  full suspicious-location weight
- roaming `AppData` is treated as moderately suspicious
- `LocalAppData` is treated as lower-risk than `Temp`/`Downloads`
- `LocalAppData\Programs\...` is treated as `Safe`

### R2. Untrusted Publisher

Checks digital signature trust where feasible in user mode. The intent is to
add points when the executable is observed as unsigned or untrusted, not to
penalize every process whose signature cannot be inspected.

Current tuning:
- inspection failures and inaccessible files map to `Unknown`
- `Unknown` does not add score
- only explicit `InvalidSignature` or explicit `Untrusted` states add the
  untrusted-publisher score

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

Adds score when the process is observed initiating repeated outbound TCP
activity rather than just a single common connection.

### R6. File And Network Correlation

Adds score when file-write behaviour and outbound network activity are both
observed for the same PID within the current `10` second correlation window,
and the file side already meets a stronger suspicious-write threshold.

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
| Downloads + untrusted publisher + repeated outbound network | 9 | Guardrail suppresses user-facing alert until a stronger behavioural signal is also present |
| Suspicious location + persistence | 9 | Yes |
| File writes + repeated outbound network + correlation | 6 | No |
| Suspicious location + file writes + repeated outbound network + correlation | 10 | Yes |

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
