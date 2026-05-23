using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-078, TR-GRAPHRAG-ADHOC-001: CQRS command to ingest raw text into the GraphRAG corpus.
/// </summary>
/// <param name="WorkspacePath">The workspace path to ingest into.</param>
/// <param name="Request">The ingest text request payload.</param>
public sealed record GraphRagIngestTextCommand(string WorkspacePath, GraphRagIngestTextRequest Request) : ICommand<GraphRagIngestTextResponse>;
