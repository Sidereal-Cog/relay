using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Relay.Models;

namespace Relay;

public static class ConfigManager
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "Relay");

    private static readonly string ConfigPath =
        Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // Volatile reference — safe for single-writer, multi-reader (reference swap is atomic on 64-bit)
    private static volatile RootConfig _current = new();

    public static RootConfig Current => _current;

    public static void Load()
    {
        EnsureDirectory();

        if (!File.Exists(ConfigPath))
        {
            _current = new RootConfig();
            return;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            _current = JsonSerializer.Deserialize<RootConfig>(json, JsonOptions) ?? new RootConfig();
        }
        catch
        {
            // Corrupt or unreadable config — fall back to defaults silently
            _current = new RootConfig();
        }
    }

    public static void Save()
    {
        EnsureDirectory();

        var json = JsonSerializer.Serialize(_current, JsonOptions);
        var tempPath   = ConfigPath + ".tmp";
        var backupPath = ConfigPath + ".bak";

        File.WriteAllText(tempPath, json);

        // Atomic replace — prevents partial writes from corrupting the config
        if (File.Exists(ConfigPath))
            File.Replace(tempPath, ConfigPath, backupPath);
        else
            File.Move(tempPath, ConfigPath);
    }

    public static void Update(RootConfig newConfig)
    {
        _current = newConfig;
        Save();
    }

    private static void EnsureDirectory() => Directory.CreateDirectory(ConfigDir);
}
