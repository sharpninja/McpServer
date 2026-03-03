using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013, TR-GH-013-004: Runs repo, session-log, and issue ingestion, persists to McpDbContext.
/// FR-SUPPORT-010: Records sync status for sync.run / sync.status. Generates embeddings for vector search.
/// </summary>
public sealed class IngestionCoordinator
{
    private readonly McpDbContext _db;
    private readonly RepoIngestor _repoIngestor;
    private readonly SessionLogIngestor _sessionLogIngestor;
    private readonly ExternalDocsIngestor _externalDocsIngestor;
    private readonly GitHubIngestor _gitHubIngestor;
    private readonly IssueIngestor _issueIngestor;
    private readonly ISyncStatusStore _syncStatusStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndexService _vectorIndexService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<IngestionCoordinator> _logger;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    public IngestionCoordinator(
        McpDbContext db,
        RepoIngestor repoIngestor,
        SessionLogIngestor sessionLogIngestor,
        ExternalDocsIngestor externalDocsIngestor,
        GitHubIngestor gitHubIngestor,
        IssueIngestor issueIngestor,
        ISyncStatusStore syncStatusStore,
        IEmbeddingService embeddingService,
        IVectorIndexService vectorIndexService,
        WorkspaceContext workspaceContext,
        ILogger<IngestionCoordinator> logger)
    {
        _db = db;
        _repoIngestor = repoIngestor;
        _sessionLogIngestor = sessionLogIngestor;
        _externalDocsIngestor = externalDocsIngestor;
        _gitHubIngestor = gitHubIngestor;
        _issueIngestor = issueIngestor;
        _syncStatusStore = syncStatusStore;
        _embeddingService = embeddingService;
        _vectorIndexService = vectorIndexService;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>FR-SUPPORT-010: Runs full ingestion and returns result.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The sync run result with ingestion counts and status.</returns>
    public async Task<SyncRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString("N");
        var started = DateTime.UtcNow;
        StoreSyncResult(new SyncRunResult
        {
            RunId = runId,
            StartedAt = started,
            Status = "Running",
            DocumentsIngested = 0,
            ChunksWritten = 0
        });

        var docsIngested = 0;
        var chunksWritten = 0;
        try
        {
            var sessionResults = await _sessionLogIngestor.IngestAsync(cancellationToken).ConfigureAwait(false);
            var repoResults = await _repoIngestor.IngestAsync(cancellationToken).ConfigureAwait(false);
            var externalResults = await _externalDocsIngestor.IngestAsync(cancellationToken).ConfigureAwait(false);
            var githubResults = await _gitHubIngestor.IngestAsync(cancellationToken).ConfigureAwait(false);
            var issueResults = await _issueIngestor.IngestAsync(cancellationToken).ConfigureAwait(false);

            foreach (var (doc, chunks) in sessionResults)
            {
                await UpsertDocumentAndChunksAsync(doc, chunks, cancellationToken).ConfigureAwait(false);
                docsIngested++;
                chunksWritten += chunks.Count;
            }

            foreach (var (doc, chunks) in repoResults)
            {
                await UpsertDocumentAndChunksAsync(doc, chunks, cancellationToken).ConfigureAwait(false);
                docsIngested++;
                chunksWritten += chunks.Count;
            }

            foreach (var (doc, chunks) in externalResults)
            {
                await UpsertDocumentAndChunksAsync(doc, chunks, cancellationToken).ConfigureAwait(false);
                docsIngested++;
                chunksWritten += chunks.Count;
            }

            foreach (var (doc, chunks) in githubResults)
            {
                await UpsertDocumentAndChunksAsync(doc, chunks, cancellationToken).ConfigureAwait(false);
                docsIngested++;
                chunksWritten += chunks.Count;
            }

            foreach (var (doc, chunks) in issueResults)
            {
                await UpsertDocumentAndChunksAsync(doc, chunks, cancellationToken).ConfigureAwait(false);
                docsIngested++;
                chunksWritten += chunks.Count;
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // MVP-SUPPORT-011: Import JSON session logs into 4NF session log tables
            var sessionLogResult = await _sessionLogIngestor.ImportToSessionLogTablesAsync(cancellationToken).ConfigureAwait(false);

            var result = new SyncRunResult
            {
                RunId = runId,
                StartedAt = started,
                CompletedAt = DateTime.UtcNow,
                Status = "Completed",
                DocumentsIngested = docsIngested,
                ChunksWritten = chunksWritten,
                SessionLogsImported = sessionLogResult.Imported
            };
            _syncStatusStore.SetLast(result);
            StoreSyncResult(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            var cancelled = new SyncRunResult
            {
                RunId = runId,
                StartedAt = started,
                CompletedAt = DateTime.UtcNow,
                Status = "Cancelled",
                Error = "Operation was cancelled",
                DocumentsIngested = docsIngested,
                ChunksWritten = chunksWritten
            };
            StoreSyncResult(cancelled);
            throw;
        }
        catch (Exception ex)
        {
            var innerMsg = ex is DbUpdateException dbEx && dbEx.InnerException is not null
                ? $"{ex.Message} -> {dbEx.InnerException.Message}"
                : ex.Message;
            _logger.LogError(ex, "Sync run {RunId} failed: {Error}", runId, innerMsg);
            var result = new SyncRunResult
            {
                RunId = runId,
                StartedAt = started,
                CompletedAt = DateTime.UtcNow,
                Status = "Failed",
                Error = innerMsg,
                DocumentsIngested = docsIngested,
                ChunksWritten = chunksWritten
            };
            StoreSyncResult(result);
            return result;
        }
    }

    /// <summary>Stores result in both global and workspace-keyed store.</summary>
    private void StoreSyncResult(SyncRunResult result)
    {
        _syncStatusStore.SetLast(result);
        var wsId = _workspaceContext.WorkspacePath;
        if (!string.IsNullOrEmpty(wsId))
            _syncStatusStore.SetLast(wsId, result);
    }

    private async Task UpsertDocumentAndChunksAsync(
        Models.ContextDocument doc,
        IReadOnlyList<Models.ContextChunk> chunks,
        CancellationToken cancellationToken)
    {
        var existing = await _db.Documents.FindAsync(new object[] { doc.Id }, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            existing.SourceType = doc.SourceType;
            existing.SourceKey = doc.SourceKey;
            existing.IngestedAt = doc.IngestedAt;
            existing.ContentHash = doc.ContentHash;
            var toRemove = await _db.Chunks.Where(c => c.DocumentId == doc.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
            _db.Chunks.RemoveRange(toRemove);
        }
        else
        {
            _db.Documents.Add(new ContextDocumentEntity
            {
                Id = doc.Id,
                SourceType = doc.SourceType,
                SourceKey = doc.SourceKey,
                IngestedAt = doc.IngestedAt,
                ContentHash = doc.ContentHash
            });
        }

        var chunkEntities = new List<ContextChunkEntity>();
        foreach (var c in chunks)
        {
            var entity = new ContextChunkEntity
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                Content = c.Content,
                TokenCount = c.TokenCount,
                ChunkIndex = c.ChunkIndex
            };
            _db.Chunks.Add(entity);
            chunkEntities.Add(entity);
        }

        // TR-PLANNED-013: Generate embeddings and add to vector index if embedding service is available
        if (_embeddingService.IsAvailable)
        {
            const int batchSize = 32;
            for (var i = 0; i < chunkEntities.Count; i += batchSize)
            {
                var batch = chunkEntities.Skip(i).Take(batchSize).ToList();
                foreach (var entity in batch)
                {
                    try
                    {
                        var embedding = _embeddingService.GenerateEmbedding(entity.Content);
                        var bytes = new byte[embedding.Length * sizeof(float)];
                        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
                        entity.Embedding = bytes;
                        _vectorIndexService.AddVector(entity.Id, embedding);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to generate embedding for chunk {ChunkId}", entity.Id);
                    }
                }

                if (i > 0 && i % 100 == 0)
                    _logger.LogDebug("Embedding progress: {Count}/{Total} chunks processed", i, chunkEntities.Count);
            }
        }
    }
}
