using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Notifications;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011, TR-MCP-WS-003, TR-MCP-MT-001: Single-port workspace registry and marker file manager.
/// In the multi-tenant model, all workspaces are served by the primary host application.
/// This manager handles workspace lifecycle: token generation, marker file writes, and marker cleanup.
/// No child <see cref="WebApplication"/> instances are created.
/// </summary>
public sealed class WorkspaceProcessManager : IWorkspaceProcessManager, IDisposable
{
    private readonly ConcurrentDictionary<string, int> _activeWorkspaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<WorkspaceProcessManager> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<MarkerPromptOptions> _promptOptions;
    private readonly IMarkerPromptProvider _markerPromptProvider;
    private readonly WorkspaceTokenService _tokenService;
    private readonly ServerRuntimeInfo _serverRuntimeInfo;
    private readonly FederationRegistry? _federationRegistry;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly IChangeEventBus? _eventBus;

    private string? _primaryWorkspaceKey;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProcessManager"/> class.</summary>
    public WorkspaceProcessManager(
        ILogger<WorkspaceProcessManager> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        IOptionsMonitor<MarkerPromptOptions> promptOptions,
        IMarkerPromptProvider markerPromptProvider,
        WorkspaceTokenService tokenService,
        ServerRuntimeInfo serverRuntimeInfo,
        FederationRegistry? federationRegistry = null,
        IHttpClientFactory? httpClientFactory = null,
        IChangeEventBus? eventBus = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _promptOptions = promptOptions;
        _markerPromptProvider = markerPromptProvider;
        _tokenService = tokenService;
        _serverRuntimeInfo = serverRuntimeInfo;
        _federationRegistry = federationRegistry;
        _httpClientFactory = httpClientFactory;
        _eventBus = eventBus;
        _ = loggerFactory;
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StartAsync(WorkspaceDto workspace, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspace.WorkspacePath);
        var port = _serverRuntimeInfo.ListenPort;

        _logger.LogInformation(
            "Registering workspace: Name={WorkspaceName}; Path={WorkspacePath}; Port={Port}",
            workspace.Name, key, port);

        var globalTemplate = _promptOptions.CurrentValue.MarkerPromptTemplate
            ?? await _markerPromptProvider.GetGlobalPromptTemplateAsync(ct).ConfigureAwait(false);
        var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
        _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);

        var name = DeriveWorkspaceName(key);
        var agentAdditions = await GetAgentAdditionsAsync(key, ct).ConfigureAwait(false);
        var (overrideBaseUrl, upstreamApiKey) = await ResolveUpstreamConnectionAsync(key, workspace.Name ?? name, token, ct).ConfigureAwait(false);
        await MarkerFileService.WriteMarkerAsync(key, port, name, _logger, ct,
            globalTemplate, workspace.PromptTemplate, upstreamApiKey ?? token, workspace, agentAdditions,
            _serverRuntimeInfo.StartedAtUtc, overrideBaseUrl).ConfigureAwait(false);
        await PublishMarkerChangeSafeAsync(ChangeEventActions.Updated, key, ct).ConfigureAwait(false);

        _activeWorkspaces[key] = port;

