using System.Globalization;

namespace KeyloggerDetection.Evaluation;

internal sealed class ProcessLaunchScenario
{
    public required string ScenarioType { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string SourceExecutablePath { get; init; }
    public string Arguments { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 12;
    public bool? ExpectedAlert { get; init; }
    public string ExpectedBehaviorLabel { get; init; } = string.Empty;
    public bool StageInSuspiciousTempLocation { get; init; }
    public bool CleanupAfterScenario { get; init; }
    public bool CapabilityLimited { get; init; }
    public string? CapabilityReason { get; init; }
    public string? Notes { get; init; }
}

internal sealed class EvaluationResultRow
{
    public required string ScenarioType { get; init; }
    public required string ScenarioName { get; init; }
    public required string Description { get; init; }
    public required bool Executed { get; init; }
    public bool? ExpectedAlert { get; init; }
    public string? ExpectedBehaviorLabel { get; init; }
    public bool CapabilityLimited { get; init; }
    public string? CapabilityReason { get; init; }
    public int? ProcessId { get; init; }
    public string? ExecutablePath { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public bool? AlertRaised { get; init; }
    public bool? FalsePositive { get; init; }
    public int? HighestScore { get; init; }
    public string? TriggeredRules { get; init; }
    public double? DetectionLatencySeconds { get; init; }
    public double? AverageCpuPercent { get; init; }
    public double? PeakCpuPercent { get; init; }
    public double? AverageWorkingSetMb { get; init; }
    public double? PeakWorkingSetMb { get; init; }
    public string? Notes { get; init; }

    public static EvaluationResultRow CreateSkipped(ProcessLaunchScenario scenario, string reason) =>
        new()
        {
            ScenarioType = scenario.ScenarioType,
            ScenarioName = scenario.Name,
            Description = scenario.Description,
            Executed = false,
            ExpectedAlert = scenario.ExpectedAlert,
            ExpectedBehaviorLabel = scenario.ExpectedBehaviorLabel,
            CapabilityLimited = scenario.CapabilityLimited,
            CapabilityReason = scenario.CapabilityReason,
            Notes = reason
        };

    public static EvaluationResultRow CreateOverheadRow(string name, string description, MetricSample metrics, string notes) =>
        new()
        {
            ScenarioType = "overhead",
            ScenarioName = name,
            Description = description,
            Executed = true,
            AverageCpuPercent = metrics.AverageCpuPercent,
            PeakCpuPercent = metrics.PeakCpuPercent,
            AverageWorkingSetMb = metrics.AverageWorkingSetMb,
            PeakWorkingSetMb = metrics.PeakWorkingSetMb,
            Notes = notes
        };
}

internal sealed record MetricSample(
    double AverageCpuPercent,
    double PeakCpuPercent,
    double AverageWorkingSetMb,
    double PeakWorkingSetMb);

internal sealed class ApprovedSampleManifest
{
    public List<ApprovedSampleEntry> Samples { get; init; } = [];
}

internal sealed class ApprovedSampleEntry
{
    public string Label { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? Arguments { get; init; }
    public string? AcademicApprovalReference { get; init; }
    public string? Notes { get; init; }
}

internal static class EvaluationFormatting
{
    public static string FormatMaybeBool(bool? value) => value.HasValue ? (value.Value ? "Yes" : "No") : "N/A";
    public static string FormatMaybeInt(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "N/A";
    public static string FormatMaybeDouble(double? value) => value?.ToString("F2", CultureInfo.InvariantCulture) ?? "N/A";

    public static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    public static string EscapeMarkdown(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
