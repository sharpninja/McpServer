using System.Text.Json;

namespace McpServer.Director;

/// <summary>
/// Persists Director CLI defaults (for non-workspace usage) under the user's profile.
/// </summary>
internal static class DirectorCliConfigStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".mcpserver");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "director.config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static DirectorCliConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new DirectorCliConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<DirectorCliConfig>(json, JsonOpts) ?? new DirectorCliConfig();
        }
        catch
        {
            return new DirectorCliConfig();
        }
    }

    public static void Save(DirectorCliConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOpts);
        File.WriteAllText(ConfigPath, json);
    }

    public static string GetConfigPath() => ConfigPath;
}

internal sealed class DirectorCliConfig
{
    public string? DefaultBaseUrl { get; set; }
}
