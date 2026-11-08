using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-SUPPORT-010G: Replace (PUT) and remove (DELETE) semantics for the session
/// log. PATCH/POST stay additive (omitted fields preserved); PUT replaces the
/// named scope (omitted/empty cleared); DELETE removes a session, turn, section,
/// or single item. Backs the PATCH-vs-PUT verb split agreed 2026-06-13.
/// </summary>
/// <remarks>
/// Requirement coverage named by a TEST record: TEST-MCP-DB-006 AC2 (DeleteTurnAsync stays correct under the keyed lock).
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TEST-MCP-DB-006 (AC2 of its description): cites the record whose description names this class, so the description no longer names the test (operator: "Must be fixed.").</description></item>
/// </list>
/// </remarks>
public sealed class SessionLogServiceReplaceDeleteTests
{
    private const string WorkspacePath = @"E:\tests\sessionlog-replace-delete";
    private const string Agent = "ClaudeCode";
    private const string RequestId = "req-20260613T120000Z-001-seed";

    /// <summary>
    /// FR-SUPPORT-016 ac-1; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: replacing a turn with a payload that
    /// restates only the status and one action clears every omitted scalar and collection (query text, tags, context,
    /// commits, dialog, design decisions, blockers) and leaves exactly that one action. Data: an in-memory SQLite
    /// database seeded by SeedFullSession with one session whose one turn fills every section.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-1; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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
            PlanFile = "None",
            TodoId = "None",
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

    /// <summary>
    /// FR-SUPPORT-016 ac-1; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: after a turn replace, every surviving
    /// child row of the turn carries the WorkspaceId of its parent session (no mismatched stamp in any child table).
    /// Data: an in-memory SQLite database seeded by SeedFullSession; a replacement turn with one tag and one action.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-1; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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
                PlanFile = "None",
                TodoId = "None",
                Tags = ["kept-tag"],
                Actions = [new UnifiedActionDto { Order = 0, Description = "a", Status = "completed" }],
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Every surviving child row carries the parent session's WorkspaceId.
        Assert.Equal(0, CountMismatchedChildStamps(connection, sessionId));
    }

    /// <summary>
    /// FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: replacing the tags section sets the tags
    /// to exactly alpha and beta and leaves the commits, the design decisions and the query text untouched. Data: an
    /// in-memory SQLite database seeded by SeedFullSession; a tags-only payload.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: replacing the designDecisions section
    /// with an empty list clears it and leaves the unrelated tags section in place. Data: an in-memory SQLite database
    /// seeded by SeedFullSession; an empty designDecisions payload.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: clearing the commits section returns
    /// true, removes every commit and leaves the actions in place. Data: an in-memory SQLite database seeded by
    /// SeedFullSession (two commits, two actions).
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-2; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: deleting the tag seed-tag-b by value
    /// returns true and leaves only seed-tag-a. Data: an in-memory SQLite database seeded by SeedFullSession (tags
    /// seed-tag-a and seed-tag-b).
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: deleting the commit sha-2 by its SHA
    /// returns true and leaves only the commit sha-1. Data: an in-memory SQLite database seeded by SeedFullSession
    /// (commits sha-1 and sha-2).
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: deleting the action with order 1 returns
    /// true and leaves only the action with order 0. Data: an in-memory SQLite database seeded by SeedFullSession
    /// (actions with orders 0 and 1).
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: deleting a tag key the turn does not
    /// hold (no-such-tag) returns false. Data: an in-memory SQLite database seeded by SeedFullSession.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-3; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task DeleteTurnItemAsync_UnknownKey_ReturnsFalse()
    {
        using var connection = OpenConnection();
        var sessionId = SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.False(await sut.DeleteTurnItemAsync(Agent, sessionId, RequestId, "tags", "no-such-tag", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// FR-MCP-105; TR-MCP-DB-003; TR-MCP-DB-006 (AC2 of its description); TEST-MCP-DB-006 (AC2 of its description):
    /// deleting the turn returns true; the session survives with a turn count of 0 and no turns, while the turn row and
    /// its child rows stay stored, each marked deleted. Data: an in-memory SQLite database seeded by SeedFullSession;
    /// row counts read with direct SQL over the session-log tables.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-105; TR-MCP-DB-003; TR-MCP-DB-006 (AC2 of its description); TEST-MCP-DB-006 (AC2 of its description): documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-MCP-105; TR-MCP-DB-003: deleting the session returns true and the session no longer reads back, while the
    /// session row, its turn row and its child rows stay stored, each marked deleted. Data: an in-memory SQLite
    /// database seeded by SeedFullSession; row counts read with direct SQL over the session-log tables.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-105; TR-MCP-DB-003: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record in the store states the result of deleting an absent
    /// session; the case proves that DeleteSessionAsync returns false for a session id that was never stored. Data: an
    /// in-memory SQLite database seeded by SeedFullSession with a different session.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for delete near session, delete a or the session, session delete, absent session, DeleteSessionAsync, returns false, session near not found, and unknown or missing (uncited-matches.txt, test 23 of 23); the nearest, FR-SUPPORT-016 ac-3 (an item delete returns false for an unknown key) and TR-MCP-DB-003 AC-001 (what resubmitting a deleted session revives), state neither the result of deleting an absent session, and the class-cited TEST-MCP-DB-006 has no criteria. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public async Task DeleteSessionAsync_Missing_ReturnsFalse()
    {
        using var connection = OpenConnection();
        SeedFullSession(connection);

        var (sut, db) = BuildSut(connection);
        using (db)
            Assert.False(await sut.DeleteSessionAsync(Agent, BuildSessionId("absent"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>
    /// FR-SUPPORT-016 ac-2; TEST-SUPPORT-024 AC-001: an additive turn upsert with one new tag appends it to the stored
    /// tags (seed-tag-a, seed-tag-b and seed-tag-c remain) and keeps the omitted query text. Data: an in-memory SQLite
    /// database seeded by SeedFullSession; an upsert payload holding only the tag seed-tag-c.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-2; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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

    /// <summary>
    /// FR-SUPPORT-016 ac-4; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: replacing a section named nonsense
    /// throws ArgumentException. Data: an in-memory SQLite database seeded by SeedFullSession.
    /// </summary>
    /// <remarks>2026-11-07, FR-SUPPORT-016 ac-4; TR-SUPPORT-CORE-016 ac-1; TEST-SUPPORT-024 AC-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
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
                        PlanFile = "None",
                        TodoId = "None",
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
