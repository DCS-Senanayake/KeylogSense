using System;
using System.IO;
using System.Text.Json;

namespace KeyloggerDetection.Core.Configuration;

/// <summary>
/// Manages the loading, saving, and persistence of the KeylogSense configuration.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigFileName = "config.json";
    private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads the configuration from the local JSON file.
    /// If the file does not exist or is invalid, a default configuration is returned and saved.
    /// </summary>
    public static DetectionConfig Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            var defaultConfig = new DetectionConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<DetectionConfig>(json, JsonOptions);
            return config ?? new DetectionConfig();
        }
        catch (Exception)
        {
            // On failure to read or deserialize, return defaults to ensure the app can still start
            return new DetectionConfig();
        }
    }

    /// <summary>
    /// Saves the provided configuration to the local JSON file.
    /// </summary>
    public static void Save(DetectionConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception)
        {
            // Fail silently or handle accordingly. The caller can log the error if needed.
            // In this specific system, logger is set up in Program.cs after config load.
        }
    }

    /// <summary>
    /// Resets the configuration file to the default settings.
    /// </summary>
    public static DetectionConfig ResetToDefaults()
    {
        var defaultConfig = new DetectionConfig();
        Save(defaultConfig);
        return defaultConfig;
    }
}
