using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-130: Writes committed Curiosity output into the context corpus after transaction commit.
/// </summary>
public sealed class BrainSlotContextAdmissionService : IBrainSlotContextAdmissionService
{
    private readonly McpDbContext _db;
    private readonly Chunker _chunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorIndexService _vectorIndexService;
    private readonly ILogger<BrainSlotContextAdmissionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="BrainSlotContextAdmissionService"/> class.</summary>
    public BrainSlotContextAdmissionService(
        McpDbContext db,
        Chunker chunker,
        IEmbeddingService embeddingService,
        IVectorIndexService vectorIndexService,
        ILogger<BrainSlotContextAdmissionService> logger)
    {
        _db = db;
        _chunker = chunker;
        _embeddingService = embeddingService;
        _vectorIndexService = vectorIndexService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> AdmitAsync(BrainSlotDefinitionEntity slot, string output, string transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (!string.Equals(slot.Role, BrainSlotRoles.CuriosityEngine, StringComparison.Ordinal))
            throw new BrainSlotValidationException(
                "Only CuriosityEngine committed output may be admitted to GraphRAG/context in this slice.",
                BrainSlotReasonCodes.DeferredFeatureDisabled);

        if (string.IsNullOrWhiteSpace(output))
            return null;

        var documentId = $"brain-slot-{slot.SlotId}-{Guid.NewGuid():N}";
        var sourceKey = $"{slot.Role}:{slot.SlotId}:{transactionId}";
        var chunks = _chunker.Chunk(documentId, output);
        var contentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output))).ToLowerInvariant();

        _db.Documents.Add(new ContextDocumentEntity
        {
            Id = documentId,
            SourceType = "brain-slot-curiosity",
            SourceKey = sourceKey,
            IngestedAt = DateTime.UtcNow,
            ContentHash = contentHash,
        });

        foreach (var chunk in chunks)
        {
            var embedding = _embeddingService.GenerateEmbedding(chunk.Content);
            _db.Chunks.Add(new ContextChunkEntity
            {
                Id = chunk.Id,
                DocumentId = documentId,
                Content = chunk.Content,
                TokenCount = chunk.TokenCount,
                ChunkIndex = chunk.ChunkIndex,
                Embedding = EmbeddingToBytes(embedding),
            });
            _vectorIndexService.AddVector(chunk.Id, embedding);
        }

        _db.DataAuditLogs.Add(new DataAuditLogEntity
        {
            WorkspaceId = _db.CurrentWorkspaceId,
            EntityKind = "BrainSlotContextAdmission",
            EntityKey = documentId,
            Action = "admit",
            SourceType = nameof(BrainSlotContextAdmissionService),
            Actor = "system",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                slot.SlotId,
                slot.Role,
                transactionId,
                chunkCount = chunks.Count,
            }),
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Brain slot Curiosity output admitted to context: {DocumentId}", documentId);
        return documentId;
    }

    private static byte[] EmbeddingToBytes(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
