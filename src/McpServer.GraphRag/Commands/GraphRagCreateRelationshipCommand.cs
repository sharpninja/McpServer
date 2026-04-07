using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to create a new graph relationship.
/// </summary>
/// <param name="WorkspacePath">The workspace path for the relationship.</param>
/// <param name="Request">The relationship creation request payload.</param>
public sealed record GraphRagCreateRelationshipCommand(string WorkspacePath, GraphRelationshipRequest Request) : ICommand<GraphRelationshipResponse>;
