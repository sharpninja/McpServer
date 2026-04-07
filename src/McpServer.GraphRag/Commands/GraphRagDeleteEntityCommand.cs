using McpServer.Cqrs;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to delete a graph entity by ID.
/// </summary>
/// <param name="WorkspacePath">The workspace path containing the entity.</param>
/// <param name="EntityId">The identifier of the entity to delete.</param>
public sealed record GraphRagDeleteEntityCommand(string WorkspacePath, string EntityId) : ICommand<bool>;
