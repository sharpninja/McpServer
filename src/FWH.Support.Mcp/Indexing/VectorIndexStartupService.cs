using FWH.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FWH.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-013: Hosted service that loads persisted HNSW index on startup and saves on shutdown.
/// FR-SUPPORT-010: Ensures vector index is populated from DB when no persisted index exists.
/// </summary>
internal sealed class VectorIndexStartupService(
    IVectorIndexService vectorIndex,
    IEmbeddingService embeddingService,
    IServiceScopeFactory scopeFactory,
    IOptions<VectorIndexOptions> options,
    ILogger<VectorIndexStartupService> logger) : IHostedService
{
    /// <summary>TR-PLANNED-013: Load or build the vector index at startup.</summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var indexPath = options.Value.IndexPath;

        if (File.Exists(indexPath + ".map"))
        {
            await vectorIndex.LoadAsync(indexPath, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("VectorIndexStartupService: loaded {Count} vectors in {Elapsed}ms", vectorIndex.Count, sw.ElapsedMilliseconds);
        }
        else if (embeddingService.IsAvailable)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            var chunks = await db.Chunks
                .Where(c => c.Embedding != null)
                .Select(c => new { c.Id, c.Embedding })
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var chunk in chunks)
            {
                if (chunk.Embedding is not null)
                {
                    var floats = new float[chunk.Embedding.Length / sizeof(float)];
                    Buffer.BlockCopy(chunk.Embedding, 0, floats, 0, chunk.Embedding.Length);
                    vectorIndex.AddVector(chunk.Id, floats);
                }
            }

            if (vectorIndex.Count > 0)
            {
                await vectorIndex.SaveAsync(indexPath, cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation("VectorIndexStartupService: built index with {Count} vectors from DB in {Elapsed}ms", vectorIndex.Count, sw.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation("VectorIndexStartupService: embedding service unavailable, skipping index build");
        }
    }

    /// <summary>TR-PLANNED-013: Save the vector index on shutdown.</summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (vectorIndex.Count > 0)
        {
            await vectorIndex.SaveAsync(options.Value.IndexPath, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("VectorIndexStartupService: saved {Count} vectors on shutdown", vectorIndex.Count);
        }
    }
}
