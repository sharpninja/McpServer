using McpServer.Cqrs;
using McpServer.GraphRag.Commands;
using McpServer.GraphRag.Queries;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.GraphRag;

/// <summary>
/// DI registration extensions for the McpServer.GraphRag library.
/// </summary>
public static class GraphRagServiceCollectionExtensions
{
    /// <summary>
    /// Registers GraphRAG services, backend adapters, and CQRS handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpGraphRag(this IServiceCollection services)
    {
        services.AddScoped<IGraphRagService, GraphRagService>();
        services.AddTransient<IGraphRagBackendAdapter, ExternalCommandGraphRagBackendAdapter>();
        services.AddTransient<IGraphRagBackendAdapter, InternalFallbackGraphRagBackendAdapter>();
        services.AddMcpGraphRagCqrsHandlers();
        return services;
    }

    private static IServiceCollection AddMcpGraphRagCqrsHandlers(this IServiceCollection services)
    {
        services.AddTransient<ICommandHandler<GraphRagCreateEntityCommand, GraphEntityResponse>, GraphRagCreateEntityCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagCreateRelationshipCommand, GraphRelationshipResponse>, GraphRagCreateRelationshipCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagDeleteDocumentCommand, GraphRagDocumentDeleteResponse>, GraphRagDeleteDocumentCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagDeleteEntityCommand, bool>, GraphRagDeleteEntityCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagDeleteRelationshipCommand, bool>, GraphRagDeleteRelationshipCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagIndexCommand, GraphRagStatusResponse>, GraphRagIndexCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagIngestTextCommand, GraphRagIngestTextResponse>, GraphRagIngestTextCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagUpdateEntityCommand, GraphEntityResponse>, GraphRagUpdateEntityCommandHandler>();
        services.AddTransient<ICommandHandler<GraphRagUpdateRelationshipCommand, GraphRelationshipResponse>, GraphRagUpdateRelationshipCommandHandler>();

        services.AddTransient<IQueryHandler<GraphRagGetDocumentChunksQuery, GraphRagDocumentChunksResponse>, GraphRagGetDocumentChunksQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagGetEntityQuery, GraphEntityResponse>, GraphRagGetEntityQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagGetRelationshipQuery, GraphRelationshipResponse>, GraphRagGetRelationshipQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagListDocumentsQuery, GraphRagDocumentListResponse>, GraphRagListDocumentsQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagListEntitiesQuery, GraphEntityListResponse>, GraphRagListEntitiesQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagListRelationshipsQuery, GraphRelationshipListResponse>, GraphRagListRelationshipsQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagQueryQuery, GraphRagQueryResponse>, GraphRagQueryQueryHandler>();
        services.AddTransient<IQueryHandler<GraphRagStatusQuery, GraphRagStatusResponse>, GraphRagStatusQueryHandler>();
        return services;
    }
}
