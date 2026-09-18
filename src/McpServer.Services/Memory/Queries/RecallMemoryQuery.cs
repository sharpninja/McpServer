using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014-14 / FR-MCP-MEMORY-011: CQRS query used by S1 to prove revert refreshes recall.
/// S2 owns full recall behavior; S1 only requires post-revert index reflection.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="Query">Recall query text.</param>
public sealed record RecallMemoryQuery(
    string WorkspacePath,
    string Query) : IQuery<MemoryQueryResult>;
