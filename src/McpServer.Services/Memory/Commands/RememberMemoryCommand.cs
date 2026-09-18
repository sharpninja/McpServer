using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010 / TR-MCP-MEMORY-API-002: CQRS command for <c>memory_remember</c>.
/// Handlers own the API. No public IMemoryService remember facade.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Request">Remember payload. Null body must fail validation.</param>
/// <param name="ReadOnlyCaller">True when the caller authenticated with a read-only key.</param>
public sealed record RememberMemoryCommand(
    string WorkspacePath,
    MemoryRememberRequest? Request,
    bool ReadOnlyCaller = false) : ICommand<MemoryRememberResult>;
