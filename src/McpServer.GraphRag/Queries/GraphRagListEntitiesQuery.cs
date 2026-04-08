using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS query to list graph entities with pagination and optional type filter.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="Skip">Number of entities to skip.</param>
/// <param name="Take">Maximum number of entities to return.</param>
/// <param name="EntityType">Optional entity type filter.</param>
public sealed record GraphRagListEntitiesQuery(string WorkspacePath, int Skip, int Take, string? EntityType) : IQuery<GraphEntityListResponse>;
