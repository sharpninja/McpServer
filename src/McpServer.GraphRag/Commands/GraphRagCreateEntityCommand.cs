using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to create a new graph entity.
/// </summary>
/// <param name="WorkspacePath">The workspace path for the entity.</param>
/// <param name="Request">The entity creation request payload.</param>
public sealed record GraphRagCreateEntityCommand(string WorkspacePath, GraphEntityRequest Request) : ICommand<GraphEntityResponse>;
