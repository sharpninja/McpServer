using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011 / TR-MCP-WS-003: Manages in-process Kestrel hosts per workspace.
/// Each workspace gets its own <see cref="WebApplication"/> listening on the assigned port.
/// On startup, all registered workspaces are automatically started.
/// Implements IHostedService for graceful shutdown of all workspace hosts on app exit.
/// </summary>
public sealed class WorkspaceProcessManager : IWorkspaceProcessManager, IDisposable
{
    private readonly ConcurrentDictionary<string, WorkspaceHostEntry> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<WorkspaceProcessManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProcessManager"/> class.</summary>
    public WorkspaceProcessManager(
        ILogger<WorkspaceProcessManager> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<WorkspaceProcessStatus> StartAsync(string workspacePath, int port, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);

        if (_hosts.TryGetValue(key, out var existing) && existing.IsRunning)
        {
            // Ensure the .mcp-server.yaml marker exists (may have been missed on initial start).
            var name = Path.GetFileName(key.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            await MarkerFileService.WriteMarkerAsync(key, port, name, _logger, CancellationToken.None).ConfigureAwait(false);
            return new WorkspaceProcessStatus(true, Uptime: DateTime.UtcNow - existing.StartedAt, Port: port);
        }

        try
        {
            var app = WorkspaceAppFactory.Create(key, port, _loggerFactory);
            await app.StartAsync(ct).ConfigureAwait(false);

            var entry = new WorkspaceHostEntry(app, DateTime.UtcNow, port);
            _hosts[key] = entry;

            // Write .mcp-server.yaml marker so agents can discover the port.
            // Use CancellationToken.None — the marker must be written regardless of HTTP request lifecycle.
            var workspaceName = Path.GetFileName(key.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            await MarkerFileService.WriteMarkerAsync(key, port, workspaceName, _logger, CancellationToken.None).ConfigureAwait(false);

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

        if (!_hosts.TryRemove(key, out var entry))
            return new WorkspaceProcessStatus(false, Error: "No running host for this workspace.");

        try
        {
            await entry.App.StopAsync(ct).ConfigureAwait(false);
            await entry.App.DisposeAsync().ConfigureAwait(false);

            // Remove .mcp-server.yaml marker on stop.
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

            _logger.LogInformation("Auto-starting {Count} registered workspace(s) ...", workspaces.TotalCount);

            foreach (var ws in workspaces.Items)
            {
                var status = await StartAsync(ws.WorkspacePath, ws.WorkspacePort, cancellationToken).ConfigureAwait(false);
                if (status.IsRunning)
                    _logger.LogInformation("  ✓ {Name} on port {Port}", ws.Name, ws.WorkspacePort);
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
    {
        return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

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
