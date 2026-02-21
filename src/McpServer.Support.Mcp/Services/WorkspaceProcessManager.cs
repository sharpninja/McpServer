using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-011 / TR-MCP-WS-003: Manages child dotnet processes per workspace.
/// Implements IHostedService for graceful shutdown of all child processes on app exit.
/// </summary>
public sealed class WorkspaceProcessManager : IWorkspaceProcessManager, IDisposable
{
    private readonly ConcurrentDictionary<string, WorkspaceProcessEntry> _processes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<WorkspaceProcessManager> _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceProcessManager"/> class.</summary>
    public WorkspaceProcessManager(ILogger<WorkspaceProcessManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<WorkspaceProcessStatus> StartAsync(string workspacePath, int port, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);

        if (_processes.TryGetValue(key, out var existing) && !existing.Process.HasExited)
            return Task.FromResult(new WorkspaceProcessStatus(true, existing.Process.Id, DateTime.UtcNow - existing.StartedAt, port));

        try
        {
            // Resolve the project path relative to the running assembly.
            var assemblyDir = Path.GetDirectoryName(typeof(WorkspaceProcessManager).Assembly.Location) ?? ".";
            var projectPath = ResolveProjectPath(assemblyDir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" --no-build -- --instance \"{Path.GetFileName(key)}\" --port {port}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Environment = { ["Mcp__RepoRoot"] = key, ["PORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.Exited += (_, _) =>
            {
                _logger.LogInformation("Workspace process exited: {Path} (PID {Pid})", key, process.Id);
                _processes.TryRemove(key, out _);
            };

            process.Start();
            var entry = new WorkspaceProcessEntry(process, DateTime.UtcNow, port);
            _processes[key] = entry;

            _logger.LogInformation("Workspace process started: {Path} on port {Port} (PID {Pid})", key, port, process.Id);
            return Task.FromResult(new WorkspaceProcessStatus(true, process.Id, TimeSpan.Zero, port));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start workspace process: {Path}", key);
            return Task.FromResult(new WorkspaceProcessStatus(false, Error: ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<WorkspaceProcessStatus> StopAsync(string workspacePath, CancellationToken ct = default)
    {
        var key = NormalizeKey(workspacePath);

        if (!_processes.TryRemove(key, out var entry))
            return Task.FromResult(new WorkspaceProcessStatus(false, Error: "No running process for this workspace."));

        try
        {
            if (!entry.Process.HasExited)
            {
                entry.Process.Kill(entireProcessTree: true);
                entry.Process.WaitForExit(5000);
            }
            var pid = entry.Process.Id;
            entry.Process.Dispose();

            _logger.LogInformation("Workspace process stopped: {Path} (PID {Pid})", key, pid);
            return Task.FromResult(new WorkspaceProcessStatus(false, pid));
        }
        catch (InvalidOperationException)
        {
            entry.Process.Dispose();
            return Task.FromResult(new WorkspaceProcessStatus(false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping workspace process: {Path}", key);
            return Task.FromResult(new WorkspaceProcessStatus(false, Error: ex.Message));
        }
    }

    /// <inheritdoc />
    public WorkspaceProcessStatus GetStatus(string workspacePath)
    {
        var key = NormalizeKey(workspacePath);

        if (!_processes.TryGetValue(key, out var entry))
            return new WorkspaceProcessStatus(false);

        if (entry.Process.HasExited)
        {
            _processes.TryRemove(key, out _);
            return new WorkspaceProcessStatus(false);
        }

        return new WorkspaceProcessStatus(true, entry.Process.Id, DateTime.UtcNow - entry.StartedAt, entry.Port);
    }

    /// <inheritdoc />
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        var keys = _processes.Keys.ToList();
        foreach (var key in keys)
        {
            await StopAsync(key, ct).ConfigureAwait(false);
        }
    }

    // IHostedService — no-op on start, cleanup on stop.
    Task IHostedService.StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAllAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var entry in _processes.Values)
        {
            try
            {
                if (!entry.Process.HasExited)
                    entry.Process.Kill(entireProcessTree: true);
                entry.Process.Dispose();
            }
            catch (InvalidOperationException) { /* already exited */ }
        }
        _processes.Clear();
    }

    private static string NormalizeKey(string path)
    {
        return Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static string ResolveProjectPath(string assemblyDir)
    {
        // Walk up from bin output to find the .csproj.
        var dir = new DirectoryInfo(assemblyDir);
        while (dir is not null)
        {
            var csproj = dir.GetFiles("McpServer.Support.Mcp.csproj", SearchOption.TopDirectoryOnly);
            if (csproj.Length > 0)
                return csproj[0].FullName;
            dir = dir.Parent;
        }
        // Fallback: assume standard repo layout.
        return Path.Combine(assemblyDir, "..", "..", "..", "McpServer.Support.Mcp.csproj");
    }

    private sealed record WorkspaceProcessEntry(Process Process, DateTime StartedAt, int Port);
}
