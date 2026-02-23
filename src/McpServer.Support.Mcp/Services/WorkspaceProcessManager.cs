using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011 / TR-MCP-WS-003: Manages in-process Kestrel hosts per workspace.
/// Each workspace gets its own <see cref="WebApplication"/> listening on the assigned port.
/// On startup, all registered workspaces are automatically started. Workspaces with
/// <see cref="WorkspaceDto.IsEnabled"/> = false are skipped. The primary workspace
/// (determined by <see cref="WorkspaceDto.IsPrimary"/>, or lowest-port enabled fallback)
/// only gets a marker file — the host process already serves it.
/// </summary>
public sealed class WorkspaceProcessManager : IWorkspaceProcessManager, IDisposable
{
    private readonly ConcurrentDictionary<string, WorkspaceHostEntry> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<WorkspaceProcessManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<MarkerPromptOptions> _promptOptions;
    private readonly WorkspaceTokenService _tokenService;

    // Resolved once during IHostedService.StartAsync; null until then.
    private string? _primaryWorkspaceKey;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProcessManager"/> class.</summary>
    public WorkspaceProcessManager(
        ILogger<WorkspaceProcessManager> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        IOptionsMonitor<MarkerPromptOptions> promptOptions,
        WorkspaceTokenService tokenService)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _promptOptions = promptOptions;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StartAsync(WorkspaceDto workspace, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspace.WorkspacePath);
        var port = workspace.WorkspacePort;
        var globalTemplate = _promptOptions.CurrentValue.MarkerPromptTemplate;
        var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);

        // Ensure a default (anonymous) token also exists for this workspace.
        _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);

        // If this workspace is the primary host, just write the marker — the primary app already serves it.
        if (IsPrimaryWorkspace(key))
        {
            var name = DeriveWorkspaceName(key);
            await MarkerFileService.WriteMarkerAsync(key, port, name, _logger, CancellationToken.None,
                globalTemplate, workspace.PromptTemplate, token, workspace).ConfigureAwait(false);
            _logger.LogInformation("Workspace {Path} is the primary host — marker written, skipping duplicate app", key);
            return new WorkspaceProcessStatus(true, Port: port);
        }

        if (_hosts.TryGetValue(key, out var existing) && existing.IsRunning)
        {
            var name = DeriveWorkspaceName(key);
            await MarkerFileService.WriteMarkerAsync(key, port, name, _logger, CancellationToken.None,
                globalTemplate, workspace.PromptTemplate, token, workspace).ConfigureAwait(false);
            return new WorkspaceProcessStatus(true, Uptime: DateTime.UtcNow - existing.StartedAt, Port: port);
        }

        try
        {
            var configEntry = LookupConfigEntry(key);
            var app = WorkspaceAppFactory.Create(key, port, _loggerFactory, workspace.DataDirectory, _tokenService, configEntry);
            await app.StartAsync(ct).ConfigureAwait(false);

            var entry = new WorkspaceHostEntry(app, DateTime.UtcNow, port);
            _hosts[key] = entry;

            var workspaceName = DeriveWorkspaceName(key);
            await MarkerFileService.WriteMarkerAsync(key, port, workspaceName, _logger, CancellationToken.None,
                globalTemplate, workspace.PromptTemplate, token, workspace).ConfigureAwait(false);

            _logger.LogInformation("Workspace Kestrel host started: {Path} on port {Port}", key, port);
            return new WorkspaceProcessStatus(true, Port: port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start workspace Kestrel host: {Path}", key);
            return new WorkspaceProcessStatus(false, Error: ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);

        // Primary workspace cannot be stopped via the process manager — it IS the host process.
        if (IsPrimaryWorkspace(key))
        {
            MarkerFileService.RemoveMarker(key, _logger);
            return new WorkspaceProcessStatus(false);
        }

        if (!_hosts.TryRemove(key, out var entry))
            return new WorkspaceProcessStatus(false, Error: "No running host for this workspace.");

        try
        {
            await entry.App.StopAsync(ct).ConfigureAwait(false);
            await entry.App.DisposeAsync().ConfigureAwait(false);

            MarkerFileService.RemoveMarker(key, _logger);

            _logger.LogInformation("Workspace Kestrel host stopped: {Path}", key);
            return new WorkspaceProcessStatus(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping workspace Kestrel host: {Path}", key);
            return new WorkspaceProcessStatus(false, Error: ex.Message);
        }
    }

    /// <inheritdoc />
    public WorkspaceProcessStatus GetStatus(string workspacePath)
    {
        var key = NormalizeKey(workspacePath);

        // Primary workspace is always running as long as the host process is alive.
        if (IsPrimaryWorkspace(key))
            return new WorkspaceProcessStatus(true);

        if (!_hosts.TryGetValue(key, out var entry))
            return new WorkspaceProcessStatus(false);

        if (!entry.IsRunning)
        {
            _hosts.TryRemove(key, out _);
            return new WorkspaceProcessStatus(false);
        }

        return new WorkspaceProcessStatus(true, Uptime: DateTime.UtcNow - entry.StartedAt, Port: entry.Port);
    }

    /// <inheritdoc />
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        var keys = _hosts.Keys.ToList();
        foreach (var key in keys)
        {
            await StopAsync(key, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RegenerateAllMarkersAsync(CancellationToken ct = default, string? globalPromptOverride = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var workspaces = await workspaceService.ListAsync(ct).ConfigureAwait(false);

        // Read the global template from IConfiguration directly (synchronous after Reload)
        // rather than IOptionsMonitor which may lag behind the config change.
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var globalTemplate = globalPromptOverride
            ?? config.GetSection("Mcp")["MarkerPromptTemplate"]
            ?? _promptOptions.CurrentValue.MarkerPromptTemplate;

        foreach (var ws in workspaces.Items)
        {
            if (!ws.IsEnabled) continue;

            var key = NormalizeKey(ws.WorkspacePath);
            var isRunning = IsPrimaryWorkspace(key) || (_hosts.TryGetValue(key, out var entry) && entry.IsRunning);
            if (!isRunning) continue;

            var name = DeriveWorkspaceName(key);
            var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
            _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);
            await MarkerFileService.WriteMarkerAsync(key, ws.WorkspacePort, name, _logger, ct,
                globalTemplate, ws.PromptTemplate, token, ws).ConfigureAwait(false);
        }

        _logger.LogInformation("Regenerated marker files for all running workspaces");
    }

    // IHostedService — start all registered workspaces on app startup, cleanup on stop.
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var workspaceService = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
            var workspaces = await workspaceService.ListAsync(cancellationToken).ConfigureAwait(false);

            if (workspaces.TotalCount == 0)
            {
                _logger.LogInformation("No registered workspaces to auto-start");
                return;
            }

            // Resolve the primary workspace: explicit IsPrimary flag wins; fallback = lowest-port enabled workspace.
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

            _logger.LogInformation("Auto-starting {Count} registered workspace(s); primary = {Primary}",
                workspaces.TotalCount, primary?.Name ?? "(none)");

            foreach (var ws in workspaces.Items)
            {
                if (!ws.IsEnabled)
                {
                    _logger.LogInformation("  ⊘ {Name} skipped (disabled)", ws.Name);
                    continue;
                }

                var status = await StartAsync(ws, cancellationToken).ConfigureAwait(false);
                if (status.IsRunning)
                    _logger.LogInformation("  ✓ {Name} on port {Port}{Primary}", ws.Name, ws.WorkspacePort,
                        IsPrimaryWorkspace(NormalizeKey(ws.WorkspacePath)) ? " (primary)" : "");
                else
                    _logger.LogWarning("  ✗ {Name} failed: {Error}", ws.Name, status.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workspace auto-start");
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAllAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var (key, entry) in _hosts)
        {
            try
            {
                entry.App.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                entry.App.DisposeAsync().AsTask().GetAwaiter().GetResult();
                MarkerFileService.RemoveMarker(key, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing workspace host during shutdown");
            }
        }
        _hosts.Clear();
    }

    private static string NormalizeKey(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>Reads the raw <see cref="WorkspaceConfigEntry"/> from <c>appsettings.json</c> for prompt overrides.</summary>
    private WorkspaceConfigEntry? LookupConfigEntry(string normalizedKey)
    {
        var config = _serviceProvider.GetRequiredService<IConfiguration>();
        var entries = config.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
        return entries.FirstOrDefault(e =>
            string.Equals(
                Path.GetFullPath(e.WorkspacePath.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                normalizedKey,
                StringComparison.OrdinalIgnoreCase));
    }

    private bool IsPrimaryWorkspace(string normalizedKey)
        => _primaryWorkspaceKey is not null
           && string.Equals(normalizedKey, _primaryWorkspaceKey, StringComparison.OrdinalIgnoreCase);

    private static string DeriveWorkspaceName(string normalizedKey)
        => Path.GetFileName(normalizedKey.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private sealed class WorkspaceHostEntry
    {
        public WebApplication App { get; }
        public DateTime StartedAt { get; }
        public int Port { get; }

        public bool IsRunning => App.Lifetime.ApplicationStarted.IsCancellationRequested
            && !App.Lifetime.ApplicationStopped.IsCancellationRequested;

        public WorkspaceHostEntry(WebApplication app, DateTime startedAt, int port)
        {
            App = app;
            StartedAt = startedAt;
            Port = port;
        }
    }
}
