namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Three-way cell evidence; null plus deletion flags distinguishes deletion from an empty value.</summary>
/// <param name="Entity">Stable fixture row identity.</param><param name="Column">Disputed column.</param>
/// <param name="Base">Original value.</param><param name="Local">B1 value.</param><param name="Incoming">A1 value.</param>
/// <param name="LocalDeleted">Whether the local alternative deletes the row.</param><param name="IncomingDeleted">Whether the incoming alternative deletes the row.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview/ac-SafeTodoMergeRules; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0MergeCell(string Entity, string Column, string? Base, string? Local, string? Incoming, bool LocalDeleted, bool IncomingDeleted);

/// <summary>One independently read physical ref and object-reachability result.</summary>
/// <param name="Purpose">Base, local, incoming or candidate role.</param><param name="Name">Actual private ref name.</param>
/// <param name="Target">Actual target commit hash.</param><param name="Readable">Whether the referenced object was read successfully.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-SameCellConflictNeedsReview; TR-MCP-FEDERATION-007; TEST-MCP-228; approved architecture section6 retention.</remarks>
internal sealed record Phase0MergePin(string Purpose, string Name, string Target, bool Readable);

/// <summary>Durable review projection read separately from the integration response.</summary>
/// <param name="Id">Stable UUID.</param><param name="Project">Project identity.</param><param name="Branch">Target branch.</param>
/// <param name="BaseHeads">Exact common ancestors.</param><param name="LocalHead">Original local source hash.</param>
/// <param name="IncomingHead">Original incoming source hash.</param><param name="CandidateRef">Retained local candidate ref.</param>
/// <param name="Cells">Exact differing alternatives.</param><param name="Errors">Recorded validation failures.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-DeleteUpdateNeedsReview/ac-CleanMergeCanFailDomainValidation/ac-StaleReviewRejected; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0MergeReview(Guid Id, string Project, string Branch, string[] BaseHeads, string LocalHead,
    string IncomingHead, string CandidateRef, Phase0MergeCell[] Cells, string[] Errors);

/// <summary>Independent live, public, candidate and durable-review readback at one checkpoint.</summary>
/// <param name="State">Internal probe outcome, not a new public protocol value.</param><param name="LiveHead">Current local main hash.</param>
/// <param name="LiveRows">Complete fixture live rows and field projections.</param><param name="PublicRefs">Complete protected public refs.</param>
/// <param name="CandidateRows">Complete isolated candidate rows.</param><param name="CellConflicts">Actual engine cell-conflict count.</param>
/// <param name="ConstraintViolations">Actual engine constraint-violation count.</param><param name="DomainErrors">Application validation failures.</param>
/// <param name="Review">Persisted review if needed.</param><param name="LocalPins">Locally retained physical refs.</param>
/// <param name="RemotePins">Privately retained remote refs, separate from public refs.</param>
/// <param name="TrackingIncomingHead">Current incoming tracking hash read independently of the review.</param>
/// <param name="CandidateHead">Current isolated candidate ref target read independently of retained pin enumeration.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-180; TR-MCP-FEDERATION-007; TEST-MCP-228. These fields are observations, never real-engine proof from a mock.</remarks>
internal sealed record Phase0MergeSnapshot(string State, string LiveHead, IReadOnlyDictionary<string, string> LiveRows,
    IReadOnlyDictionary<string, string> PublicRefs, IReadOnlyDictionary<string, string> CandidateRows, int CellConflicts,
    int ConstraintViolations, string[] DomainErrors, Phase0MergeReview? Review, Phase0MergePin[] LocalPins, Phase0MergePin[] RemotePins,
    string TrackingIncomingHead, string CandidateHead);

/// <summary>Stale-resolution response and independently measured merge-history effect.</summary>
/// <param name="Status">HTTP status.</param><param name="Error">Stable protocol error.</param><param name="NewMergeCommits">Actual commits created by the resolution attempt.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-StaleReviewRejected; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0MergeResolution(int Status, string Error, int NewMergeCommits);

/// <summary>Disposable merge boundary, later bound to a real integration harness without changing assertions.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006; FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview/ac-CleanMergeCanFailDomainValidation/ac-StaleReviewRejected/ac-SafeTodoMergeRules; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal interface IPhase0MergeValidationProbe
{
    /// <summary>Creates the independently specified C0/B1/A1 fixture for the named scenario.</summary><param name="scenario">Contract fixture name.</param>
    void Prepare(string scenario);
    /// <summary>Attempts integration in an isolated candidate; no public publication is requested.</summary>
    void Integrate();
    /// <summary>Reads actual live/public/candidate/review state independently.</summary><returns>Persisted observations.</returns>
    Phase0MergeSnapshot? Read();
    /// <summary>Closes and reopens the fixture's owning processes/stores before another independent read.</summary>
    void Restart();
    /// <summary>Advances the incoming tracking source to A2 while preserving the original review evidence.</summary>
    void AdvanceIncoming();
    /// <summary>Attempts resolution using the original preview source binding.</summary>
    /// <param name="reviewId">Original review UUID.</param><param name="localHead">B1.</param><param name="incomingHead">A1.</param>
    /// <returns>Response and measured history delta.</returns>
    Phase0MergeResolution? Resolve(Guid reviewId, string localHead, string incomingHead);
}

