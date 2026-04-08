using McpServer.Cqrs;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: CQRS command to delete a graph relationship by ID.
/// </summary>
/// <param name="WorkspacePath">The workspace path containing the relationship.</param>
/// <param name="RelationshipId">The identifier of the relationship to delete.</param>
public sealed record GraphRagDeleteRelationshipCommand(string WorkspacePath, string RelationshipId) : ICommand<bool>;
