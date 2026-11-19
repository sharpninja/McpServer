namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Immutable publication input plus the authenticated fixture node identity.</summary>
/// <param name="RequestId">Stable operation UUID.</param><param name="NodeId">Authenticated node, not an extra publication wire field.</param>
/// <param name="ProjectId">Requested project UUID.</param><param name="Branch">Published branch.</param>
/// <param name="HubEpoch">Measured hub epoch.</param><param name="CandidateHead">Existing committed candidate.</param>
/// <param name="ExpectedPublishedHead">Original compare-and-swap head.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233; Ph0 feasibility only.</remarks>
internal sealed record Phase0PublicationRequest(Guid RequestId, Guid NodeId, Guid ProjectId, string Branch,
    Guid HubEpoch, string CandidateHead, string ExpectedPublishedHead);

/// <summary>Independently read durable receipt, never inferred from an HTTP success.</summary>
/// <param name="Id">Opaque receipt key.</param><param name="Request">Persisted operation bindings.</param>
/// <param name="ResultHead">Persisted result hash.</param><param name="Generation">Persisted published generation.</param>
/// <param name="Completed">Whether the stored operation has reached successful completion.</param>
/// <remarks>2026-11-18: FR-MCP-181 ac-LostPushAckDoesNotDuplicateCommit; TR-MCP-FEDERATION-008; TEST-MCP-229.</remarks>
internal sealed record Phase0PublicationReceipt(string Id, Phase0PublicationRequest Request, string ResultHead,
    long Generation, bool Completed);

/// <summary>Independent source, hub, public repository and durable control observations.</summary>
/// <param name="Epoch">Current hub epoch.</param><param name="SourceHead">Source committed head.</param>
/// <param name="SourceCommittedTitle">Source committed row.</param><param name="SourceWorkingTitle">Source working row.</param>
/// <param name="SourceHistory">All source-head ancestors.</param><param name="HubHead">Independent hub application head.</param>
/// <param name="HubWorkingTitle">Independent hub working row.</param><param name="PublicRefs">main, feature and other-project heads.</param>
/// <param name="PublicTitle">Public main committed row.</param><param name="PublicHistory">All public-main ancestors.</param>
/// <param name="Generation">Published generation.</param><param name="UploadHead">Durable isolated upload ref, absent before upload.</param>
/// <param name="RecoveryRoots">Actual reachable candidate hashes rooted by persisted refs.</param>
/// <param name="Intent">Durable operation input.</param><param name="IntentCompleted">Durable sender completion.</param>
/// <param name="Receipts">All receipts for this isolated fixture.</param><param name="ForceUpdates">Observed force-ref commands.</param>
/// <remarks>2026-11-18: FR-MCP-178 ac-HubDraftDoesNotBlockRemote; FR-MCP-181 ac-LostPushAckDoesNotDuplicateCommit; TR-MCP-FEDERATION-005/008; TEST-MCP-226/229.</remarks>
internal sealed record Phase0RemotePublicationState(Guid Epoch, string SourceHead, string SourceCommittedTitle,
    string SourceWorkingTitle, IReadOnlyList<Phase0HistoryPoint> SourceHistory, string HubHead, string HubWorkingTitle,
    IReadOnlyDictionary<string, string> PublicRefs, string PublicTitle, IReadOnlyList<Phase0HistoryPoint> PublicHistory,
    long Generation, string? UploadHead, IReadOnlyList<string> RecoveryRoots, Phase0PublicationRequest Intent,
    bool IntentCompleted, IReadOnlyList<Phase0PublicationReceipt> Receipts, int ForceUpdates);

/// <summary>Delivered protocol response and actual fixture-only fault observation.</summary>
/// <param name="Kind">Http or TransportFailure.</param><param name="Status">Delivered HTTP status, absent on a lost reply.</param>
/// <param name="Code">Stable failure code, absent on success or transport loss.</param>
/// <param name="Receipt">Decoded successful response receipt projection.</param><param name="ReachedFault">Observed test boundary or None.</param>
/// <param name="ProcessTerminated">Whether the instrumented publisher actually exited at that boundary.</param>
/// <remarks>2026-11-18: FR-MCP-181 ac-LostPushAckDoesNotDuplicateCommit; TR-MCP-FEDERATION-008; TEST-MCP-229.</remarks>
internal sealed record Phase0PublicationReply(string Kind, int? Status, string? Code, Phase0PublicationReceipt? Receipt,
    string ReachedFault, bool ProcessTerminated);

