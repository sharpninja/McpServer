namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Uses the original workspace path without additional isolation.
/// </summary>
public sealed class NoneAgentIsolationStrategy : IAgentIsolationStrategy
{
    /// <summary>
    /// Canonical mode name for no isolation.
    /// </summary>
    public const string ModeName = "none";

    /// <inheritdoc/>
    public string StrategyName => ModeName;

    /// <inheritdoc/>
    public Task<string> PrepareWorkDirectoryAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return Task.FromResult(NormalizePath(workspacePath));
    }

    /// <inheritdoc/>
    public Task CleanupAsync(string workspacePath, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
