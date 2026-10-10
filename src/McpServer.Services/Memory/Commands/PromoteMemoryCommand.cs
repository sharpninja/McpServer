using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-015 / TEST-MCP-MEMORY-015: CQRS command for explicit promote into memory.
/// S4 Green registers the CQRS handler.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Request">Promote payload.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
public sealed record PromoteMemoryCommand(
    string WorkspacePath,
    MemoryPromoteRequest? Request,
    bool ReadOnlyCaller = false) : ICommand<MemoryPromoteResult>;
