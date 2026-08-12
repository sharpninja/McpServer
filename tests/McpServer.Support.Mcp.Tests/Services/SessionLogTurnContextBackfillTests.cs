using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-005 / AC-FR-MCP-SESSIONLOGCTX-001-006:
/// backfill upgrades only None columns.
/// </summary>
public sealed class SessionLogTurnContextBackfillTests
{
    /// <summary>AC-TR-MCP-SESSIONLOG-006-005: extractable TODO upgrades None.</summary>
    [Fact]
    public async Task RunAsync_NoneRowWithExtractableTodo_UpdatesTodoId()
    {
        await using var db = CreateDb();
        var session = AddSession(db);
        session.Turns.Add(new SessionLogTurnEntity
        {
            RequestId = "req-20260304T113901Z-bf1",
            QueryText = "working MCP-BACKFILL-001",
            PlanFile = "None",
            TodoId = "None",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
        var changed = await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true);
        Assert.Equal(1, changed);
        Assert.Equal("MCP-BACKFILL-001", (await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-005: no signals stay None.</summary>
    [Fact]
    public async Task RunAsync_NoneRowWithNoSignals_LeavesNone()
    {
        await using var db = CreateDb();
        var session = AddSession(db);
        session.Turns.Add(new SessionLogTurnEntity { RequestId = "req-20260304T113901Z-bf2", QueryText = "nothing", PlanFile = "None", TodoId = "None" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
        await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true);
        var turn = await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("None", turn.PlanFile);
        Assert.Equal("None", turn.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-005: non-None is not overwritten.</summary>
    [Fact]
    public async Task RunAsync_NonNoneRow_NotOverwritten()
    {
        await using var db = CreateDb();
        var session = AddSession(db);
        session.Turns.Add(new SessionLogTurnEntity
        {
            RequestId = "req-20260304T113901Z-bf3",
            QueryText = "MCP-OTHER-001",
            PlanFile = "docs/plans/kept.md",
            TodoId = "MCP-KEEP-001",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
        await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true);
        var turn = await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/kept.md", turn.PlanFile);
        Assert.Equal("MCP-KEEP-001", turn.TodoId);
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-005: rerun is a no-op after upgrade.</summary>
    [Fact]
    public async Task RunAsync_IsIdempotent()
    {
        await using var db = CreateDb();
        var session = AddSession(db);
        session.Turns.Add(new SessionLogTurnEntity { RequestId = "req-20260304T113901Z-bf4", QueryText = "MCP-IDEM-001", PlanFile = "None", TodoId = "None" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
        Assert.Equal(1, await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true));
        Assert.Equal(0, await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true));
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-006: fake ~ history upgrades an empty turn.</summary>
    [Fact]
    public async Task RunAsync_UsesAgentHistoryUnderFakeHome_WhenTurnTextHasNoTodo()
    {
        var home = Path.Combine(Path.GetTempPath(), "sesslog-bf-" + Guid.NewGuid().ToString("N"));
        var hist = Path.Combine(home, ".grok", "sessions", "sid-xyz");
        Directory.CreateDirectory(hist);
        File.WriteAllText(Path.Combine(hist, "t.jsonl"), "TODO MCP-HOMEBF-001");
        try
        {
            await using var db = CreateDb();
            var session = AddSession(db);
            session.AgentSessionId = "sid-xyz";
            session.Turns.Add(new SessionLogTurnEntity { RequestId = "req-20260304T113901Z-bf5", QueryText = "empty", PlanFile = "None", TodoId = "None" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var sut = new SessionLogTurnContextBackfill(db, new SessionLogTurnContextExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
            await sut.RunAsync(TestContext.Current.CancellationToken, home).ConfigureAwait(true);
            Assert.Equal("MCP-HOMEBF-001", (await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).TodoId);
        }
        finally
        {
            Directory.Delete(home, recursive: true);
        }
    }

    /// <summary>
    /// Startup helper uses the migrated <see cref="McpDbContext"/> instance and upgrades None rows.
    /// </summary>
    [Fact]
    public async Task TryRunAsync_UsesProvidedDb_AndUpgradesNone()
    {
        await using var db = CreateDb();
        var session = AddSession(db);
        session.Turns.Add(new SessionLogTurnEntity
        {
            RequestId = "req-20260304T113901Z-bf-start",
            QueryText = "working MCP-STARTBF-001",
            PlanFile = "None",
            TodoId = "None",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var changed = await SessionLogTurnContextBackfillStartup.TryRunAsync(
            db,
            new SessionLogTurnContextExtractor(),
            NullLogger<SessionLogTurnContextBackfill>.Instance,
            TestContext.Current.CancellationToken,
            IsolatedHome()).ConfigureAwait(true);

        Assert.Equal(1, changed);
        Assert.Equal("MCP-STARTBF-001", (await db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).TodoId);
    }

    /// <summary>
    /// Startup helper swallows backfill failures so host startup can continue.
    /// </summary>
    [Fact]
    public async Task TryRunAsync_DisposedDb_ReturnsZeroAndDoesNotThrow()
    {
        var db = CreateDb();
        await db.DisposeAsync().ConfigureAwait(true);

        var changed = await SessionLogTurnContextBackfillStartup.TryRunAsync(
            db,
            new SessionLogTurnContextExtractor(),
            NullLogger<SessionLogTurnContextBackfill>.Instance,
            TestContext.Current.CancellationToken,
            IsolatedHome()).ConfigureAwait(true);

        Assert.Equal(0, changed);
    }

    private static string IsolatedHome() =>
        Path.Combine(Path.GetTempPath(), "sesslog-isolated-home-missing");

    private static McpDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase("bf-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new McpDbContext(options);
        db.Database.EnsureCreated();
        db.OverrideWorkspaceId(@"F:\ws");
        return db;
    }

    private static SessionLogEntity AddSession(McpDbContext db)
    {
        var session = new SessionLogEntity
        {
            SourceType = "Cursor",
            SessionId = "Cursor-20260304T113901Z-bf",
            WorkspaceId = @"F:\ws",
        };
        db.SessionLogs.Add(session);
        return session;
    }
}
