using McpServer.Client;
using McpServer.UI.Core.ViewModels;
using McpServer.Web.Authorization;

namespace McpServer.Web;

internal sealed class WebMcpContext
{
    private readonly object _gate = new();
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly McpServerClient _controlApiClient;
    private readonly McpServerClient _activeWorkspaceApiClient;
    private readonly BearerTokenAccessor _bearerTokenAccessor;
    private string? _apiKey;

    public WebMcpContext(
        IConfiguration configuration,
        WorkspaceContextViewModel workspaceContext,
        BearerTokenAccessor bearerTokenAccessor)
    {
        _workspaceContext = workspaceContext;
        _bearerTokenAccessor = bearerTokenAccessor;

        var baseUrl = configuration["McpServer:BaseUrl"] ?? "http://localhost:7147";
        _apiKey = configuration["McpServer:ApiKey"];
        var configuredWorkspacePath = NormalizeWorkspacePath(configuration["McpServer:WorkspacePath"]);
        ActiveWorkspacePath = configuredWorkspacePath;
        BaseUrl = new Uri(baseUrl, UriKind.Absolute);

        _controlApiClient = CreateTypedClient(BaseUrl, _apiKey, workspacePath: null);
        _activeWorkspaceApiClient = CreateTypedClient(BaseUrl, _apiKey, ActiveWorkspacePath);

        if (string.IsNullOrWhiteSpace(_workspaceContext.ActiveWorkspacePath))
            _workspaceContext.ActiveWorkspacePath = configuredWorkspacePath;
        else
            TrySetActiveWorkspace(_workspaceContext.ActiveWorkspacePath);

        _workspaceContext.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                TrySetActiveWorkspace(_workspaceContext.ActiveWorkspacePath, updateViewModel: false);
        };
    }

    public Uri BaseUrl { get; }

    public string? ActiveWorkspacePath { get; private set; }

    public bool TrySetActiveWorkspace(string? workspacePath, bool updateViewModel = true)
    {
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);

        lock (_gate)
        {
            ActiveWorkspacePath = normalizedWorkspacePath;
            _activeWorkspaceApiClient.WorkspacePath = normalizedWorkspacePath ?? string.Empty;
        }

        if (updateViewModel &&
            !string.Equals(_workspaceContext.ActiveWorkspacePath, normalizedWorkspacePath, StringComparison.Ordinal))
        {
            _workspaceContext.ActiveWorkspacePath = normalizedWorkspacePath;
        }

        return true;
    }

    public Task<McpServerClient> GetApiClientAsync(CancellationToken cancellationToken = default)
        => GetRequiredActiveWorkspaceApiClientAsync(cancellationToken);

    public async Task<McpServerClient> GetRequiredControlApiClientAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(_controlApiClient, cancellationToken).ConfigureAwait(false);
        return _controlApiClient;
    }

    public async Task<McpServerClient> GetRequiredActiveWorkspaceApiClientAsync(CancellationToken cancellationToken = default)
    {
        // Always pull from the singleton source of truth — WorkspaceContextViewModel owns the active workspace.
        var contextPath = _workspaceContext.ActiveWorkspacePath;
        if (!string.IsNullOrWhiteSpace(contextPath) &&
            !string.Equals(_activeWorkspaceApiClient.WorkspacePath, contextPath, StringComparison.Ordinal))
        {
            lock (_gate)
            {
                _activeWorkspaceApiClient.WorkspacePath = contextPath;
                ActiveWorkspacePath = contextPath;
            }
        }

        await EnsureInitializedAsync(_activeWorkspaceApiClient, cancellationToken).ConfigureAwait(false);
        return _activeWorkspaceApiClient;
    }

    private async Task EnsureInitializedAsync(McpServerClient client, CancellationToken cancellationToken)
    {
        // If the user is authenticated, prefer their OIDC access token over the static API key.
        var bearerToken = await _bearerTokenAccessor.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            lock (_gate)
            {
                _controlApiClient.BearerToken = bearerToken;
                _activeWorkspaceApiClient.BearerToken = bearerToken;
            }
            return;
        }

        // Already initialized with a static API key or previously discovered key.
        if (!string.IsNullOrWhiteSpace(client.ApiKey) || !string.IsNullOrWhiteSpace(client.BearerToken))
            return;

        // Auto-discover the API key from the McpServer AGENTS-README-FIRST.yaml initialization endpoint.
        var apiKey = await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
        lock (_gate)
        {
            _apiKey = apiKey;
            if (string.IsNullOrWhiteSpace(_controlApiClient.ApiKey))
                _controlApiClient.ApiKey = apiKey;
            if (string.IsNullOrWhiteSpace(_activeWorkspaceApiClient.ApiKey))
                _activeWorkspaceApiClient.ApiKey = apiKey;
        }
    }

    private static McpServerClient CreateTypedClient(Uri baseUrl, string? apiKey, string? workspacePath)
    {
        return McpServerClientFactory.Create(new McpServerClientOptions
        {
            BaseUrl = baseUrl,
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey,
            WorkspacePath = workspacePath,
            Timeout = TimeSpan.FromMinutes(10),
        });
    }

    private static string? NormalizeWorkspacePath(string? workspacePath)
        => string.IsNullOrWhiteSpace(workspacePath) ? null : workspacePath.Trim();
}
