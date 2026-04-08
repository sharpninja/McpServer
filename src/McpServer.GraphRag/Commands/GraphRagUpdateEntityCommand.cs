using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to update an existing graph entity.
/// </summary>
/// <param name="WorkspacePath">The workspace path for the entity.</param>
/// <param name="EntityId">The identifier of the entity to update.</param>
/// <param name="Request">The entity update request payload.</param>
public sealed record GraphRagUpdateEntityCommand(string WorkspacePath, string EntityId, GraphEntityRequest Request) : ICommand<GraphEntityResponse>;
