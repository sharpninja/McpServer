using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: CQRS query to list documents in the GraphRAG corpus with pagination.
/// </summary>
/// <param name="WorkspacePath">The workspace path to query.</param>
/// <param name="Skip">Number of documents to skip.</param>
/// <param name="Take">Maximum number of documents to return.</param>
/// <param name="SourceType">Optional source type filter.</param>
public sealed record GraphRagListDocumentsQuery(string WorkspacePath, int Skip, int Take, string? SourceType) : IQuery<GraphRagDocumentListResponse>;
