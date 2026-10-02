using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-013 / TEST-MCP-MEMORY-013: CQRS command for consolidate / sleep merge.
/// S4 Green registers the CQRS handler.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Request">Consolidate payload. Null body uses dry-run default.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
public sealed record ConsolidateMemoryCommand(
    string WorkspacePath,
    MemoryConsolidateRequest? Request,
    bool ReadOnlyCaller = false) : ICommand<MemoryConsolidateResult>;
