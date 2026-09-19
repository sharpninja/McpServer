using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: CQRS command to index one memory into the dedicated memory ANN/FTS index.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="MemoryId">MEMORY-* id to index.</param>
/// <param name="Provider">Optional embedding provider (onnx or cloud).</param>
/// <param name="CloudEnabled">When false, cloud embedding must fail closed.</param>
public sealed record IndexMemoryCommand(
    string WorkspacePath,
    string MemoryId,
    string? Provider = null,
    bool CloudEnabled = false) : ICommand<MemoryIndexResult>;

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: CQRS command to reconcile stale EmbeddingStatus vs row hash.
/// Production S2 Green handler repairs stale EmbeddingStatus.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
public sealed record ReconcileMemoryIndexCommand(
    string WorkspacePath) : ICommand<MemoryIndexResult>;

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: CQRS command to batch-index memories to a terminal EmbeddingStatus.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="MemoryIds">Optional explicit id set. Null means all pending in the workspace effective set.</param>
public sealed record BatchIndexMemoriesCommand(
    string WorkspacePath,
    IReadOnlyList<string>? MemoryIds = null) : ICommand<MemoryIndexResult>;
