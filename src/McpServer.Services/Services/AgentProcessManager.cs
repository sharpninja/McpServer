using System.Collections.Concurrent;
using System.Diagnostics;
using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Default in-memory runtime manager for agent processes.
/// </summary>
public sealed class AgentProcessManager : IAgentProcessManager
{
    private readonly ConcurrentDictionary<(string WorkspacePath, string AgentId), ManagedAgentProcess> _processes = new();
    private readonly ILogger<AgentProcessManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentProcessManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AgentProcessManager(ILogger<AgentProcessManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<AgentProcessInfo> LaunchAsync(string workspacePath, string agentId, string resolvedCommand, string workDirectory, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var normalizedWorkDirectory = NormalizePath(workDirectory);
        var key = (normalizedWorkspace, agentId);

        if (_processes.TryGetValue(key, out var existing))
        {
            var existingInfo = Snapshot(existing);
            if (existingInfo.Status is AgentProcessStatus.Starting or AgentProcessStatus.Running)
                throw new InvalidOperationException($"Agent '{agentId}' is already running for workspace '{normalizedWorkspace}'.");
        }

        var startedAt = DateTime.UtcNow;
        var startingInfo = new AgentProcessInfo
        {
            AgentId = agentId,
            WorkspacePath = normalizedWorkspace,
            StartedAt = startedAt,
            Status = AgentProcessStatus.Starting,
            WorkDirectory = normalizedWorkDirectory,
        };

        try
        {
            var (fileName, arguments) = AgentProcessCommandResolver.SplitCommand(resolvedCommand);
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = normalizedWorkDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            process.Exited += (_, _) => HandleProcessExited(key, process);

            if (!process.Start())
                throw new InvalidOperationException($"Agent process for '{agentId}' failed to start.");

            var managed = new ManagedAgentProcess(
                process,
                new AgentProcessInfo
                {
                    AgentId = agentId,
                    WorkspacePath = normalizedWorkspace,
                    StartedAt = startedAt,
                    Status = AgentProcessStatus.Running,
                    ProcessId = process.Id,
                    WorkDirectory = normalizedWorkDirectory,
                });

            _processes[key] = managed;
            _logger.LogInformation("Started agent process {ProcessId} for {AgentId} in {WorkspacePath}", process.Id, agentId, normalizedWorkspace);
            return Task.FromResult(Snapshot(managed));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to launch agent process for {AgentId} in {WorkspacePath}", agentId, normalizedWorkspace);
            var failedInfo = CloneInfo(startingInfo);
            failedInfo.Status = AgentProcessStatus.Failed;
            failedInfo.ErrorMessage = ex.Message;
            _processes[key] = new ManagedAgentProcess(null, failedInfo);
            return Task.FromResult(CloneInfo(failedInfo));
        }
    }

    /// <inheritdoc/>
    public Task<bool> StopAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var key = (normalizedWorkspace, agentId);
        if (!_processes.TryGetValue(key, out var managed) || managed.Process is null)
            return Task.FromResult(false);

        try
        {
            if (!managed.Process.HasExited)
                managed.Process.Kill(entireProcessTree: true);

            managed.Info.Status = AgentProcessStatus.Stopped;
            if (managed.Process.HasExited)
                managed.Info.ExitCode = managed.Process.ExitCode;

            _logger.LogInformation("Stopped agent process {ProcessId} for {AgentId} in {WorkspacePath}", managed.Process.Id, agentId, normalizedWorkspace);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop agent process for {AgentId} in {WorkspacePath}", agentId, normalizedWorkspace);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<AgentProcessInfo?> GetStatusAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var key = (normalizedWorkspace, agentId);
        if (!_processes.TryGetValue(key, out var managed))
            return Task.FromResult<AgentProcessInfo?>(null);

        RefreshStatus(managed);
        return Task.FromResult<AgentProcessInfo?>(Snapshot(managed));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<AgentProcessInfo>> ListRunningAsync(string? workspacePath = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var normalizedWorkspace = string.IsNullOrWhiteSpace(workspacePath) ? null : NormalizePath(workspacePath);

        var results = new List<AgentProcessInfo>();
        foreach (var pair in _processes)
        {
            if (normalizedWorkspace is not null && !string.Equals(pair.Key.WorkspacePath, normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
                continue;

            RefreshStatus(pair.Value);
            results.Add(Snapshot(pair.Value));
        }

        return Task.FromResult<IReadOnlyList<AgentProcessInfo>>(results);
    }

    private void HandleProcessExited((string WorkspacePath, string AgentId) key, Process process)
    {
        if (!_processes.TryGetValue(key, out var managed))
            return;

        try
        {
            managed.Info.Status = process.ExitCode == 0 ? AgentProcessStatus.Stopped : AgentProcessStatus.Failed;
            managed.Info.ExitCode = process.ExitCode;

            _logger.LogInformation(
                "Agent process {ProcessId} for {AgentId} in {WorkspacePath} exited with code {ExitCode}",
                process.Id,
                key.AgentId,
                key.WorkspacePath,
                process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update exit status for agent {AgentId} in {WorkspacePath}", key.AgentId, key.WorkspacePath);
        }
    }

    private static void RefreshStatus(ManagedAgentProcess managed)
    {
        if (managed.Process is null)
            return;

        if (!managed.Process.HasExited)
        {
            managed.Info.Status = AgentProcessStatus.Running;
            managed.Info.ProcessId = managed.Process.Id;
            return;
        }

        managed.Info.Status = managed.Process.ExitCode == 0 ? AgentProcessStatus.Stopped : AgentProcessStatus.Failed;
        managed.Info.ExitCode = managed.Process.ExitCode;
        managed.Info.ProcessId = managed.Process.Id;
    }

    private static AgentProcessInfo Snapshot(ManagedAgentProcess managed) => CloneInfo(managed.Info);

    private static AgentProcessInfo CloneInfo(AgentProcessInfo info) => new()
    {
        ProcessId = info.ProcessId,
        AgentId = info.AgentId,
        WorkspacePath = info.WorkspacePath,
        StartedAt = info.StartedAt,
        Status = info.Status,
        ExitCode = info.ExitCode,
        WorkDirectory = info.WorkDirectory,
        ErrorMessage = info.ErrorMessage,
    };

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    private sealed class ManagedAgentProcess
    {
        public ManagedAgentProcess(Process? process, AgentProcessInfo info)
        {
            Process = process;
            Info = info;
        }

        public Process? Process { get; }

        public AgentProcessInfo Info { get; }
    }
}
