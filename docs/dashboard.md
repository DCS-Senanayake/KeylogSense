# Dashboard

This document describes the simple WinForms dashboard added to KeylogSense. The dashboard window is cleanly titled `KeylogSense` and manages the background monitoring, which now starts automatically upon application launch.

## Purpose

The dashboard provides a consolidated view of the current monitoring state, telemetry coverage, score thresholds, and recent alerts. It serves as a visual hub for demonstration and academic evaluation without complicating the core background tray monitoring.

## UI Sections

The dashboard is structured into four main tabs:

### 1. Live Monitoring
- **Top Panel**: Displays real-time status.
  - **Monitoring Status**: Running / Stopped.
  - **Telemetry Status**: Current availability of Process, File, Network, and Persistence telemetry.
  - **Alert Count**: Total suspicious activity detections since launch.
  - **Last Alert Time**: Timestamp of the most recent alert.
  - **Current Scoring Threshold**: Summarizes the configured threshold and weights.
- **Detections Grid**: A data table showing recent alerts. Displays up to the last 100 recent alerts. Includes a context menu to quickly add an alerted process to the Allowlist.
- **Control Buttons**: Start Monitoring, Stop Monitoring, Open Logs Folder, Refresh Data.

### 2. Previous Logs
- A viewer for historical detection CSV files.
- Provides a dropdown to select any existing log file.
- Renders the CSV data in a readable grid.
- Allows deleting selected log files safely.

### 3. Allowlist Management
- Provides list views for Trusted Publishers, Trusted Paths, and Trusted Process Names.
- Allows manually adding and removing entries.
- Saves changes persistently across application restarts.

### 4. Configuration Management
- Allows tuning of the `AlertThreshold` and all telemetry monitoring intervals and counts.
- **Save Configuration**: Persists the changes to a local `config.json` file.
- **Reset to Defaults**: Restores all configuration values to their factory defaults.

## Architecture Connection

The `DashboardForm` integrates with the existing `TrayApplicationContext`:
- It is instantiated on-demand when the user clicks "Open Dashboard" from the tray menu.
- It receives the active configuration, the elevation state, and callbacks for actions directly from the tray context.
- Alerts surfaced through `OnSuspiciousActivityDetected` are buffered in the tray context, allowing the dashboard to read recent detections without disrupting the core scoring engine's pipeline.

## Limitations

- The live monitoring grid provides a read-only view of recent alerts. It is not a persistent historical database (historical data is in the Previous Logs tab).
- The UI is built entirely in code (no `.resx` designer files) to keep the project structure lightweight.

## Safety Boundaries

- **No Remediation**: The dashboard does not provide any buttons to kill processes, quarantine files, or block network traffic. It is strictly an observation tool.
- **No Keystroke Capture**: The dashboard does not capture, store, or display keystrokes. It only displays process behaviour risk scores.
