using McpServer.Client;
using McpServer.Director.Auth;
using System.Text.Json;

namespace McpServer.Director;

/// <summary>
/// Director runtime connection context:
/// - Control-plane connection (default URL / admin host) for workspace management and health.
/// - Active workspace connection selected by the user for workspace-scoped tabs.
/// </summary>
internal sealed class DirectorMcpContext : IDisposable
{
    private readonly object _gate = new();
    private McpHttpClient? _controlClient;
    private McpHttpClient? _activeWorkspaceClient;
    private McpServerClient? _controlApiClient;
    private McpServerClient? _activeWorkspaceApiClient;

    public DirectorMcpContext(McpHttpClient? controlClient, McpHttpClient? activeWorkspaceClient)
    {
        _controlClient = controlClient;
        _activeWorkspaceClient = activeWorkspaceClient;
        _controlApiClient = CreateTypedClient(controlClient);
        _activeWorkspaceApiClient = CreateTypedClient(activeWorkspaceClient);
        ActiveWorkspacePath = activeWorkspaceClient?.WorkspacePath;
        RefreshBearerTokens();
    }

    /// <summary>Raised when the active workspace context changes.</summary>
    public event EventHandler? ActiveWorkspaceChanged;

    /// <summary>Current control-plane HTTP client (default/admin host), if available.</summary>
    public McpHttpClient? ControlClient
    {
        get
        {
            lock (_gate)
                return _controlClient;
        }
    }

    /// <summary>Current active workspace HTTP client, if available.</summary>
    public McpHttpClient? ActiveWorkspaceClient
    {
        get
        {
            lock (_gate)
                return _activeWorkspaceClient;
        }
    }

    /// <summary>The active workspace path selected in the Director UI.</summary>
    public string? ActiveWorkspacePath { get; private set; }

    public bool HasControlConnection => ControlClient is not null;

    public bool HasActiveWorkspaceConnection => ActiveWorkspaceClient is not null;

    /// <summary>Applies the currently cached bearer token to available HTTP clients.</summary>
    public void RefreshBearerTokens()
    {
        lock (_gate)
        {
            _controlClient?.TrySetCachedBearerToken();
            _activeWorkspaceClient?.TrySetCachedBearerToken();
            ApplyCachedBearerTokenToTypedClients_NoLock();
        }
    }

    /// <summary>Switches the active workspace context to the specified workspace root path.</summary>
    public bool TrySetActiveWorkspace(string workspacePath, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            error = "Workspace path is required.";
            return false;
        }

        // Single-port model: reuse control client's base URL, change only the workspace path header.
        McpHttpClient? controlRef;
        lock (_gate)
            controlRef = _controlClient;

        McpHttpClient newClient;
        if (controlRef is not null)
        {
            // Reuse control connection base URL + API key; only workspace path changes.
            newClient = new McpHttpClient(controlRef.BaseUrl, controlRef.ApiKey ?? string.Empty, workspacePath);
            newClient.TrySetCachedBearerToken();
        }
        else
        {
            // Fallback: read marker file from target workspace for bootstrap
            var markerClient = McpHttpClient.FromMarkerOnly(workspacePath);
            if (markerClient is null)
            {
                error = $"Workspace marker not found at '{workspacePath}'.";
                return false;
            }
            markerClient.TrySetCachedBearerToken();
            newClient = markerClient;
        }

