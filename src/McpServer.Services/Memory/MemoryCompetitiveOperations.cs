using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010 / FR-MCP-MEMORY-014: CQRS-owned remember/version/revert/recall operations.
/// Not a public <see cref="IMemoryService"/> facade.
/// </summary>
public sealed class MemoryCompetitiveOperations
{
    private readonly McpDbContext _db;

    /// <summary>Creates operations bound to an already-scoped <see cref="McpDbContext"/>.</summary>
    public MemoryCompetitiveOperations(McpDbContext db)
    {
        _db = db;
    }

    /// <summary>AC-FR-MCP-MEMORY-010-01: Remembers a multi-layer memory.</summary>
    public async Task<MemoryRememberResult> RememberAsync(
        MemoryRememberRequest? request,
        bool readOnlyCaller,
        CancellationToken cancellationToken)
    {
        if (readOnlyCaller)
            return new MemoryRememberResult(403, FailureKind: MemoryMutationFailureKind.Validation, Error: "Read-only caller cannot remember.");

        if (request is null)
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Request body is required.");

        var validation = ValidateRemember(request);
        if (validation is not null)
            return validation;

        var service = new MemoryService(_db, Microsoft.Extensions.Logging.Abstractions.NullLogger<MemoryService>.Instance);
        var add = await service.AddAsync(new MemoryAddRequest
        {
            Id = request.Id,
            Category = request.Type ?? request.Category ?? "fact",
            Scope = request.Scope ?? MemoryScope.Workspace,
            Text = request.Content ?? string.Empty,
            Title = request.Title,
            Summary = request.Summary,
            Content = request.Content,
            Type = request.Type,
            Tags = request.Tags,
            Confidence = request.Confidence,
            SourceKind = request.SourceKind,
            SourceRef = request.SourceRef,
            CreatedBy = request.CreatedBy,
            UpdatedBy = request.UpdatedBy,
        }, cancellationToken).ConfigureAwait(false);

        if (!add.Success)
        {
            var status = add.FailureKind switch
            {
                MemoryMutationFailureKind.Conflict => 409,
                MemoryMutationFailureKind.NotFound => 404,
                _ => 400,
            };
            return new MemoryRememberResult(status, FailureKind: add.FailureKind, Error: add.Error);
        }

        return new MemoryRememberResult(201, add.Memory!.Id, add.Memory);
    }

