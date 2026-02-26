using System.Net.Http.Json;
using System.Text.Json;

namespace McpServer.Director;

/// <summary>
/// HTTP client for communicating with the MCP server REST API.
/// Reads connection details from AGENTS-README-FIRST.yaml in the workspace, with optional
/// fallback to Director CLI defaults when no marker exists.
/// </summary>
internal sealed class McpHttpClient : IDisposable
{
    private readonly HttpClient _http;

    /// <summary>Base URL of the MCP server.</summary>
    public string BaseUrl { get; }

    /// <summary>API key for authentication.</summary>
    public string ApiKey { get; }

    /// <summary>Workspace path.</summary>
    public string WorkspacePath { get; }

    /// <summary>Creates a new client from explicit connection details.</summary>
    public McpHttpClient(string baseUrl, string apiKey, string workspacePath)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        ApiKey = apiKey;
        WorkspacePath = workspacePath;
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        if (!string.IsNullOrWhiteSpace(ApiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        if (!string.IsNullOrWhiteSpace(WorkspacePath))
            _http.DefaultRequestHeaders.Add("X-Workspace-Path", WorkspacePath);
    }

    /// <summary>
    /// Sets a Bearer token for JWT authentication on mutation endpoints.
    /// The token is sent alongside the API key.
    /// </summary>
    public void SetBearerToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Attempts to load and set a cached Bearer token from the token cache.
    /// If the token is expired but a refresh token is available, attempts to refresh it.
    /// Returns true if a valid token was set.
    /// </summary>
    public bool TrySetCachedBearerToken()
    {
        var cached = Auth.TokenCache.Load();
        if (cached is null)
            return false;

        if (!cached.IsExpired)
        {
            SetBearerToken(cached.AccessToken);
            return true;
        }

        // Token expired — try to refresh synchronously
        if (string.IsNullOrWhiteSpace(cached.RefreshToken))
            return false;

        try
        {
            var refreshed = RefreshTokenSync(cached);
            if (refreshed is not null)
            {
                SetBearerToken(refreshed.AccessToken);
                return true;
            }
        }
        catch
        {
            // Refresh failed — user needs to re-login
        }

        return false;
    }

    /// <summary>
    /// Attempts to refresh the cached token (if expired and refreshable) and persist it back to the token cache.
    /// Returns true when a non-expired cached token exists after the call (either already valid or successfully refreshed).
    /// </summary>
    internal static bool TryRefreshCachedToken()
    {
        var cached = Auth.TokenCache.Load();
        if (cached is null)
            return false;

        if (!cached.IsExpired)
            return true;

        if (string.IsNullOrWhiteSpace(cached.RefreshToken))
            return false;

        try
        {
            return RefreshTokenSync(cached) is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Synchronously refreshes an expired token using the refresh token.
    /// Returns the updated CachedToken or null on failure.
    /// </summary>
    private static Auth.CachedToken? RefreshTokenSync(Auth.CachedToken cached)
    {
        using var http = new HttpClient();
        var tokenEndpoint = ResolveTokenEndpointForRefresh(cached);
        var clientId = string.IsNullOrWhiteSpace(cached.ClientId) ? "mcp-director" : cached.ClientId;

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["refresh_token"] = cached.RefreshToken,
        });

        var response = http.PostAsync(tokenEndpoint, content).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
            return null;

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var atProp))
            return null;

