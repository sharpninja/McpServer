using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.GraphRag;

/// <summary>
/// FR-MCP-084: Abstraction for querying GraphRAG data (entities, relationships,
/// documents, and graph queries) from a remote federated MCP server target.
/// Methods return <c>null</c> on remote failure so the federated decorator
/// can gracefully fall back to local-only results.
/// </summary>
public interface IGraphRagFederationClient
{
    /// <summary>FR-MCP-084: Query graph entities from a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="skip">Number of entities to skip.</param>
    /// <param name="take">Number of entities to take.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote entity list, or <c>null</c> if the remote call failed.</returns>
    Task<GraphEntityListResponse?> QueryEntitiesAsync(FederationTarget target, int skip, int take, string? entityType, CancellationToken ct = default);

    /// <summary>FR-MCP-084: Query graph relationships from a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="skip">Number of relationships to skip.</param>
    /// <param name="take">Number of relationships to take.</param>
    /// <param name="entityId">Optional entity ID filter.</param>
    /// <param name="relationshipType">Optional relationship type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote relationship list, or <c>null</c> if the remote call failed.</returns>
    Task<GraphRelationshipListResponse?> QueryRelationshipsAsync(FederationTarget target, int skip, int take, string? entityId, string? relationshipType, CancellationToken ct = default);

    /// <summary>FR-MCP-084: Query GraphRAG documents from a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="skip">Number of documents to skip.</param>
    /// <param name="take">Number of documents to take.</param>
    /// <param name="sourceType">Optional source type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote document list, or <c>null</c> if the remote call failed.</returns>
    Task<GraphRagDocumentListResponse?> QueryDocumentsAsync(FederationTarget target, int skip, int take, string? sourceType, CancellationToken ct = default);

    /// <summary>FR-MCP-084: Execute a GraphRAG query against a remote federation target.</summary>
    /// <param name="target">Resolved federation target.</param>
    /// <param name="request">Graph query request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Remote query response, or <c>null</c> if the remote call failed.</returns>
    Task<GraphRagQueryResponse?> QueryGraphRagAsync(FederationTarget target, GraphRagQueryRequest request, CancellationToken ct = default);
}
