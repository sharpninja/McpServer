using McpServer.Client;
using McpServer.UI.Core.ViewModels;

namespace McpServer.Web;

internal sealed class WebMcpContext
{
    private readonly object _gate = new();
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly McpServerClient _controlApiClient;
    private readonly McpServerClient _activeWorkspaceApiClient;
    private string? _apiKey;

    public WebMcpContext(IConfiguration configuration, WorkspaceContextViewModel workspaceContext)
    {
        _workspaceContext = workspaceContext;
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
        await EnsureInitializedAsync(_activeWorkspaceApiClient, cancellationToken).ConfigureAwait(false);
        return _activeWorkspaceApiClient;
    }

    private async Task EnsureInitializedAsync(McpServerClient client, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(client.ApiKey) || !string.IsNullOrWhiteSpace(client.BearerToken))
            return;

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