/// <summary>Observed process lifecycle, with the replacement paused before recovery admission.</summary>
/// <param name="OldPid">Previous publisher process identity.</param><param name="NewPid">Replacement process identity.</param>
/// <param name="OldExited">Confirmed previous-process exit.</param><param name="StorageReopened">Actual store reopen completed.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-181; TR-MCP-FEDERATION-008; TEST-MCP-229.</remarks>
internal sealed record Phase0PublicationRestart(int OldPid, int NewPid, bool OldExited, bool StorageReopened);

/// <summary>Test-only seam for committed upload, guarded publication and same-request recovery.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185/178/181; TR-MCP-FEDERATION-012/005/008; TEST-MCP-233/226/229.
/// Native adapters belong to an integration project after the group barrier opens; no adapter or gateway is implemented here.</remarks>
internal interface IPhase0RemotePublicationProbe
{
    /// <summary>Seeds original C0, committed A1 and independently optional source/hub drafts, with one durable pending intent.</summary>
    /// <param name="sourceDirty">Leave source-draft after committing published.</param><param name="hubDirty">Leave hub-draft on independent C0.</param>
    void Prepare(bool sourceDirty, bool hubDirty);
    /// <summary>Reads actual stores through separate connections; checkpoint is a test instrumentation label only.</summary>
    /// <param name="checkpoint">Before, Uploaded, Attempted, Restarted, Recovered, Retried, Reopened or Conflict.</param>
    /// <returns>Actual full fixture observation, or null.</returns>
    Phase0RemotePublicationState? Read(string checkpoint);
    /// <summary>Transfers only the committed candidate into the isolated upload namespace.</summary>
    /// <param name="request">Immutable operation bindings.</param>
    void Upload(Phase0PublicationRequest request);
    /// <summary>Attempts the guarded operation; a fault is injected only in the private fixture.</summary>
    /// <param name="request">Canonical request bindings.</param><param name="fault">None, DropReply, BeforeRef, AfterRef or AfterReceipt.</param>
    /// <returns>Delivered response and observed injection boundary.</returns>
    Phase0PublicationReply? Publish(Phase0PublicationRequest request, string fault);
    /// <summary>Restarts the publisher and reopens stores while pausing automatic recovery before admission.</summary>
    /// <returns>Independent native process/store observations.</returns>
    Phase0PublicationRestart? Restart();
    /// <summary>Resumes the original durable operation and records sender completion after durable success.</summary>
    /// <param name="request">Same request and payload, never a new operation.</param><returns>Actual response.</returns>
    Phase0PublicationReply? Recover(Phase0PublicationRequest request);
}

