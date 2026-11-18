namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Durably allocated identity independent of a versioned branch.</summary>
/// <param name="Id">Opaque canonical identity; the B4 syntax proposal is not accepted here.</param>
/// <param name="Origin">Creator tag.</param><param name="Sequence">Durable nonreused sequence.</param>
/// <param name="Title">Persisted content.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-BranchRevertDoesNotReuseSequence; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal sealed record Phase0AllocatedIdentity(string Id, string Origin, long Sequence, string Title);

/// <summary>Independent allocator and branch fixture boundary.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; B4; FR-MCP-182 ac-BranchRevertDoesNotReuseSequence.</remarks>
internal interface IPhase0AllocatorLifecycleProbe
{
    /// <summary>Establishes allocated sequence42 and a branch that can be reverted.</summary>
    void PrepareAllocated42();
    /// <summary>Reads the durable allocator outside the project branch.</summary>
    /// <returns>Actual durable high-water value.</returns>
    long ReadHighWater();
    /// <summary>Reverts the original branch to before42 and switches to a new branch.</summary>
    void RevertAndSwitchBranch();
    /// <summary>Creates one new TODO on the new branch.</summary>
    /// <param name="requestId">Idempotent creation request.</param><param name="title">Required content.</param>
    /// <returns>Created identity and actual persisted sequence.</returns>
    Phase0AllocatedIdentity? CreateTodo(string requestId, string title);
    /// <summary>Reads the new branch TODOs independently of the create return value.</summary>
    /// <returns>Complete persisted rows.</returns>
    IReadOnlyList<Phase0AllocatedIdentity>? ReadTodos();
}

/// <summary>A persisted human-ID reservation, not a response-derived synthetic receipt.</summary>
/// <param name="Project">Project scope.</param><param name="Kind">Entity kind.</param>
/// <param name="HumanId">Exact requested human identity.</param><param name="Node">Owning node.</param>
/// <param name="RequestId">Winning request.</param><param name="ReceiptId">Stable reservation receipt.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-SimultaneousHumanIdReservation; TEST-MCP-230.</remarks>
internal sealed record Phase0HumanReservation(string Project, string Kind, string HumanId, string Node, string RequestId, string ReceiptId);

/// <summary>Reservation response with its stable stored record.</summary>
/// <param name="Status">Protocol status.</param><param name="Code">Normalized failure, or null on success.</param>
/// <param name="Reservation">Stored reservation on success; conflict-body shape is not constrained here.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-SimultaneousHumanIdReservation; TR-MCP-FEDERATION-009.</remarks>
internal sealed record Phase0ReservationReply(int Status, string? Code, Phase0HumanReservation? Reservation);

/// <summary>Concurrent hub reservation fixture boundary.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-SimultaneousHumanIdReservation; TEST-MCP-230.</remarks>
internal interface IPhase0HumanReservationProbe
{
    /// <summary>Establishes no reservation for the fixture human ID.</summary>
    void Prepare();
    /// <summary>Atomically attempts a reservation; the fixture sends both first requests concurrently.</summary>
    /// <param name="project">Project scope.</param><param name="kind">Entity kind.</param><param name="humanId">Exact label.</param>
    /// <param name="node">Requesting node.</param><param name="requestId">Idempotent request identity.</param>
    /// <returns>Protocol outcome and recorded reservation.</returns>
    Task<Phase0ReservationReply?> ReserveAsync(string project, string kind, string humanId, string node, string requestId);
    /// <summary>Reads persisted reservations independently.</summary>
    /// <returns>All fixture reservation rows.</returns>
    IReadOnlyList<Phase0HumanReservation>? ReadReservations();
}

/// <summary>Offline provisional TODO with requested human identity retained as data.</summary>
/// <param name="CanonicalId">Generated provisional canonical identity.</param><param name="RequestedHumanId">Unreserved label.</param>
/// <param name="Title">Authored content.</param><param name="Origin">Creator tag.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-OfflineHumanIdPublicationDenied; TEST-MCP-230.</remarks>
internal sealed record Phase0ProvisionalTodo(string CanonicalId, string RequestedHumanId, string Title, string Origin);

/// <summary>Publication rejection result.</summary>
/// <param name="Status">HTTP-style outcome.</param><param name="Code">Normalized protocol error.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-OfflineHumanIdPublicationDenied; TR-MCP-FEDERATION-009.</remarks>
internal sealed record Phase0IdentityPublishReply(int Status, string? Code);

/// <summary>Offline human-ID publication boundary with independent local and remote readbacks.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-OfflineHumanIdPublicationDenied.</remarks>
internal interface IPhase0OfflineHumanIdentityProbe
{
    /// <summary>Prepares an offline local row and connected remote C0 with no human reservation.</summary>
    /// <param name="todo">Fixture content to retain.</param>
    void Prepare(Phase0ProvisionalTodo todo);
    /// <summary>Reconnects and attempts publication of the row without a human-ID reservation.</summary>
    /// <returns>The actual publication rejection.</returns>
    Phase0IdentityPublishReply? ReconnectAndPublish();
    /// <summary>Reads the durable local TODO after the rejected publication.</summary>
    /// <returns>Unchanged local row.</returns>
    Phase0ProvisionalTodo? ReadLocalTodo();
    /// <summary>Reads the actual remote project head.</summary>
    /// <returns>Canonical remote head.</returns>
    string? ReadRemoteHead();
    /// <summary>Reads actual hub reservations.</summary>
    /// <returns>Complete fixture reservation list.</returns>
    IReadOnlyList<Phase0HumanReservation>? ReadReservations();
}

