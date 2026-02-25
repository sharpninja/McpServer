using System.Text.Json;

namespace McpServer.Director.Auth;

/// <summary>
/// Cached OAuth token data, persisted to <c>~/.mcpserver/tokens.json</c>.
/// </summary>
internal sealed class CachedToken
{
    /// <summary>JWT access token.</summary>
    public string AccessToken { get; set; } = "";

    /// <summary>Refresh token for obtaining new access tokens.</summary>
    public string RefreshToken { get; set; } = "";

    /// <summary>UTC timestamp when the access token expires.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Keycloak authority this token was issued by.</summary>
    public string Authority { get; set; } = "";

    /// <summary>Whether the access token has expired (with 30-second buffer).</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc.AddSeconds(-30);
}

/// <summary>
/// Manages reading and writing cached OAuth tokens to <c>~/.mcpserver/tokens.json</c>.
/// </summary>
internal static class TokenCache
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".mcpserver");

    private static readonly string CachePath = Path.Combine(CacheDir, "tokens.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Loads the cached token, or returns null if none exists.</summary>
    public static CachedToken? Load()
    {
        if (!File.Exists(CachePath))
            return null;

        try
        {
            var json = File.ReadAllText(CachePath);
            return JsonSerializer.Deserialize<CachedToken>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Saves a token to the cache file.</summary>
    public static void Save(CachedToken token)
    {
        Directory.CreateDirectory(CacheDir);
        var json = JsonSerializer.Serialize(token, JsonOpts);
        File.WriteAllText(CachePath, json);
    }

    /// <summary>Deletes the cached token file.</summary>
    public static void Clear()
    {
        if (File.Exists(CachePath))
            File.Delete(CachePath);
    }

    /// <summary>Returns the cache file path for display purposes.</summary>
    public static string GetCachePath() => CachePath;
}
