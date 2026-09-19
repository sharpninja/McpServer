using McpServer.Cqrs;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014 / TR-MCP-MEMORY-API-002: CQRS query for ordered memory versions.
/// </summary>
/// <param name="WorkspacePath">Active workspace path.</param>
/// <param name="MemoryId">Parent MEMORY-* id.</param>
public sealed record ListMemoryVersionsQuery(
    string WorkspacePath,
    string MemoryId) : IQuery<MemoryVersionListResult>;