/// <summary>Approved merge outcomes with fixed input fixtures and discriminating observations.</summary>
/// <remarks>
/// 2026-11-17: FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview/ac-CleanMergeCanFailDomainValidation/ac-StaleReviewRejected/ac-SafeTodoMergeRules;
/// TR-MCP-FEDERATION-007; TEST-MCP-228. Source-pinned Dolt settings and private conflict persistence remain unexecuted feasibility questions.
/// </remarks>
internal static class Phase0MergeValidationContract
{
    /// <summary>Checks candidate/review state and all protected data before and after restart.</summary>
    /// <param name="probe">Scripted or later real disposable boundary.</param><param name="scenario">One specified merge fixture.</param>
    internal static void AssertIntegration(IPhase0MergeValidationProbe probe, string scenario)
    {
        probe.Prepare(scenario); probe.Integrate();
        var first = probe.Read(); Assert.NotNull(first); AssertSnapshot(first, scenario);
        probe.Restart(); var reopened = probe.Read(); Assert.NotNull(reopened); AssertSnapshot(reopened, scenario);
        Assert.Equal(first.Review?.Id, reopened.Review?.Id);
        Assert.Equal(first.Review?.CandidateRef, reopened.Review?.CandidateRef);
        Assert.Equal(first.CandidateHead, reopened.CandidateHead);
        Assert.Equal(first.LocalPins.OrderBy(pin => pin.Purpose), reopened.LocalPins.OrderBy(pin => pin.Purpose));
        Assert.Equal(first.RemotePins.OrderBy(pin => pin.Purpose), reopened.RemotePins.OrderBy(pin => pin.Purpose));
    }

    /// <summary>Checks old preview rejection after incoming moves, preserving original evidence and refs.</summary>
    /// <param name="probe">Scripted or later real disposable boundary.</param>
    internal static void AssertStaleReview(IPhase0MergeValidationProbe probe)
    {
        probe.Prepare("same_cell"); probe.Integrate(); var before = probe.Read(); Assert.NotNull(before);
        AssertSnapshot(before, "same_cell"); Assert.NotNull(before.Review);
        probe.AdvanceIncoming(); var result = probe.Resolve(before.Review.Id, "B1", "A1");
        Assert.NotNull(result); Assert.Equal(409, result.Status); Assert.Equal("stale_review", result.Error); Assert.Equal(0, result.NewMergeCommits);
        var after = probe.Read(); Assert.NotNull(after); AssertSnapshot(after, "same_cell", incoming: "A2");
        Assert.Equal(before.Review.Id, after.Review!.Id);
        Assert.Equal(before.LocalPins.OrderBy(pin => pin.Purpose), after.LocalPins.OrderBy(pin => pin.Purpose));
        Assert.Equal(before.RemotePins.OrderBy(pin => pin.Purpose), after.RemotePins.OrderBy(pin => pin.Purpose));
        probe.Restart(); var reopened = probe.Read(); Assert.NotNull(reopened); AssertSnapshot(reopened, "same_cell", incoming: "A2");
        Assert.Equal(before.Review.Id, reopened.Review!.Id);
        Assert.Equal(before.LocalPins.OrderBy(pin => pin.Purpose), reopened.LocalPins.OrderBy(pin => pin.Purpose));
        Assert.Equal(before.RemotePins.OrderBy(pin => pin.Purpose), reopened.RemotePins.OrderBy(pin => pin.Purpose));
    }

