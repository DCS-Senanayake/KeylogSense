# Safe Testing Lab Guidelines

This document defines the safety and ethical rules for testing the KeylogSense
keylogger detection system.

The project must stay academically defensible:
- use isolated environments for suspicious testing
- never capture real keystroke content
- never distribute malware samples
- report limitations honestly

## 1. Core Principles

| Principle | Meaning |
|---|---|
| Isolated VM first | Suspicious or sample-based testing belongs in an isolated VM or lab |
| No real user data | Do not capture, store, display, transmit, or reconstruct actual keystroke content |
| No sample distribution | Any approved sample is for academic evaluation only and must not be redistributed |
| Privacy-preserving logs | Logs should contain behavioural indicators, not sensitive personal content |

## 2. VM Isolation Rules

Recommended VM baseline:
- Windows 11 guest
- clean snapshot before each session
- shared folders disabled for sample testing
- Host-Only or disabled networking for sample testing

What should run inside the VM:
- safe simulator scenarios for final reporting
- benign false-positive runs for final reporting
- any approved-sample evaluation
- final latency and overhead measurement passes

What must not happen:
- do not run approved samples on the host machine
- do not leave suspicious test processes running after the session
- do not export or keep unnecessary sample binaries after evaluation

## 3. Testing Order

### Phase 1. Safe Simulators

Start with safe simulator scenarios only.

These are acceptable because they:
- do not hook the keyboard
- do not capture input
- do not contain malware behaviour
- are built specifically for academic testing

### Phase 2. Benign Applications

Use signed or normal applications to measure false positives.

Suggested categories:
- browsers
- editors
- productivity tools
- communication tools
- developer tools
- background and security tools

### Phase 3. Optional Approved Samples

Use approved non-destructive samples only when all of the following are true:
- academic approval exists
- the run is inside an isolated Windows VM
- networking restrictions are applied
- the sample is treated as a measured evaluation input, not as a general test
  dependency

## 4. Evaluation Metrics To Capture

Every formal test pass should record:
- detection capability
- false positives
- detection latency
- CPU overhead
- RAM overhead

Record these measurements manually in your dissertation notes or lab worksheet
because the repository does not currently ship an evaluation runner.

## 5. Log Handling

- Keep detector logs and lab notes for analysis only
- Do not store real keystroke content in logs
- Export only the files needed for reporting
- Remove unnecessary sample artifacts after the evaluation session

## 6. Session Checklist

- clean VM snapshot available
- latest build copied or built inside the VM
- test plan for the session written down
- baseline environment noted
- output files and observations collected after the run
- VM reverted or cleaned after the session
