using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Connection info emitted when a connection/auth flow completes.
/// </summary>
public sealed record ConnectionEstablishedInfo(string BaseUrl, string? ApiKey, string? BearerToken = null);

/// <summary>
/// Connection/authentication ViewModel for host applications.
/// </summary>
public partial class ConnectionViewModel : ViewModelBase
{
    private readonly ILogger<ConnectionViewModel> _logger;
    private readonly IConnectionAuthService _connectionAuthService;

    [ObservableProperty]
    private string _host = "10.0.2.2";

    [ObservableProperty]
    private string _port = "7147";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isOidcSignInRequired;

    [ObservableProperty]
    private string _oidcStatusMessage = "";

    [ObservableProperty]
    private string _oidcUserCode = "";

    [ObservableProperty]
    private string _oidcVerificationUrl = "";

    [ObservableProperty]
    private bool _oidcCanOpenBrowser;

    private Func<string, bool>? _externalUrlOpener;
    private Func<string?>? _cachedOidcTokenReader;
    private Action<string?>? _cachedOidcTokenWriter;
    private Func<bool>? _oidcPostTokenForegroundActivator;
    private Func<Task<string?>>? _qrCodeScanner;
    private string? _oidcBearerToken;
    private string? _lastOidcAuthority;
    private string? _lastMcpBaseUrl;
    private string? _lastOidcClientId;
    private CancellationTokenSource? _connectCts;
    private static readonly TimeSpan CachedJwtExpirySkew = TimeSpan.FromMinutes(1);

    [ObservableProperty]
    private bool _canScanQrCode;

    /// <summary>Raised when the user completes connect (and auth, if required).</summary>
    public event Action<ConnectionEstablishedInfo>? Connected;

    /// <summary>
    /// Creates a new connection/authentication ViewModel.
    /// </summary>
    public ConnectionViewModel(
        IConnectionAuthService connectionAuthService,
        ILogger<ConnectionViewModel>? logger = null)
    {
        _connectionAuthService = connectionAuthService ?? throw new ArgumentNullException(nameof(connectionAuthService));
        _logger = logger ?? NullLogger<ConnectionViewModel>.Instance;
    }

    /// <summary>
    /// Configures URL opener used to launch external browser for OIDC.
    /// </summary>
    public void SetExternalUrlOpener(Func<string, bool>? externalUrlOpener)
    {
        _externalUrlOpener = externalUrlOpener;
        OidcCanOpenBrowser = !string.IsNullOrWhiteSpace(OidcVerificationUrl) && _externalUrlOpener != null;
        _logger.LogInformation("External URL opener set: {HasOpener}", _externalUrlOpener != null);
    }

    /// <summary>
    /// Configures cached token read/write accessors.
    /// </summary>
    public void SetOidcTokenCacheAccessors(Func<string?>? readCachedToken, Action<string?>? writeCachedToken)
    {
        _cachedOidcTokenReader = readCachedToken;
        _cachedOidcTokenWriter = writeCachedToken;
        _logger.LogInformation(
            "OIDC token cache accessors configured. HasReader={HasReader}, HasWriter={HasWriter}",
            _cachedOidcTokenReader != null,
            _cachedOidcTokenWriter != null);
    }

    /// <summary>
    /// Configures callback used to bring app to foreground after auth.
    /// </summary>
    public void SetOidcPostTokenForegroundActivator(Func<bool>? foregroundActivator)
    {
        _oidcPostTokenForegroundActivator = foregroundActivator;
        _logger.LogInformation(
            "OIDC post-token foreground activator configured: {HasActivator}",
            _oidcPostTokenForegroundActivator != null);
    }

    /// <summary>
    /// Configures optional QR code scanner used to populate host/port.
    /// </summary>
    public void SetQrCodeScanner(Func<Task<string?>>? scanner)
    {
        _qrCodeScanner = scanner;
        CanScanQrCode = scanner != null;
        _logger.LogInformation("QR code scanner configured: {HasScanner}", scanner != null);
    }