    /// <summary>Checks independently known fixture rows, review alternatives and actual retained source refs.</summary>
    /// <param name="snapshot">One independent read.</param><param name="scenario">Fixture expectation selector.</param>
    /// <param name="incoming">Current tracking hash, independent of the original review binding.</param>
    private static void AssertSnapshot(Phase0MergeSnapshot snapshot, string scenario, string incoming = "A1")
    {
        Assert.Equal(incoming, snapshot.TrackingIncomingHead);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.CandidateHead));
        Assert.Equal("B1", snapshot.LiveHead);
        AssertMap(LiveRows(scenario), snapshot.LiveRows);
        AssertMap(new Dictionary<string, string> { ["Atlas/main"] = "A1", ["Atlas/feature"] = "F1", ["Other/main"] = "X1" }, snapshot.PublicRefs);
        Assert.Equal(0, snapshot.ConstraintViolations);
        var valid = scenario == "different_cells"; var cycle = scenario == "dependency_cycle";
        Assert.Equal(valid ? "valid_candidate" : "needs_review", snapshot.State);
        Assert.Equal(valid || cycle ? 0 : 1, snapshot.CellConflicts);
        Assert.Equal(cycle ? new[] { "dependency_cycle" } : Array.Empty<string>(), snapshot.DomainErrors);
        if (valid || cycle)
        {
            var expectedCandidate = LiveRows(scenario);
            if (valid) expectedCandidate["T1.title"] = "incoming title";
            else expectedCandidate["T2.dependsOn"] = "T1";
            AssertMap(expectedCandidate, snapshot.CandidateRows);
        }
        if (valid) { Assert.Null(snapshot.Review); return; }
        var review = snapshot.Review; Assert.NotNull(review); Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal("Atlas", review.Project); Assert.Equal("main", review.Branch); Assert.Equal(new[] { "C0" }, review.BaseHeads);
        Assert.Equal("B1", review.LocalHead); Assert.Equal("A1", review.IncomingHead); Assert.False(string.IsNullOrWhiteSpace(review.CandidateRef));
        var expectedCells = scenario switch
        {
            "delete_update" => new[] { new Phase0MergeCell("T1", "title", "original", "edited", null, false, true) },
            "completion" => [new Phase0MergeCell("T1", "completion", "todo", "doing", "done", false, false)],
            "dependency_cycle" => [],
            _ => [new Phase0MergeCell("T1", "priority", "low", "medium", "high", false, false)]
        };
        Assert.Equal(expectedCells, review.Cells); Assert.Equal(snapshot.DomainErrors, review.Errors);
        AssertPins(snapshot.LocalPins, review, local: true); AssertPins(snapshot.RemotePins, review, local: false);
        Assert.Equal(snapshot.CandidateHead, snapshot.LocalPins.Single(pin => pin.Purpose == "candidate").Target);
        Assert.Equal(snapshot.CandidateHead, snapshot.RemotePins.Single(pin => pin.Purpose == "candidate").Target);
    }

    /// <summary>Checks complete local or remote pin roles, unique names, expected source hashes and readable objects.</summary>
    /// <param name="pins">Independently enumerated private refs.</param><param name="review">Durable review.</param><param name="local">Whether the candidate ref uses the local name.</param>
    private static void AssertPins(Phase0MergePin[] pins, Phase0MergeReview review, bool local)
    {
        Assert.Equal(new[] { "base", "candidate", "incoming", "local" }, pins.Select(pin => pin.Purpose).OrderBy(value => value));
        Assert.Equal(4, pins.Select(pin => pin.Name).Distinct(StringComparer.Ordinal).Count());
        foreach (var pin in pins)
        {
            Assert.False(string.IsNullOrWhiteSpace(pin.Name)); Assert.False(string.IsNullOrWhiteSpace(pin.Target)); Assert.True(pin.Readable);
            if (pin.Purpose == "candidate") { if (local) Assert.Equal(review.CandidateRef, pin.Name); }
            else Assert.Equal(pin.Purpose switch { "base" => "C0", "local" => "B1", _ => "A1" }, pin.Target);
        }
    }

    /// <summary>Gets fixed B1 rows; this is fixture specification, not implementation output.</summary>
    /// <param name="scenario">Specified scenario.</param><returns>Complete ordered-independent row projection.</returns>
    private static Dictionary<string, string> LiveRows(string scenario)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["T1.title"] = "original", ["T1.description"] = "original description", ["T1.priority"] = "low", ["T1.completion"] = "todo",
            ["T1.dependsOn"] = "", ["T2.title"] = "second", ["T2.dependsOn"] = ""
        };
        if (scenario is "same_cell" or "priority") result["T1.priority"] = "medium";
        if (scenario == "completion") result["T1.completion"] = "doing";
        if (scenario == "delete_update") result["T1.title"] = "edited";
        if (scenario == "dependency_cycle") result["T1.dependsOn"] = "T2";
        if (scenario == "different_cells") result["T1.description"] = "local description";
        return result;
    }

    /// <summary>Compares every row/field and rejects extra, missing or modified data.</summary>
    /// <param name="expected">Fixed fixture values.</param><param name="actual">Independent read.</param>
    private static void AssertMap(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual)
        => Assert.Equal(expected.OrderBy(pair => pair.Key), actual.OrderBy(pair => pair.Key));
}
