using System;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Auth/probe service abstraction used by connection UI flows.
/// </summary>
public interface IConnectionAuthService
{
    /// <summary>Probes server health endpoint and returns resolved base URL.</summary>
    Task<string> ProbeHealthAndResolveUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>Fetches auth configuration from server when available.</summary>
    Task<ConnectionAuthConfig?> TryGetAuthConfigAsync(string mcpBaseUrl, CancellationToken cancellationToken = default);

    /// <summary>Returns true when auth config indicates OIDC/device flow is enabled.</summary>
    bool IsEnabled(ConnectionAuthConfig? config);

    /// <summary>Attempts OIDC logout/revocation.</summary>
    Task<bool> TryLogoutAsync(
        string mcpBaseUrl,
        string? authority,
        string? clientId,
        string? accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>Starts device authorization flow.</summary>
    Task<ConnectionDeviceAuthorizationPrompt> StartDeviceAuthorizationAsync(
        ConnectionAuthConfig authConfig,
        string mcpBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Polls for device-flow access token.</summary>
    Task<ConnectionDeviceTokenResult> PollForAccessTokenAsync(
        ConnectionAuthConfig authConfig,
        ConnectionDeviceAuthorizationPrompt prompt,
        string mcpBaseUrl,
        Action<string>? statusCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>Attempts to fetch MCP API key with optional bearer token.</summary>
    Task<ConnectionApiKeyFetchResult> TryFetchMcpApiKeyAsync(
        string mcpBaseUrl,
        string? bearerAccessToken,
        CancellationToken cancellationToken = default);

    /// <summary>Checks whether JWT has expired or is near expiry.</summary>
    bool IsJwtExpiredOrNearExpiry(string jwtToken, TimeSpan skew, out DateTimeOffset? expiresAtUtc);
}

/// <summary>
/// OIDC auth configuration.
/// </summary>
public sealed class ConnectionAuthConfig
{
    /// <summary>Whether OIDC is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>OIDC authority URL.</summary>
    public string? Authority { get; init; }

    /// <summary>OIDC client id.</summary>
    public string? ClientId { get; init; }

    /// <summary>Space-separated scopes.</summary>
    public string? Scopes { get; init; }

    /// <summary>Device authorization endpoint.</summary>
    public string? DeviceAuthorizationEndpoint { get; init; }

    /// <summary>Token endpoint.</summary>
    public string? TokenEndpoint { get; init; }
}

/// <summary>
/// Device authorization prompt payload.
/// </summary>
public sealed class ConnectionDeviceAuthorizationPrompt
{
    /// <summary>Device code.</summary>
    public required string DeviceCode { get; init; }

    /// <summary>User code.</summary>
    public required string UserCode { get; init; }

    /// <summary>Verification URI.</summary>
    public required string VerificationUri { get; init; }

    /// <summary>Verification URI including code when provided by server.</summary>
    public string? VerificationUriComplete { get; init; }

    /// <summary>Prompt lifetime in seconds.</summary>
    public int ExpiresInSeconds { get; init; }

    /// <summary>Polling interval in seconds.</summary>
    public int PollIntervalSeconds { get; init; }
}

/// <summary>
/// Device token polling result.
/// </summary>
public sealed class ConnectionDeviceTokenResult
{
    /// <summary>Access token.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Token lifetime in seconds.</summary>
    public int? ExpiresInSeconds { get; init; }

    /// <summary>Token type.</summary>
    public string? TokenType { get; init; }
}

/// <summary>
/// MCP API key fetch result.
/// </summary>
public sealed class ConnectionApiKeyFetchResult
{
    /// <summary>MCP API key when available.</summary>
    public string? ApiKey { get; init; }

    /// <summary>True when server explicitly rejected token/authorization.</summary>
    public bool WasRejected { get; init; }

    /// <summary>True when ApiKey is present.</summary>
    public bool IsSuccess => !string.IsNullOrWhiteSpace(ApiKey);
}
