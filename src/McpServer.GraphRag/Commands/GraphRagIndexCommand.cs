using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// CQRS command to trigger a GraphRAG index operation for a workspace.
/// </summary>
/// <param name="WorkspacePath">The workspace path to index.</param>
/// <param name="ForceReindex">When true, forces a full re-index even if already indexed.</param>
public sealed record GraphRagIndexCommand(string WorkspacePath, bool ForceReindex) : ICommand<GraphRagStatusResponse>;
