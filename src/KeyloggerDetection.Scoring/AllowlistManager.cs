using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.Scoring;

/// <summary>
/// Evaluates events and feature vectors against the configured allowlist arrays
/// to proactively suppress false positives for known benign software.
/// </summary>
public sealed class AllowlistManager
{
    private readonly RuleAllowlist _whitelist;

    public AllowlistManager(DetectionConfig config)
    {
        _whitelist = config.Allowlist;
    }

    /// <summary>
    /// Checks if a process context should be explicitly trusted.
    /// </summary>
    public bool IsTrusted(string? executablePath, string? processName, TrustState trustState, string? publisherName = null)
    {
        // 1. Publisher validation (If trust is proven and publisher is on list)
        if (trustState == TrustState.Trusted && !string.IsNullOrWhiteSpace(publisherName))
        {
            if (_whitelist.TrustedPublishers.Contains(publisherName, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        // 2. Exact Path Validation
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var normalizedPath = TryNormalizePath(executablePath);
            if (normalizedPath != null &&
                _whitelist.TrustedExecutablePaths
                    .Select(TryNormalizePath)
                    .Where(p => p != null)
                    .Any(p => string.Equals(p, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // 3. Name Validation (e.g. 'chrome.exe')
        if (!string.IsNullOrWhiteSpace(processName))
        {
            var rawName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName : processName + ".exe";
            var executableName = !string.IsNullOrWhiteSpace(executablePath) ? Path.GetFileName(executablePath) : null;
            
            if (_whitelist.TrustedProcessNames.Any(n => 
            {
                var nExe = n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? n : n + ".exe";
                return string.Equals(nExe, rawName, StringComparison.OrdinalIgnoreCase) &&
                    (string.IsNullOrWhiteSpace(executableName) || string.Equals(nExe, executableName, StringComparison.OrdinalIgnoreCase));
            })) return true;
        }

        return false;
    }

    /// <summary>
    /// File hash validation (Optional Enhancement).
    /// </summary>
    public bool IsHashTrusted(string sha256Hex)
    {
        if (string.IsNullOrWhiteSpace(sha256Hex)) return false;
        
        return _whitelist.TrustedHashes.Contains(sha256Hex, StringComparer.OrdinalIgnoreCase);
    }

    private static string? TryNormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }
}
