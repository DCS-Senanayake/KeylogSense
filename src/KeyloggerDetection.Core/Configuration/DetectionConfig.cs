namespace KeyloggerDetection.Core.Configuration;

/// <summary>
/// Central configuration for the KeylogSense detection system.
/// All scoring weights are from the proposal's Table 1 (§ 2.1.3).
/// The alert threshold and monitoring intervals are engineering assumptions.
/// </summary>
public sealed class DetectionConfig
{
    // ---------------------------------------------------------------
    //  Scoring weights — initial values from proposal Table 1 (§ 2.1.3)
    // ---------------------------------------------------------------

    /// <summary>
    /// Score added when the process runs from a suspicious user-writable
    /// location (AppData, LocalAppData, Temp, Downloads).
    /// Proposal Table 1: +4.
    /// </summary>
    public int SuspiciousLocationScore { get; set; } = 4;

    /// <summary>
    /// Score added when the process executable is unsigned or has an
    /// invalid/untrusted digital signature.
    /// Proposal Table 1: +3.
    /// </summary>
    public int UntrustedPublisherScore { get; set; } = 3;

    /// <summary>
    /// Score added when the process performs frequent small file writes
    /// consistent with a log-like pattern.
    /// Proposal Table 1: +2.
    /// </summary>
    public int FrequentSmallWritesScore { get; set; } = 2;

    /// <summary>
    /// Score added when the process writes to the same file repeatedly
    /// within a short time window.
    /// Proposal Table 1: +2.
    /// </summary>
    public int RepeatedSameFileWritesScore { get; set; } = 2;

    /// <summary>
    /// Score added when the process has outbound network connections.
    /// Proposal Table 1: +2.
    /// </summary>
    public int OutboundNetworkScore { get; set; } = 2;

    /// <summary>
    /// Score added when file logging behaviour and network activity
    /// occur in the same time window for the same process.
    /// Proposal Table 1: +2.
    /// </summary>
    public int FileNetworkCorrelationScore { get; set; } = 2;

    /// <summary>
    /// Score added when the process has a persistence entry
    /// (e.g., Run-key registry entry).
    /// Proposal Table 1: +5.
    /// </summary>
    public int PersistenceDetectedScore { get; set; } = 5;

    // ---------------------------------------------------------------
    //  Alert threshold
    // ---------------------------------------------------------------

    /// <summary>
    /// A process is flagged when its total risk score is STRICTLY GREATER
    /// than this threshold (score > threshold, NOT score >= threshold).
    /// Proposal reference: § 2.1.3 — "If score > threshold then alert."
    ///
    /// ENGINEERING ASSUMPTION (A-THRESHOLD): Default value is 7.
    /// The proposal does not fix this value. Rationale: requires 2–3
    /// independent indicators to trigger, reducing false positives.
    /// </summary>
    public int AlertThreshold { get; set; } = 7;

    // ---------------------------------------------------------------
    //  Monitoring intervals
    // ---------------------------------------------------------------

    /// <summary>
    /// How often the monitoring loop scans processes, in milliseconds.
    ///
    /// ENGINEERING ASSUMPTION (A-MONITORING-INTERVAL): Default is 5000ms.
    /// The proposal says "real-time" but does not specify an interval.
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = 5000;

    // ---------------------------------------------------------------
    //  Logging
    // ---------------------------------------------------------------

    /// <summary>
    /// Directory where CSV/text detection logs are stored.
    /// </summary>
    public string LogDirectory { get; set; } = "Logs";

    /// <summary>
    /// Prefix for log file names.
    /// </summary>
    public string LogFilePrefix { get; set; } = "KeylogSense";

    /// <summary>
    /// Minimum severity level for application logs.
    /// </summary>
    public Models.LogLevel LogLevel { get; set; } = Models.LogLevel.Info;

    // ---------------------------------------------------------------
    //  File activity thresholds (to be tuned in P4/P9)
    // ---------------------------------------------------------------

    /// <summary>
    /// File roots or extensions to monitor. Empty means monitor all.
    /// ENGINEERING ASSUMPTION: Exclude noisy system logging roots by default if tracking everything.
    /// </summary>
    public string[] MonitoredFileRoots { get; set; } = [];

    /// <summary>
    /// Minimum number of small file writes in the monitoring window
    /// to trigger the "frequent small writes" rule.
    ///
    /// ENGINEERING ASSUMPTION (A-SMALL-WRITES): Default is 10.
    /// The proposal does not define "frequent." Will be tuned in P9.
    /// </summary>
    public int SmallWriteCountThreshold { get; set; } = 12;