        _logger.LogInformation("Workspace registered and marker written: {Path} (port {Port})", key, port);
        return new WorkspaceProcessStatus(true, Port: port);
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);
        _activeWorkspaces.TryRemove(key, out _);
        MarkerFileService.RemoveMarker(key, _logger);
        await PublishMarkerChangeSafeAsync(ChangeEventActions.Deleted, key, ct).ConfigureAwait(false);
        _logger.LogInformation("Workspace unregistered and marker removed: {Path}", key);
        return new WorkspaceProcessStatus(false);
    }

    /// <inheritdoc />
    public WorkspaceProcessStatus GetStatus(string workspacePath)
    {
        var key = NormalizeKey(workspacePath);
        if (_activeWorkspaces.TryGetValue(key, out var port))
            return new WorkspaceProcessStatus(true, Port: port, Uptime: DateTimeOffset.UtcNow - _serverRuntimeInfo.StartedAtUtc);
        return new WorkspaceProcessStatus(false);
    }

    /// <inheritdoc />
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        var keys = _activeWorkspaces.Keys.ToList();
        foreach (var key in keys)
            await StopAsync(key, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> RegenerateAllMarkersAsync(CancellationToken ct = default, string? globalPromptOverride = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var workspaces = await workspaceService.ListAsync(ct).ConfigureAwait(false);

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var fileTemplate = await _markerPromptProvider.GetGlobalPromptTemplateAsync(ct).ConfigureAwait(false);
        var globalTemplate = globalPromptOverride
            ?? config.GetSection("Mcp")["MarkerPromptTemplate"]
            ?? _promptOptions.CurrentValue.MarkerPromptTemplate
            ?? fileTemplate;

        var regenerated = 0;
        foreach (var ws in workspaces.Items)
        {
            if (!ws.IsEnabled) continue;
            if (string.IsNullOrWhiteSpace(ws.WorkspacePath)) continue;
            var key = NormalizeKey(ws.WorkspacePath);
            if (!_activeWorkspaces.ContainsKey(key)) continue;

            var name = DeriveWorkspaceName(key);
            var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
            _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);
            var agentAdditions = await GetAgentAdditionsAsync(key, ct).ConfigureAwait(false);
            var (overrideBaseUrl, upstreamApiKey) = await ResolveUpstreamConnectionAsync(key, ws.Name ?? name, token, ct).ConfigureAwait(false);
            await MarkerFileService.WriteMarkerAsync(key, _serverRuntimeInfo.ListenPort, name, _logger, ct,
                globalTemplate, ws.PromptTemplate, upstreamApiKey ?? token, ws, agentAdditions,
                _serverRuntimeInfo.StartedAtUtc, overrideBaseUrl).ConfigureAwait(false);
            await PublishMarkerChangeSafeAsync(ChangeEventActions.Updated, key, ct).ConfigureAwait(false);
            regenerated++;
        }

        _logger.LogInformation("Regenerated marker files for {Count} registered workspaces", regenerated);
        return regenerated;
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        string? currentWorkspaceName = null;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
            var workspaces = await workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);

            if (workspaces.TotalCount == 0)
            {
                _logger.LogInformation("No registered workspaces to register");
                return;
            }

            var primary = workspaces.Items
                .Where(w => w.IsPrimary && w.IsEnabled)
                .FirstOrDefault();
            primary ??= workspaces.Items
                .Where(w => w.IsEnabled)
                .FirstOrDefault();
            if (primary is not null)
                _primaryWorkspaceKey = NormalizeKey(primary.WorkspacePath);

            _logger.LogInformation("Registering {Count} workspace(s); primary = {Primary}",
                workspaces.TotalCount, primary?.Name ?? "(none)");

            foreach (var ws in workspaces.Items)
            {
                currentWorkspaceName = ws.Name;

                if (!ws.IsEnabled)
                {
                    _logger.LogInformation("  ⊘ {Name} skipped (disabled)", ws.Name);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ws.WorkspacePath))
                {
                    _logger.LogInformation("  ⊘ {Name} skipped (no workspace path)", ws.Name);
                    continue;
                }

                var status = await StartAsync(ws, cancellationToken).ConfigureAwait(false);
                if (status.IsRunning)
                    _logger.LogInformation("  ✓ {Name}{Primary}", ws.Name,
                        IsPrimaryWorkspace(NormalizeKey(ws.WorkspacePath)) ? " (primary)" : "");
                else
                    _logger.LogWarning("  ✗ {Name} failed: {Error}", ws.Name, status.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workspace registration for {Name}", currentWorkspaceName ?? "(unknown)");
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAllAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var key in _activeWorkspaces.Keys)
        {
            MarkerFileService.RemoveMarker(key, _logger);
            _ = PublishMarkerChangeSafeAsync(ChangeEventActions.Deleted, key, CancellationToken.None);
        }
        _activeWorkspaces.Clear();
    }

    private static string NormalizeKey(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private bool IsPrimaryWorkspace(string normalizedKey)
        => _primaryWorkspaceKey is not null
           && string.Equals(normalizedKey, _primaryWorkspaceKey, StringComparison.OrdinalIgnoreCase);

    private static string DeriveWorkspaceName(string normalizedKey)
        => Path.GetFileName(normalizedKey.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private async Task<IReadOnlyList<(string AgentId, string Content)>> GetAgentAdditionsAsync(string workspacePath, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();
        var agentConfigs = await agentService.ListWorkspaceAgentsAsync(workspacePath, ct).ConfigureAwait(false);
        return agentConfigs.Items
            .Where(x => !x.Banned && !string.IsNullOrWhiteSpace(x.MarkerAdditions))
            .Select(x => (x.AgentId, x.MarkerAdditions!))
            .ToList();
    }

    /// <summary>
    /// When federation is enabled and a default target is configured, attempts to fetch
    /// connection credentials from the upstream server's
    /// <c>GET /mcpserver/federation/connection</c> endpoint by workspace name.
    /// Workspace names are used instead of paths because paths may differ across machines.
    /// Returns the upstream's public base URL and workspace-specific API key so the marker
    /// file can point agents directly at the federated server.
    /// Falls back gracefully to <c>(null, null)</c> — indicating local credentials — when
    /// federation is disabled, no target is configured, the workspace is not found on the
    /// remote server (404), or any other error occurs.
    /// </summary>
    private async Task<(string? OverrideBaseUrl, string? UpstreamApiKey)> ResolveUpstreamConnectionAsync(
        string workspacePath, string workspaceName, string localApiKey, CancellationToken ct)
    {
        if (_federationRegistry is null || !_federationRegistry.IsEnabled)
            return (null, null);

        var target = _federationRegistry.ResolveTarget(workspacePath);
        if (target is null)
            return (null, null);

        if (_httpClientFactory is null)
        {
            _logger.LogWarning(
                "Federation is enabled for {WorkspacePath} but IHttpClientFactory is not available — using local credentials.",
                workspacePath);
            return (target.BaseUrl, null);
        }

        // Use the target's ApiKey if configured; otherwise use the local workspace token (for self-federation).
        var authKey = target.ApiKey ?? localApiKey;
        var encodedName = HttpUtility.UrlEncode(workspaceName);
        var url = $"{target.BaseUrl}/mcpserver/federation/connection?workspaceName={encodedName}";

        try
        {
            using var client = _httpClientFactory.CreateClient(FederationProxyService.HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("X-Api-Key", authKey);

            using var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Upstream federation/connection returned {StatusCode} for workspace {WorkspacePath} — using local credentials.",
                    (int)response.StatusCode, workspacePath);
                return (null, null);
            }

            var info = await response.Content
                .ReadFromJsonAsync(McpServicesJsonContext.Default.FederationConnectionResult, ct)
                .ConfigureAwait(false);
            if (info is null || string.IsNullOrWhiteSpace(info.ApiKey))
            {
                _logger.LogWarning(
                    "Upstream federation/connection returned an empty API key for workspace {WorkspacePath} — using local credentials.",
                    workspacePath);
                return (null, null);
            }

            _logger.LogInformation(
                "Marker for {WorkspacePath} will use upstream {BaseUrl}.", workspacePath, target.BaseUrl);
            return (target.BaseUrl, info.ApiKey);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Could not reach upstream at {Url} for workspace {WorkspacePath} — using local credentials.",
                url, workspacePath);
            return (null, null);
        }
    }

    private async Task PublishMarkerChangeSafeAsync(string action, string entityId, CancellationToken cancellationToken)
    {
        if (_eventBus is null)
            return;

        try
        {
            await _eventBus.PublishAsync(
                new ChangeEvent
                {
                    Category = ChangeEventCategories.Marker,
                    Action = action,
                    EntityId = entityId,
                    ResourceUri = $"mcp://workspace/marker/{entityId}",
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed publishing marker change event for {EntityId}", entityId);
        }
    }
}

/// <summary>
/// Internal DTO used to deserialize the JSON body returned by the upstream server's
/// <c>GET /mcpserver/federation/connection</c> endpoint.
/// </summary>
internal sealed record FederationConnectionResult(string BaseUrl, int Port, string ApiKey);
