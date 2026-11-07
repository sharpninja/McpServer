using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 (historical) / TR-MCP-MEMORY-MODEL-002 (historical): S1 Red acceptance for model/migrations.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-010; TR-MCP-MEMORY-MODEL-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (Applies_OnAllProviders and SchemaRecreate_Works) and the helpers only they use (FindRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_harness and Dispose) moved once into the abstract base class MemoryMigrationTestsBase in Memory/MemoryMigrationTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class MemoryMigrationTests : MemoryMigrationTestsBase, IDisposable
{
    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-02: Backfill sets Content from legacy text/guidance for all pre-existing rows.</summary>
    [Fact]
    public async Task Backfill_ContentFromLegacyText()
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        db.Memories.Add(new MemoryEntity
        {
            Id = "MEMORY-LEGACY-001",
            Category = "LEGACY",
            Scope = MemoryEntity.WorkspaceScope,
            WorkspaceId = _harness.WorkspaceA,
            Text = "legacy guidance text",
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _harness.GetCompatAsync("MEMORY-LEGACY-001", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal("legacy guidance text", stored?.Content);
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-04: MemoryVersion FK to Memory forbids orphan versions.</summary>
    [Fact]
    public void VersionFk_NoOrphans()
    {
        var entity = _harness.FindMappedEntity(typeof(MemoryVersionEntity));
        Assert.NotNull(entity);
        var fk = entity!.GetForeignKeys().SingleOrDefault(key =>
            key.PrincipalEntityType.ClrType == typeof(MemoryEntity));
        Assert.NotNull(fk);
        Assert.True(fk!.DeleteBehavior is DeleteBehavior.Cascade or DeleteBehavior.Restrict);
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-05: Soft-delete column/filter remains enforced by default queries.</summary>
    [Fact]
    public void SoftDeleteFilter_Default()
    {
        var memory = _harness.FindMappedEntity(typeof(MemoryEntity));
        Assert.NotNull(memory);
        var filters = memory!.GetDeclaredQueryFilters().Select(filter => filter.Key).ToArray();
        Assert.Contains("SoftDelete", filters);
        Assert.NotNull(memory.FindProperty("IsDeleted"));
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-07: Indexes exist for (WorkspaceId, Scope), EmbeddingStatus, and side tables.</summary>
    [Fact]
    public void Indexes_Present()
    {
        var memory = _harness.FindMappedEntity(typeof(MemoryEntity));
        Assert.NotNull(memory);
        Assert.NotNull(memory!.FindProperty("EmbeddingStatus"));
        var indexNames = memory.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(p => p.Name)))
            .ToArray();
        Assert.Contains(indexNames, name => name.Contains("WorkspaceId", StringComparison.Ordinal) && name.Contains("Scope", StringComparison.Ordinal));
        Assert.Contains(indexNames, name => name.Contains("EmbeddingStatus", StringComparison.Ordinal));
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-08: Column lengths match validated max lengths in API (no silent truncate).</summary>
    [Fact]
    public void ColumnLengths_MatchValidation()
    {
        var memory = _harness.FindMappedEntity(typeof(MemoryEntity));
        Assert.NotNull(memory);
        var title = memory!.FindProperty("Title");
        var content = memory.FindProperty("Content");
        Assert.NotNull(title);
        Assert.NotNull(content);
        Assert.Equal(MemoryLimits.MaxTitleLength, title!.GetMaxLength());
        Assert.True(content!.GetMaxLength() is null || content.GetMaxLength() >= MemoryLimits.MaxContentLength);
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-09: Provider-specific types do not break Tags JSON/array round-trip.</summary>
    [Fact]
    public async Task TagsRoundTrip_AllProviders()
    {
        var remembered = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Content = "tags round trip",
            Type = "fact",
            Tags = ["alpha", "beta"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var id = remembered.MemoryId;
        if (string.IsNullOrWhiteSpace(id))
        {
            var added = await _harness.AddCompatAsync(new MemoryAddRequest
            {
                Category = "tags",
                Text = "tags round trip",
                Tags = ["alpha", "beta"],
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            id = added.Memory?.Id;
        }

        var stored = await _harness.GetCompatAsync(id!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(["alpha", "beta"], stored?.Tags);
        Assert.NotNull(_harness.FindMappedEntity(typeof(MemoryEntity))?.FindProperty("Tags"));
    }

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-10: Audit/append-only versions hold under concurrent inserts.</summary>
    [Fact]
    public async Task VersionInsert_ConcurrentSafe()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "conc",
            Text = "base",
            Content = "base",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var updates = Enumerable.Range(0, 8).Select(i =>
            _harness.UpdateCompatAsync(
                added.Memory!.Id,
                new MemoryUpdateRequest { Text = "u" + i, Content = "u" + i },
                cancellationToken: TestContext.Current.CancellationToken));
        await Task.WhenAll(updates).ConfigureAwait(true);

        var versions = await _harness.ListVersionsAsync(added.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        var numbers = versions.Items!.Select(item => item.VersionNumber).ToArray();
        Assert.Equal(numbers.Distinct().Count(), numbers.Length);
    }
}
