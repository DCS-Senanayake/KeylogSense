# Dashboard

This document describes the simple WinForms dashboard added to KeylogSense.

## Purpose

The dashboard provides a consolidated view of the current monitoring state, telemetry coverage, score thresholds, and recent alerts. It serves as a visual hub for demonstration and academic evaluation without complicating the core background tray monitoring.

## UI Sections

1. **Top Panel**: Displays real-time status.
   - **Monitoring Status**: Running / Stopped.
   - **Telemetry Status**: Current availability of Process, File, Network, and Persistence telemetry. Notes whether File ETW telemetry is active or offline (due to lack of Administrator privileges).
   - **Alert Count**: Total suspicious activity detections since launch.
   - **Last Alert Time**: Timestamp of the most recent alert.
   - **Current Scoring Threshold**: Summarizes the configured threshold and the weights for each behaviour rule.

2. **Detections Grid**: A data table showing recent alerts.
   - Includes Time, Process Name, PID, Risk Score, and the Triggered Reasons.
   - Displays up to the last 100 recent alerts.

3. **Control Buttons**:
   - Start Monitoring
   - Stop Monitoring
   - Open Logs
   - Refresh Data

## Architecture Connection

The `DashboardForm` integrates with the existing `TrayApplicationContext`:
- It is instantiated on-demand when the user clicks "Open Dashboard" from the tray menu.
- It receives the active configuration, the elevation state, and callbacks for actions directly from the tray context.
- Alerts surfaced through `OnSuspiciousActivityDetected` are buffered in the tray context, allowing the dashboard to read recent detections without disrupting the core scoring engine's pipeline.

## Limitations

- The dashboard provides a read-only view of recent alerts. It is not a persistent historical database.
- It does not modify configuration files; it only displays the active defaults.
- The UI is built entirely in code (no `.resx` designer files) to keep the project structure lightweight.

## Safety Boundaries

- **No Remediation**: The dashboard does not provide any buttons to kill processes, quarantine files, or block network traffic. It is strictly an observation tool.
- **No Keystroke Capture**: The dashboard does not capture, store, or display keystrokes. It only displays process behaviour risk scores.
