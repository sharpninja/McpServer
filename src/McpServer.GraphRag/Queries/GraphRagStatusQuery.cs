using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// CQRS query to retrieve the current GraphRAG status for a workspace.
/// </summary>
/// <param name="WorkspacePath">The workspace path to retrieve status for.</param>
public sealed record GraphRagStatusQuery(string WorkspacePath) : IQuery<GraphRagStatusResponse>;
