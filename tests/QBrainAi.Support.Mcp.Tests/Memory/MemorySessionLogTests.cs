using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-015 / FR-MCP-MEMORY-010: Mutations append sessionlog actions when a turn is open.
/// FR-MCP-MEMORY-007 criterion 6 (PLAN-SESSIONTRIAGEFIX-001 D6, triage-report-e1f01430): the appended action uses type
/// edit and takes the next free order on its turn.
/// </summary>
public sealed class MemorySessionLogTests : IDisposable
{
    /// <summary>
    /// D6 fixture: live actions at orders 5 to 8 and a deleted action at order 9, so the turn's action count (5) is not
    /// its highest order and count + 1 (6) is an order a live action holds.
    /// </summary>
    private static readonly (int Order, bool Deleted)[] GappedTurnOrders = [(5, false), (6, false), (7, false), (8, false), (9, true)];

    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-37: remember/add/update/remove append a sessionlog action when a turn is open.</summary>
    [Fact]
    public async Task Mutations_AppendActionWhenTurnOpen()
    {
        long turnId;
        await using (var db = _harness.CreateContext(_harness.WorkspaceA))
        {
            var session = new SessionLogEntity
            {
                SourceType = "CursorGrok",
                SessionId = "CursorGrok-20260918T210000Z-s1",
                Model = "grok",
                Started = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                WorkspaceId = _harness.WorkspaceA,
            };
            db.SessionLogs.Add(session);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var turn = new SessionLogTurnEntity
            {
                SessionLogId = session.Id,
                RequestId = "req-20260918T210000Z-001-s1-red",
                Timestamp = DateTimeOffset.UtcNow,
                QueryTitle = "S1 red",
                Response = "open",
            };
            db.SessionLogTurns.Add(turn);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            turnId = turn.Id;
        }

        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "slog",
            Text = "sessionlog mutation",
            UpdatedBy = "CursorGrok",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        await using var after = _harness.CreateContext(_harness.WorkspaceA);
        var actions = await after.SessionLogActions
            .IgnoreQueryFilters()
            .Where(action => action.SessionLogTurnId == turnId)
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Contains(actions, action =>
            (action.Description ?? string.Empty).Contains(added.Memory!.Id, StringComparison.OrdinalIgnoreCase)
            || (action.Type ?? string.Empty).Contains("memory", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// FR-MCP-MEMORY-007 criterion 6 (PLAN-SESSIONTRIAGEFIX-001 D6, triage-report-e1f01430, mock gate): the memory-action
    /// assertion accepts type edit at order 10 on <see cref="GappedTurnOrders"/> and at order 1 on a turn with no actions,
    /// and rejects the count rule the hook used (order 6, held by a live action), type memory, a highest order that leaves
    /// out the deleted action (order 9), and order 0 on an empty turn. Data: hand-built order lists; the accepted orders
    /// are pinned from the criterion by hand.
    /// </summary>
    [Fact]
    public void MemoryActionAssertion_AcceptsEditNextFreeOrderAndRejectsCountOrMemoryType()
    {
        AssertMemoryAction(GappedTurnOrders, ("edit", 10));
        AssertMemoryAction([], ("edit", 1));

        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertMemoryAction(GappedTurnOrders, ("edit", 6)));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertMemoryAction(GappedTurnOrders, ("memory", 10)));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertMemoryAction(GappedTurnOrders, ("edit", 9)));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertMemoryAction([], ("edit", 0)));
    }

    /// <summary>
    /// FR-MCP-MEMORY-007 criterion 6 (PLAN-SESSIONTRIAGEFIX-001 D6, triage-report-e1f01430): a memory add on an open turn
    /// that records no actions appends one action of type edit, status completed, order 1, described "memory_add {id}".
    /// Data: MemoryS1Harness in-memory SQLite; one session and one in_progress turn in workspace A; one memory added through
    /// MemoryService.
    /// </summary>
    [Fact]
    public async Task Mutation_ActionUsesDocumentedEditType()
    {
        var turnId = await SeedTurnAsync("documented-type", []).ConfigureAwait(true);

        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "slog",
            Text = "documented action type",
            UpdatedBy = "CursorGrok",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var action = Assert.Single(await ReadLiveActionsAsync(turnId).ConfigureAwait(true));
        AssertMemoryAction([], (action.Type, action.Order));
        Assert.Equal("completed", action.Status);
        Assert.Equal($"memory_add {added.Memory!.Id}", action.Description);
    }

    /// <summary>
    /// FR-MCP-MEMORY-007 criterion 6 (PLAN-SESSIONTRIAGEFIX-001 D6, triage-report-e1f01430): on an open turn that records
    /// <see cref="GappedTurnOrders"/>, a memory add takes order 10, an update of the same memory then takes order 11, and
    /// no live order is held twice. Data: MemoryS1Harness in-memory SQLite; one session and one in_progress turn in
    /// workspace A whose order-9 action is removed through the context (stored as a soft delete, checked before the
    /// mutations); one memory added and then updated through MemoryService.
    /// </summary>
    [Fact]
    public async Task Mutation_ActionTakesNextFreeOrderAfterGappedAndDeletedOrders()
    {
        var ct = TestContext.Current.CancellationToken;
        var turnId = await SeedTurnAsync("gapped-orders", GappedTurnOrders).ConfigureAwait(true);
        var seeded = await ReadRecordedOrdersAsync(turnId).ConfigureAwait(true);
        Assert.Equal(GappedTurnOrders, seeded);

        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "slog",
            Text = "gapped and deleted orders",
            UpdatedBy = "CursorGrok",
        }, cancellationToken: ct).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        var memoryId = added.Memory!.Id;
        var addAction = Assert.Single(
            await ReadLiveActionsAsync(turnId).ConfigureAwait(true),
            action => action.Description == $"memory_add {memoryId}");
        AssertMemoryAction(seeded, (addAction.Type, addAction.Order));

        var beforeUpdate = await ReadRecordedOrdersAsync(turnId).ConfigureAwait(true);
        var updated = await _harness.UpdateCompatAsync(memoryId, new MemoryUpdateRequest
        {
            Text = "gapped and deleted orders, updated",
            UpdatedBy = "CursorGrok",
        }, cancellationToken: ct).ConfigureAwait(true);
        Assert.True(updated.Success, updated.Error);
        var live = await ReadLiveActionsAsync(turnId).ConfigureAwait(true);
        var updateAction = Assert.Single(live, action => action.Description == $"memory_update {memoryId}");
        AssertMemoryAction(beforeUpdate, (updateAction.Type, updateAction.Order));

        Assert.Equal(10, addAction.Order);
        Assert.Equal(11, updateAction.Order);
        Assert.Equal(live.Count, live.Select(action => action.Order).Distinct().Count());
    }

    /// <summary>
    /// FR-MCP-MEMORY-007 criterion 6: <paramref name="appended"/> uses type edit and takes the next free order on a turn
    /// that already records <paramref name="recorded"/>: one more than the highest recorded order, deleted actions
    /// included, or 1 when the turn records none, so it repeats no recorded order.
    /// </summary>
    private static void AssertMemoryAction(IReadOnlyCollection<(int Order, bool Deleted)> recorded, (string? Type, int Order) appended)
    {
        Assert.Equal("edit", appended.Type);
        Assert.DoesNotContain(recorded, action => action.Order == appended.Order);
        Assert.Equal(recorded.Count == 0 ? 1 : recorded.Max(action => action.Order) + 1, appended.Order);
    }

    /// <summary>
    /// Seeds one session and one in_progress turn in workspace A with one action per <paramref name="orders"/> entry;
    /// entries marked deleted are then removed through the context, which the store keeps as a soft delete.
    /// </summary>
    private async Task<long> SeedTurnAsync(string suffix, IReadOnlyCollection<(int Order, bool Deleted)> orders)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var session = new SessionLogEntity
        {
            SourceType = "CursorGrok",
            SessionId = $"CursorGrok-20261014T120000Z-{suffix}",
            Model = "grok",
            Started = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
            WorkspaceId = _harness.WorkspaceA,
        };
        db.SessionLogs.Add(session);
        await db.SaveChangesAsync(ct).ConfigureAwait(true);
        var turn = new SessionLogTurnEntity
        {
            SessionLogId = session.Id,
            RequestId = $"req-20261014T120000Z-{suffix}",
            Timestamp = DateTimeOffset.UtcNow,
            QueryTitle = "D6 fixture",
            Status = "in_progress",
        };
        db.SessionLogTurns.Add(turn);
        await db.SaveChangesAsync(ct).ConfigureAwait(true);
        var seeded = orders.Select(entry => (entry.Deleted, Action: new SessionLogActionEntity
        {
            SessionLogTurnId = turn.Id,
            Order = entry.Order,
            Type = "edit",
            Status = "completed",
            Description = $"seeded action {entry.Order}",
        })).ToList();
        db.SessionLogActions.AddRange(seeded.Select(entry => entry.Action));
        await db.SaveChangesAsync(ct).ConfigureAwait(true);
        db.SessionLogActions.RemoveRange(seeded.Where(entry => entry.Deleted).Select(entry => entry.Action));
        await db.SaveChangesAsync(ct).ConfigureAwait(true);
        return turn.Id;
    }

    /// <summary>Reads every action recorded on the turn, deleted ones included, as (order, deleted) sorted by order.</summary>
    private async Task<(int Order, bool Deleted)[]> ReadRecordedOrdersAsync(long turnId)
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        var rows = await db.SessionLogActions
            .IgnoreQueryFilters()
            .Where(action => action.SessionLogTurnId == turnId)
            .Select(action => new { action.Order, Deleted = EF.Property<bool>(action, "IsDeleted") })
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        return [.. rows.Select(row => (row.Order, row.Deleted)).OrderBy(row => row.Order)];
    }

    /// <summary>Reads the turn's live (not deleted) actions.</summary>
    private async Task<List<SessionLogActionEntity>> ReadLiveActionsAsync(long turnId)
    {
        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        return await db.SessionLogActions
            .IgnoreQueryFilters()
            .Where(action => action.SessionLogTurnId == turnId && !EF.Property<bool>(action, "IsDeleted"))
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
    }
}
