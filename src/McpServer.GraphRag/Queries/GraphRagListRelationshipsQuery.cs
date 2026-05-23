using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS query to list graph relationships with pagination and optional filters.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="Skip">Number of relationships to skip.</param>
/// <param name="Take">Maximum number of relationships to return.</param>
/// <param name="EntityId">Optional entity ID filter.</param>
/// <param name="RelationshipType">Optional relationship type filter.</param>
public sealed record GraphRagListRelationshipsQuery(string WorkspacePath, int Skip, int Take, string? EntityId, string? RelationshipType) : IQuery<GraphRelationshipListResponse>;
