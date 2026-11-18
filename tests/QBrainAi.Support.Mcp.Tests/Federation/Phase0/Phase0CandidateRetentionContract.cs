namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Exact retained alternatives read independently from a displayed review.</summary>
/// <param name="Table">Affected table.</param><param name="Key">Affected row or schema key.</param><param name="Kind">Artifact classification, not an HTTP error code.</param>
/// <param name="Base">Base alternative.</param><param name="Local">Local alternative.</param><param name="Incoming">Incoming alternative.</param>
/// <param name="BaseDeleted">Base deletion marker.</param><param name="LocalDeleted">Local deletion marker.</param><param name="IncomingDeleted">Incoming deletion marker.</param>
/// <param name="Error">Retained validation detail.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007 B6/B12; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview; schema/constraint B6 supplement.</remarks>
internal sealed record Phase0RetainedArtifact(string Table, string Key, string Kind, string? Base, string? Local, string? Incoming, bool BaseDeleted, bool LocalDeleted, bool IncomingDeleted, string? Error);

/// <summary>Independently read immutable object contents; labels stand for real run-manifest hashes later.</summary>
/// <param name="Hash">Observed object identity.</param><param name="Schema">Complete fixture schema projection.</param><param name="Rows">Complete fixture row projection.</param><param name="Artifacts">Actual conflict/validation evidence.</param>
/// <remarks>2026-11-17: B6/B12; FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview; TEST-MCP-228.</remarks>
internal sealed record Phase0RetainedObject(string Hash, string? Schema, string? Rows, Phase0RetainedArtifact[]? Artifacts);

/// <summary>Durable review binding, read separately from physical refs and objects.</summary>
/// <param name="Id">Review UUID.</param><param name="Project">Project identity.</param><param name="Branch">Branch identity.</param>
/// <param name="Base">Base hash.</param><param name="Local">Local hash.</param><param name="Incoming">Incoming hash.</param><param name="Candidate">Candidate hash.</param><param name="State">Unresolved state.</param>
/// <remarks>2026-11-17: B6/B12; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228.</remarks>
internal sealed record Phase0RetentionReview(Guid Id, string Project, string Branch, string Base, string Local, string Incoming, string Candidate, string State);

/// <summary>Complete protected live-state projection and independently read mutation evidence.</summary>
/// <param name="Local">Live local head.</param><param name="Public">Published main head.</param><param name="Feature">Unrelated feature head.</param><param name="Other">Other-project head.</param>
/// <param name="Rows">Live row projection.</param><param name="Fingerprint">Full protected live/working-set/history fingerprint.</param><param name="Conflicts">Live conflict count.</param><param name="Publications">Public ref updates.</param>
/// <remarks>2026-11-17: B6/B12; FR-MCP-180 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview.</remarks>
internal sealed record Phase0RetentionSafety(string Local, string Public, string Feature, string Other, string Rows, string? Fingerprint, int Conflicts, int Publications);

/// <summary>Independent harness execution trace, not a success flag returned by the operation.</summary>
/// <param name="Transfers">Completed fresh-remote round trips.</param><param name="Reopens">Completed close/reopen boundaries.</param>
/// <param name="LocalGc">Completed local native GC commands.</param><param name="RemoteGc">Completed remote native GC commands.</param><param name="CleanupAttempts">Unresolved cleanup attempts.</param><param name="OwningSessions">Remaining candidate-owning sessions.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007 B6/B12; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228. Real adapters derive observations from retained command/process receipts, not counters incremented merely by entering a probe method.</remarks>
internal sealed record Phase0RetentionOperations(int Transfers, int Reopens, int LocalGc, int RemoteGc, int CleanupAttempts, int OwningSessions);

/// <summary>Test-only persistence/transfer/GC boundary; no engine or cleanup implementation lives here.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228; B6/B12 source-backed experiment.</remarks>
internal interface IPhase0CandidateRetentionProbe
{
    /// <summary>Creates and durably pins the selected unresolved review locally and remotely.</summary><param name="kind">cell, delete, schema or constraint fixture.</param>
    void Prepare(string kind);
    /// <summary>Reads the durable review without trusting its ref assertions.</summary><returns>Actual review record.</returns>
    Phase0RetentionReview? ReadReview();
    /// <summary>Lists the physical branches for this review.</summary><param name="side">local or remote.</param><returns>All selected review refs and targets.</returns>
    IReadOnlyDictionary<string, string>? ReadRefs(string side);
    /// <summary>Reads the actual object, independently of review display.</summary><param name="side">local or remote.</param><param name="hash">Run-manifest object hash.</param><returns>Object contents, or missing.</returns>
    Phase0RetainedObject? ReadObject(string side, string hash);
    /// <summary>Reads protected live data, refs, working sets and mutation counters independently.</summary><returns>Protected observation.</returns>
    Phase0RetentionSafety? ReadSafety();
    /// <summary>Reads all global, live and newly reopened connection override settings.</summary><returns>Nine named setting values.</returns>
    IReadOnlyDictionary<string, int>? ReadOverrides();
    /// <summary>Reads completed native operations and remaining owners from independent harness receipts.</summary><returns>Actual execution trace counts.</returns>
    Phase0RetentionOperations? ReadOperations();
    /// <summary>Transfers the retained objects and refs through a fresh private remote round trip.</summary>
    void TransferRoundTrip();
    /// <summary>Closes owning sessions and reopens persistence without reconstructing missing evidence from display data.</summary>
    void Reopen();
    /// <summary>Collects both repositories with no candidate-owning session left alive.</summary>
    void CollectBoth();
    /// <summary>Attempts retention cleanup while no authorized resolution receipt exists.</summary>
    void AttemptUnresolvedCleanup();
}