/// <summary>Reusable assertions for clean/dirty committed uploads and exact idempotent process recovery.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Supplemental Ph0 proof: FR-MCP-178 ac-HubDraftDoesNotBlockRemote and FR-MCP-181 ac-LostPushAckDoesNotDuplicateCommit;
/// TR-MCP-FEDERATION-005/008; TEST-MCP-226/229. Mock success is neither native proof nor later-phase AC satisfaction.</remarks>
internal static class Phase0RemotePublicationContract
{
    /// <summary>Exercises each durable boundary, exact retry, reopen and conflicting-payload denial.</summary>
    /// <param name="probe">Independent mock or later disposable native adapter.</param><param name="sourceDirty">Source draft fixture.</param>
    /// <param name="hubDirty">Hub draft fixture.</param><param name="fault">Fixture boundary to interrupt.</param>
    internal static void AssertPublication(IPhase0RemotePublicationProbe probe, bool sourceDirty, bool hubDirty, string fault)
    {
        Assert.Contains(fault, new[] { "None", "DropReply", "BeforeRef", "AfterRef", "AfterReceipt" });
        probe.Prepare(sourceDirty, hubDirty);
        var before = Freeze(Require(probe.Read("Before")));
        Assert.NotEqual(Guid.Empty, before.Epoch);
        Assert.False(string.IsNullOrWhiteSpace(before.SourceHead));
        Assert.Equal(new[] { "feature", "main", "other" }, before.PublicRefs.Keys.Order(StringComparer.Ordinal));
        Assert.All(before.PublicRefs.Values, head => Assert.False(string.IsNullOrWhiteSpace(head)));
        Assert.Equal(4, before.PublicRefs.Values.Append(before.SourceHead).Distinct(StringComparer.Ordinal).Count());
        var request = new Phase0PublicationRequest(Guid.Parse("33333333-3333-4333-8333-333333333333"),
            Guid.Parse("22222222-2222-4222-8222-222222222222"), Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "main", before.Epoch, before.SourceHead, before.PublicRefs["main"]);
        Assert.True(before.Generation >= 0);
        AssertHistory(before.PublicHistory, before.PublicHistory);
        Assert.Contains(before.PublicHistory, point => point.Hash == request.ExpectedPublishedHead);
        var expectedSource = before.PublicHistory.Append(new Phase0HistoryPoint(request.CandidateHead, [request.ExpectedPublishedHead])).ToArray();
        AssertHistory(expectedSource, before.SourceHistory);
        Assert.Equal(request.ExpectedPublishedHead, before.HubHead);
        Assert.Equal(sourceDirty ? "source-draft" : "published", before.SourceWorkingTitle);
        Assert.Equal(hubDirty ? "hub-draft" : "original", before.HubWorkingTitle);
        Check(before, before, request, published: false, uploaded: false, receipts: 0, completed: false);

        probe.Upload(request);
        Check(Require(probe.Read("Uploaded")), before, request, false, true, 0, false);
        var first = probe.Publish(request, fault);
        Assert.NotNull(first);
        Assert.Equal(fault, first.ReachedFault);
        Assert.Equal(fault is "BeforeRef" or "AfterRef" or "AfterReceipt", first.ProcessTerminated);
        var published = fault != "BeforeRef";
        var hasReceipt = fault is "None" or "DropReply" or "AfterReceipt";
        var attempted = Freeze(Require(probe.Read("Attempted")));
        Check(attempted, before, request, published, true, hasReceipt ? 1 : 0, fault == "None");
        if (fault == "None") AssertSuccess(first, Assert.Single(attempted.Receipts));
        else
        {
            Assert.Equal("TransportFailure", first.Kind);
            Assert.Null(first.Status); Assert.Null(first.Code); Assert.Null(first.Receipt);
        }
        AssertRestart(probe.Restart());
        var restarted = Require(probe.Read("Restarted"));
        Check(restarted, before, request, published, true, hasReceipt ? 1 : 0, fault == "None");
        Assert.Equal(attempted.Generation, restarted.Generation);
        Assert.Equal(attempted.Receipts, restarted.Receipts);

        var recoveredReply = probe.Recover(request);
        var recovered = Freeze(Require(probe.Read("Recovered")));
        Check(recovered, before, request, true, true, 1, true);
        var receipt = Assert.Single(recovered.Receipts);
        AssertSuccess(recoveredReply, receipt);
        if (hasReceipt) Assert.Equal(Assert.Single(attempted.Receipts), receipt);
        var retry = probe.Publish(request, "None");
        var retried = Require(probe.Read("Retried"));
        Check(retried, before, request, true, true, 1, true, receipt);
        AssertSuccess(retry, receipt);
        Assert.Equal(recovered.Generation, retried.Generation);
        AssertRestart(probe.Restart());
        Check(Require(probe.Read("Reopened")), before, request, true, true, 1, true, receipt);
        var rejected = probe.Publish(request with { CandidateHead = "different-candidate" }, "None");
        Assert.NotNull(rejected);
        Assert.Equal("Http", rejected.Kind); Assert.Equal(409, rejected.Status);
        Assert.Equal("request_id_conflict", rejected.Code); Assert.Null(rejected.Receipt);
        Assert.Equal("None", rejected.ReachedFault); Assert.False(rejected.ProcessTerminated);
        Check(Require(probe.Read("Conflict")), before, request, true, true, 1, true, receipt);
    }

