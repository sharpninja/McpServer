using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// CQRS query to execute a GraphRAG retrieval query against a workspace.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query against.</param>
/// <param name="QueryText">The query text to search for.</param>
/// <param name="TopK">The maximum number of context chunks to retrieve.</param>
public sealed record GraphRagQueryQuery(string WorkspacePath, string QueryText, int TopK) : IQuery<GraphRagQueryResponse>;
