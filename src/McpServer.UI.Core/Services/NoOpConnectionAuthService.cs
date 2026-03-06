using System;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op implementation used when host does not provide auth service.
/// </summary>
public sealed class NoOpConnectionAuthService : IConnectionAuthService
{
    /// <inheritdoc />
    public Task<string> ProbeHealthAndResolveUrlAsync(string url, CancellationToken cancellationToken = default)
        => Task.FromResult(url);

    /// <inheritdoc />
    public Task<ConnectionAuthConfig?> TryGetAuthConfigAsync(string mcpBaseUrl, CancellationToken cancellationToken = default)
        => Task.FromResult<ConnectionAuthConfig?>(null);

    /// <inheritdoc />
    public bool IsEnabled(ConnectionAuthConfig? config)
        => false;

    /// <inheritdoc />
    public Task<bool> TryLogoutAsync(
        string mcpBaseUrl,
        string? authority,
        string? clientId,
        string? accessToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    /// <inheritdoc />
    public Task<ConnectionDeviceAuthorizationPrompt> StartDeviceAuthorizationAsync(
        ConnectionAuthConfig authConfig,
        string mcpBaseUrl,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ConnectionDeviceAuthorizationPrompt
        {
            DeviceCode = string.Empty,
            UserCode = string.Empty,
            VerificationUri = string.Empty,
            VerificationUriComplete = null,
            ExpiresInSeconds = 0,
            PollIntervalSeconds = 0
        });

    /// <inheritdoc />
    public Task<ConnectionDeviceTokenResult> PollForAccessTokenAsync(
        ConnectionAuthConfig authConfig,
        ConnectionDeviceAuthorizationPrompt prompt,
        string mcpBaseUrl,
        Action<string>? statusCallback = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ConnectionDeviceTokenResult
        {
            AccessToken = string.Empty,
            ExpiresInSeconds = null,
            TokenType = null
        });

    /// <inheritdoc />
    public Task<ConnectionApiKeyFetchResult> TryFetchMcpApiKeyAsync(
        string mcpBaseUrl,
        string? bearerAccessToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ConnectionApiKeyFetchResult
        {
            ApiKey = null,
            WasRejected = false
        });

    /// <inheritdoc />
    public bool IsJwtExpiredOrNearExpiry(string jwtToken, TimeSpan skew, out DateTimeOffset? expiresAtUtc)
    {
        expiresAtUtc = null;
        return false;
    }
}
