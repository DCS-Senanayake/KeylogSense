# P9 Evaluation Summary

- Run ID: `20260411-140221`
- Generated: `2026-04-11T14:04:36.6036829Z`
- Detector alert threshold: `7`
- Monitoring interval: `5000 ms`
- Detector log artifacts: `D:\ESoft\Semster 5\Individual Project\e220523\KeylogSense\evaluation\artifacts\20260411-140221`

## Detection Capability

- Eligible positive-control scenarios: `2`
- True positives: `2`
- False negatives: `0`
- Detection rate: `100.0%`

| Scenario | Expected | Alert Raised | Highest Score | Latency (s) | Notes |
|---|---:|---:|---:|---:|---|
| network-only-temp | Yes | Yes | 9 | 3.62 |  |
| combined-temp | Yes | Yes | 9 | 2.01 |  |
| file-only-temp | N/A | No | 7 | N/A |  |
| persistence-only-temp | No | No | 0 | N/A | This scenario is retained to document the current attribution limitation instead of overclaiming persistence coverage. |

## False Positives

- Benign scenarios executed: `4`
- False positives: `0`
- False-positive rate: `0.0%`

| Scenario | Alert Raised | Highest Score | Notes |
|---|---:|---:|---|
| notepad | No | 3 |  |
| calc | No | 3 |  |
| cmd-timeout | No | 3 |  |
| powershell-sleep | No | 3 |  |

## Detection Latency

- Min latency: `2.01 s`
- Max latency: `3.62 s`
- Mean latency: `2.82 s`

## CPU and RAM Overhead

| Phase | Avg CPU % | Peak CPU % | Avg RAM MB | Peak RAM MB |
|---|---:|---:|---:|---:|
| Baseline (before monitoring) | 0.15 | 0.51 | 23.15 | 23.48 |
| Monitoring idle | 4.41 | 16.63 | 93.93 | 371.73 |
| Monitoring during combined-temp | 0.30 | 1.80 | 42.82 | 46.64 |

## Interpretation

- `results.csv` records each executed scenario, whether an alert was expected, whether it actually occurred, the highest observed score for the tracked PID, and any measured latency.
- The detector's built-in CSV log only contains suspicious detections. A missing alert row does not imply that every telemetry source was active; consult the limitations section and `evaluation-app.log` when a scenario is capability-limited.
- Positive-control simulator scenarios are staged under `%TEMP%` so the existing suspicious-location rule can be exercised safely without modifying the simulator's code or introducing real malicious behavior.

## Limitations

- File telemetry available in this run: `False`
- CAPABILITY LIMITATION: ETW file tracing requires Administrator privileges.
- Real file behaviour monitoring is currently offline. The application will continue running.
- To test file logging logic safely without Admin, use the MockFileTelemetryProvider as specified in the evaluation plan.
- The persistence-only simulator remains intentionally safety-limited: it writes an inert Run-key entry for `notepad.exe`, so end-to-end attribution back to the simulator PID is not currently expected.
- CPU and RAM figures in this workflow are process-level measurements for the evaluation host itself. Re-run inside an isolated Windows VM for dissertation-grade overhead figures and a cleaner baseline.
- The tool cannot prove whether the current host is an isolated VM. Treat any approved-sample run as valid only when you have independently enforced the VM checklist in `docs/safe-testing-lab.md`.
