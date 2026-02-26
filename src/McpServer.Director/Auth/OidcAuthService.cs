using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Auth;

/// <summary>
/// Result of a login attempt via the Device Authorization Flow.
/// </summary>
internal sealed record LoginResult(bool IsSuccess, string? Error = null, string? Username = null);

/// <summary>
/// Implements Keycloak OAuth 2.0 Device Authorization Grant flow for CLI authentication.
/// Manages token acquisition, caching, and refresh.
/// </summary>
internal sealed class OidcAuthService : IDisposable
{
    private readonly DirectorAuthOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger<OidcAuthService> _logger;


    /// <summary>Creates a new auth service with the given options.</summary>
    public OidcAuthService(DirectorAuthOptions options,
        ILogger<OidcAuthService>? logger = null)
    {
        _logger = logger ?? NullLogger<OidcAuthService>.Instance;
        _options = options;
        _http = new HttpClient();
    }

    /// <summary>
    /// Initiates the Device Authorization Flow. Displays a user code and verification URI,
    /// then polls until the user completes login or the flow times out.
    /// </summary>
    /// <param name="onUserCode">Callback invoked with (userCode, verificationUri, verificationUriComplete) for display.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Login result indicating success or failure.</returns>
    public async Task<LoginResult> LoginAsync(
        Action<string, string, string?> onUserCode,
        CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return new LoginResult(false, "Keycloak authority not configured. Set Mcp:Auth:Authority in appsettings.json or environment.");

        // Step 1: Request device authorization
        var deviceResponse = await RequestDeviceAuthorizationAsync(ct).ConfigureAwait(false);
        if (deviceResponse is null)
            return new LoginResult(false, "Failed to initiate device authorization flow.");

        // Display user code
        onUserCode(
            deviceResponse.UserCode,
            deviceResponse.VerificationUri,
            deviceResponse.VerificationUriComplete);

        // Step 2: Poll for token
        var interval = deviceResponse.Interval > 0 ? deviceResponse.Interval : _options.PollingIntervalSeconds;
        var deadline = DateTime.UtcNow.AddSeconds(_options.TimeoutSeconds);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);

            var tokenResult = await PollForTokenAsync(deviceResponse.DeviceCode, ct).ConfigureAwait(false);

            if (tokenResult.IsSuccess)
            {
                // Cache the token
                TokenCache.Save(new CachedToken
                {
                    AccessToken = tokenResult.AccessToken!,
                    RefreshToken = tokenResult.RefreshToken ?? "",
                    ExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn),
                    Authority = _options.Authority,
                    TokenEndpoint = _options.GetTokenEndpoint(),
                    ClientId = _options.ClientId,
                });

                var username = ExtractUsername(tokenResult.AccessToken!);
                return new LoginResult(true, Username: username);
            }

            if (tokenResult.Error == "authorization_pending")
                continue;

            if (tokenResult.Error == "slow_down")
            {
                interval += 5;
                continue;
            }

            // Any other error is terminal
            return new LoginResult(false, $"Device flow error: {tokenResult.Error} — {tokenResult.ErrorDescription}");
        }

        return new LoginResult(false, "Device authorization flow timed out. Please try again.");
    }

    /// <summary>
    /// Gets a valid access token, refreshing if necessary.
    /// Returns null if no token is cached or refresh fails.
    /// </summary>
    public async Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        var cached = TokenCache.Load();
        if (cached is null)
            return null;

        if (!cached.IsExpired)
            return cached.AccessToken;

        // Try to refresh
        if (string.IsNullOrWhiteSpace(cached.RefreshToken))
            return null;

        var refreshed = await RefreshTokenAsync(cached.RefreshToken, ct).ConfigureAwait(false);
        if (refreshed is null || !refreshed.IsSuccess)
        {
            TokenCache.Clear();
            return null;
        }

        TokenCache.Save(new CachedToken
        {
            AccessToken = refreshed.AccessToken!,
            RefreshToken = refreshed.RefreshToken ?? cached.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn),
            Authority = _options.Authority,
            TokenEndpoint = _options.GetTokenEndpoint(),
            ClientId = _options.ClientId,
        });

        return refreshed.AccessToken;
    }

    /// <summary>Clears cached tokens (logout).</summary>
    public static void Logout() => TokenCache.Clear();

    /// <summary>
    /// Returns info about the currently cached token, or null if not logged in.
    /// </summary>
    public static TokenInfo? GetCurrentUser()
    {
        var cached = TokenCache.Load();
        if (cached is null)
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(cached.AccessToken);
            return new TokenInfo
            {
                Username = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                    ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "unknown",
                Subject = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "",
                Email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                Roles = jwt.Claims.Where(c => c.Type == "realm_roles" || c.Type == "roles")
                    .Select(c => c.Value).ToList(),
                ExpiresAtUtc = cached.ExpiresAtUtc,
                IsExpired = cached.IsExpired,
                Authority = cached.Authority,
            };
        }
        catch
        {
            return new TokenInfo
            {
                Username = "(unable to parse token)",
                ExpiresAtUtc = cached.ExpiresAtUtc,
                IsExpired = cached.IsExpired,
                Authority = cached.Authority,
            };
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private async Task<DeviceAuthResponse?> RequestDeviceAuthorizationAsync(CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["scope"] = _options.Scopes,
        });

        try
        {
            var response = await _http.PostAsync(_options.GetDeviceAuthorizationEndpoint(), content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<DeviceAuthResponse>(json, s_jsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private async Task<TokenResponse> PollForTokenAsync(string deviceCode, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["client_id"] = _options.ClientId,
            ["device_code"] = deviceCode,
        });

        try
        {
            var response = await _http.PostAsync(_options.GetTokenEndpoint(), content, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TokenResponse>(json, s_jsonOpts) ?? new TokenResponse { Error = "deserialization_failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return new TokenResponse { Error = "network_error", ErrorDescription = ex.Message };
        }
    }

    private async Task<TokenResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["refresh_token"] = refreshToken,
        });

        try
        {
            var response = await _http.PostAsync(_options.GetTokenEndpoint(), content, ct).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TokenResponse>(json, s_jsonOpts);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractUsername(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(accessToken);
            return jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    // ── Response DTOs ────────────────────────────────────────────────────

    private sealed class DeviceAuthResponse
    {
        public string DeviceCode { get; set; } = "";
        public string UserCode { get; set; } = "";
        public string VerificationUri { get; set; } = "";
        public string? VerificationUriComplete { get; set; }
        public int ExpiresIn { get; set; }
        public int Interval { get; set; }
    }

    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public bool IsSuccess => !string.IsNullOrEmpty(AccessToken) && string.IsNullOrEmpty(Error);
    }
}

/// <summary>Information about the currently authenticated user.</summary>
internal sealed class TokenInfo
{
    /// <summary>Username from the JWT.</summary>
    public string Username { get; set; } = "";

    /// <summary>Subject (sub) claim.</summary>
    public string Subject { get; set; } = "";

    /// <summary>Email claim.</summary>
    public string? Email { get; set; }

    /// <summary>Realm roles from the JWT.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>When the token expires.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Whether the token is expired.</summary>
    public bool IsExpired { get; set; }

    /// <summary>Keycloak authority.</summary>
    public string Authority { get; set; } = "";
}
