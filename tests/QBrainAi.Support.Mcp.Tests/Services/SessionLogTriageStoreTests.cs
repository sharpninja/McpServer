using System.Diagnostics;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGESTORE-001: session-log persist is idempotent on identical actions,
/// session tags persist, replace missing turn is not-found, canceled statuses round-trip.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESTORE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (SubmitAsync_HungSaveChanges_FailsFastWithStorageUnavailable) and the helpers only it uses (HungSaveChangesInterceptor) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (WorkspacePath, _db, _sut, .ctor, Dispose and CreateSession) moved once into the abstract base class SessionLogTriageStoreTestsBase in Services/SessionLogTriageStoreTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogTriageStoreTests : SessionLogTriageStoreTestsBase, IDisposable
{
    /// <summary>TEST-MCP-TRIAGESTORE-001: identical actions[] resubmit does not duplicate rows.</summary>
    [Fact]
    public async Task SubmitAsync_IdenticalActions_DoesNotDuplicate()
    {
        var sessionId = "Cursor-20260818T200000Z-identical-actions";
        var dto = CreateSession(sessionId);
        dto.Turns!.First().Actions =
        [
            new UnifiedActionDto
            {
                Order = 1,
                Type = "edit",
                Description = "Edit Program.cs",
                Status = "completed",
                FilePath = "Program.cs",
            },
        ];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        dto.Turns!.First().Actions!.First().Status = "failed";
        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogActions
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(1, stored);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: session-level tags persist and return on query.</summary>
    [Fact]
    public async Task SubmitAsync_SessionTags_RoundTrip()
    {
        var sessionId = "Cursor-20260818T200000Z-session-tags";
        var dto = CreateSession(sessionId);
        dto.Tags = ["triage", "cluster"];

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var queried = await _sut.QueryAsync(
            new SessionLogQueryRequest { Agent = "Cursor" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var session = Assert.Single(queried.Items, item => item.SessionId == sessionId);
        Assert.NotNull(session.Tags);
        Assert.Contains("triage", session.Tags!);
        Assert.Contains("cluster", session.Tags!);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: replace of a missing requestId is not-found, not upsert.</summary>
    [Fact]
    public async Task ReplaceTurnAsync_MissingRequestId_ThrowsNotFound()
    {
        var sessionId = "Cursor-20260818T200000Z-replace-missing";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var missing = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260818T200000Z-missing-turn",
            Status = "completed",
            PlanFile = SessionLogTurnContextValidator.NoneSentinel,
            TodoId = SessionLogTurnContextValidator.NoneSentinel,
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ReplaceTurnAsync("Cursor", sessionId, missing, TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>TEST-MCP-TRIAGESTORE-001: canceled and cancelled persist and re-query.</summary>
    [Theory]
    [InlineData("canceled")]
    [InlineData("cancelled")]
    public async Task SubmitAsync_CanceledStatus_RoundTrips(string status)
    {
        var sessionId = $"Cursor-20260818T200000Z-{status}-status";
        var dto = CreateSession(sessionId);
        dto.Turns!.First().Status = status;

        await _sut.SubmitAsync(dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync("Cursor", sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(fetched);
        Assert.Equal(status, fetched!.Turns!.First().Status);
    }

    /// <summary>
    /// TEST-MCP-TRIAGEPLUGIN-001 / BUG-TRIAGE-121: query filter
    /// <c>turnStatus=in_progress</c> plus <c>staleOlderThanHours</c> lists only
    /// sessions with stale in-progress turns and does not mass-close them. The service reads a settable test clock and
    /// the seeds are placed from the same instant (TR-MCP-USECASE-018 AC-006), so no row depends on the system clock.
    /// </summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): the reference time and the seeds come from a <see cref="SettableTimeProvider"/> handed to the service instead of DateTimeOffset.UtcNow; the assertions are unchanged (hostile validation round 12 finding R12-F2).</remarks>
    [Fact]
    public async Task QueryAsync_TurnStatusInProgressAndStaleOlderThanHours_ReturnsOnlyStaleOpenTurns()
    {
        var now = AgeBoundaryContractTests.References[0];
        var sut = CreateSut(new SettableTimeProvider(now));
        var staleStamp = Stamp(now.AddHours(-30));
        var recentStamp = Stamp(now.AddHours(-1));

        await sut.SubmitAsync(
            CreateSession("Cursor-20260818T200000Z-stale-open", turnStatus: "in_progress", timestamp: staleStamp),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.SubmitAsync(
            CreateSession("Cursor-20260818T200000Z-recent-open", turnStatus: "in_progress", timestamp: recentStamp),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.SubmitAsync(
            CreateSession("Cursor-20260818T200000Z-stale-done", turnStatus: "completed", timestamp: staleStamp),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var queried = await sut.QueryAsync(
            new SessionLogQueryRequest
            {
                Agent = "Cursor",
                TurnStatus = "in_progress",
                StaleOlderThanHours = 24,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var session = Assert.Single(queried.Items, item => item.SessionId is not null && item.SessionId.Contains("stale-open", StringComparison.Ordinal));
        Assert.DoesNotContain(queried.Items, item => item.SessionId is not null && item.SessionId.Contains("recent-open", StringComparison.Ordinal));
        Assert.DoesNotContain(queried.Items, item => item.SessionId is not null && item.SessionId.Contains("stale-done", StringComparison.Ordinal));
        var turn = Assert.Single(session.Turns!);
        Assert.Equal("in_progress", turn.Status);

        var fetched = await sut.GetAsync("Cursor", "Cursor-20260818T200000Z-stale-open", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("in_progress", fetched!.Turns!.First().Status);
    }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 binding, decision D1 of <see cref="AgeBoundaryContractTests"/>: for each scripted reference
    /// time and each contract row, one in-progress turn recorded at the row's time is listed by the 24 hour stale-turn
    /// query exactly when the contract says (at the threshold too, because only a turn after the cutoff is excluded), a
    /// completed turn recorded at the same time is never listed, and the query leaves the in-progress turn in progress.
    /// Test data: the contract's reference and recorded times, one pair of sessions per row in the in-memory store.
    /// </summary>
    /// <returns>A task that completes when every row has been asserted.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added for hostile validation round 12 finding R12-F2 (the reviewer's proposed StaleTurnQuery_UsesScriptedAsOfTime).</remarks>
    [Fact]
    public Task StaleTurnQuery_UsesScriptedAsOfTime() =>
        AgeBoundaryContractTests.AssertDecisionAsync("D1 stale in-progress turn query", async (reference, recorded) =>
        {
            var sut = CreateSut(new SettableTimeProvider(reference));
            var suffix = Guid.NewGuid().ToString("N")[..12];
            var openId = "Cursor-20260818T200000Z-d1open" + suffix;
            var doneId = "Cursor-20260818T200000Z-d1done" + suffix;
            await sut.SubmitAsync(CreateSession(openId, turnStatus: "in_progress", timestamp: Stamp(recorded)),
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            await sut.SubmitAsync(CreateSession(doneId, turnStatus: "completed", timestamp: Stamp(recorded)),
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var queried = await sut.QueryAsync(
                new SessionLogQueryRequest { Agent = "Cursor", TurnStatus = "in_progress", StaleOlderThanHours = 24, Limit = 1000 },
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.DoesNotContain(queried.Items, item => item.SessionId == doneId);
            var fetched = await sut.GetAsync("Cursor", openId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("in_progress", fetched!.Turns!.First().Status);
            return queried.Items.Any(item => item.SessionId == openId);
        });

    /// <summary>TEST-MCP-TRIAGESTORE-001: superseded persist with omitted context writes None sentinels and canceled.</summary>
    [Fact]
    public async Task UpsertTurnAsync_OmittedPlanFileTodoId_WritesNoneAndCanceled()
    {
        var sessionId = "Cursor-20260818T200000Z-supersede-none";
        await _sut.SubmitAsync(CreateSession(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var hookTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260818T200000Z-hook-open",
            Status = "canceled",
            Response = "Superseded by req-20260818T200001Z-new",
        };

        await _sut.UpsertTurnAsync("Cursor", sessionId, hookTurn, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = await _sut.GetAsync("Cursor", sessionId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!, item => item.RequestId == hookTurn.RequestId);
        Assert.Equal("canceled", turn.Status);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, turn.PlanFile);
        Assert.Equal(SessionLogTurnContextValidator.NoneSentinel, turn.TodoId);
    }

    /// <summary>A session-log service over this class's in-memory store that reads <paramref name="clock"/>.</summary>
    /// <param name="clock">The service's clock.</param>
    /// <returns>The service.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006 (appended): added for the stale-turn cases (hostile validation round 12 finding R12-F2).</remarks>
    private SessionLogService CreateSut(TimeProvider clock) => new(
        _db,
        NullLogger<SessionLogService>.Instance,
        Substitute.For<IChangeEventBus>(),
        new WorkspaceContext { WorkspacePath = WorkspacePath },
        submitOptions: null,
        time: clock);

    /// <summary>The second-precision UTC text a session-log turn records its time in.</summary>
    /// <param name="value">The time.</param>
    /// <returns>The text.</returns>
    private static string Stamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
}
