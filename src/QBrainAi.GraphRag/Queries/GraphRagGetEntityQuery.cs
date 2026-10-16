using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Models;

namespace QBrainAi.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS query to retrieve a graph entity by ID.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="EntityId">The identifier of the entity to retrieve.</param>
public sealed record GraphRagGetEntityQuery(string WorkspacePath, string EntityId) : IQuery<GraphEntityResponse>;
