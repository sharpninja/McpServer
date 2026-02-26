using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

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
    private readonly WorkspaceTokenService _tokenService;
    private readonly ServerRuntimeInfo _serverRuntimeInfo;

    private string? _primaryWorkspaceKey;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProcessManager"/> class.</summary>
    public WorkspaceProcessManager(
        ILogger<WorkspaceProcessManager> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        IOptionsMonitor<MarkerPromptOptions> promptOptions,
        WorkspaceTokenService tokenService,
        ServerRuntimeInfo serverRuntimeInfo)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _promptOptions = promptOptions;
        _tokenService = tokenService;
        _serverRuntimeInfo = serverRuntimeInfo;
        // loggerFactory kept in signature for backward compat (DI registration) but no longer needed
        _ = loggerFactory;
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StartAsync(WorkspaceDto workspace, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspace.WorkspacePath);
        var port = workspace.WorkspacePort;

        _logger.LogInformation(
            "Registering workspace: Name={WorkspaceName}; Path={WorkspacePath}; Port={Port}",
            workspace.Name, key, port);

        var globalTemplate = _promptOptions.CurrentValue.MarkerPromptTemplate;
        var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
        _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);

        var name = DeriveWorkspaceName(key);
        await MarkerFileService.WriteMarkerAsync(key, port, name, _logger, ct,
            globalTemplate, workspace.PromptTemplate, token, workspace, _serverRuntimeInfo.StartedAtUtc).ConfigureAwait(false);

        _activeWorkspaces[key] = port;

        _logger.LogInformation("Workspace registered and marker written: {Path} (port {Port})", key, port);
        return new WorkspaceProcessStatus(true, Port: port);
    }

    /// <inheritdoc />
    public Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);
        _activeWorkspaces.TryRemove(key, out _);
        MarkerFileService.RemoveMarker(key, _logger);
        _logger.LogInformation("Workspace unregistered and marker removed: {Path}", key);
        return Task.FromResult(new WorkspaceProcessStatus(false));
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
    public async Task RegenerateAllMarkersAsync(CancellationToken ct = default, string? globalPromptOverride = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var workspaces = await workspaceService.ListAsync(ct).ConfigureAwait(false);

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var globalTemplate = globalPromptOverride
            ?? config.GetSection("Mcp")["MarkerPromptTemplate"]
            ?? _promptOptions.CurrentValue.MarkerPromptTemplate;

        foreach (var ws in workspaces.Items)
        {
            if (!ws.IsEnabled) continue;
            var key = NormalizeKey(ws.WorkspacePath);
            if (!_activeWorkspaces.ContainsKey(key)) continue;

            var name = DeriveWorkspaceName(key);
            var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
            _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);
            await MarkerFileService.WriteMarkerAsync(key, ws.WorkspacePort, name, _logger, ct,
                globalTemplate, ws.PromptTemplate, token, ws, _serverRuntimeInfo.StartedAtUtc).ConfigureAwait(false);
        }

        _logger.LogInformation("Regenerated marker files for all registered workspaces");
    }

    // IHostedService — register all workspaces on startup, cleanup on stop.
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
                .OrderBy(w => w.WorkspacePort)
                .FirstOrDefault();
            primary ??= workspaces.Items
                .Where(w => w.IsEnabled)
                .OrderBy(w => w.WorkspacePort)
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

                var status = await StartAsync(ws, cancellationToken).ConfigureAwait(false);
                if (status.IsRunning)
                    _logger.LogInformation("  ✓ {Name} on port {Port}{Primary}", ws.Name, status.Port ?? ws.WorkspacePort,
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
            MarkerFileService.RemoveMarker(key, _logger);
        _activeWorkspaces.Clear();
    }

    private static string NormalizeKey(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private bool IsPrimaryWorkspace(string normalizedKey)
        => _primaryWorkspaceKey is not null
           && string.Equals(normalizedKey, _primaryWorkspaceKey, StringComparison.OrdinalIgnoreCase);

    private static string DeriveWorkspaceName(string normalizedKey)
        => Path.GetFileName(normalizedKey.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
