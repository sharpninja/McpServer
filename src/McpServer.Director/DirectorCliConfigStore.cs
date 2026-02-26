using System.Text.Json;

namespace McpServer.Director;

/// <summary>
/// Persists Director CLI defaults (for non-workspace usage) under the user's profile.
/// </summary>
internal static class DirectorCliConfigStore
{
    private static readonly string s_configDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".mcpserver");

    private static readonly string s_configPath = Path.Combine(s_configDir, "director.config.json");

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static DirectorCliConfig Load()
    {
        if (!File.Exists(s_configPath))
            return new DirectorCliConfig();

        try
        {
            var json = File.ReadAllText(s_configPath);
            return JsonSerializer.Deserialize<DirectorCliConfig>(json, s_jsonOpts) ?? new DirectorCliConfig();
        }
        catch
        {
            return new DirectorCliConfig();
        }
    }

    public static void Save(DirectorCliConfig config)
    {
        Directory.CreateDirectory(s_configDir);
        var json = JsonSerializer.Serialize(config, s_jsonOpts);
        File.WriteAllText(s_configPath, json);
    }

    public static string GetConfigPath() => s_configPath;
}

internal sealed class DirectorCliConfig
{
    public string? DefaultBaseUrl { get; set; }
}
