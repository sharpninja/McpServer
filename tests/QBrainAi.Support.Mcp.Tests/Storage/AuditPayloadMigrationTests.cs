using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>Integration checks for additive audit payload migration and legacy reads.</summary>
public sealed class AuditPayloadMigrationTests
{
    [Fact]
    public async Task SqliteMigration_PreservesLegacyText_AndPersistsCompressedBytes()
    {
        await using var database = ProviderTestDatabase.CreateSqlite();
        await using (var db = database.CreateContext())
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var original = "{\"name\":\"Tokyo 東京\",\"data\":\"" + new string('x', 2000) + "\"}";
        await using (var db = database.CreateContext())
        {
            db.DataAuditLogs.Add(NewAudit("legacy", version: null, legacy: "{\"old\":true}"));
            var compressed = NewAudit("compressed", version: 1, legacy: null);
            compressed.PreviousSnapshotJson = "{\"before\":\"Å\"}";
            compressed.CurrentSnapshotJson = original;
            compressed.DiffJson = "{\"change\":true}";
            compressed.MetadataJson = "{\"actor\":\"東京\"}";
            db.DataAuditLogs.Add(compressed);
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await using (var db = database.CreateContext())
        {
            var rows = await db.DataAuditLogs.AsNoTracking().ToDictionaryAsync(
                row => row.EntityKey, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("{\"old\":true}", rows["legacy"].CurrentSnapshotJson);
            Assert.Null(rows["legacy"].CurrentSnapshotPayload);
            Assert.Equal("{\"before\":\"Å\"}", rows["compressed"].PreviousSnapshotJson);
            Assert.Equal(original, rows["compressed"].CurrentSnapshotJson);
            Assert.Equal("{\"change\":true}", rows["compressed"].DiffJson);
            Assert.Equal("{\"actor\":\"東京\"}", rows["compressed"].MetadataJson);
            Assert.Null(rows["compressed"].PreviousSnapshotJsonLegacy);
            Assert.Null(rows["compressed"].CurrentSnapshotJsonLegacy);
            Assert.Null(rows["compressed"].DiffJsonLegacy);
            Assert.Null(rows["compressed"].MetadataJsonLegacy);
            Assert.NotNull(rows["compressed"].PreviousSnapshotPayload);
            Assert.NotNull(rows["compressed"].CurrentSnapshotPayload);
            Assert.NotNull(rows["compressed"].DiffPayload);
            Assert.NotNull(rows["compressed"].MetadataPayload);
            Assert.True(rows["compressed"].CurrentSnapshotPayload!.Length < original.Length);
        }
    }

    [Fact]
    public async Task RuntimeWithoutAutoMigrate_RejectsPendingSchema_ThenAcceptsMigratedSchema()
    {
        await using var database = ProviderTestDatabase.CreateSqlite();
        var runtime = new McpDatabaseRuntimeOptions(
            database.ProviderOptions,
            new McpDatabaseEncryptionOptions(false, null, null, null, null, null, null),
            autoMigrate: false);
        await using var db = database.CreateContext();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => McpDatabaseMigrationCoordinator.EnsureReadyAsync(
                db, runtime, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Contains("Pending:", ex.Message, StringComparison.Ordinal);
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await McpDatabaseMigrationCoordinator.EnsureReadyAsync(
            db, runtime, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static DataAuditLogEntity NewAudit(string key, int? version, string? legacy) => new()
    {
        WorkspaceId = string.Empty,
        EntityKind = "test",
        EntityKey = key,
        Action = "create",
        Actor = "test",
        SourceType = "test",
        OccurredAtUtc = DateTimeOffset.UtcNow,
        PayloadEncodingVersion = version,
        CurrentSnapshotJsonLegacy = legacy,
    };
}
