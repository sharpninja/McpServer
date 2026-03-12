using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Creates a shallow git clone under the configured agents directory for agent execution.
/// </summary>
public sealed class CloneAgentIsolationStrategy : IAgentIsolationStrategy
{
    /// <summary>
    /// Canonical mode name for clone isolation.
    /// </summary>
    public const string ModeName = "clone";

    private readonly IProcessRunner _processRunner;
    private readonly ILogger<CloneAgentIsolationStrategy> _logger;
    private readonly string _agentsDirectoryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloneAgentIsolationStrategy"/> class.
    /// </summary>
    public CloneAgentIsolationStrategy(
        IProcessRunner processRunner,
        IOptions<AgentProcessManagerOptions> options,
        ILogger<CloneAgentIsolationStrategy> logger)
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
        var clonePath = GetClonePath(normalizedWorkspace, agentId);
        Directory.CreateDirectory(Path.GetDirectoryName(clonePath)!);

        if (Directory.Exists(clonePath))
        {
            _logger.LogInformation("Reusing existing agent clone directory at {ClonePath}", clonePath);
            await CopyMarkerFileIfPresentAsync(normalizedWorkspace, clonePath, ct).ConfigureAwait(false);
            return clonePath;
        }

        var result = await _processRunner.RunAsync(
            new ProcessRunRequest(
                "git",
                $"clone --depth 1 --single-branch \"{normalizedWorkspace}\" \"{clonePath}\"",
                WorkingDirectory: normalizedWorkspace),
            ct).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Failed to create shallow clone for agent '{agentId}': {result.Stderr}");

        await CopyMarkerFileIfPresentAsync(normalizedWorkspace, clonePath, ct).ConfigureAwait(false);
        _logger.LogInformation("Created agent shallow clone at {ClonePath}", clonePath);
        return clonePath;
    }

    /// <inheritdoc/>
    public Task CleanupAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkspace = NormalizePath(workspacePath);
        var clonePath = GetClonePath(normalizedWorkspace, agentId);
        if (!Directory.Exists(clonePath))
            return Task.CompletedTask;

        Directory.Delete(clonePath, recursive: true);
        _logger.LogInformation("Removed agent clone directory at {ClonePath}", clonePath);
        return Task.CompletedTask;
    }

    private async Task CopyMarkerFileIfPresentAsync(string workspacePath, string clonePath, CancellationToken ct)
    {
        var markerSourcePath = Path.Combine(workspacePath, MarkerFileService.MarkerFileName);
        if (!File.Exists(markerSourcePath))
            return;

        var markerDestinationPath = Path.Combine(clonePath, MarkerFileService.MarkerFileName);
        Directory.CreateDirectory(clonePath);
        await using var source = File.OpenRead(markerSourcePath);
        await using var destination = File.Create(markerDestinationPath);
        await source.CopyToAsync(destination, ct).ConfigureAwait(false);
    }

    private string GetClonePath(string workspacePath, string agentId)
        => Path.Combine(workspacePath, _agentsDirectoryName, $"{agentId}-clone");

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