        var newToken = new Auth.CachedToken
        {
            AccessToken = atProp.GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var rtProp)
                ? rtProp.GetString() ?? cached.RefreshToken
                : cached.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(
                root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 300),
            Authority = cached.Authority,
            TokenEndpoint = tokenEndpoint,
            ClientId = clientId,
        };

        Auth.TokenCache.Save(newToken);
        return newToken;
    }

    /// <summary>
    /// Resolves the best token endpoint for refresh. Prefers the cached endpoint (which may be the MCP proxy
    /// <c>/auth/token</c>) and falls back to the authority-derived Keycloak endpoint for older cached tokens.
    /// </summary>
    private static string ResolveTokenEndpointForRefresh(Auth.CachedToken cached)
    {
        if (!string.IsNullOrWhiteSpace(cached.TokenEndpoint))
            return cached.TokenEndpoint;

        var discovered = TryDiscoverProxyTokenEndpointForRefresh(cached.Authority);
        if (!string.IsNullOrWhiteSpace(discovered))
            return discovered;

        // Back-compat for tokens cached before proxy endpoint persistence was added.
        return $"{cached.Authority.TrimEnd('/')}/protocol/openid-connect/token";
    }

    private static string? TryDiscoverProxyTokenEndpointForRefresh(string expectedAuthority)
    {
        try
        {
            using var client = FromDefaultUrlOrMarker();
            if (client is null)
                return null;

            var config = client.GetAuthConfigAsync().GetAwaiter().GetResult();
            if (config is null || !config.Enabled || string.IsNullOrWhiteSpace(config.TokenEndpoint))
                return null;

            if (!string.IsNullOrWhiteSpace(expectedAuthority) &&
                !string.IsNullOrWhiteSpace(config.Authority) &&
                !string.Equals(
                    config.Authority.TrimEnd('/'),
                    expectedAuthority.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return config.TokenEndpoint;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>GET request returning deserialized JSON.</summary>
    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
    }

    /// <summary>GET request returning raw string.</summary>
    public async Task<string> GetStringAsync(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    /// <summary>POST request with JSON body returning deserialized JSON.</summary>
    public async Task<T?> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOpts, ct).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
    }

    /// <summary>POST request with JSON body returning raw response.</summary>
    public async Task<HttpResponseMessage> PostRawAsync(string path, object? body = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOpts, ct).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(response, ct).ConfigureAwait(false);
        return response;
    }

    /// <summary>DELETE request returning deserialized JSON.</summary>
    public async Task<T?> DeleteAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync(path, ct).ConfigureAwait(false);
        await EnsureSuccessOrThrowAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new HttpRequestException(
            $"HTTP {(int)response.StatusCode} {response.StatusCode}: {(string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body)}");
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    /// <summary>
    /// Discovers connection details from AGENTS-README-FIRST.yaml in the given directory
    /// (or current directory if not specified). If no marker exists, falls back to the
    /// Director CLI default base URL (if configured) with an empty API key.
    /// </summary>
    public static McpHttpClient? FromMarkerFile(string? directory = null)
    {
        var markerClient = FromMarkerOnly(directory);
        if (markerClient is not null)
            return markerClient;

        var cfg = DirectorCliConfigStore.Load();
        if (string.IsNullOrWhiteSpace(cfg.DefaultBaseUrl))
            return null;

        // No marker found — don't send CWD as workspace path; it's not a registered workspace.
        return new McpHttpClient(cfg.DefaultBaseUrl, apiKey: string.Empty, workspacePath: string.Empty);
    }

    /// <summary>
    /// Discovers connection details from AGENTS-README-FIRST.yaml in the given directory
    /// without falling back to Director defaults.
    /// </summary>
    public static McpHttpClient? FromMarkerOnly(string? directory = null)
    {
        var dir = directory ?? Directory.GetCurrentDirectory();
        var markerPath = Path.Combine(dir, "AGENTS-README-FIRST.yaml");
        if (!File.Exists(markerPath))
            return null;

        var lines = File.ReadAllLines(markerPath);
        string? baseUrl = null, apiKey = null, workspacePath = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("baseUrl:", StringComparison.OrdinalIgnoreCase))
                baseUrl = trimmed["baseUrl:".Length..].Trim();
            else if (trimmed.StartsWith("apiKey:", StringComparison.OrdinalIgnoreCase))
                apiKey = trimmed["apiKey:".Length..].Trim();
            else if (trimmed.StartsWith("workspacePath:", StringComparison.OrdinalIgnoreCase))
                workspacePath = trimmed["workspacePath:".Length..].Trim();
        }

        if (baseUrl is null || apiKey is null)
            return null;

        return new McpHttpClient(baseUrl, apiKey, workspacePath ?? dir);
    }

    /// <summary>
    /// Creates a control-plane connection using the configured default base URL when present,
    /// falling back to the detected workspace marker connection otherwise. When a default URL
    /// is configured, the connection intentionally starts without a seeded API key so the
    /// typed client can initialize against that server's own default key.
    /// </summary>
    public static McpHttpClient? FromDefaultUrlOrMarker(string? directory = null)
    {
        var dir = directory ?? Directory.GetCurrentDirectory();
        var markerClient = FromMarkerOnly(dir);
        var cfg = DirectorCliConfigStore.Load();
        if (!string.IsNullOrWhiteSpace(cfg.DefaultBaseUrl))
        {
            // Use marker workspace path if available; otherwise try the primary workspace
            // from the deployed appsettings. Never fall back to CWD — it may be the service
            // install directory (e.g. C:\ProgramData\McpServer), not a registered workspace.
            var workspacePath = markerClient?.WorkspacePath
                                ?? TryFromLocalPrimaryWorkspaceMarker()?.WorkspacePath
                                ?? string.Empty;
            return new McpHttpClient(
                cfg.DefaultBaseUrl,
                apiKey: string.Empty,
                workspacePath);
        }

        var primaryMarkerClient = TryFromLocalPrimaryWorkspaceMarker();
        if (primaryMarkerClient is not null)
            return primaryMarkerClient;

        return markerClient;
    }

    /// <summary>
    /// Best-effort local bootstrap for the control-plane connection. When running on a machine with a local
    /// McpServer Windows service install, prefer the primary workspace marker from the deployed appsettings.
    /// This avoids pointing control-plane tabs (e.g. Agents/Workspaces) at a child workspace host.
    /// </summary>
    private static McpHttpClient? TryFromLocalPrimaryWorkspaceMarker()
    {
        try
        {
            const string appSettingsPath = @"C:\ProgramData\McpServer\appsettings.json";
            if (!File.Exists(appSettingsPath))
                return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            if (!doc.RootElement.TryGetProperty("Mcp", out var mcp) ||
                !mcp.TryGetProperty("Workspaces", out var workspaces) ||
                workspaces.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var ws in workspaces.EnumerateArray())
            {
                if (ws.ValueKind != JsonValueKind.Object)
                    continue;

                if (!ws.TryGetProperty("IsPrimary", out var isPrimary) || isPrimary.ValueKind != JsonValueKind.True)
                    continue;

                if (!ws.TryGetProperty("WorkspacePath", out var pathProp) || pathProp.ValueKind != JsonValueKind.String)
                    continue;

                var workspacePath = pathProp.GetString();
                if (string.IsNullOrWhiteSpace(workspacePath))
                    continue;

                var client = FromMarkerOnly(workspacePath);
                if (client is not null)
                    return client;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Discovers OIDC auth configuration from the MCP server's <c>/auth/config</c> endpoint.
    /// This endpoint is unauthenticated and returns only public metadata.
    /// </summary>
    /// <returns>The auth config response, or null if the endpoint is unreachable.</returns>
    public async Task<AuthConfigResponse?> GetAuthConfigAsync(CancellationToken ct = default)
    {
        try
        {
            return await GetAsync<AuthConfigResponse>("/auth/config", ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}

/// <summary>
/// Public OIDC configuration response from the MCP server's <c>/auth/config</c> endpoint.
/// </summary>
internal sealed class AuthConfigResponse
{
    /// <summary>Whether OIDC authentication is enabled on this server.</summary>
    public bool Enabled { get; set; }

    /// <summary>Keycloak realm authority URL.</summary>
    public string Authority { get; set; } = "";

    /// <summary>Public client ID for the Director CLI (Device Authorization Flow).</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth scopes to request.</summary>
    public string Scopes { get; set; } = "";

    /// <summary>OAuth 2.0 Device Authorization endpoint.</summary>
    public string DeviceAuthorizationEndpoint { get; set; } = "";

    /// <summary>OAuth 2.0 Token endpoint.</summary>
    public string TokenEndpoint { get; set; } = "";
}