    /// <summary>
    /// Scans a QR code and updates host/port when successful.
    /// </summary>
    protected async Task ScanQrCodeAsync()
    {
        if (_qrCodeScanner == null) return;
        try
        {
            var result = await _qrCodeScanner().ConfigureAwait(true);
            if (!string.IsNullOrWhiteSpace(result))
            {
                // If the scanned value is a URL, extract just the host
                if (Uri.TryCreate(result, UriKind.Absolute, out var uri))
                {
                    Host = uri.Host;
                    if (uri.Port > 0 && uri.Port != 80 && uri.Port != 443)
                        Port = uri.Port.ToString();
                }
                else
                {
                    Host = result.Trim();
                }
                ErrorMessage = "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "QR code scan failed");
            ErrorMessage = $"QR scan failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Logs out current session and immediately retries connection.
    /// </summary>
    protected async Task LogoutAndRetryAsync()
    {
        _logger.LogInformation("LogoutAndRetryAsync invoked");
        await PerformOidcLogoutAsync().ConfigureAwait(true);
        await ConnectAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Logs out current session.
    /// </summary>
    protected async Task LogoutAsync()
    {
        _logger.LogInformation("LogoutAsync invoked");
        await PerformOidcLogoutAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Cancels an in-progress connect/auth flow.
    /// </summary>
    protected void CancelConnect()
    {
        _logger.LogInformation("CancelConnect invoked — aborting in-progress OIDC flow");
        _connectCts?.Cancel();
        _connectCts = null;
        IsConnecting = false;
        IsOidcSignInRequired = false;
        OidcStatusMessage = "";
        TryBringAppToForegroundAfterOidcTokenAcquired();
    }

    private async Task PerformOidcLogoutAsync()
    {
        ErrorMessage = "";
        var token = _oidcBearerToken ?? TryReadCachedOidcToken();

        if (_lastMcpBaseUrl != null && _lastOidcAuthority != null)
        {
            _logger.LogInformation("Performing OIDC logout via revocation/end-session API");
            var success = await _connectionAuthService.TryLogoutAsync(
                _lastMcpBaseUrl,
                _lastOidcAuthority,
                _lastOidcClientId,
                token).ConfigureAwait(true);
            _logger.LogInformation("OIDC API logout result: {Success}", success);
        }
        else
        {
            _logger.LogInformation("No OIDC authority/baseUrl cached; skipping Keycloak SSO logout");
        }

        ClearCachedOidcToken();
        _oidcBearerToken = null;
        TryBringAppToForegroundAfterOidcTokenAcquired();
    }

    /// <summary>
    /// Starts connection and authentication flow.
    /// </summary>
    protected async Task ConnectAsync()
    {
        _logger.LogInformation("ConnectAsync invoked. Host='{Host}', Port='{Port}', IsConnecting={IsConnecting}", Host, Port, IsConnecting);

        ErrorMessage = "";
        OidcStatusMessage = "";
        OidcUserCode = "";
        OidcVerificationUrl = "";
        OidcCanOpenBrowser = false;
        IsOidcSignInRequired = false;

        if (string.IsNullOrWhiteSpace(Host))
        {
            ErrorMessage = "Host is required.";
            _logger.LogWarning("ConnectAsync validation failed: host missing");
            return;
        }

        if (string.IsNullOrWhiteSpace(Port) || !int.TryParse(Port.Trim(), out var portNumber) || portNumber < 1 || portNumber > 65535)
        {
            ErrorMessage = "Port must be between 1 and 65535.";
            _logger.LogWarning("ConnectAsync validation failed: invalid port '{Port}'", Port);
            return;
        }

        var scheme = portNumber switch
        {
            443  => "https",
            80 or 8080 => "http",
            _    => "http"
        };
        var url = $"{scheme}://{Host.Trim()}:{portNumber}";
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            ErrorMessage = "Invalid host or port.";
            _logger.LogWarning("ConnectAsync validation failed: invalid URL '{Url}'", url);
            return;
        }

        if (IsConnecting)
        {
            _logger.LogInformation("ConnectAsync ignored because connect is already in progress");
            return;
        }

        IsConnecting = true;
        _connectCts?.Cancel();
        _connectCts = new CancellationTokenSource();
        var ct = _connectCts.Token;
        _logger.LogInformation("ConnectAsync started for {Url}", url);

        try
        {
            // Verify the server is reachable and resolve any HTTP→HTTPS redirects.
            try
            {
                url = await _connectionAuthService.ProbeHealthAndResolveUrlAsync(url, ct).ConfigureAwait(true);
            }
            catch (Exception probeEx)
            {
                throw new InvalidOperationException($"Server unreachable at {url}: {probeEx.Message}", probeEx);
            }

            var authToken = await TryAuthenticateWithOidcAsync(url, ct).ConfigureAwait(true);
            _logger.LogInformation("ConnectAsync auth stage complete for {Url}. TokenPresent={HasToken}, BearerTokenPresent={HasBearer}", url, !string.IsNullOrWhiteSpace(authToken), !string.IsNullOrWhiteSpace(_oidcBearerToken));
            Connected?.Invoke(new ConnectionEstablishedInfo(url, authToken, _oidcBearerToken));
            _logger.LogInformation("ConnectAsync raised Connected event for {Url}", url);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("ConnectAsync cancelled by user for {Url}", url);
            IsConnecting = false;
            IsOidcSignInRequired = false;
            TryBringAppToForegroundAfterOidcTokenAcquired();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsConnecting = false;
            IsOidcSignInRequired = false;

            // Close the OIDC WebView (if open) so the user returns to the connection screen.
            TryBringAppToForegroundAfterOidcTokenAcquired();

            _logger.LogError(ex, "ConnectAsync failed for {Url}", url);
        }
    }

    /// <summary>
    /// Opens the OIDC verification URL in an external browser.
    /// </summary>
    protected void OpenOidcVerificationUrl()
    {
        if (string.IsNullOrWhiteSpace(OidcVerificationUrl))
        {
            _logger.LogWarning("OpenOidcVerificationUrl ignored: URL is empty");
            return;
        }

        if (_externalUrlOpener == null)
        {
            ErrorMessage = "No browser launcher is available on this device.";
            _logger.LogWarning("OpenOidcVerificationUrl failed: no browser launcher available");
            return;
        }

        _logger.LogInformation("Opening OIDC verification URL via external browser: {Url}", OidcVerificationUrl);
        if (!_externalUrlOpener.Invoke(OidcVerificationUrl))
        {
            ErrorMessage = "Could not open the sign-in page.";
            _logger.LogWarning("OpenOidcVerificationUrl failed to launch browser");
        }
    }

    private async Task<string?> TryAuthenticateWithOidcAsync(string mcpBaseUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking MCP auth config at {BaseUrl}/auth/config", mcpBaseUrl);
        var authConfig = await _connectionAuthService.TryGetAuthConfigAsync(mcpBaseUrl, cancellationToken).ConfigureAwait(true);
        _logger.LogInformation(
            "MCP auth config result: enabled={Enabled}, clientId={ClientId}, hasDeviceEndpoint={HasDeviceEndpoint}, hasTokenEndpoint={HasTokenEndpoint}",
            authConfig?.Enabled,
            authConfig?.ClientId ?? "<null>",
            !string.IsNullOrWhiteSpace(authConfig?.DeviceAuthorizationEndpoint),
            !string.IsNullOrWhiteSpace(authConfig?.TokenEndpoint));
        if (!_connectionAuthService.IsEnabled(authConfig))
        {
            _logger.LogInformation("OIDC not enabled/configured for {BaseUrl}; continuing without interactive auth", mcpBaseUrl);
            _oidcBearerToken = null;
            return await TryFetchDefaultApiKeyFallbackAsync(mcpBaseUrl).ConfigureAwait(true);
        }

        var cachedOidcToken = TryReadCachedOidcToken();
        if (!string.IsNullOrWhiteSpace(cachedOidcToken) &&
            IsJwtExpiredOrNearExpiry(cachedOidcToken, CachedJwtExpirySkew, out var expiresAtUtc))
        {
            OidcStatusMessage = "Session expired. Sign in again.";
            _logger.LogWarning(
                "Cached OIDC token is expired/near expiry (expUtc={ExpiresAtUtc}); clearing cache and requiring sign-in",
                expiresAtUtc?.ToString("O") ?? "<unknown>");
            ClearCachedOidcToken();
            cachedOidcToken = null;
            _oidcBearerToken = null;
        }

        if (!string.IsNullOrWhiteSpace(cachedOidcToken))
        {
            OidcStatusMessage = "Reusing previous sign-in…";
            _logger.LogInformation("Attempting cached OIDC token reuse for {BaseUrl}", mcpBaseUrl);

            var cachedApiKeyResult = await _connectionAuthService
                .TryFetchMcpApiKeyAsync(mcpBaseUrl, cachedOidcToken, cancellationToken)
                .ConfigureAwait(true);

            if (cachedApiKeyResult.IsSuccess)
            {
                OidcStatusMessage = "Opening MCP…";
                _logger.LogInformation("Cached OIDC token reuse succeeded; MCP API key acquired");
                _oidcBearerToken = cachedOidcToken;
                return cachedApiKeyResult.ApiKey;
            }

            if (cachedApiKeyResult.WasRejected)
            {
                OidcStatusMessage = "Session expired. Sign in again.";
                _logger.LogWarning("Cached OIDC token was rejected by server; clearing cache and falling back to interactive sign-in");
                ClearCachedOidcToken();
            }
            else
            {
                _logger.LogInformation("Cached OIDC token reuse did not yield an MCP API key; falling back to interactive sign-in");
            }
        }

        // NOTE: We intentionally do NOT try /api-key before OIDC here.
        // The default API key from /api-key only works for the primary workspace.
        // When OIDC is enabled, we need a Bearer token for cross-workspace auth.
        _logger.LogInformation("OIDC enabled for {BaseUrl}; starting device authorization", mcpBaseUrl);
        _lastOidcAuthority = authConfig!.Authority;
        _lastMcpBaseUrl = mcpBaseUrl;
        _lastOidcClientId = authConfig!.ClientId;
        var prompt = await _connectionAuthService
            .StartDeviceAuthorizationAsync(authConfig!, mcpBaseUrl, cancellationToken)
            .ConfigureAwait(true);

        IsOidcSignInRequired = true;
        OidcUserCode = prompt.UserCode;
        OidcVerificationUrl = string.IsNullOrWhiteSpace(prompt.VerificationUriComplete)
            ? prompt.VerificationUri
            : prompt.VerificationUriComplete!;
        OidcCanOpenBrowser = !string.IsNullOrWhiteSpace(OidcVerificationUrl) && _externalUrlOpener != null;
        OidcStatusMessage = "Sign in to the identity provider and approve this device.";
        _logger.LogInformation(
            "OIDC device prompt ready. UserCodePresent={HasUserCode}, VerificationUrl='{VerificationUrl}', CanOpenBrowser={CanOpenBrowser}",
            !string.IsNullOrWhiteSpace(OidcUserCode),
            OidcVerificationUrl,
            OidcCanOpenBrowser);

        if (OidcCanOpenBrowser && _externalUrlOpener != null)
        {
            _logger.LogInformation("Auto-opening OIDC verification page");
            _ = _externalUrlOpener.Invoke(OidcVerificationUrl);
        }

        var token = await _connectionAuthService
            .PollForAccessTokenAsync(
                authConfig!,
                prompt,
                mcpBaseUrl,
                status =>
                {
                    OidcStatusMessage = status;
                    _logger.LogInformation("OIDC status update: {Status}", status);
                },
                cancellationToken)
            .ConfigureAwait(true);

        _logger.LogInformation("OIDC sign-in complete. Access token acquired for {BaseUrl}", mcpBaseUrl);
        _oidcBearerToken = token.AccessToken;
        WriteCachedOidcToken(token.AccessToken);
        TryBringAppToForegroundAfterOidcTokenAcquired();

        OidcStatusMessage = "Sign-in complete. Acquiring MCP API key…";
        _logger.LogInformation("Fetching MCP default API key from {BaseUrl}/api-key after OIDC sign-in", mcpBaseUrl);
        var mcpApiKeyResult = await _connectionAuthService
            .TryFetchMcpApiKeyAsync(mcpBaseUrl, token.AccessToken, cancellationToken)
            .ConfigureAwait(true);

        if (mcpApiKeyResult.IsSuccess)
        {
            OidcStatusMessage = "Sign-in complete. Opening MCP…";
            _logger.LogInformation("Fetched MCP default API key after OIDC sign-in; proceeding to main view");
            return mcpApiKeyResult.ApiKey;
        }

        if (mcpApiKeyResult.WasRejected)
        {
            _logger.LogWarning("Fresh OIDC token was rejected while fetching MCP API key; clearing cached token");
            ClearCachedOidcToken();
        }

        // Bearer-authenticated key fetch failed. Try without bearer — the /api-key
        // endpoint is unprotected so this should succeed even when the OIDC token
        // is not accepted by the server for API key retrieval.
        OidcStatusMessage = "Sign-in complete. Opening MCP…";
        _logger.LogWarning("Bearer-authenticated /api-key fetch failed; trying default key without bearer token");
        var postOidcDefaultKey = await TryFetchDefaultApiKeyFallbackAsync(mcpBaseUrl).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(postOidcDefaultKey))
            return postOidcDefaultKey;

        _logger.LogWarning("Could not acquire any MCP API key for {BaseUrl}; proceeding without explicit key", mcpBaseUrl);
        return null;
    }

    /// <summary>
    /// Fetches the default (anonymous) API key from the server's unprotected <c>/api-key</c>
    /// endpoint without sending any bearer token. Returns null on failure.
    /// </summary>
    private async Task<string?> TryFetchDefaultApiKeyFallbackAsync(string mcpBaseUrl)
    {
        try
        {
            var result = await _connectionAuthService
                .TryFetchMcpApiKeyAsync(mcpBaseUrl, bearerAccessToken: null)
                .ConfigureAwait(true);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.ApiKey))
            {
                _logger.LogInformation("Default API key fetched from /api-key without bearer auth for {BaseUrl}", mcpBaseUrl);
                return result.ApiKey;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch default API key from /api-key for {BaseUrl}", mcpBaseUrl);
        }

        return null;
    }

    private string? TryReadCachedOidcToken()
    {
        if (_cachedOidcTokenReader == null)
            return null;

        try
        {
            var token = _cachedOidcTokenReader.Invoke();
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogInformation("No cached OIDC token available");
                return null;
            }

            _logger.LogInformation("Cached OIDC token found");
            return token.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cached OIDC token");
            return null;
        }
    }

    private void WriteCachedOidcToken(string? token)
    {
        if (_cachedOidcTokenWriter == null)
            return;

        try
        {
            _cachedOidcTokenWriter.Invoke(string.IsNullOrWhiteSpace(token) ? null : token.Trim());
            _logger.LogInformation("OIDC token cache updated. TokenPresent={HasToken}", !string.IsNullOrWhiteSpace(token));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update cached OIDC token");
        }
    }

    private void ClearCachedOidcToken() => WriteCachedOidcToken(null);

    private void TryBringAppToForegroundAfterOidcTokenAcquired()
    {
        if (_oidcPostTokenForegroundActivator == null)
            return;

        try
        {
            var success = _oidcPostTokenForegroundActivator.Invoke();
            _logger.LogInformation("Requested app foreground after OIDC token acquisition. Success={Success}", success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed requesting app foreground after OIDC token acquisition");
        }
    }

    private bool IsJwtExpiredOrNearExpiry(
        string jwtToken,
        TimeSpan skew,
        out DateTimeOffset? expiresAtUtc)
    {
        expiresAtUtc = null;
        if (string.IsNullOrWhiteSpace(jwtToken))
            return true;

        return _connectionAuthService.IsJwtExpiredOrNearExpiry(jwtToken, skew, out expiresAtUtc);
    }
}
