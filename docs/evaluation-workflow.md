# Evaluation Workflow

**Purpose:** Execute Phase P9 in a repeatable, academically defensible way.

This workflow runs structured evaluation for:
- detection capability
- false positives
- detection latency
- CPU usage
- RAM usage

The workflow writes:
- `evaluation/results.csv`
- `evaluation/summary.md`
- `evaluation/artifacts/<run-id>/...`

## 1  Recommended Safety Model

- Use an **isolated Windows 11 VM** for dissertation-grade results.
- Use **safe simulator scenarios first**.
- Run **approved non-destructive samples only inside the VM**.
- Do not treat host-machine runs as final proof of effectiveness.

See [safe-testing-lab.md](safe-testing-lab.md) for the mandatory lab rules.

## 2  How To Run

### 2.1  Baseline P9 Workflow

```powershell
dotnet build KeyloggerDetection.slnx
dotnet run --project tools/KeyloggerDetection.Evaluation
```

### 2.1.1  Tray Application Elevation

The tray application itself now uses an application manifest with:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

So when `KeyloggerDetection.App.exe` is launched normally, Windows will show
a UAC prompt on every start. This is intentional and is required for the
ETW-based file behaviour collector.

### 2.2  Optional Tuning Overrides

Use these only when deliberately comparing configurations:

```powershell
dotnet run --project tools/KeyloggerDetection.Evaluation -- `
  --alert-threshold 8 `
  --monitoring-interval-ms 3000
```

If tuning is performed, record the changed values and justify them in the final report.

### 2.3  Optional Approved Sample Workflow

Only use this inside an isolated VM after academic approval has been obtained.

1. Copy the template manifest:

```powershell
Copy-Item evaluation\approved-samples.template.json evaluation\approved-samples.json
```

2. Fill in the sample paths and approval references.
3. Run:

```powershell
dotnet run --project tools/KeyloggerDetection.Evaluation -- `
  --approved-samples-manifest evaluation\approved-samples.json `
  --acknowledge-isolated-vm
```

The acknowledgement flag is required so approved-sample runs are not mixed with casual host-machine testing.

## 3  What The Workflow Executes

### 3.1  Safe Simulator Scenarios

| Scenario | Purpose | Expected Outcome |
|---|---|---|
| `network-only-temp` | Positive control using a staged simulator copy in `%TEMP%` | Alert expected |
| `combined-temp` | Positive control using staged simulator copy in `%TEMP%` with multiple signals | Alert expected |
| `file-only-temp` | File telemetry probe | Alert expected only when file telemetry is actually available |
| `persistence-only-temp` | Safety-limited persistence probe | No attributed alert currently expected |

**Why stage to `%TEMP%`?**

The current scoring model gives `+4` for suspicious process location. Staging a safe simulator copy under `%TEMP%` allows the proposal's location rule to be exercised without changing the simulator code or introducing real malicious behavior.

### 3.2  Benign Application Scenarios

The automated runner currently uses built-in Windows apps that are likely to exist on any Windows 11 machine:
- `notepad.exe`
- `calc.exe`
- `cmd.exe`
- `powershell.exe`

These are used for a conservative false-positive baseline.

## 4  Suggested Benign Application Test Set

For the final dissertation evaluation, extend the automated baseline with the following manual/VM test set when available:

| Category | Suggested Apps |
|---|---|
| Web browsers | Edge, Chrome, Firefox |
| Editors | Notepad, VS Code, Notepad++ |
| Communication | Teams, Discord, Slack |
| Office/productivity | Word, Excel, PowerPoint |
| Developer tools | Visual Studio, Git Bash, Windows Terminal |
| Background/security | Windows Defender, antivirus agent, update services |
| Installers/updaters | Microsoft Office updater, app installers, vendor update services |

Record the exact executable path, signature status, highest observed score, whether an alert occurred, and why.

## 5  How To Interpret The Output

### 5.1  `evaluation/results.csv`

Each row records:
- scenario type
- expected alert state
- whether the run was capability-limited
- alert/no alert
- highest observed score for the tracked PID
- triggered rules for the highest score
- latency when an alert occurred
- CPU/RAM measurements where collected

### 5.2  `evaluation/summary.md`

This summarizes:
- detection capability
- false-positive rate
- latency figures
- overhead figures
- runtime limitations captured during the run

### 5.3  Important Scoring Interpretation

The proposal rule is:

```text
Alert only when score > threshold
```

This means:
- score `7` with threshold `7` is **not** an alert
- a sub-threshold result is not a failure by itself
- single-signal or borderline scenarios can be intentionally non-alerting

## 6  VM Repetition Checklist

Before running final measurements in a VM:

1. Start from a clean snapshot.
2. Confirm Windows 11 guest configuration matches the report.
3. Disable shared folders for approved-sample runs.
4. Use Host-Only or disabled networking for approved-sample runs.
5. Build or copy the latest KeylogSense binaries into the VM.
6. Run the evaluation workflow.
7. Export `evaluation/results.csv` and `evaluation/summary.md`.
8. Revert to the clean snapshot after the session.

## 7  Honest Reporting Guidance

- Do not count a capability-limited scenario as a false negative.
- Do not generalize host-machine measurements into universal effectiveness claims.
- If ETW file telemetry is unavailable, say so explicitly.
- If a scenario is safety-limited by design, say so explicitly.
- Treat the automated workflow as a baseline and the isolated VM run as the authoritative evaluation pass.