    /// <summary>AC-FR-MCP-MEMORY-014: Lists versions for a visible memory.</summary>
    public async Task<MemoryVersionListResult> ListVersionsAsync(string memoryId, CancellationToken cancellationToken)
    {
        var parent = await _db.Memories
            .AsNoTracking()
            .FirstOrDefaultAsync(memory => memory.Id == memoryId, cancellationToken)
            .ConfigureAwait(false);
        if (parent is null)
            return new MemoryVersionListResult(404, Error: $"Memory '{memoryId}' not found.");

        var rows = await _db.MemoryVersions
            .AsNoTracking()
            .Where(version => version.MemoryId == memoryId)
            .OrderBy(version => version.VersionNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = rows.Select(row => new MemoryVersionSnapshot
        {
            VersionNumber = row.VersionNumber,
            Title = row.Title,
            Content = row.Content,
            CreatedAtUtc = row.CreatedAtUtc,
        }).ToList();

        return new MemoryVersionListResult(200, items);
    }

    /// <summary>AC-FR-MCP-MEMORY-014: Reverts a memory to snapshot N and appends a new version.</summary>
    public async Task<MemoryRevertResult> RevertAsync(
        string memoryId,
        int versionNumber,
        bool readOnlyCaller,
        CancellationToken cancellationToken)
    {
        if (readOnlyCaller)
            return new MemoryRevertResult(403, FailureKind: MemoryMutationFailureKind.Validation, Error: "Read-only caller cannot revert.");

        var entity = await _db.Memories
            .FirstOrDefaultAsync(memory => memory.Id == memoryId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return new MemoryRevertResult(404, FailureKind: MemoryMutationFailureKind.NotFound, Error: $"Memory '{memoryId}' not found.");

        var snapshot = await _db.MemoryVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                version => version.MemoryId == memoryId && version.VersionNumber == versionNumber,
                cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
            return new MemoryRevertResult(404, FailureKind: MemoryMutationFailureKind.NotFound, Error: $"Version {versionNumber} was not found.");

        entity.Title = snapshot.Title;
        entity.Content = snapshot.Content;
        entity.Text = snapshot.Content;
        entity.Version++;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
        entity.EmbeddingStatus = "pending";

        var nextNumber = await NextVersionNumberAsync(memoryId, cancellationToken).ConfigureAwait(false);
        _db.MemoryVersions.Add(new MemoryVersionEntity
        {
            MemoryId = entity.Id,
            VersionNumber = nextNumber,
            Title = entity.Title,
            Content = MemoryLayerMapper.EffectiveContent(entity),
            CreatedAtUtc = entity.UpdatedAtUtc,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await MemorySessionLogHook.TryAppendAsync(_db, entity.Id, "memory_revert", cancellationToken).ConfigureAwait(false);
        return new MemoryRevertResult(200, MemoryLayerMapper.ToItem(entity));
    }

    /// <summary>S1 recall used to prove revert refreshes the visible index (keyword over Title/Content).</summary>
    public async Task<MemoryQueryResult> RecallAsync(string query, CancellationToken cancellationToken)
    {
        var keyword = query.Trim().ToLowerInvariant();
        var rows = await _db.Memories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var items = rows
            .Select(MemoryLayerMapper.ToItem)
            .Where(item =>
                (item.Content ?? item.Text ?? string.Empty).ToLowerInvariant().Contains(keyword, StringComparison.Ordinal)
                || (item.Title ?? string.Empty).ToLowerInvariant().Contains(keyword, StringComparison.Ordinal)
                || item.Id.ToLowerInvariant().Contains(keyword, StringComparison.Ordinal))
            .ToList();
        return new MemoryQueryResult(items, items.Count);
    }

    /// <summary>Appends a version snapshot for create or content change.</summary>
    public async Task AppendVersionAsync(MemoryEntity entity, CancellationToken cancellationToken)
    {
        var nextNumber = await NextVersionNumberAsync(entity.Id, cancellationToken).ConfigureAwait(false);
        _db.MemoryVersions.Add(new MemoryVersionEntity
        {
            MemoryId = entity.Id,
            VersionNumber = nextNumber,
            Title = entity.Title,
            Content = MemoryLayerMapper.EffectiveContent(entity),
            CreatedAtUtc = entity.UpdatedAtUtc,
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> NextVersionNumberAsync(string memoryId, CancellationToken cancellationToken)
    {
        var max = await _db.MemoryVersions
            .IgnoreQueryFilters()
            .Where(version => version.MemoryId == memoryId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);
        return (max ?? 0) + 1;
    }

    private static MemoryRememberResult? ValidateRemember(MemoryRememberRequest request)
    {
        var content = request.Content;
        if (string.IsNullOrWhiteSpace(content))
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Content is required.");

        if (MemoryPolicy.RejectsSecrets(content))
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Memory content matches a rejected secret pattern.");

        if (content.Length > MemoryLimits.MaxContentLength)
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Content exceeds the configured maximum length.");

        if (request.Title is { Length: > MemoryLimits.MaxTitleLength })
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Title exceeds the configured maximum length.");

        if (!string.IsNullOrWhiteSpace(request.Type)
            && !MemoryLimits.AllowedTypes.Contains(request.Type.Trim()))
        {
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Type is not an allowed memory type.");
        }

        if (request.Confidence is < 0 or > 1)
            return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Confidence must be in [0,1].");

        if (request.Tags is not null)
        {
            foreach (var tag in request.Tags)
            {
                if (tag is { Length: > MemoryLimits.MaxTagLength })
                    return new MemoryRememberResult(400, FailureKind: MemoryMutationFailureKind.Validation, Error: "Tag exceeds the configured maximum length.");
            }
        }

        return null;
    }
}
