using System.Collections.Concurrent;
using System.Diagnostics;
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
        var isPrimary = IsPrimaryWorkspace(key);

        _logger.LogInformation(
            "Workspace instance startup requested: Name={WorkspaceName}; Path={WorkspacePath}; Port={Port}; Primary={IsPrimary}; PID={ProcessId}; Command={CommandLine}",
            workspace.Name,
            key,
            port,
            isPrimary,
            Environment.ProcessId,
            Environment.CommandLine);

        var globalTemplate = _promptOptions.CurrentValue.MarkerPromptTemplate;
        var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);

        // Ensure a default (anonymous) token also exists for this workspace.
        _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);

        // If this workspace is the primary host, just write the marker — the primary app already serves it.
        if (isPrimary)
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
            await MarkerFileService.WriteMarkerAsync(key, existing.Port, name, _logger, CancellationToken.None,
                globalTemplate, workspace.PromptTemplate, token, workspace).ConfigureAwait(false);
            return new WorkspaceProcessStatus(true, Uptime: DateTime.UtcNow - existing.StartedAt, Port: existing.Port);
        }

        var requestedPort = port;
        var effectivePort = requestedPort;
        try
        {
            var configEntry = LookupConfigEntry(key);

            if (TryGetPortConflictDetails(requestedPort, out var configuredConflict))
            {
                if (!TryGetPrefixedPortFallback(requestedPort, out effectivePort, out var fallbackError))
                {
                    _logger.LogError(
                        "Workspace startup failed due to configured port conflict with no valid fallback: Name={WorkspaceName}; Path={WorkspacePath}; Port={Port}; ConflictPid={ConflictPid}; ConflictProcess={ConflictProcess}; ConflictPath={ConflictPath}; Reason={Reason}",
                        workspace.Name,
                        key,
                        requestedPort,
                        configuredConflict?.Pid,
                        configuredConflict?.ProcessName,
                        configuredConflict?.ExecutablePath,
                        fallbackError);
                    return new WorkspaceProcessStatus(false, Error: fallbackError);
                }

                _logger.LogWarning(
                    "Workspace startup port conflict detected: Name={WorkspaceName}; Path={WorkspacePath}; ConfiguredPort={ConfiguredPort}; FallbackPort={FallbackPort}; ConflictPid={ConflictPid}; ConflictProcess={ConflictProcess}; ConflictPath={ConflictPath}",
                    workspace.Name,
                    key,
                    requestedPort,
                    effectivePort,
                    configuredConflict?.Pid,
                    configuredConflict?.ProcessName,
                    configuredConflict?.ExecutablePath);

                if (TryGetPortConflictDetails(effectivePort, out var fallbackConflict))
                {
                    var error = $"Fallback port {effectivePort} is also in use.";
                    _logger.LogError(
                        "Workspace startup failed because fallback port is also in use: Name={WorkspaceName}; Path={WorkspacePath}; ConfiguredPort={ConfiguredPort}; FallbackPort={FallbackPort}; ConflictPid={ConflictPid}; ConflictProcess={ConflictProcess}; ConflictPath={ConflictPath}",
                        workspace.Name,
                        key,
                        requestedPort,
                        effectivePort,
                        fallbackConflict?.Pid,
                        fallbackConflict?.ProcessName,
                        fallbackConflict?.ExecutablePath);
                    return new WorkspaceProcessStatus(false, Error: error);
                }
            }

            var entry = await StartWorkspaceHostWithFallbackAsync(
                key,
                workspace.Name,
                workspace.DataDirectory,
                requestedPort,
                effectivePort,
                configEntry,
                ct).ConfigureAwait(false);

            _hosts[key] = entry;

            var workspaceName = DeriveWorkspaceName(key);
            await MarkerFileService.WriteMarkerAsync(key, entry.Port, workspaceName, _logger, CancellationToken.None,
                globalTemplate, workspace.PromptTemplate, token, workspace).ConfigureAwait(false);

            _logger.LogInformation(
                "Workspace Kestrel host started: {Path} on port {Port} (configured {ConfiguredPort})",
                key,
                entry.Port,
                requestedPort);
            return new WorkspaceProcessStatus(true, Port: entry.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to start workspace Kestrel host: {Path}; ConfiguredPort={ConfiguredPort}; PlannedPort={PlannedPort}",
                key,
                requestedPort,
                effectivePort);
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

            _logger.LogInformation("Workspace Kestrel host stopped: {Path} on port {Port}", key, entry.Port);
            return new WorkspaceProcessStatus(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping workspace Kestrel host: {Path}; Port={Port}", key, entry.Port);
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
            var markerPort = IsPrimaryWorkspace(key)
                ? ws.WorkspacePort
                : (_hosts.TryGetValue(key, out var runningEntry) && runningEntry.IsRunning ? runningEntry.Port : ws.WorkspacePort);

            var name = DeriveWorkspaceName(key);
            var token = _tokenService.GetToken(key) ?? _tokenService.GenerateToken(key);
            _ = _tokenService.GetDefaultToken(key) ?? _tokenService.GenerateDefaultToken(key);
            await MarkerFileService.WriteMarkerAsync(key, markerPort, name, _logger, ct,
                globalTemplate, ws.PromptTemplate, token, ws).ConfigureAwait(false);
        }

        _logger.LogInformation("Regenerated marker files for all running workspaces");
    }

    // IHostedService — start all registered workspaces on app startup, cleanup on stop.
    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        string? currentWorkspaceName = null;
        int? currentWorkspacePort = null;
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
                currentWorkspaceName = ws.Name;
                currentWorkspacePort = ws.WorkspacePort;

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
                    _logger.LogWarning("  ✗ {Name} failed on configured port {Port}: {Error}", ws.Name, ws.WorkspacePort, status.Error);
            }
        }
        catch (Exception ex)
        {
            if (currentWorkspacePort.HasValue)
            {
                _logger.LogError(
                    ex,
                    "Error during workspace auto-start while processing {Name}; Port={Port}",
                    currentWorkspaceName ?? "(unknown)",
                    currentWorkspacePort.Value);
            }
            else
            {
                _logger.LogError(ex, "Error during workspace auto-start before workspace port could be determined");
            }
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
                _logger.LogWarning(ex, "Error disposing workspace host during shutdown: {Path}; Port={Port}", key, entry.Port);
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

    private async Task<WorkspaceHostEntry> StartWorkspaceHostWithFallbackAsync(
        string workspacePath,
        string workspaceName,
        string? dataDirectory,
        int requestedPort,
        int initialPort,
        WorkspaceConfigEntry? configEntry,
        CancellationToken ct)
    {
        try
        {
            return await StartWorkspaceHostAsync(workspacePath, initialPort, dataDirectory, configEntry, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (initialPort == requestedPort && IsPortInUseException(ex))
        {
            if (!TryGetPrefixedPortFallback(requestedPort, out var fallbackPort, out var fallbackError))
                throw new InvalidOperationException(fallbackError ?? $"No valid fallback port for {requestedPort}.", ex);

            if (TryGetPortConflictDetails(requestedPort, out var conflict))
            {
                _logger.LogWarning(
                    ex,
                    "Workspace startup bind conflict on configured port; retrying fallback: Name={WorkspaceName}; Path={WorkspacePath}; ConfiguredPort={ConfiguredPort}; FallbackPort={FallbackPort}; ConflictPid={ConflictPid}; ConflictProcess={ConflictProcess}; ConflictPath={ConflictPath}",
                    workspaceName,
                    workspacePath,
                    requestedPort,
                    fallbackPort,
                    conflict?.Pid,
                    conflict?.ProcessName,
                    conflict?.ExecutablePath);
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Workspace startup bind conflict on configured port; retrying fallback: Name={WorkspaceName}; Path={WorkspacePath}; ConfiguredPort={ConfiguredPort}; FallbackPort={FallbackPort}",
                    workspaceName,
                    workspacePath,
                    requestedPort,
                    fallbackPort);
            }

            if (TryGetPortConflictDetails(fallbackPort, out var fallbackConflict))
            {
                throw new InvalidOperationException(
                    $"Fallback port {fallbackPort} is also in use by PID {fallbackConflict?.Pid} ({fallbackConflict?.ProcessName}).",
                    ex);
            }

            return await StartWorkspaceHostAsync(workspacePath, fallbackPort, dataDirectory, configEntry, ct).ConfigureAwait(false);
        }
    }

    private async Task<WorkspaceHostEntry> StartWorkspaceHostAsync(
        string workspacePath,
        int port,
        string? dataDirectory,
        WorkspaceConfigEntry? configEntry,
        CancellationToken ct)
    {
        var app = WorkspaceAppFactory.Create(workspacePath, port, _loggerFactory, dataDirectory, _tokenService, configEntry);
        try
        {
            await app.StartAsync(ct).ConfigureAwait(false);
            return new WorkspaceHostEntry(app, DateTime.UtcNow, port);
        }
        catch
        {
            try
            {
                await app.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cleanup after failed startup.
            }

            throw;
        }
    }

    private static bool TryGetPrefixedPortFallback(int port, out int fallbackPort, out string? error)
    {
        fallbackPort = port;
        error = null;

        var fallbackText = $"1{port}";
        if (!int.TryParse(fallbackText, out fallbackPort))
        {
            error = $"Could not compute fallback port from configured port {port}.";
            return false;
        }

        if (fallbackPort is <= 0 or > 65535)
        {
            error = $"Computed fallback port {fallbackPort} is outside valid range.";
            return false;
        }

        return true;
    }

    private static bool IsPortInUseException(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException!)
        {
            if (current is IOException ioEx &&
                ioEx.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (current.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Only one usage of each socket address", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPortConflictDetails(int port, out PortConflictDetails? details)
    {
        details = null;

        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano -p tcp",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (!process.Start())
                return false;

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(2000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return false;
            }

            foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                    continue;

                if (!parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!EndpointMatchesPort(parts[1], port))
                    continue;

                if (!int.TryParse(parts[4], out var pid))
                    continue;

                string? processName = null;
                string? executablePath = null;

                try
                {
                    var owner = Process.GetProcessById(pid);
                    processName = owner.ProcessName;
                    try { executablePath = owner.MainModule?.FileName; } catch { }
                }
                catch
                {
                    // Best effort only.
                }

                details = new PortConflictDetails(pid, processName, executablePath, parts[1]);
                return true;
            }
        }
        catch
        {
            // Best effort only — startup should continue even if conflict introspection fails.
        }

        return false;
    }

    private static bool EndpointMatchesPort(string endpoint, int port)
    {
        var index = endpoint.LastIndexOf(':');
        if (index < 0 || index == endpoint.Length - 1)
            return false;

        return int.TryParse(endpoint[(index + 1)..], out var parsed) && parsed == port;
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

    private sealed record PortConflictDetails(int Pid, string? ProcessName, string? ExecutablePath, string LocalEndpoint);
}
