using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: CQRS command to delete a document and its chunks from the GraphRAG corpus.
/// </summary>
/// <param name="WorkspacePath">The workspace path containing the document.</param>
/// <param name="DocumentId">The identifier of the document to delete.</param>
public sealed record GraphRagDeleteDocumentCommand(string WorkspacePath, string DocumentId) : ICommand<GraphRagDocumentDeleteResponse>;