    /// <summary>Checks every protected field independently of response bodies or the implementation's success flags.</summary>
    private static void Check(Phase0RemotePublicationState actual, Phase0RemotePublicationState before,
        Phase0PublicationRequest request, bool published, bool uploaded, int receipts, bool completed,
        Phase0PublicationReceipt? receipt = null)
    {
        Assert.Equal(before.Epoch, actual.Epoch);
        Assert.Equal(before.SourceHead, actual.SourceHead);
        Assert.Equal("published", actual.SourceCommittedTitle);
        Assert.Equal(before.SourceWorkingTitle, actual.SourceWorkingTitle);
        AssertHistory(before.SourceHistory, actual.SourceHistory);
        Assert.Equal(before.HubHead, actual.HubHead); Assert.Equal(before.HubWorkingTitle, actual.HubWorkingTitle);
        Assert.NotNull(actual.PublicRefs);
        var refs = before.PublicRefs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        if (published) refs["main"] = request.CandidateHead;
        Assert.Equal(refs.OrderBy(pair => pair.Key, StringComparer.Ordinal), actual.PublicRefs.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.Equal(published ? "published" : "original", actual.PublicTitle);
        AssertHistory(published ? before.SourceHistory : before.PublicHistory, actual.PublicHistory);
        if (published && receipts != 0) Assert.True(actual.Generation > before.Generation);
        else if (published) Assert.True(actual.Generation >= before.Generation);
        else Assert.Equal(before.Generation, actual.Generation);
        Assert.Equal(uploaded ? request.CandidateHead : null, actual.UploadHead);
        Assert.NotNull(actual.RecoveryRoots);
        Assert.Equal(actual.RecoveryRoots.Count, actual.RecoveryRoots.Distinct(StringComparer.Ordinal).Count());
        Assert.All(actual.RecoveryRoots, root => Assert.False(string.IsNullOrWhiteSpace(root)));
        if (uploaded) Assert.Contains(request.CandidateHead, actual.RecoveryRoots); else Assert.Empty(actual.RecoveryRoots);
        Assert.Equal(request, actual.Intent); Assert.Equal(completed, actual.IntentCompleted);
        Assert.NotNull(actual.Receipts); Assert.Equal(receipts, actual.Receipts.Count);
        foreach (var row in actual.Receipts)
        {
            Assert.NotNull(row); Assert.False(string.IsNullOrWhiteSpace(row.Id)); Assert.Equal(request, row.Request);
            Assert.Equal(request.CandidateHead, row.ResultHead); Assert.Equal(actual.Generation, row.Generation); Assert.True(row.Completed);
            if (receipt is not null) Assert.Equal(receipt, row);
        }
        Assert.Equal(0, actual.ForceUpdates);
    }

    /// <summary>Checks a successful delivered response against its independently persisted receipt.</summary>
    private static void AssertSuccess(Phase0PublicationReply? reply, Phase0PublicationReceipt receipt)
    {
        Assert.NotNull(reply); Assert.Equal("Http", reply.Kind); Assert.Equal(200, reply.Status); Assert.Null(reply.Code);
        Assert.Equal(receipt, reply.Receipt);
        Assert.Equal("None", reply.ReachedFault); Assert.False(reply.ProcessTerminated);
    }

    /// <summary>Requires a new real process identity and a confirmed exit/reopen; not merely a simulated restart counter.</summary>
    private static void AssertRestart(Phase0PublicationRestart? restart)
    {
        Assert.NotNull(restart); Assert.True(restart.OldPid > 0); Assert.True(restart.NewPid > 0);
        Assert.NotEqual(restart.OldPid, restart.NewPid); Assert.True(restart.OldExited); Assert.True(restart.StorageReopened);
    }

    /// <summary>Checks complete reachable history with unique nonblank hashes and exact ordered parent sets.</summary>
    private static void AssertHistory(IReadOnlyList<Phase0HistoryPoint> expected, IReadOnlyList<Phase0HistoryPoint> actual)
    {
        Assert.NotNull(actual); Assert.NotEmpty(actual); Assert.Equal(expected.Count, actual.Count);
        Assert.All(actual, point => Assert.NotNull(point));
        Assert.Equal(actual.Count, actual.Select(point => point.Hash).Distinct(StringComparer.Ordinal).Count());
        foreach (var old in expected)
        {
            Assert.False(string.IsNullOrWhiteSpace(old.Hash));
            var found = Assert.Single(actual, point => point.Hash == old.Hash);
            Assert.NotNull(found.Parents); Assert.Equal(old.Parents, found.Parents);
        }
    }

    /// <summary>Requires a returned observation without substituting a default empty state.</summary>
    private static Phase0RemotePublicationState Require(Phase0RemotePublicationState? state) { Assert.NotNull(state); return state; }

    /// <summary>Detaches all mutable collections before subsequent probe calls.</summary>
    private static Phase0RemotePublicationState Freeze(Phase0RemotePublicationState state)
    {
        Assert.NotNull(state.SourceHistory); Assert.NotNull(state.PublicHistory); Assert.NotNull(state.PublicRefs);
        Assert.NotNull(state.RecoveryRoots); Assert.NotNull(state.Receipts);
        foreach (var point in state.SourceHistory.Concat(state.PublicHistory))
        { Assert.NotNull(point); Assert.NotNull(point.Parents); }
        return state with
        {
            SourceHistory = state.SourceHistory.Select(point => point with { Parents = point.Parents.ToArray() }).ToArray(),
            PublicHistory = state.PublicHistory.Select(point => point with { Parents = point.Parents.ToArray() }).ToArray(),
            PublicRefs = state.PublicRefs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            RecoveryRoots = state.RecoveryRoots.ToArray(), Receipts = state.Receipts.ToArray()
        };
    }
}
