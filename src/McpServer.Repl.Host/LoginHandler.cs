using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel.Client;
using McpServer.Client;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace McpServer.Repl.Host;

/// <summary>
/// Handles interactive login against the MCP Server's OIDC authority.
/// Persists tokens to <c>~/.mcpserver/tokens.json</c> (shared with Director).
/// On startup, loads cached token; if expired, refreshes; if no token, auto-starts device flow.
/// Falls back to manual login menu only when device flow fails.
/// </summary>
public class LoginHandler
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly McpServerClient _client;

    // File-based token cache (shared with Director at ~/.mcpserver/tokens.json)
    private static readonly string s_cacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".mcpserver");
    private static readonly string s_cachePath = Path.Combine(s_cacheDir, "tokens.json");

    // In-memory cached credentials for automatic token refresh within session
    private string? _cachedTokenEndpoint;
    private string? _cachedClientId;
    private string? _cachedScopes;
    private string? _cachedUsername;
    private string? _cachedPassword;
    private string? _cachedClientSecret;
    private string? _cachedRefreshToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;
    private bool _isClientCredentials;

    /// <summary>Initializes a new instance of the <see cref="LoginHandler"/> class.</summary>
    public LoginHandler(ILogger<LoginHandler> logger, McpServerClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <summary>Gets the current username if logged in, or null.</summary>
    public string? CurrentUser { get; private set; }

    /// <summary>Gets whether the user is currently authenticated with a non-expired bearer token.</summary>
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(_client.BearerToken) && !IsTokenExpired;

    /// <summary>Gets whether the cached token has expired (with a 30-second buffer).</summary>
    private bool IsTokenExpired => _tokenExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(30);

    /// <summary>Gets the time remaining on the current token, or null if not logged in.</summary>
    public TimeSpan? TokenTimeRemaining =>
        _tokenExpiresAt > DateTimeOffset.UtcNow ? _tokenExpiresAt - DateTimeOffset.UtcNow : null;

    // ── File-based token cache ──────────────────────────────────────────

    private void SaveTokenToFile()
    {
        try
        {
            Directory.CreateDirectory(s_cacheDir);
            var cached = new CachedToken
            {
                AccessToken = _client.BearerToken ?? "",
                RefreshToken = _cachedRefreshToken ?? "",
                ExpiresAtUtc = _tokenExpiresAt.UtcDateTime,
                Authority = "",
                TokenEndpoint = _cachedTokenEndpoint ?? "",
                ClientId = _cachedClientId ?? "mcp-director",
            };
            var json = JsonSerializer.Serialize(cached, s_cacheJsonOpts);
            File.WriteAllText(s_cachePath, json);
            _logger.LogDebug("Token saved to {Path}", s_cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save token cache to {Path}", s_cachePath);
        }
    }

    private CachedToken? LoadTokenFromFile()
    {
        if (!File.Exists(s_cachePath))
            return null;

        try
        {
            var json = File.ReadAllText(s_cachePath);
            return JsonSerializer.Deserialize<CachedToken>(json, s_cacheJsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load token cache from {Path}", s_cachePath);
            return null;
        }
    }

    private void ClearTokenFile()
    {
        try
        {
            if (File.Exists(s_cachePath))
                File.Delete(s_cachePath);
        }
        catch { /* best effort */ }
    }

    /// <summary>
    /// Tries to restore a session from the file-based token cache.
    /// Returns true if a valid (or refreshed) token was loaded.
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken)
    {
        var cached = LoadTokenFromFile();
        if (cached is null || string.IsNullOrWhiteSpace(cached.AccessToken))
            return false;

        _client.BearerToken = cached.AccessToken;
        _tokenExpiresAt = new DateTimeOffset(cached.ExpiresAtUtc, TimeSpan.Zero);
        _cachedRefreshToken = cached.RefreshToken;
        _cachedTokenEndpoint = cached.TokenEndpoint;
        _cachedClientId = cached.ClientId;

        // Extract username from cached JWT
        CurrentUser = ExtractUsernameFromJwt(cached.AccessToken);

        if (!IsTokenExpired)
        {
            AnsiConsole.MarkupLine($"[green]Restored session for [bold]{Markup.Escape(CurrentUser ?? "authenticated")}[/][/]");
            AnsiConsole.MarkupLine($"[dim]Token expires at {_tokenExpiresAt.LocalDateTime:HH:mm:ss} ({TokenTimeRemaining?.TotalMinutes:F0}m remaining)[/]");
            return true;
        }

        // Token expired — try refresh
        _logger.LogInformation("Cached token expired, attempting refresh");
        AnsiConsole.MarkupLine("[yellow]Cached token expired, refreshing...[/]");

        if (await TryRefreshTokenAsync(cancellationToken))
        {
            SaveTokenToFile();
            return true;
        }

        // Refresh failed — clear stale cache
        _client.BearerToken = "";
        CurrentUser = null;
        _tokenExpiresAt = DateTimeOffset.MinValue;
        return false;
    }

    /// <summary>
    /// Ensures the bearer token is still valid. If expired, attempts automatic refresh.
    /// Call this before making API requests.
    /// </summary>
    public async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!IsLoggedIn && !string.IsNullOrWhiteSpace(_client.BearerToken))
        {
            _logger.LogInformation("Bearer token expired, attempting refresh");
            AnsiConsole.MarkupLine("[yellow]Token expired, refreshing...[/]");

            if (await TryRefreshTokenAsync(cancellationToken))
            {
                SaveTokenToFile();
                return true;
            }

            AnsiConsole.MarkupLine("[yellow]Token refresh failed. Please log in again.[/]");
            return false;
        }

        return IsLoggedIn;
    }

    /// <summary>
    /// Runs the automatic login flow:
    /// 1. Try cached token from file
    /// 2. Auto-start device flow if available
    /// 3. Fall back to manual login menu only if device flow fails
    /// </summary>
    public async Task<bool> LoginAsync(CancellationToken cancellationToken)
    {
        // Step 1: Try cached token
        if (await TryRestoreSessionAsync(cancellationToken))
            return true;

        // Step 2: Discover auth config
        AnsiConsole.MarkupLine("[blue]Discovering auth configuration...[/]");

        Client.Models.AuthConfigResponse authConfig;
        try
        {
            authConfig = await _client.AuthConfig.GetConfigAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to fetch auth config: {Markup.Escape(ex.Message)}[/]");
            _logger.LogWarning(ex, "Failed to fetch auth config");
            return false;
        }

        if (!authConfig.Enabled)
        {
            AnsiConsole.MarkupLine("[yellow]Authentication is not enabled on this server.[/]");
            AnsiConsole.MarkupLine("[dim]Continuing without authentication.[/]");
            return false;
        }

        AnsiConsole.MarkupLine($"[green]Authority:[/] {Markup.Escape(authConfig.Authority ?? "")}");
        AnsiConsole.WriteLine();

        // Step 3: Auto-start device flow if available
        if (!string.IsNullOrWhiteSpace(authConfig.DeviceAuthorizationEndpoint))
        {
            AnsiConsole.MarkupLine("[blue]Starting device authorization flow...[/]");
            if (await DeviceFlowLoginAsync(authConfig, cancellationToken))
                return true;

            // Device flow failed — fall through to manual menu
            AnsiConsole.MarkupLine("[yellow]Device flow did not complete. Select an alternative login method.[/]");
            AnsiConsole.WriteLine();
        }

        // Step 4: Manual login menu (fallback)
        return await ManualLoginMenuAsync(authConfig, cancellationToken);
    }

    /// <summary>
    /// Shows the manual login method selection menu.
    /// Called when device flow fails or is unavailable.
    /// </summary>
    public async Task<bool> ManualLoginMenuAsync(
        Client.Models.AuthConfigResponse? authConfig,
        CancellationToken cancellationToken)
    {
        if (authConfig is null)
        {
            try
            {
                authConfig = await _client.AuthConfig.GetConfigAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to fetch auth config: {Markup.Escape(ex.Message)}[/]");
                return false;
            }

            if (!authConfig.Enabled)
            {
                AnsiConsole.MarkupLine("[yellow]Authentication is not enabled on this server.[/]");
                return false;
            }
        }

        var choices = new List<string>();
        if (!string.IsNullOrWhiteSpace(authConfig.DeviceAuthorizationEndpoint))
            choices.Add("Device Flow");
        choices.AddRange(["Password Login", "Client Credentials", "Skip Login"]);

        var loginMethod = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Select login method:[/]")
                .AddChoices(choices));

        return loginMethod switch
        {
            "Device Flow" => await DeviceFlowLoginAsync(authConfig, cancellationToken),
            "Password Login" => await PasswordLoginAsync(authConfig, cancellationToken),
            "Client Credentials" => await ClientCredentialsLoginAsync(authConfig, cancellationToken),
            _ => false
        };
    }

    /// <summary>Clears the current authentication state, in-memory and file caches.</summary>
    public void Logout()
    {
        _client.Logout();
        CurrentUser = null;
        ClearCachedCredentials();
        ClearTokenFile();
        AnsiConsole.MarkupLine("[yellow]Logged out. Token cache cleared.[/]");
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_cachedTokenEndpoint))
            return false;

        try
        {
            using var httpClient = new HttpClient();

            // Try refresh token first
            if (!string.IsNullOrWhiteSpace(_cachedRefreshToken))
            {
                var refreshResponse = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = _cachedTokenEndpoint,
                    ClientId = _cachedClientId ?? "mcp-director",
                    RefreshToken = _cachedRefreshToken,
                }, cancellationToken);

                if (!refreshResponse.IsError)
                {
                    ApplyTokenResponse(refreshResponse);
                    _logger.LogInformation("Token refreshed via refresh_token for {User}", CurrentUser);
                    AnsiConsole.MarkupLine("[green]Token refreshed.[/]");
                    return true;
                }

                _logger.LogDebug("Refresh token failed: {Error}", refreshResponse.Error);
            }

            // Fall back to re-authentication with cached credentials
            if (_isClientCredentials && !string.IsNullOrWhiteSpace(_cachedClientSecret))
            {
                var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(
                    new ClientCredentialsTokenRequest
                    {
                        Address = _cachedTokenEndpoint,
                        ClientId = _cachedClientId ?? "mcp-agent",
                        ClientSecret = _cachedClientSecret,
                        Scope = _cachedScopes ?? "mcp-api",
                    }, cancellationToken);

                if (!tokenResponse.IsError)
                {
                    ApplyTokenResponse(tokenResponse);
                    _logger.LogInformation("Token refreshed via client_credentials for {ClientId}", _cachedClientId);
                    AnsiConsole.MarkupLine("[green]Token refreshed.[/]");
                    return true;
                }
            }
            else if (!string.IsNullOrWhiteSpace(_cachedUsername) && !string.IsNullOrWhiteSpace(_cachedPassword))
            {
                var tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
                {
                    Address = _cachedTokenEndpoint,
                    ClientId = _cachedClientId ?? "mcp-director",
                    Scope = _cachedScopes ?? "openid profile email",
                    UserName = _cachedUsername,
                    Password = _cachedPassword,
                }, cancellationToken);

                if (!tokenResponse.IsError)
                {
                    ApplyTokenResponse(tokenResponse);
                    _logger.LogInformation("Token refreshed via password grant for {User}", _cachedUsername);
                    AnsiConsole.MarkupLine("[green]Token refreshed.[/]");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
        }

        return false;
    }

    private void ApplyTokenResponse(TokenResponse tokenResponse)
    {
        _client.BearerToken = tokenResponse.AccessToken!;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
            _cachedRefreshToken = tokenResponse.RefreshToken;

        SaveTokenToFile();
    }

    private void ClearCachedCredentials()
    {
        _cachedTokenEndpoint = null;
        _cachedClientId = null;
        _cachedScopes = null;
        _cachedUsername = null;
        _cachedPassword = null;
        _cachedClientSecret = null;
        _cachedRefreshToken = null;
        _tokenExpiresAt = DateTimeOffset.MinValue;
        _isClientCredentials = false;
    }

    private async Task<bool> PasswordLoginAsync(
        Client.Models.AuthConfigResponse authConfig,
        CancellationToken cancellationToken)
    {
        var username = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Username:[/]")
                .PromptStyle("yellow"));

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Password:[/]")
                .PromptStyle("red")
                .Secret());

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[blue]Authenticating...[/]", async ctx =>
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var tokenEndpoint = authConfig.TokenEndpoint!;
                    var clientId = authConfig.ClientId ?? "mcp-director";
                    var scopes = authConfig.Scopes ?? "openid profile email";

                    var tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest
                    {
                        Address = tokenEndpoint,
                        ClientId = clientId,
                        Scope = scopes,
                        UserName = username,
                        Password = password,
                    }, cancellationToken);

                    if (tokenResponse.IsError)
                    {
                        AnsiConsole.MarkupLine($"[red]Login failed: {Markup.Escape(tokenResponse.Error ?? "unknown error")}[/]");
                        if (!string.IsNullOrWhiteSpace(tokenResponse.ErrorDescription))
                            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(tokenResponse.ErrorDescription)}[/]");
                        _logger.LogWarning("Password login failed: {Error} {Description}",
                            tokenResponse.Error, tokenResponse.ErrorDescription);
                        return false;
                    }

                    _cachedTokenEndpoint = tokenEndpoint;
                    _cachedClientId = clientId;
                    _cachedScopes = scopes;
                    _cachedUsername = username;
                    _cachedPassword = password;
                    _isClientCredentials = false;

                    ApplyTokenResponse(tokenResponse);
                    CurrentUser = username;

                    AnsiConsole.MarkupLine($"[green]Logged in as [bold]{Markup.Escape(username)}[/][/]");
                    AnsiConsole.MarkupLine($"[dim]Token expires at {_tokenExpiresAt.LocalDateTime:HH:mm:ss} ({tokenResponse.ExpiresIn}s)[/]");
                    _logger.LogInformation("Password login successful for user {User}, expires at {ExpiresAt}",
                        username, _tokenExpiresAt);
                    return true;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Login error: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Password login error");
                    return false;
                }
            });
    }

    private async Task<bool> ClientCredentialsLoginAsync(
        Client.Models.AuthConfigResponse authConfig,
        CancellationToken cancellationToken)
    {
        var clientId = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Client ID:[/]")
                .DefaultValue("mcp-agent")
                .PromptStyle("yellow"));

        var clientSecret = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Client Secret:[/]")
                .PromptStyle("red")
                .Secret());

        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("[blue]Authenticating...[/]", async ctx =>
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var tokenEndpoint = authConfig.TokenEndpoint!;

                    var scopes = authConfig.Scopes?.Split(' ')
                        .Where(s => s != "openid" && s != "profile" && s != "email")
                        .FirstOrDefault() ?? "mcp-api";

                    var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(
                        new ClientCredentialsTokenRequest
                        {
                            Address = tokenEndpoint,
                            ClientId = clientId,
                            ClientSecret = clientSecret,
                            Scope = scopes,
                        }, cancellationToken);

                    if (tokenResponse.IsError)
                    {
                        AnsiConsole.MarkupLine($"[red]Login failed: {Markup.Escape(tokenResponse.Error ?? "unknown error")}[/]");
                        if (!string.IsNullOrWhiteSpace(tokenResponse.ErrorDescription))
                            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(tokenResponse.ErrorDescription)}[/]");
                        _logger.LogWarning("Client credentials login failed: {Error} {Description}",
                            tokenResponse.Error, tokenResponse.ErrorDescription);
                        return false;
                    }

                    _cachedTokenEndpoint = tokenEndpoint;
                    _cachedClientId = clientId;
                    _cachedScopes = scopes;
                    _cachedClientSecret = clientSecret;
                    _isClientCredentials = true;

                    ApplyTokenResponse(tokenResponse);
                    CurrentUser = $"{clientId} (service)";

                    AnsiConsole.MarkupLine($"[green]Authenticated as [bold]{Markup.Escape(clientId)}[/] (client credentials)[/]");
                    AnsiConsole.MarkupLine($"[dim]Token expires at {_tokenExpiresAt.LocalDateTime:HH:mm:ss} ({tokenResponse.ExpiresIn}s)[/]");
                    _logger.LogInformation("Client credentials login successful for {ClientId}, expires at {ExpiresAt}",
                        clientId, _tokenExpiresAt);
                    return true;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Login error: {Markup.Escape(ex.Message)}[/]");
                    _logger.LogError(ex, "Client credentials login error");
                    return false;
                }
            });
    }

    private async Task<bool> DeviceFlowLoginAsync(
        Client.Models.AuthConfigResponse authConfig,
        CancellationToken cancellationToken)
    {
        var deviceEndpoint = authConfig.DeviceAuthorizationEndpoint!;
        var tokenEndpoint = authConfig.TokenEndpoint!;
        var clientId = authConfig.ClientId ?? "mcp-director";
        var scopes = authConfig.Scopes ?? "openid profile email";

        try
        {
            using var httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["scope"] = scopes,
            });

            var deviceResponse = await httpClient.PostAsync(deviceEndpoint, content, cancellationToken);
            if (!deviceResponse.IsSuccessStatusCode)
            {
                var body = await deviceResponse.Content.ReadAsStringAsync(cancellationToken);
                AnsiConsole.MarkupLine($"[red]Device authorization failed: {Markup.Escape(body)}[/]");
                return false;
            }

            var deviceJson = await deviceResponse.Content.ReadAsStringAsync(cancellationToken);
            var device = JsonSerializer.Deserialize<DeviceAuthResponse>(deviceJson, s_jsonOpts);
            if (device is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to parse device authorization response.[/]");
                return false;
            }

            var targetUrl = device.VerificationUriComplete ?? device.VerificationUri;

            var panel = new Panel(
                new Rows(
                    new Markup($"[bold yellow]User Code:[/] [bold white on blue] {Markup.Escape(device.UserCode)} [/]"),
                    new Markup(""),
                    new Markup($"[blue]Go to:[/] [link]{Markup.Escape(targetUrl)}[/]"),
                    new Markup(""),
                    new Markup("[dim]Enter the code above in your browser to complete login.[/]"),
                    new Markup("[dim]Waiting for authentication...[/]")))
            {
                Header = new PanelHeader("[bold]Device Authorization[/]"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1),
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            try
            {
                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
                AnsiConsole.MarkupLine("[dim]Browser opened automatically.[/]");
            }
            catch
            {
                AnsiConsole.MarkupLine("[dim]Could not open browser automatically. Please navigate manually.[/]");
            }

            var interval = device.Interval > 0 ? device.Interval : 5;
            var deadline = DateTime.UtcNow.AddSeconds(device.ExpiresIn > 0 ? device.ExpiresIn : 300);

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);

                var pollContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    ["client_id"] = clientId,
                    ["device_code"] = device.DeviceCode,
                });

                var pollResponse = await httpClient.PostAsync(tokenEndpoint, pollContent, cancellationToken);
                var pollJson = await pollResponse.Content.ReadAsStringAsync(cancellationToken);
                var tokenResult = JsonSerializer.Deserialize<DeviceTokenResponse>(pollJson, s_jsonOpts);

                if (tokenResult is null)
                    continue;

                if (!string.IsNullOrEmpty(tokenResult.AccessToken) && string.IsNullOrEmpty(tokenResult.Error))
                {
                    _client.BearerToken = tokenResult.AccessToken;
                    _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResult.ExpiresIn);
                    _cachedRefreshToken = tokenResult.RefreshToken;
                    _cachedTokenEndpoint = tokenEndpoint;
                    _cachedClientId = clientId;
                    _cachedScopes = scopes;
                    _isClientCredentials = false;

                    var username = ExtractUsernameFromJwt(tokenResult.AccessToken);
                    CurrentUser = username ?? "authenticated";

                    SaveTokenToFile();

                    AnsiConsole.MarkupLine($"[green]Logged in as [bold]{Markup.Escape(CurrentUser)}[/] (device flow)[/]");
                    AnsiConsole.MarkupLine($"[dim]Token expires at {_tokenExpiresAt.LocalDateTime:HH:mm:ss} ({tokenResult.ExpiresIn}s)[/]");
                    _logger.LogInformation("Device flow login successful for {User}, expires at {ExpiresAt}",
                        CurrentUser, _tokenExpiresAt);
                    return true;
                }

                if (tokenResult.Error == "authorization_pending")
                    continue;

                if (tokenResult.Error == "slow_down")
                {
                    interval += 5;
                    continue;
                }

                AnsiConsole.MarkupLine($"[red]Device flow error: {Markup.Escape(tokenResult.Error ?? "unknown")}[/]");
                if (!string.IsNullOrWhiteSpace(tokenResult.ErrorDescription))
                    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(tokenResult.ErrorDescription)}[/]");
                return false;
            }

            AnsiConsole.MarkupLine("[red]Device authorization flow timed out.[/]");
            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Device flow error: {Markup.Escape(ex.Message)}[/]");
            _logger.LogError(ex, "Device flow login error");
            return false;
        }
    }

    private static string? ExtractUsernameFromJwt(string accessToken)
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

    private static readonly JsonSerializerOptions s_jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions s_cacheJsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // ── DTOs ────────────────────────────────────────────────────────────

    private sealed class CachedToken
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAtUtc { get; set; }
        public string Authority { get; set; } = "";
        public string TokenEndpoint { get; set; } = "";
        public string ClientId { get; set; } = "mcp-director";
    }

    private sealed class DeviceAuthResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; } = "";
        [JsonPropertyName("user_code")]
        public string UserCode { get; set; } = "";
        [JsonPropertyName("verification_uri")]
        public string VerificationUri { get; set; } = "";
        [JsonPropertyName("verification_uri_complete")]
        public string? VerificationUriComplete { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")]
        public int Interval { get; set; }
    }

    private sealed class DeviceTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }
}
