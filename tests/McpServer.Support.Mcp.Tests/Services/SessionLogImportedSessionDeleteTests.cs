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
/// TR-MCP-TRANSCRIPT-004 / TEST-MCP-TRANSCRIPT-012: management operations on imported transcript
/// sessions. Transcript imports persist provider-native identifiers (UUID session ids, tool-call
/// request ids) through the <c>sourceFilePath</c> import path of SubmitAsync. Turn-level keyed
/// operations (delete turn, replace/clear section, delete item) must accept those identifiers so
/// turns can be repaired by resubmission; session delete stays canonical-only by policy, so
/// imported sessions are never deletable. Uses an in-memory SQLite McpDbContext seeded through
/// SubmitAsync with a source file path.
/// </summary>
public sealed class SessionLogImportedSessionDeleteTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-imported-delete";
    private const string Agent = "Codex";
    private const string ImportedSessionId = "019f2580-48c8-7912-b6a9-27f61b18d0d3";
    private const string ImportedRequestId = "fc_08eff9c03a00059d016a470a6942688197";

    /// <summary>Session delete stays canonical-only: imported UUID session ids are rejected.</summary>
    [Fact]
    public async Task DeleteSessionAsync_ImportedProviderNativeIds_IsRejected()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken)).ConfigureAwait(true);

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
            Assert.NotNull(await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>Imported turns with provider-native request ids must be deletable.</summary>
    [Fact]
    public async Task DeleteTurnAsync_ImportedProviderNativeIds_DeletesTurn()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
        {
            var deleted = await sut.DeleteTurnAsync(Agent, ImportedSessionId, ImportedRequestId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(deleted);
        }

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);
            Assert.DoesNotContain(session!.Turns ?? [], turn => turn.RequestId == ImportedRequestId);
        }
    }

    /// <summary>Imported turns must accept section-level replace operations.</summary>
    [Fact]
    public async Task ReplaceTurnSectionAsync_ImportedProviderNativeIds_ReplacesSection()
    {
        using var connection = OpenConnection();
        SeedImportedSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
        {
            var replaced = await sut.ReplaceTurnSectionAsync(Agent, ImportedSessionId, ImportedRequestId, "tags", new UnifiedRequestEntryDto
            {
                RequestId = ImportedRequestId,
                Tags = ["retagged"],
            }, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(replaced);
        }

        var (reader, readerDb) = BuildSut(connection);
        using (readerDb)
        {
            var session = await reader.GetAsync(Agent, ImportedSessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            var turn = session!.Turns!.Single(item => item.RequestId == ImportedRequestId);
            Assert.Equal(["retagged"], turn.Tags);
        }
    }

    /// <summary>Whitespace identifiers stay rejected on the delete path.</summary>
    [Fact]
    public async Task DeleteSessionAsync_WhitespaceSessionId_Throws()
    {
        using var connection = OpenConnection();
        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, "   ", TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>Non-canonical ids are rejected by session delete before any lookup.</summary>
    [Fact]
    public async Task DeleteSessionAsync_NonCanonicalId_IsRejected()
    {
        using var connection = OpenConnection();
        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteSessionAsync(Agent, "no-such-imported-session", TestContext.Current.CancellationToken)).ConfigureAwait(true);
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

    /// <summary>Seeds one imported session (SubmitAsync with sourceFilePath) carrying provider-native ids.</summary>
    private static void SeedImportedSession(SqliteConnection connection)
    {
        var (sut, db) = BuildSut(connection);
        using (db)
        {
            sut.SubmitAsync(new UnifiedSessionLogDto
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
                        Status = "completed",
                        Tags = ["transcript-import"],
                    },
                    new UnifiedRequestEntryDto
                    {
                        RequestId = "codex-event-2",
                        Timestamp = "2026-07-03T01:03:45Z",
                        QueryTitle = "output",
                        Status = "completed",
                    },
                ]
            }, sourceFilePath: @"F:\imports\rollout.jsonl", contentHash: "hash-1").GetAwaiter().GetResult();
        }
    }
}