/// <summary>Reusable assertions for exact review evidence retention with unchanged live/public data.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007 B6/B12; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview; schema/constraint supplement. Mock success is not Dolt feasibility.</remarks>
internal static class Phase0CandidateRetentionContract
{
    /// <summary>Checks independently observed retention at every transfer, reopen, GC and unresolved-cleanup boundary.</summary>
    /// <param name="probe">Disposable adapter boundary.</param><param name="kind">Contract-derived fixture.</param>
    internal static void AssertRetention(IPhase0CandidateRetentionProbe probe, string kind)
    {
        probe.Prepare(kind);
        var initial = probe.ReadSafety();
        Assert.NotNull(initial); Assert.False(string.IsNullOrWhiteSpace(initial.Fingerprint));
        Assert.Equal(new Phase0RetentionSafety("B1", "A1", "F1", "X0", kind == "constraint" ? "T1:S;T2:D" : "T1:edited:medium", initial.Fingerprint, 0, 0), initial);
        var operationIndex = 0;
        Phase0RetentionOperations[] expectedOperations = [new(0, 0, 0, 0, 0, 0), new(1, 0, 0, 0, 0, 0), new(1, 1, 0, 0, 0, 0), new(1, 1, 1, 1, 0, 0), new(1, 2, 1, 1, 0, 0), new(1, 2, 1, 1, 1, 0), new(1, 3, 1, 1, 1, 0)];
        Checkpoint();
        probe.TransferRoundTrip(); Checkpoint();
        probe.Reopen(); Checkpoint();
        probe.CollectBoth(); Checkpoint();
        probe.Reopen(); Checkpoint();
        probe.AttemptUnresolvedCleanup(); Checkpoint();
        probe.Reopen(); Checkpoint();

        void Checkpoint()
        {
            var id = Guid.Parse("35ce4ee2-297b-47e1-9df4-84555d8bc118");
            Assert.Equal(new Phase0RetentionReview(id, "Atlas", "main", "C0", "B1", "A1", "K1", "needs_review"), probe.ReadReview());
            foreach (var side in new[] { "local", "remote" })
            {
                var refs = probe.ReadRefs(side);
                Assert.NotNull(refs); Assert.Equal(4, refs.Count);
                foreach (var pair in new[] { ("base", "C0"), ("local", "B1"), ("incoming", "A1"), ("candidate", "K1") })
                {
                    var name = "refs/heads/review/" + id + "/" + pair.Item1;
                    Assert.True(refs.TryGetValue(name, out var target)); Assert.Equal(pair.Item2, target);
                    var observed = probe.ReadObject(side, pair.Item2);
                    var expected = Expected(kind, pair.Item2);
                    Assert.NotNull(observed); Assert.Equal(expected.Hash, observed.Hash); Assert.Equal(expected.Schema, observed.Schema); Assert.Equal(expected.Rows, observed.Rows);
                    Assert.NotNull(observed.Artifacts); Assert.Equal<Phase0RetainedArtifact>(expected.Artifacts!, observed.Artifacts);
                }
            }
            Assert.Equal(expectedOperations[operationIndex++], probe.ReadOperations());
            Assert.Equal(initial, probe.ReadSafety());
            var settings = probe.ReadOverrides();
            Assert.NotNull(settings); Assert.Equal(9, settings.Count);
            foreach (var scope in new[] { "global", "live", "fresh" })
                foreach (var flag in new[] { "automatic", "allowConflicts", "forceTransaction" })
                { Assert.True(settings.TryGetValue(scope + "." + flag, out var value)); Assert.Equal(0, value); }
        }
    }

    /// <summary>Literal requirement/experiment-derived oracle, never supplied by the implementation under test.</summary>
    /// <param name="kind">Fixture classification.</param><param name="hash">Immutable fixture object label.</param><returns>Expected full projected object.</returns>
    private static Phase0RetainedObject Expected(string kind, string hash)
    {
        var schema = kind == "constraint" ? "code:VARCHAR(8):UNIQUE" : kind == "schema" && hash == "A1" ? "title:INT" : kind == "schema" && hash is "B1" or "K1" ? "title:VARCHAR(128)" : "title:VARCHAR(64)";
        var rows = kind == "constraint" ? hash switch { "C0" => "T1:S", "B1" => "T1:S;T2:D", "A1" => "T1:S;T3:D", _ => "T1:S;T2:D;T3:D" }
            : hash switch { "C0" => "T1:original:low", "B1" or "K1" => "T1:edited:medium", _ => kind == "delete" ? "" : kind == "schema" ? "T1:17:low" : "T1:original:high" };
        Phase0RetainedArtifact[] artifacts = hash != "K1" ? [] : [kind switch
        {
            "cell" => new("Todo", "T1.priority", "cell", "low", "medium", "high", false, false, false, null),
            "delete" => new("Todo", "T1.title", "delete_update", "original", "edited", null, false, false, true, null),
            "schema" => new("Todo", "title.type", "schema", "VARCHAR(64)", "VARCHAR(128)", "INT", false, false, false, null),
            "constraint" => new("Todo", "UX_Code:D", "unique", null, "T2:D", "T3:D", false, false, false, "unique index UX_Code duplicated D"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        }];
        return new(hash, schema, rows, artifacts);
    }
}
