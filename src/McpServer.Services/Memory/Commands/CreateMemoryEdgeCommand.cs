using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: CQRS command to create a directed memory edge.
/// S3 Red ships the port only; no production handler is registered so edge tests stay Red.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Request">Create-edge payload.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
public sealed record CreateMemoryEdgeCommand(
    string WorkspacePath,
    MemoryCreateEdgeRequest? Request,
    bool ReadOnlyCaller = false) : ICommand<MemoryCreateEdgeResult>;

/// <summary>
/// FR-MCP-MEMORY-012: CQRS command to strengthen co-retrieved edges after a co-retrieval.
/// Must not change the recall ranking path. S3 Red ships the port only.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="MemoryIds">Co-retrieved MEMORY-* ids.</param>
/// <param name="HebbianEnabled">When false, the command must be a no-op.</param>
public sealed record RecordHebbianCoRetrievalCommand(
    string WorkspacePath,
    IReadOnlyList<string> MemoryIds,
    bool HebbianEnabled = true) : ICommand<MemoryHebbianResult>;
