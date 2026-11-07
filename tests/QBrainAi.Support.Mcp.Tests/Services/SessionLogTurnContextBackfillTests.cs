using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.SessionLogTurnContextBackfillTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-005 / AC-FR-MCP-SESSIONLOGCTX-001-006:
/// backfill upgrades only None columns.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-005; FR-MCP-SESSIONLOGCTX-001-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (RunAsync_UsesAgentHistoryUnderFakeHome_WhenTurnTextHasNoTodo) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (CreateDb and AddSession) moved once into Services/SessionLogTurnContextBackfillTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
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

        var sut = new SessionLogTurnContextBackfill(db, PureExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
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
        var sut = new SessionLogTurnContextBackfill(db, PureExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
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
        var sut = new SessionLogTurnContextBackfill(db, PureExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
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
        var sut = new SessionLogTurnContextBackfill(db, PureExtractor(), NullLogger<SessionLogTurnContextBackfill>.Instance);
        Assert.Equal(1, await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true));
        Assert.Equal(0, await sut.RunAsync(TestContext.Current.CancellationToken, IsolatedHome()).ConfigureAwait(true));
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
            PureExtractor(),
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
            PureExtractor(),
            NullLogger<SessionLogTurnContextBackfill>.Instance,
            TestContext.Current.CancellationToken,
            IsolatedHome()).ConfigureAwait(true);

        Assert.Equal(0, changed);
    }

    private static string IsolatedHome() => "/fixture/no-history";

    private static SessionLogTurnContextExtractor PureExtractor() => new(new DirectoryTreeWalker(_ => []));
}
