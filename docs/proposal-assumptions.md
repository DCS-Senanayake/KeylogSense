# Proposal Assumptions Register

This register records engineering decisions that are not fixed explicitly by
the proposal but are now fixed in the implementation.

## A-DOTNET. Target Framework

| Field | Value |
|---|---|
| Assumption | The repository targets .NET 10. |
| What the proposal says | The proposal specifies C# and modern .NET, but not a specific SDK version. |
| Rationale | .NET 10 is the SDK currently used in this repository. |
| Impact if wrong | The solution would need retargeting, but the architecture is not tightly coupled to .NET 10-only concepts. |
| Recorded in | `README.md`, `docs/tooling-decisions.md`, `docs/setup-dev-environment.md` |

## A-WINFORMS. Tray UI Framework

| Field | Value |
|---|---|
| Assumption | WinForms is used for the tray application. |
| What the proposal says | The proposal specifies a tray application but not a UI framework. |
| Rationale | WinForms provides the most direct implementation of tray icon and balloon notification behaviour. |
| Impact if wrong | The UI layer could be replaced later without changing the detection pipeline. |
| Recorded in | `docs/tooling-decisions.md`, `docs/architecture.md` |

## A-THRESHOLD. Default Alert Threshold

| Field | Value |
|---|---|
| Assumption | `AlertThreshold` defaults to `7`. |
| What the proposal says | The proposal requires `score > threshold` but does not define a threshold value. |
| Rationale | A threshold of 7 reduces borderline alerts such as location plus unsigned publisher alone. |
| Impact if wrong | The threshold is configurable and can be tuned during evaluation. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Core/Configuration/DetectionConfig.cs` |

## A-LOCATIONS. Suspicious Location Resolution

| Field | Value |
|---|---|
| Assumption | Suspicious user-writable locations are resolved through standard environment folder paths and temp path resolution, with subdirectories included. |
| What the proposal says | The proposal names AppData, LocalAppData, Temp, and Downloads. |
| Rationale | This is the normal Windows and .NET way to resolve those paths reliably. |
| Impact if wrong | Path classification could miss some edge cases but is easy to refine later. |
| Recorded in | `docs/scoring-plan.md` |

## A-SIGNATURE. Trust Verification Behaviour

| Field | Value |
|---|---|
| Assumption | Digital signature trust is checked best-effort in user mode, and failure to inspect a signature does not automatically mean malicious. |
| What the proposal says | The proposal says trust should be assessed where feasible. |
| Rationale | Failing open on inaccessible signatures avoids inflating scores for protected or inaccessible processes. |
| Impact if wrong | Some suspicious files may avoid the trust penalty when trust inspection fails. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Monitoring/ProcessContext/SignatureVerifier.cs` |

## A-SMALL-WRITES. Small-Write Defaults

| Field | Value |
|---|---|
| Assumption | Frequent small writes default to at least `10` writes with each write at most `1024` bytes. |
| What the proposal says | The proposal requires repeated small file writes but does not define numeric thresholds. |
| Rationale | The defaults give a concrete starting point for P4 and P9 without claiming they are universal. |
| Impact if wrong | Tuning may be needed to improve sensitivity or reduce false positives. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Core/Configuration/DetectionConfig.cs` |

## A-REPEATED-WRITES. Same-File Window Defaults

| Field | Value |
|---|---|
| Assumption | Repeated writes to the same file default to at least `5` writes within `60` seconds. |
| What the proposal says | The proposal says repeated writes in a short time window but does not define the count or duration. |
| Rationale | The implementation needs concrete values to aggregate file-write events consistently. |
| Impact if wrong | These values can be tuned in evaluation and documented as changed settings. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Core/Configuration/DetectionConfig.cs` |

## A-CORRELATION-WINDOW. File-Network Correlation Window

| Field | Value |
|---|---|
| Assumption | File and network correlation defaults to a `30` second window. |
| What the proposal says | The proposal says file and network activity should be correlated when they occur close in time, but it does not define the window. |
| Rationale | A fixed correlation window is required for deterministic scoring. |
| Impact if wrong | A wider or narrower window may change alert sensitivity. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Core/Configuration/DetectionConfig.cs` |

## A-PERSISTENCE-COVERAGE. Version 1 Persistence Scope

| Field | Value |
|---|---|
| Assumption | Version 1 persistence coverage includes Run, RunOnce, and Startup folders. |
| What the proposal says | The proposal mentions startup entries and Run-key style persistence. |
| Rationale | These are common, readable, user-mode accessible persistence mechanisms that fit the scope. |
| Impact if wrong | Other persistence methods such as scheduled tasks or WMI subscriptions are out of scope for the current baseline. |
| Recorded in | `docs/architecture.md`, `docs/scoring-plan.md`, `src/KeyloggerDetection.Monitoring/Persistence/PersistenceCollector.cs` |

## A-MONITORING-INTERVAL. Monitoring Defaults

| Field | Value |
|---|---|
| Assumption | The default process monitoring interval is `5000` ms, network polling interval is `2000` ms, and persistence polling interval is `15000` ms. |
| What the proposal says | The proposal requires real-time behaviour monitoring but does not define polling intervals. |
| Rationale | These values provide a practical balance between responsiveness and overhead for a user-mode academic tool. |
| Impact if wrong | Latency and CPU overhead may shift; the settings can be tuned in evaluation. |
| Recorded in | `docs/scoring-plan.md`, `src/KeyloggerDetection.Core/Configuration/DetectionConfig.cs` |

## A-DI-MECHANISM. Manual Composition Root

| Field | Value |
|---|---|
| Assumption | The tray application uses manual composition in `Program.cs` instead of a DI container. |
| What the proposal says | Nothing; this is purely an implementation detail. |
| Rationale | The current app is small enough that manual composition is straightforward and keeps dependencies low. |
| Impact if wrong | The composition approach can be refactored later without changing the proposal-facing design. |
| Recorded in | `docs/architecture.md`, `src/KeyloggerDetection.App/Program.cs` |

## A-ELEVATION. Full File Telemetry Requires Elevation

| Field | Value |
|---|---|
| Assumption | The app requests elevation on startup so ETW file telemetry can be collected reliably. |
| What the proposal says | The proposal requires user-mode operation but does not specify the startup privilege model. |
| Rationale | ETW kernel file tracing requires Administrator privileges even though the app itself remains user-mode. |
| Impact if wrong | Without elevation, file telemetry becomes unavailable and file-related rules cannot be measured end-to-end. |
| Recorded in | `docs/file-behaviour-monitoring.md`, `docs/setup-dev-environment.md`, `docs/tooling-decisions.md` |

## A-OVERHEAD. No Pass-Fail Overhead Threshold

| Field | Value |
|---|---|
| Assumption | CPU and RAM overhead are reported observationally, without a fixed pass-fail threshold. |
| What the proposal says | The proposal requires CPU and RAM overhead measurement but does not define an acceptable limit. |
| Rationale | Reporting the measured overhead honestly is more defensible than inventing a threshold not approved in the proposal. |
| Impact if wrong | A supervisor-defined threshold could be added later for reporting. |
| Recorded in | `docs/evaluation-plan.md`, `docs/evaluation-workflow.md` |
