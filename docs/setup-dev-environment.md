# Development Environment Setup

This document describes how to build, run, and safely evaluate the current
KeylogSense repository.

## 1. Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| Windows | Windows 11 | Target OS from the proposal |
| .NET SDK | 10.0 | Build and run the solution |
| Visual Studio | 2022 Community or later | Optional IDE |
| Git | 2.x | Version control |
| VirtualBox or Hyper-V | Current version | Safe VM testing |

Engineering note:
- the proposal specifies C# and modern .NET but does not lock a specific SDK
  version
- this repository currently targets .NET 10

## 2. Repository Layout

```text
KeylogSense/
  src/
    KeyloggerDetection.App/
    KeyloggerDetection.Core/
    KeyloggerDetection.Infrastructure/
    KeyloggerDetection.Monitoring/
    KeyloggerDetection.Scoring/
  tools/
    KeyloggerDetection.Simulator/
  tests/
    KeyloggerDetection.Tests/
  docs/
  KeyloggerDetection.slnx
```

## 3. Build The Solution

Open PowerShell in the repository root and run:

```powershell
dotnet build KeyloggerDetection.slnx
dotnet test KeyloggerDetection.slnx
```

Release build:

```powershell
dotnet build KeyloggerDetection.slnx -c Release
```

## 4. Run The Tray Application

The tray app project is `src\KeyloggerDetection.App`.

Command:

```powershell
dotnet run --project src\KeyloggerDetection.App
```

### 4.1 UAC And Administrator Requirement

The application manifest contains:

```xml
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

This means:
- the built tray executable requests Administrator privileges through its
  embedded manifest
- if the user accepts the UAC prompt, the app launches elevated
- if the user rejects the prompt, the app does not start

Because of this manifest:
- full ETW-backed file telemetry is only available when the tray process is
  elevated
- `dotnet run` uses the generated apphost executable for this project, so it
  does not intentionally bypass the manifest
- if no UAC prompt appears on a given machine, verify whether the shell is
  already elevated or whether UAC policy is disabled

Alternative:
- build the solution first
- then launch `src\KeyloggerDetection.App\bin\Debug\net10.0-windows\KeyloggerDetection.App.exe`
- accept the UAC prompt

### 4.2 Tray Behaviour

After launch:
- the app appears in the Windows system tray
- right-click the tray icon to access:
  - Start Monitoring
  - Stop Monitoring
  - Open Logs
  - Exit

## 5. Run The Safe Simulator

Open a second PowerShell window in the repository root.

Examples:

```powershell
dotnet run --project tools\KeyloggerDetection.Simulator
dotnet run --project tools\KeyloggerDetection.Simulator -- --enable-persistence
dotnet run --project tools\KeyloggerDetection.Simulator -- --cleanup
```

The simulator now uses one default combined behaviour flow for safe academic
validation. It does not capture keystrokes. See [simulator.md](simulator.md)
for flags, cleanup, and safety constraints.

## 6. Visual Studio Workflow

If you use Visual Studio:

1. Open `KeyloggerDetection.slnx`
2. Build the solution
3. To run the tray app, launch Visual Studio itself with Administrator rights
   or start the built executable manually

Recommended workload:
- .NET desktop development

## 7. VM Guidance

Use an isolated Windows VM for final measurements and any approved-sample
testing.

Suggested baseline:
- Windows 11 guest
- at least 4 GB RAM
- at least 40 GB storage
- clean snapshot before testing

For approved-sample runs:
- use Host-Only networking or disable networking
- disable shared folders
- revert to a clean snapshot after the session

See [safe-testing-lab.md](safe-testing-lab.md) for the safety procedure.
