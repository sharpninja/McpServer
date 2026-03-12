using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Creates a git worktree under the configured agents directory for agent execution.
/// </summary>
public sealed class WorktreeAgentIsolationStrategy : IAgentIsolationStrategy
{
    /// <summary>
    /// Canonical mode name for worktree isolation.
    /// </summary>
    public const string ModeName = "worktree";

    private readonly IProcessRunner _processRunner;
    private readonly ILogger<WorktreeAgentIsolationStrategy> _logger;
    private readonly string _agentsDirectoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorktreeAgentIsolationStrategy"/> class.
    /// </summary>
    public WorktreeAgentIsolationStrategy(
        IProcessRunner processRunner,
        IOptions<AgentProcessManagerOptions> options,
        ILogger<WorktreeAgentIsolationStrategy> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
        _agentsDirectoryName = string.IsNullOrWhiteSpace(options.Value.AgentsDirectory)
            ? ".agents"
            : options.Value.AgentsDirectory.Trim();
    }

    /// <inheritdoc/>
    public string StrategyName => ModeName;

    /// <inheritdoc/>
    public async Task<string> PrepareWorkDirectoryAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var worktreePath = GetWorktreePath(normalizedWorkspace, agentId);
        var branchName = $"agent/{agentId}/{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        Directory.CreateDirectory(Path.GetDirectoryName(worktreePath)!);

        if (Directory.Exists(worktreePath))
        {
            _logger.LogInformation("Reusing existing agent worktree directory at {WorktreePath}", worktreePath);
            await CopyMarkerFileIfPresentAsync(normalizedWorkspace, worktreePath, ct).ConfigureAwait(false);
            return worktreePath;
        }

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(
                "git",
                $"worktree add \"{worktreePath}\" -b \"{branchName}\"",
                WorkingDirectory: normalizedWorkspace),
            ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Failed to create git worktree for agent '{agentId}': {result.Stderr}");

        await CopyMarkerFileIfPresentAsync(normalizedWorkspace, worktreePath, ct).ConfigureAwait(false);
        _logger.LogInformation("Created agent worktree at {WorktreePath} using branch {BranchName}", worktreePath, branchName);
        return worktreePath;
    }

    /// <inheritdoc/>
    public async Task CleanupAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var worktreePath = GetWorktreePath(normalizedWorkspace, agentId);
        if (!Directory.Exists(worktreePath))
            return;

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(
                "git",
                $"worktree remove \"{worktreePath}\" --force",
                WorkingDirectory: normalizedWorkspace),
            ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("Failed to remove agent worktree at {WorktreePath}: {Error}", worktreePath, result.Stderr);
            return;
        }

        _logger.LogInformation("Removed agent worktree at {WorktreePath}", worktreePath);
    }

    private async Task CopyMarkerFileIfPresentAsync(string workspacePath, string worktreePath, CancellationToken ct)
    {
        var markerSourcePath = Path.Combine(workspacePath, MarkerFileService.MarkerFileName);
        if (!File.Exists(markerSourcePath))
            return;

        Directory.CreateDirectory(worktreePath);
        var markerDestinationPath = Path.Combine(worktreePath, MarkerFileService.MarkerFileName);
        Directory.CreateDirectory(worktreePath);
        await using var source = File.OpenRead(markerSourcePath);
        await using var destination = File.Create(markerDestinationPath);
        await source.CopyToAsync(destination, ct).ConfigureAwait(false);
    }

    private string GetWorktreePath(string workspacePath, string agentId)
        => Path.Combine(workspacePath, _agentsDirectoryName, agentId);

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
