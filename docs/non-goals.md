# Non-Goals and Scope Exclusions

This document records what KeylogSense is not intended to do.

## 1. Hard Scope Boundaries

### NG-1. No Kernel Drivers

The project remains user-mode only. It does not include:
- kernel drivers
- minifilters
- hypervisor components

### NG-2. No Automated Remediation

The system detects and alerts only. It does not:
- kill suspicious processes
- quarantine files
- delete registry entries
- block network traffic automatically

### NG-3. No Network-Wide IDS

The project monitors the local machine only. It is not a network-wide
intrusion detection platform.

### NG-4. No Keystroke Content Capture

The project must never capture, store, display, transmit, or reconstruct
actual user keystroke content.

### NG-5. No Machine Learning Detection Engine

The current project is intentionally rule-based and explainable, not ML-based.

### NG-6. No Cloud Backend Or Remote Dashboard

Monitoring, alerting, and logging are local to the machine being monitored.

### NG-7. No Cross-Platform Support

The current target platform is Windows 11.

## 2. Important Clarifications

### ETW With Elevation Is Not A Scope Violation

The project now uses ETW kernel file events for file behaviour monitoring, and
that requires Administrator privileges. This does not violate the user-mode
scope:
- the app is still a user-mode process
- it simply requests elevation through UAC for full telemetry coverage

### Optional Input-Related Research Is Not The Foundation

The proposal mentions input-related indicators where feasible, but the current
implementation foundation is still:
- process context
- file behaviour
- network behaviour
- persistence

## 3. Integrity Boundary

### NG-8. No Fake Telemetry Or Fabricated Results

If a telemetry source is unavailable:
- log the limitation
- document the coverage gap
- avoid claiming the capability worked when it did not

This is especially important for:
- ETW availability
- elevation-dependent file telemetry
- safety-limited persistence scenarios
