using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014 / TR-MCP-MEMORY-API-002: CQRS command to revert a memory to snapshot N.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="MemoryId">Target MEMORY-* id.</param>
/// <param name="VersionNumber">Snapshot to restore.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
public sealed record RevertMemoryCommand(
    string WorkspacePath,
    string MemoryId,
    int VersionNumber,
    bool ReadOnlyCaller = false) : ICommand<MemoryRevertResult>;
