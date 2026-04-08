using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS query to retrieve a graph relationship by ID.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="RelationshipId">The identifier of the relationship to retrieve.</param>
public sealed record GraphRagGetRelationshipQuery(string WorkspacePath, string RelationshipId) : IQuery<GraphRelationshipResponse>;
