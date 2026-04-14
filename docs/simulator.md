# Simulator Guide

This document describes the repository's single safe keylogger-behaviour
simulator used for demos, manual validation, and academic testing.

## What It Does

By default, the simulator runs one combined behavioural flow that:
- repeatedly appends small chunks to the same file
- repeats writes to the same artifact over a short interval
- generates outbound TCP activity

Optional persistence simulation can be enabled explicitly with a flag.

## What It Does Not Do

The simulator does not:
- capture real keyboard input
- hook the keyboard or reconstruct keystrokes
- hide itself, evade detection, or install stealth mechanisms
- add real malware behaviours beyond safe indicator simulation
- create a self-launching persistence loop

When persistence is enabled, the simulator writes an inert HKCU Run-key value
that points to `C:\Windows\System32\notepad.exe`. This is only to exercise a
safe persistence indicator.

## How To Run

Default combined flow:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator
```

Combined flow with optional persistence enabled:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator -- --enable-persistence
```

Cleanup:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator -- --cleanup
```

Help:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator -- --help
```

## Optional Flags

Safe configuration flags:
- `--enable-persistence`
- `--cleanup`
- `--artifact-path <path>`
- `--file-write-iterations <n>`
- `--file-write-interval-ms <n>`
- `--network-bursts <n>`
- `--network-hold-ms <n>`
- `--network-pause-ms <n>`

These flags tune the single simulator flow. They do not switch between
separate simulator types.

## Expected Detector Behaviour

When the detector is running with full telemetry coverage, the simulator is
intended to contribute evidence for:
- frequent small file writes
- repeated writes to the same file
- outbound network activity
- optional persistence detected, only when enabled

Whether an alert occurs still depends on:
- the configured threshold
- whether the simulator is launched from a suspicious location such as `%TEMP%`
- whether file telemetry is available in the current session

For focused validation, you can stage the simulator under `%TEMP%` to exercise
the proposal's suspicious-location rule safely.

## Cleanup

The simulator may leave:
- a file artifact under `%TEMP%\KeylogSense\keylogger-behaviour-sim.log`
- an optional registry marker named `KeylogSenseSimulatorTest`

Remove them with:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator -- --cleanup
```

## Safety And Ethics

Use the simulator only for defensive testing and academic evaluation.

- Do not modify it to capture real keystrokes.
- Do not add stealth, persistence abuse, or destructive behaviour.
- Prefer isolated VM testing for formal validation.
- Report capability limits honestly when telemetry is unavailable.
