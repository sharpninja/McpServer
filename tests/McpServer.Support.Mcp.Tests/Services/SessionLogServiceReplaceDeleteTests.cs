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
/// FR-SUPPORT-010G: Replace (PUT) and remove (DELETE) semantics for the session
/// log. PATCH/POST stay additive (omitted fields preserved); PUT replaces the
/// named scope (omitted/empty cleared); DELETE removes a session, turn, section,
/// or single item. Backs the PATCH-vs-PUT verb split agreed 2026-06-13.
/// </summary>
public sealed class SessionLogServiceReplaceDeleteTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-replace-delete";
    private const string Agent = "ClaudeCode";
    private const string RequestId = "req-20260613T120000Z-001-seed";

    [Fact]
    public async Task ReplaceTurnAsync_ClearsOmittedScalarsAndCollections()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        // PUT a turn that re-states only RequestId + status + ONE action: every
        // omitted scalar and every omitted collection must be cleared.
        var replacement = new UnifiedRequestEntryDto
        {
            RequestId = RequestId,
            Status = "completed",
            Actions = [new UnifiedActionDto { Order = 0, Description = "only remaining action", Type = "edit", Status = "completed" }],
        };

        var (sut, db) = BuildSut(connection);
        using (db)
            await sut.ReplaceTurnAsync(Agent, sessionId, replacement, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Equal("completed", turn.Status);
        Assert.Null(turn.QueryText);            // was "seed query" -> cleared by PUT
        Assert.Null(turn.Tags);                 // cleared
        Assert.Null(turn.ContextList);          // cleared
        Assert.Null(turn.Commits);              // cleared
        Assert.Null(turn.ProcessingDialog);     // cleared
        Assert.Null(turn.DesignDecisions);      // cleared
        Assert.Null(turn.Blockers);             // cleared
        Assert.NotNull(turn.Actions);
        Assert.Single(turn.Actions!);
        Assert.Equal("only remaining action", turn.Actions!.First().Description);
    }

    [Fact]
    public async Task ReplaceTurnAsync_PreservesWorkspaceStampOnChildren()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await sut.ReplaceTurnAsync(Agent, sessionId, new UnifiedRequestEntryDto
            {
                RequestId = RequestId,
                Status = "completed",
                Tags = ["kept-tag"],
                Actions = [new UnifiedActionDto { Order = 0, Description = "a", Status = "completed" }],
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Every surviving child row carries the parent session's WorkspaceId.
        Assert.Equal(0, CountMismatchedChildStamps(connection, sessionId));
    }

    [Fact]
    public async Task ReplaceTurnSectionAsync_ReplacesOnlyNamedSection()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await sut.ReplaceTurnSectionAsync(Agent, sessionId, RequestId, "tags", new UnifiedRequestEntryDto
            {
                RequestId = RequestId,
                Tags = ["alpha", "beta"],
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        // tags replaced...
        Assert.Equal(new[] { "alpha", "beta" }, turn.Tags!.OrderBy(t => t).ToArray());
        // ...but other sections untouched.
        Assert.NotNull(turn.Commits);
        Assert.NotNull(turn.DesignDecisions);
        Assert.Equal("seed query", turn.QueryText);
    }

    [Fact]
    public async Task ReplaceTurnSectionAsync_EmptyPayload_ClearsSection()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await sut.ReplaceTurnSectionAsync(Agent, sessionId, RequestId, "designDecisions", new UnifiedRequestEntryDto
            {
                RequestId = RequestId,
                DesignDecisions = [],
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Null(turn.DesignDecisions);
        Assert.NotNull(turn.Tags); // unrelated section preserved
    }

    [Fact]
    public async Task ClearTurnSectionAsync_RemovesAllItemsInSection()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.ClearTurnSectionAsync(Agent, sessionId, RequestId, "commits", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Null(turn.Commits);
        Assert.NotNull(turn.Actions);
    }

    [Fact]
    public async Task DeleteTurnItemAsync_StringSection_RemovesByValue()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "tags", "seed-tag-b", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Equal(new[] { "seed-tag-a" }, turn.Tags!.ToArray());
    }

    [Fact]
    public async Task DeleteTurnItemAsync_Commit_RemovesBySha()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "commits", "sha-2", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Single(turn.Commits!);
        Assert.Equal("sha-1", turn.Commits!.First().Sha);
    }

    [Fact]
    public async Task DeleteTurnItemAsync_Action_RemovesByOrder()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "actions", "1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Single(turn.Actions!);
        Assert.Equal(0, turn.Actions!.First().Order);
    }

    [Fact]
    public async Task DeleteTurnItemAsync_UnknownKey_ReturnsFalse()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.False(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "tags", "no-such-tag", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task DeleteTurnAsync_SoftDeletesTurnAndChildren()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.DeleteTurnAsync(Agent, sessionId, RequestId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var (sut2, db2) = BuildSut(connection);
        using (db2)
        {
            var session = await sut2.GetAsync(Agent, sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(session);                 // session survives
            Assert.Equal(0, session!.TurnCount);     // turn gone
            Assert.True(session.Turns is null or { Count: 0 });
        }
        Assert.Equal(1, CountTurnRows(connection, sessionId));     // durable row retained
        Assert.True(CountAllChildRows(connection, sessionId) > 0); // child rows retained
        Assert.Equal(1, CountSoftDeletedTurnRows(connection, sessionId));
        Assert.Equal(CountAllChildRows(connection, sessionId), CountSoftDeletedChildRows(connection, sessionId));
    }

    [Fact]
    public async Task DeleteSessionAsync_SoftDeletesSessionAndEverything()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.True(await sut.DeleteSessionAsync(Agent, sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        var (sut2, db2) = BuildSut(connection);
        using (db2)
            Assert.Null(await sut2.GetAsync(Agent, sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        Assert.Equal(1, CountSessionRows(connection, sessionId));
        Assert.Equal(1, CountTurnRows(connection, sessionId));
        Assert.True(CountAllChildRows(connection, sessionId) > 0);
        Assert.Equal(1, CountSoftDeletedSessionRows(connection, sessionId));
        Assert.Equal(1, CountSoftDeletedTurnRows(connection, sessionId));
        Assert.Equal(CountAllChildRows(connection, sessionId), CountSoftDeletedChildRows(connection, sessionId));
    }

    [Fact]
    public async Task DeleteSessionAsync_Missing_ReturnsFalse()
    {
        using var connection = OpenConnection();
        SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.False(await sut.DeleteSessionAsync(Agent, BuildSessionId("absent"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    [Fact]
    public async Task UpsertTurnAsync_StaysAdditive_DoesNotRemoveExistingItems()
    {
        // Regression: PATCH/merge path must keep appending, never clobber.
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await sut.UpsertTurnAsync(Agent, sessionId, new UnifiedRequestEntryDto
            {
                RequestId = RequestId,
                Tags = ["seed-tag-c"], // additive: appended, existing kept
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var turn = await GetTurnAsync(connection, sessionId).ConfigureAwait(true);
        Assert.Equal(new[] { "seed-tag-a", "seed-tag-b", "seed-tag-c" }, turn.Tags!.OrderBy(t => t).ToArray());
        Assert.Equal("seed query", turn.QueryText); // omitted scalar preserved
    }

    [Fact]
    public async Task ReplaceTurnSectionAsync_UnknownSection_Throws()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            await Assert.ThrowsAsync<ArgumentException>(() =>
                sut.ReplaceTurnSectionAsync(Agent, sessionId, RequestId, "nonsense", new UnifiedRequestEntryDto { RequestId = RequestId }, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    // ---- harness ------------------------------------------------------------

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

    /// <summary>Seeds one session with one turn populated across all 9 sections.</summary>
    private static string SeedFullSession(SqliteConnection connection)
    {
        var sessionId = BuildSessionId("seed");
        var (sut, db) = BuildSut(connection);
        using (db)
        {
            sut.SubmitAsync(new UnifiedSessionLogDto
            {
                SourceType = Agent,
                SessionId = sessionId,
                Title = "Seed",
                Model = "claude-opus-4-8",
                Started = "2026-06-13T12:00:00Z",
                LastUpdated = "2026-06-13T12:30:00Z",
                Status = "in_progress",
                Turns =
                [
                    new UnifiedRequestEntryDto
                    {
                        RequestId = RequestId,
                        Timestamp = "2026-06-13T12:00:00Z",
                        QueryText = "seed query",
                        QueryTitle = "seed",
                        Response = "seed response",
                        Status = "in_progress",
                        Tags = ["seed-tag-a", "seed-tag-b"],
                        ContextList = ["docs/a.md", "docs/b.md"],
                        Actions =
                        [
                            new UnifiedActionDto { Order = 0, Description = "action zero", Type = "edit", Status = "completed", FilePath = "src/a.cs" },
                            new UnifiedActionDto { Order = 1, Description = "action one", Type = "create", Status = "completed", FilePath = "src/b.cs" },
                        ],
                        ProcessingDialog =
                        [
                            new ProcessingDialogItemDto { Timestamp = "2026-06-13T12:01:00Z", Role = "model", Content = "thinking", Category = "reasoning" },
                            new ProcessingDialogItemDto { Timestamp = "2026-06-13T12:02:00Z", Role = "tool", Content = "ran", Category = "tool_call" },
                        ],
                        Commits =
                        [
                            new SessionLogCommitDto { Sha = "sha-1", Branch = "main", Message = "first", Author = "p" },
                            new SessionLogCommitDto { Sha = "sha-2", Branch = "main", Message = "second", Author = "p" },
                        ],
                        DesignDecisions = ["chose A over B", "deferred C"],
                        RequirementsDiscovered = ["FR-SUPPORT-010G"],
                        FilesModified = ["src/a.cs", "src/b.cs"],
                        Blockers = ["needs review"],
                    }
                ]
            }).GetAwaiter().GetResult();
        }
        return sessionId;
    }

    private static async Task<UnifiedRequestEntryDto> GetTurnAsync(SqliteConnection connection, string sessionId)
    {
        var (sut, db) = BuildSut(connection);
        using (db)
        {
            var session = await sut.GetAsync(Agent, sessionId).ConfigureAwait(true);
            Assert.NotNull(session);
            Assert.NotNull(session!.Turns);
            return session.Turns!.Single(t => t.RequestId == RequestId);
        }
    }

    private static int CountSessionRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = $sid", ("$sid", sessionId));

    private static int CountTurnRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection,
            "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid",
            ("$sid", sessionId));

    private static int CountSoftDeletedSessionRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection, "SELECT COUNT(*) FROM SessionLogs WHERE SessionId = $sid AND IsDeleted = 1", ("$sid", sessionId));

    private static int CountSoftDeletedTurnRows(SqliteConnection connection, string sessionId)
        => ScalarCount(connection,
            "SELECT COUNT(*) FROM SessionLogTurns t JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid AND t.IsDeleted = 1",
            ("$sid", sessionId));

    private static int CountAllChildRows(SqliteConnection connection, string sessionId)
    {
        var tables = new[]
        {
            "SessionLogActions", "SessionLogTurnTags", "SessionLogTurnContexts",
            "SessionLogProcessingDialogs", "SessionLogCommits", "SessionLogTurnStringLists",
        };
        var total = 0;
        foreach (var table in tables)
        {
            total += ScalarCount(connection,
                $"SELECT COUNT(*) FROM {table} c JOIN SessionLogTurns t ON t.Id = c.SessionLogTurnId " +
                "JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid",
                ("$sid", sessionId));
        }
        return total;
    }

    private static int CountSoftDeletedChildRows(SqliteConnection connection, string sessionId)
    {
        var tables = new[]
        {
            "SessionLogActions", "SessionLogTurnTags", "SessionLogTurnContexts",
            "SessionLogProcessingDialogs", "SessionLogCommits", "SessionLogTurnStringLists",
        };
        var total = 0;
        foreach (var table in tables)
        {
            total += ScalarCount(connection,
                $"SELECT COUNT(*) FROM {table} c JOIN SessionLogTurns t ON t.Id = c.SessionLogTurnId " +
                "JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid AND c.IsDeleted = 1",
                ("$sid", sessionId));
        }
        return total;
    }

    private static int CountMismatchedChildStamps(SqliteConnection connection, string sessionId)
    {
        var tables = new[]
        {
            "SessionLogActions", "SessionLogTurnTags", "SessionLogTurnContexts",
            "SessionLogProcessingDialogs", "SessionLogCommits", "SessionLogTurnStringLists",
        };
        var total = 0;
        foreach (var table in tables)
        {
            total += ScalarCount(connection,
                $"SELECT COUNT(*) FROM {table} c JOIN SessionLogTurns t ON t.Id = c.SessionLogTurnId " +
                "JOIN SessionLogs s ON s.Id = t.SessionLogId WHERE s.SessionId = $sid AND c.WorkspaceId <> s.WorkspaceId",
                ("$sid", sessionId));
        }
        return total;
    }

    private static int ScalarCount(SqliteConnection connection, string sql, params (string Name, object Value)[] args)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args)
            cmd.Parameters.AddWithValue(name, value);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static string BuildSessionId(string suffix) => $"{Agent}-20260613T120000Z-{suffix}";
}
