using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to update an existing graph relationship.
/// </summary>
/// <param name="WorkspacePath">The workspace path for the relationship.</param>
/// <param name="RelationshipId">The identifier of the relationship to update.</param>
/// <param name="Request">The relationship update request payload.</param>
public sealed record GraphRagUpdateRelationshipCommand(string WorkspacePath, string RelationshipId, GraphRelationshipRequest Request) : ICommand<GraphRelationshipResponse>;
