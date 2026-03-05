using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;

namespace McpServer.Storage;

/// <summary>
/// DI registration extension methods for McpServer.Storage services.
/// </summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers stateless storage services: embedding, vector index, and sync status store.
    /// DbContext registration stays in Program.cs in McpServer.Support.Mcp because it needs
    /// runtime config (connection string, workspace path).
    /// </summary>
    public static IServiceCollection AddMcpStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<IVectorIndexService, VectorIndexService>();
        services.AddHostedService<VectorIndexStartupService>();
        services.AddTransient<ISyncStatusStore, SyncStatusStore>();
        return services;
    }
}