        lock (_gate)
        {
            if (_activeWorkspaceClient is not null &&
                string.Equals(_activeWorkspaceClient.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_activeWorkspaceClient.BaseUrl, newClient.BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                if (!ReferenceEquals(_activeWorkspaceClient, newClient))
                    newClient.Dispose();
                return true;
            }
        }

        var typed = CreateTypedClient(newClient);

        McpHttpClient? oldHttp = null;
        lock (_gate)
        {
            oldHttp = _activeWorkspaceClient;
            _activeWorkspaceClient = newClient;
            _activeWorkspaceApiClient = typed;
            ActiveWorkspacePath = workspacePath;
        }

        if (!ReferenceEquals(oldHttp, newClient))
            oldHttp?.Dispose();

        ActiveWorkspaceChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task<McpServerClient> GetRequiredControlApiClientAsync(CancellationToken cancellationToken = default)
    {
        McpServerClient? client;
        lock (_gate)
            client = _controlApiClient;

        if (client is null)
            throw new InvalidOperationException(
                "No control-plane connection is available. Configure a default URL with 'director config set-default-url <url>' " +
                "or run Director from a workspace with AGENTS-README-FIRST.yaml.");

        TrySeedControlApiKeyFromPrimaryWorkspaceMarker(client);
        await EnsureInitializedAsync(client, cancellationToken).ConfigureAwait(false);
        return client;
    }

    public async Task<McpServerClient> GetRequiredActiveWorkspaceApiClientAsync(CancellationToken cancellationToken = default)
    {
        McpServerClient? client;
        lock (_gate)
            client = _activeWorkspaceApiClient;

        if (client is null)
            throw new InvalidOperationException(
                "No active workspace is selected. Select a workspace from the bottom workspace picker " +
                "or run Director from a workspace with AGENTS-README-FIRST.yaml.");

        await EnsureInitializedAsync(client, cancellationToken).ConfigureAwait(false);
        return client;
    }

    public McpHttpClient GetRequiredControlHttpClient()
        => ControlClient
           ?? throw new InvalidOperationException(
               "No control-plane connection is available. Configure a default URL with 'director config set-default-url <url>' " +
               "or run Director from a workspace with AGENTS-README-FIRST.yaml.");

    public McpHttpClient GetRequiredActiveWorkspaceHttpClient()
        => ActiveWorkspaceClient
           ?? throw new InvalidOperationException(
               "No active workspace is selected. Select a workspace from the bottom workspace picker " +
               "or run Director from a workspace with AGENTS-README-FIRST.yaml.");

    private static async Task EnsureInitializedAsync(McpServerClient client, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(client.ApiKey))
            return;
        if (!string.IsNullOrWhiteSpace(client.BearerToken))
            return;

        await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private static McpServerClient? CreateTypedClient(McpHttpClient? client)
    {
        if (client is null)
            return null;

        return McpServerClientFactory.Create(new McpServerClientOptions
        {
            BaseUrl = new Uri(client.BaseUrl),
            ApiKey = string.IsNullOrWhiteSpace(client.ApiKey) ? null : client.ApiKey,
            BearerToken = TryGetCachedBearerTokenValue(),
            WorkspacePath = client.WorkspacePath,
            Timeout = TimeSpan.FromMinutes(10),
        });
    }

    private void ApplyCachedBearerTokenToTypedClients_NoLock()
    {
        var bearerToken = TryGetCachedBearerTokenValue() ?? string.Empty;

        if (_controlApiClient is not null)
            _controlApiClient.BearerToken = bearerToken;

        if (_activeWorkspaceApiClient is not null)
            _activeWorkspaceApiClient.BearerToken = bearerToken;
    }

    private static string? TryGetCachedBearerTokenValue()
    {
        _ = McpHttpClient.TryRefreshCachedToken();
        var cached = TokenCache.Load();
        if (cached is null || cached.IsExpired || string.IsNullOrWhiteSpace(cached.AccessToken))
            return null;

        return cached.AccessToken;
    }

    private void TrySeedControlApiKeyFromPrimaryWorkspaceMarker(McpServerClient client)
    {
        if (!string.IsNullOrWhiteSpace(client.ApiKey))
            return;

        var controlBaseUrl = ControlClient?.BaseUrl;
        if (string.IsNullOrWhiteSpace(controlBaseUrl))
            return;

        try
        {
            const string appSettingsPath = @"C:\ProgramData\McpServer\appsettings.json";
            if (!File.Exists(appSettingsPath))
                return;

            using var doc = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            if (!doc.RootElement.TryGetProperty("Mcp", out var mcp) ||
                !mcp.TryGetProperty("Workspaces", out var workspaces) ||
                workspaces.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            string? primaryWorkspacePath = null;
            foreach (var ws in workspaces.EnumerateArray())
            {
                if (ws.TryGetProperty("IsPrimary", out var isPrimary) && isPrimary.ValueKind == JsonValueKind.True &&
                    ws.TryGetProperty("WorkspacePath", out var pathProp) && pathProp.ValueKind == JsonValueKind.String)
                {
                    primaryWorkspacePath = pathProp.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(primaryWorkspacePath))
                return;

            var markerClient = McpHttpClient.FromMarkerOnly(primaryWorkspacePath);
            if (markerClient is null || string.IsNullOrWhiteSpace(markerClient.ApiKey))
                return;

            if (!string.Equals(
                    markerClient.BaseUrl.TrimEnd('/'),
                    controlBaseUrl.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            client.ApiKey = markerClient.ApiKey;
        }
        catch
        {
            // Best-effort local bootstrap only; normal default-key initialization remains available.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _controlClient?.Dispose();
            _controlClient = null;
            _activeWorkspaceClient?.Dispose();
            _activeWorkspaceClient = null;
            _controlApiClient = null;
            _activeWorkspaceApiClient = null;
        }
    }
}
