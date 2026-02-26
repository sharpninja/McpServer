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

    /// <summary>OIDC token endpoint used to mint/refresh this token (may be an MCP proxy endpoint).</summary>
    public string TokenEndpoint { get; set; } = "";

    /// <summary>OIDC client id used to mint/refresh this token.</summary>
    public string ClientId { get; set; } = "mcp-director";

    /// <summary>Whether the access token has expired (with 30-second buffer).</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc.AddSeconds(-30);
}

/// <summary>
/// Manages reading and writing cached OAuth tokens to <c>~/.mcpserver/tokens.json</c>.
/// </summary>
internal static class TokenCache
{
    private static readonly string s_cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".mcpserver");

    private static readonly string s_cachePath = Path.Combine(s_cacheDir, "tokens.json");

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Loads the cached token, or returns null if none exists.</summary>
    public static CachedToken? Load()
    {
        if (!File.Exists(s_cachePath))
            return null;

        try
        {
            var json = File.ReadAllText(s_cachePath);
            return JsonSerializer.Deserialize<CachedToken>(json, s_jsonOpts);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Saves a token to the cache file.</summary>
    public static void Save(CachedToken token)
    {
        Directory.CreateDirectory(s_cacheDir);
        var json = JsonSerializer.Serialize(token, s_jsonOpts);
        File.WriteAllText(s_cachePath, json);
    }

    /// <summary>Deletes the cached token file.</summary>
    public static void Clear()
    {
        if (File.Exists(s_cachePath))
            File.Delete(s_cachePath);
    }

    /// <summary>Returns the cache file path for display purposes.</summary>
    public static string GetCachePath() => s_cachePath;
}
