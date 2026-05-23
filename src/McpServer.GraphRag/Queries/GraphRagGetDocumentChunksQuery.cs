using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: CQRS query to retrieve all chunks for a specific document.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="DocumentId">The identifier of the document whose chunks to retrieve.</param>
public sealed record GraphRagGetDocumentChunksQuery(string WorkspacePath, string DocumentId) : IQuery<GraphRagDocumentChunksResponse>;
