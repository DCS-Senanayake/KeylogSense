using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Monitoring.ProcessContext;

/// <summary>
/// Determines if an executable path belongs to a suspicious user-writable location.
/// Proposal references: AppData, LocalAppData, Temp, Downloads.
/// </summary>
public sealed class LocationClassifier
{
    private readonly string _appDataPattern;
    private readonly string _localAppDataPattern;
    private readonly string _localProgramsPattern;
    private readonly string _tempPattern;
    private readonly string _downloadsPattern;

    public LocationClassifier()
    {
        // Use environment variables for current user context in standard deployments.
        // We ensure a trailing separator to avoid substring mismatches 
        // (e.g. C:\Temp vs C:\Templates)
        _appDataPattern = EnsureTrailingSlash(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        _localAppDataPattern = EnsureTrailingSlash(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        _localProgramsPattern = EnsureTrailingSlash(Path.Combine(_localAppDataPattern, "Programs"));
        _tempPattern = EnsureTrailingSlash(Path.GetTempPath());

        // Downloads doesn't have a reliable SpecialFolder enum in .NET standard environments,
        // so we approximate via UserProfile.
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _downloadsPattern = EnsureTrailingSlash(Path.Combine(userProfile, "Downloads"));
    }

    /// <summary>
    /// Evaluates the path and returns a SuspiciousLocationClassification.
    /// Handles null, empty, and inaccessible path representations safely.
    /// </summary>
    public SuspiciousLocationClassification Classify(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            // If we can't read the path (e.g. access denied), 
            // the proposal dictates we do not automatically assume malicious.
            return SuspiciousLocationClassification.Safe;
        }

        try
        {
            // Normalize path to avoid backslash/forward slash mixing
            var normalized = Path.GetFullPath(executablePath);
            
            // Check against known suspicious locations
            if (normalized.StartsWith(_tempPattern, StringComparison.OrdinalIgnoreCase))
                return SuspiciousLocationClassification.Temp;
                
            if (normalized.StartsWith(_appDataPattern, StringComparison.OrdinalIgnoreCase))
                return SuspiciousLocationClassification.AppData;

            if (normalized.StartsWith(_localProgramsPattern, StringComparison.OrdinalIgnoreCase))
                return SuspiciousLocationClassification.Safe;

            if (normalized.StartsWith(_localAppDataPattern, StringComparison.OrdinalIgnoreCase))
                return SuspiciousLocationClassification.LocalAppData;
                
            if (normalized.StartsWith(_downloadsPattern, StringComparison.OrdinalIgnoreCase))
                return SuspiciousLocationClassification.Downloads;

            return SuspiciousLocationClassification.Safe;
        }
        catch (Exception)
        {
            // If path format is completely mangled or illegal, default safe
            // "Unknown or inaccessible data must not be treated as automatically malicious."
            return SuspiciousLocationClassification.Safe;
        }
    }

    private static string EnsureTrailingSlash(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        var sep = Path.DirectorySeparatorChar.ToString();
        return path.EndsWith(sep) ? path : path + sep;
    }
}
