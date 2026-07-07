using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Tests SQLite FTS5 search behavior where raw SQL must mirror EF soft-delete filters.
/// </summary>
public sealed class Fts5SearchServiceTests
{
    private const string WorkspacePath = @"E:\tests\fts5-soft-delete";

    /// <summary>
    /// TR-MCP-DB-003: soft-deleted document content remains durable but is excluded from
    /// FTS search before and after an FTS rebuild.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ExcludesSoftDeletedDocumentsBeforeAndAfterRebuild()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using var db = CreateDb(connection);
        await CreateFtsTableAsync(connection).ConfigureAwait(true);
        await SeedDocumentsAsync(db).ConfigureAwait(true);

        var sut = new Fts5SearchService(db, NullLogger<Fts5SearchService>.Instance);
        await sut.RebuildAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Single((await sut.SearchAsync("needle", ct: TestContext.Current.CancellationToken).ConfigureAwait(true)).Chunks);

        var deletedAtUtc = DateTimeOffset.UtcNow;
        await SoftDeleteAsync(db.Chunks.Where(chunk => chunk.DocumentId == "doc-deleted"), deletedAtUtc).ConfigureAwait(true);
        await SoftDeleteAsync(db.Documents.Where(doc => doc.Id == "doc-deleted"), deletedAtUtc).ConfigureAwait(true);

        Assert.Equal(1, await CountFtsRowsAsync(connection, "chunk-deleted").ConfigureAwait(true));

        var beforeRebuild = await sut.SearchAsync("needle", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(beforeRebuild.Chunks);
        Assert.Empty(beforeRebuild.SourceKeys);

        await sut.RebuildAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(0, await CountFtsRowsAsync(connection, "chunk-deleted").ConfigureAwait(true));

        var afterRebuild = await sut.SearchAsync("needle", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(afterRebuild.Chunks);
        Assert.Empty(afterRebuild.SourceKeys);

        var retainedRows = await db.Documents
            .IgnoreQueryFilters()
            .CountAsync(doc => doc.Id == "doc-deleted" && EF.Property<bool>(doc, "IsDeleted"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(1, retainedRows);
    }

    private static McpDbContext CreateDb(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(WorkspacePath);
        return db;
    }

    private static async Task CreateFtsTableAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                ChunkId UNINDEXED,
                Content
            );
            """;
        await command.ExecuteNonQueryAsync().ConfigureAwait(true);
    }

    private static async Task<int> CountFtsRowsAsync(SqliteConnection connection, string chunkId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM chunks_fts WHERE ChunkId = $chunkId";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$chunkId";
        parameter.Value = chunkId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync().ConfigureAwait(true);
        return Convert.ToInt32(result);
    }

    private static async Task SeedDocumentsAsync(McpDbContext db)
    {
        db.Documents.AddRange(
            new ContextDocumentEntity
            {
                Id = "doc-visible",
                WorkspaceId = WorkspacePath,
                SourceType = "repo",
                SourceKey = "visible.md",
                IngestedAt = DateTime.UtcNow,
                ContentHash = "visible-hash",
            },
            new ContextDocumentEntity
            {
                Id = "doc-deleted",
                WorkspaceId = WorkspacePath,
                SourceType = "repo",
                SourceKey = "deleted.md",
                IngestedAt = DateTime.UtcNow,
                ContentHash = "deleted-hash",
            });
        db.Chunks.AddRange(
            new ContextChunkEntity
            {
                Id = "chunk-visible",
                WorkspaceId = WorkspacePath,
                DocumentId = "doc-visible",
                Content = "visible retained content",
                TokenCount = 3,
                ChunkIndex = 0,
            },
            new ContextChunkEntity
            {
                Id = "chunk-deleted",
                WorkspaceId = WorkspacePath,
                DocumentId = "doc-deleted",
                Content = "soft deleted needle content",
                TokenCount = 4,
                ChunkIndex = 0,
            });
        await db.SaveChangesAsync().ConfigureAwait(true);
    }

    private static Task SoftDeleteAsync<TEntity>(IQueryable<TEntity> query, DateTimeOffset deletedAtUtc)
        where TEntity : class
    {
        return query.ExecuteUpdateAsync(setters => setters
            .SetProperty(entity => EF.Property<bool>(entity, "IsDeleted"), true)
            .SetProperty(entity => EF.Property<DateTimeOffset?>(entity, "DeletedAtUtc"), deletedAtUtc)
            .SetProperty(entity => EF.Property<string?>(entity, "DeletedBy"), nameof(Fts5SearchServiceTests))
            .SetProperty(entity => EF.Property<string?>(entity, "DeleteReason"), "fts5_soft_delete_test"));
    }
}
