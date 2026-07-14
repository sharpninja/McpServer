using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: resubmission revives a soft-deleted session.
/// Sessions are soft-delete only; the unique (WorkspaceId, SourceType, SessionId) index keeps the
/// tombstone holding the key, so SubmitAsync must recognize a tombstoned session with the same key,
/// restore its row graph, and apply the resubmitted turn data instead of failing on the unique
/// index. Uses an in-memory SQLite McpDbContext; tombstones are produced through
/// DeleteSessionAsync for canonical ids and through direct soft-delete shadow-property updates for
/// imported provider-native ids (which session delete rejects by policy).
/// </summary>
public sealed class SessionLogResubmissionReviveTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-resubmit-revive";
    private const string Agent = "Codex";
    private const string CanonicalSessionId = "Codex-20260714T120000Z-revive";
    private const string CanonicalRequestId = "req-20260714T120000Z-revive-turn";
    private const string ImportedSessionId = "019f2580-48c8-7912-b6a9-27f61b18d0d3";
    private const string ImportedRequestId = "fc_08eff9c03a00059d016a470a6942688197";

    /// <summary>Resubmitting a canonically named session after DeleteSessionAsync revives it with the corrected data.</summary>
    [Fact]
    public async Task SubmitAsync_ResubmitAfterSessionDelete_RevivesSessionWithCorrectedTurns()
    {
        using var connection = OpenConnection();
        Submit(connection, BuildCanonicalDto(response: "stale response"));

        var (deleter, deleterDb) = BuildSut(connection);
        using (deleterDb)
            Assert.True(await deleter.DeleteSessionAsync(Agent, CanonicalSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));

        Submit(connection, BuildCanonicalDto(response: "corrected response"));

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, CanonicalSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            var turn = Assert.Single(session!.Turns ?? []);
            Assert.Equal("corrected response", turn.Response);
        }
    }

    /// <summary>Resubmitting an imported session after its rows were tombstoned revives it through the import path.</summary>
    [Fact]
    public async Task SubmitAsync_ImportResubmitAfterTombstone_RevivesSession()
    {
        using var connection = OpenConnection();
        Submit(connection, BuildImportedDto(output: "stale output"), sourceFilePath: @"F:\imports\rollout.jsonl");
        TombstoneSessionGraph(connection, ImportedSessionId);

        var (probe, probeDb) = BuildSut(connection);
        using (probeDb)
            Assert.Null(await probe.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));

        Submit(connection, BuildImportedDto(output: "corrected output"), sourceFilePath: @"F:\imports\rollout.jsonl");

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            var turn = Assert.Single(session!.Turns ?? []);
            Assert.Equal(ImportedRequestId, turn.RequestId);
            Assert.Equal("corrected output", turn.Response);
        }
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static (SessionLogService Sut, McpDbContext Db) BuildSut(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection).Options;
        var workspaceContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(options, workspaceContext);
        db.Database.EnsureCreated();
        var sut = new SessionLogService(db, NullLogger<SessionLogService>.Instance, Substitute.For<IChangeEventBus>(), workspaceContext);
        return (sut, db);
    }

    private static void Submit(SqliteConnection connection, UnifiedSessionLogDto dto, string? sourceFilePath = null)
    {
        var (sut, db) = BuildSut(connection);
        using (db)
            sut.SubmitAsync(dto, sourceFilePath, contentHash: sourceFilePath is null ? null : "hash-1").GetAwaiter().GetResult();
    }

    private static UnifiedSessionLogDto BuildCanonicalDto(string response) => new()
    {
        SourceType = Agent,
        SessionId = CanonicalSessionId,
        Title = "Revive test",
        Started = "2026-07-14T12:00:00Z",
        LastUpdated = "2026-07-14T12:30:00Z",
        Status = "completed",
        Turns =
        [
            new UnifiedRequestEntryDto
            {
                RequestId = CanonicalRequestId,
                Timestamp = "2026-07-14T12:00:00Z",
                QueryText = "revive query",
                Response = response,
                Status = "completed",
            },
        ],
    };

    private static UnifiedSessionLogDto BuildImportedDto(string output) => new()
    {
        SourceType = Agent,
        SessionId = ImportedSessionId,
        Title = "Imported Codex transcript",
        Started = "2026-07-02T20:03:25Z",
        LastUpdated = "2026-07-13T17:21:42Z",
        Status = "completed",
        Turns =
        [
            new UnifiedRequestEntryDto
            {
                RequestId = ImportedRequestId,
                Timestamp = "2026-07-03T01:03:38Z",
                QueryTitle = "shell_command",
                Response = output,
                Status = "completed",
            },
        ],
    };

    /// <summary>Tombstones the session row and every turn row via the soft-delete shadow properties.</summary>
    private static void TombstoneSessionGraph(SqliteConnection connection, string sessionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE SessionLogTurns SET IsDeleted = 1, DeletedAtUtc = @now, DeletedBy = 'test', DeleteReason = 'test-tombstone' " +
            "WHERE SessionLogId IN (SELECT Id FROM SessionLogs WHERE SessionId = @sessionId); " +
            "UPDATE SessionLogs SET IsDeleted = 1, DeletedAtUtc = @now, DeletedBy = 'test', DeleteReason = 'test-tombstone' " +
            "WHERE SessionId = @sessionId;";
        command.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("@sessionId", sessionId);
        command.ExecuteNonQuery();
    }
}
