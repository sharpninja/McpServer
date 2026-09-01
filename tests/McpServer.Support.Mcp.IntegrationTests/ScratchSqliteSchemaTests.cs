using Microsoft.Data.Sqlite;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Reproduces the scratch-host SessionLogs.AgentExecutablePath miss and proves
/// the test-host bootstrap refuses that schema.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ScratchSqliteSchemaTests
{
    /// <summary>
    /// Prior failure: backfill Include(SessionLog) queries AgentExecutablePath and throws
    /// when the scratch database was created without that column.
    /// </summary>
    [Fact]
    public async Task Backfill_LegacySessionLogsMissingAgentExecutablePath_Throws()
    {
        var path = CreateDbPath();
        try
        {
            await CreateLegacySessionLogsWithoutAgentExecutablePathAsync(path).ConfigureAwait(true);
            Assert.False(await ScratchSqliteSchema.HasAgentExecutablePathAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true));

            var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new McpDbContext(options);
            db.OverrideWorkspaceId("F:/scratch-schema");

            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => db.SessionLogs.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);
            Assert.Contains("no such column", ex.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDelete(path);
        }
    }

    /// <summary>Current contract: coordinator migrate plus verify creates the required column.</summary>
    [Fact]
    public async Task ApplyAndVerify_EmptyDatabase_CreatesAgentExecutablePath()
    {
        var path = CreateDbPath();
        try
        {
            await ScratchSqliteSchema.ApplyAndVerifyAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(await ScratchSqliteSchema.HasAgentExecutablePathAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true));

            var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite($"Data Source={path}").Options;
            await using var db = new McpDbContext(options);
            db.OverrideWorkspaceId("F:/scratch-schema");
            db.SessionLogs.Add(new SessionLogEntity
            {
                SourceType = "GrokCode",
                SessionId = "GrokCode-20260817T082548Z-schema",
                WorkspaceId = "F:/scratch-schema",
                AgentExecutablePath = "C:/scratch/grok.exe",
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var stored = await db.SessionLogs.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("C:/scratch/grok.exe", stored.AgentExecutablePath);
        }
        finally
        {
            TryDelete(path);
        }
    }

    /// <summary>Legacy leftover tables without the column fail closed instead of starting the host.</summary>
    [Fact]
    public async Task EnsureAgentExecutablePath_LegacySchema_ThrowsBeforeHostStart()
    {
        var path = CreateDbPath();
        try
        {
            await CreateLegacySessionLogsWithoutAgentExecutablePathAsync(path).ConfigureAwait(true);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ScratchSqliteSchema.EnsureAgentExecutablePathAsync(path, TestContext.Current.CancellationToken)).ConfigureAwait(true);
            Assert.Contains("AgentExecutablePath", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string CreateDbPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "scratch-schema-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "mcp.db");
    }

    private static async Task CreateLegacySessionLogsWithoutAgentExecutablePathAsync(string path)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE SessionLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WorkspaceId TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                SessionId TEXT NOT NULL
            );
            CREATE TABLE SessionLogTurns (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionLogId INTEGER NOT NULL,
                WorkspaceId TEXT,
                RequestId TEXT NOT NULL,
                PlanFile TEXT,
                TodoId TEXT
            );
            """;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private static void TryDelete(string path)
    {
        SqliteConnection.ClearAllPools();
        var dir = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return;
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