    /// <summary>
    /// Maximum file write size in bytes to be considered a "small write."
    ///
    /// ENGINEERING ASSUMPTION (A-SMALL-WRITES): Default is 1024 bytes.
    /// The proposal does not define "small." Will be tuned in P9.
    /// </summary>
    public int SmallWriteMaxBytes { get; set; } = 512;

    /// <summary>
    /// Minimum number of writes to the same file to trigger the
    /// "repeated same file writes" rule.
    ///
    /// ENGINEERING ASSUMPTION (A-REPEATED-WRITES): Default is 5.
    /// The proposal does not define the count. Will be tuned in P9.
    /// </summary>
    public int RepeatedSameFileWriteThreshold { get; set; } = 8;

    /// <summary>
    /// Time window in seconds for counting "repeated same file writes."
    ///
    /// ENGINEERING ASSUMPTION (A-REPEATED-WRITES): Default is 60 seconds.
    /// The proposal says "short time window" without specifying a value.
    /// </summary>
    public int RepeatedWriteWindowSeconds { get; set; } = 30;

    // ---------------------------------------------------------------
    //  Network activity configuration (P5)
    // ---------------------------------------------------------------

    /// <summary>
    /// How often the network loop scans connections, in milliseconds.
    /// ENGINEERING ASSUMPTION: 2000ms ensures short-lived telemetry bounds without stressing CPU.
    /// </summary>
    public int NetworkPollingIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Exclude localhost/loopback connections from being flagged as outbound activity.
    /// Default true. Local IPC isn't exfiltration.
    /// </summary>
    public bool ExcludeLoopbackTraffic { get; set; } = true;

    /// <summary>
    /// Supported protocols to snapshot. Starting with TCP per proposal.
    /// </summary>
    public string[] MonitoredNetworkProtocols { get; set; } = ["TCP"];

    /// <summary>
    /// Minimum number of distinct outbound connections in the current process window
    /// before the outbound-network rule contributes score.
    /// ENGINEERING ASSUMPTION: A single connection is too common in benign apps.
    /// </summary>
    public int OutboundConnectionCountThreshold { get; set; } = 2;

    // ---------------------------------------------------------------
    //  File-network correlation window (to be tuned in P5/P9)
    // ---------------------------------------------------------------

    /// <summary>
    /// Time window in seconds for correlating file logging and
    /// network activity for the same process.
    ///
    /// ENGINEERING ASSUMPTION (A-CORRELATION-WINDOW): Default is 30 seconds.
    /// The proposal says "close in time" without specifying a value.
    /// </summary>
    public int FileNetworkCorrelationWindowSeconds { get; set; } = 10;

    // ---------------------------------------------------------------
    //  Persistence and Allowlist (P6)
    // ---------------------------------------------------------------

    /// <summary>
    /// How often the persistence loop checks registry/startup folders.
    /// ENGINEERING ASSUMPTION: Persistence doesn't change rapidly like network/files.
    /// Polling every 15 seconds reduces unnecessary Windows API polling overhead.
    /// </summary>
    public int PersistencePollingIntervalMs { get; set; } = 15000;

    /// <summary>
    /// Benign cache/state paths to exclude from file-write scoring by default.
    /// These reduce known browser/editor cache noise without removing the file
    /// behaviour category from the detector.
    /// </summary>
    public string[] BenignFilePathExclusions { get; set; } =
    [
        @"\AppData\Local\Google\Chrome\User Data\",
        @"\AppData\Local\Microsoft\Edge\User Data\",
        @"\AppData\Local\Mozilla\Firefox\Profiles\",
        @"\AppData\Local\Packages\"
    ];

    /// <summary>
    /// Configuration for trusted items to bypass scoring.
    /// </summary>
    public RuleAllowlist Allowlist { get; set; } = new();
}

/// <summary>
/// Defines entities that should be explicitly trusted, reducing false positives.
/// ENGINEERING ASSUMPTION: Allowlist entries string comparisons are OrdinalIgnoreCase.
/// </summary>
public sealed class RuleAllowlist
{
    public string[] TrustedPublishers { get; set; } = ["Microsoft Corporation"];
    public string[] TrustedExecutablePaths { get; set; } = [];
    public string[] TrustedProcessNames { get; set; } = [];
    public string[] TrustedHashes { get; set; } = [];
}