/// <summary>Requirements-derived identity lifecycle assertions reusable by later real disposable bindings.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-BranchRevertDoesNotReuseSequence,
/// ac-SimultaneousHumanIdReservation, ac-OfflineHumanIdPublicationDenied;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. These assertions select no new ID grammar or production allocator.
/// </remarks>
internal static class Phase0IdentityLifecycleContract
{
    /// <summary>Checks a branch revert cannot roll the allocator back or lose the subsequent creation.</summary>
    /// <param name="probe">Independent allocator and project read boundary.</param>
    internal static void AssertBranchRevert(IPhase0AllocatorLifecycleProbe probe)
    {
        probe.PrepareAllocated42();
        Assert.Equal(42, probe.ReadHighWater());
        probe.RevertAndSwitchBranch();
        Assert.Equal(42, probe.ReadHighWater());
        var row = probe.CreateTodo("create-after-revert", "task after revert");
        Assert.NotNull(row);
        Assert.False(string.IsNullOrWhiteSpace(row.Id));
        Assert.Equal("LA", row.Origin);
        Assert.True(row.Sequence > 42, "Branch revert reused a sequence at or below42.");
        Assert.Equal("task after revert", row.Title);
        Assert.Equal(row.Sequence, probe.ReadHighWater());
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Phase0AllocatedIdentity>>(probe.ReadTodos());
        Assert.Equal(row, Assert.Single(rows));
    }

    /// <summary>Checks simultaneous hub requests have one winner, durable uniqueness and idempotent winning retry.</summary>
    /// <param name="probe">Concurrent reservation boundary.</param>
    /// <returns>Completion after every durable reservation readback is checked.</returns>
    internal static async Task AssertConcurrentReservationAsync(IPhase0HumanReservationProbe probe)
    {
        probe.Prepare();
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<Phase0HumanReservation>>(probe.ReadReservations()));
        var first = probe.ReserveAsync("Atlas", "Todo", "IMPL-UNITGREEN-001", "LA", "reserve-LA");
        var second = probe.ReserveAsync("Atlas", "Todo", "IMPL-UNITGREEN-001", "LG", "reserve-LG");
        var replies = await Task.WhenAll(first, second);
        Assert.All(replies, reply => Assert.NotNull(reply));
        Assert.Equal(new[] { 201, 409 }, replies.Select(reply => reply!.Status).OrderBy(status => status));
        var winner = replies.Single(reply => reply!.Status == 201)!;
        var loser = replies.Single(reply => reply!.Status == 409)!;
        Assert.Null(winner.Code);
        Assert.Equal("human_id_reserved", loser.Code);
        var stored = Assert.IsType<Phase0HumanReservation>(winner.Reservation);
        Assert.Equal("Atlas", stored.Project);
        Assert.Equal("Todo", stored.Kind);
        Assert.Equal("IMPL-UNITGREEN-001", stored.HumanId);
        var winnerIndex = Array.IndexOf(replies, winner);
        Assert.Equal(winnerIndex == 0 ? "LA" : "LG", stored.Node);
        Assert.Equal(winnerIndex == 0 ? "reserve-LA" : "reserve-LG", stored.RequestId);
        Assert.False(string.IsNullOrWhiteSpace(stored.ReceiptId));
        // The approved contract constrains the conflict code, not a conflict response payload.
        Assert.Equal(stored, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<Phase0HumanReservation>>(probe.ReadReservations())));
        var retry = await probe.ReserveAsync(stored.Project, stored.Kind, stored.HumanId, stored.Node, stored.RequestId);
        Assert.NotNull(retry);
        Assert.Equal(200, retry.Status);
        Assert.Null(retry.Code);
        Assert.Equal(stored, retry.Reservation);
        Assert.Equal(stored, Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<Phase0HumanReservation>>(probe.ReadReservations())));
    }

    /// <summary>Checks unreserved human publication refuses without deleting or silently reserving the local work.</summary>
    /// <param name="probe">Local content, remote ref and hub-reservation boundary.</param>
    internal static void AssertOfflineHumanPublication(IPhase0OfflineHumanIdentityProbe probe)
    {
        var expected = new Phase0ProvisionalTodo("fixture-provisional-LA-1", "IMPL-UNITGREEN-001", "offline café 雪", "LA");
        probe.Prepare(expected with { });
        Assert.Equal(expected, probe.ReadLocalTodo());
        Assert.Equal("C0", probe.ReadRemoteHead());
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<Phase0HumanReservation>>(probe.ReadReservations()));
        var result = probe.ReconnectAndPublish();
        Assert.NotNull(result);
        Assert.Equal(409, result.Status);
        Assert.Equal("human_id_unreserved", result.Code);
        Assert.Equal(expected, probe.ReadLocalTodo());
        Assert.Equal("C0", probe.ReadRemoteHead());
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<Phase0HumanReservation>>(probe.ReadReservations()));
    }
}
