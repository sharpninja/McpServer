namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>An independently read operational record, excluded from the versioned project context.</summary>
/// <param name="Id">Fixture operation identity.</param>
/// <param name="Kind">Operational writer or retained proposal/import-plan kind.</param>
/// <param name="Payload">Persisted fixture data.</param>
/// <remarks>2026-11-17: FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TEST-MCP-232.</remarks>
internal sealed record Phase0OperationalRow(string Id, string Kind, string Payload);

/// <summary>Disposable operational-writer fixture boundary.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-184; TR-MCP-FEDERATION-011; TEST-MCP-232.</remarks>
internal interface IPhase0OperationalWriterProbe
{
    /// <summary>Establishes clean main C0 and empty operational fixture rows.</summary>
    void Prepare();
    /// <summary>Reads the actual main ref.</summary>
    /// <returns>Current main head.</returns>
    string? ReadHead();
    /// <summary>Reads the complete versioned working-set fingerprint, including uncommitted changes.</summary>
    /// <returns>Nonempty fingerprint computed by the fixture.</returns>
    string? ReadWorkingSetFingerprint();
    /// <summary>Runs one operational writer and waits for its durable write.</summary>
    /// <param name="id">Fixture record identity.</param>
    /// <param name="kind">Writer kind.</param>
    /// <param name="payload">Fixture data to persist.</param>
    void RunWriter(string id, string kind, string payload);
    /// <summary>Reads persisted operational rows independently of the writer's return value.</summary>
    /// <returns>All fixture operational rows.</returns>
    IReadOnlyList<Phase0OperationalRow>? ReadOperationalRows();
    /// <summary>Reads the editing lease independently; automatic work must not acquire one.</summary>
    /// <returns>Null when this initially unowned fixture remains unowned.</returns>
    /// <remarks>2026-11-17: FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; B2 indirect-writer closure.</remarks>
    string? ReadEditingLease();
}

/// <summary>A created or resolved TODO with stable provenance and content.</summary>
/// <param name="Id">Opaque canonical identity; this fixture does not select the pending per-family grammar.</param>
/// <param name="Origin">Immutable creator tag.</param>
/// <param name="Sequence">Durable creator sequence.</param>
/// <param name="Title">Persisted TODO value.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-TwoNodesCreateDistinctIds, ac-LegacyIdsRemainValid; TEST-MCP-230.</remarks>
internal sealed record Phase0IdentityRow(string Id, string Origin, long Sequence, string Title);

/// <summary>Two independent creators followed by reconciliation of their project rows.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-182; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal interface IPhase0IdentityProbe
{
    /// <summary>Prepares LA and LG with their next TODO sequence both equal to one.</summary>
    void Prepare();
    /// <summary>Creates a TODO on one independent node.</summary>
    /// <param name="origin">LA or LG.</param>
    /// <param name="title">Fixture title.</param>
    /// <returns>Created row, including actual generated identity.</returns>
    Phase0IdentityRow? CreateTodo(string origin, string title);
    /// <summary>Reconciles both node stores without discarding either creation.</summary>
    void Reconcile();
    /// <summary>Reads all resulting project TODO rows.</summary>
    /// <returns>Complete reconciled rows.</returns>
    IReadOnlyList<Phase0IdentityRow>? ReadTodos();
}

/// <summary>Scoped permanent alias input with its original legacy identity and expected entity.</summary>
/// <param name="Project">Project scope.</param>
/// <param name="Origin">Original node scope.</param>
/// <param name="Kind">Entity kind.</param>
/// <param name="LegacyId">Unmodified original reference.</param>
/// <param name="Target">Canonical entity to resolve.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal sealed record Phase0LegacyAlias(string Project, string Origin, string Kind, string LegacyId, Phase0IdentityRow Target);

/// <summary>Disposable legacy import and reopen boundary with byte-preserved historical text.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyIdsRemainValid; TEST-MCP-230.</remarks>
internal interface IPhase0LegacyAliasProbe
{
    /// <summary>Imports explicit aliases and historical text without rewriting the text.</summary>
    /// <param name="aliases">Scoped alias inputs.</param>
    /// <param name="history">Commit, PR, session, memory, document and old TODO reference fixtures.</param>
    void Import(IReadOnlyList<Phase0LegacyAlias> aliases, IReadOnlyDictionary<string, string> history);
    /// <summary>Resolves by the original scoped identity, never by a rewritten reference.</summary>
    /// <param name="project">Original project scope.</param>
    /// <param name="origin">Original node scope.</param>
    /// <param name="kind">Original entity kind.</param>
    /// <param name="legacyId">Original legacy reference.</param>
    /// <returns>Resolved canonical entity.</returns>
    Phase0IdentityRow? Resolve(string project, string origin, string kind, string legacyId);
    /// <summary>Reads every historical text fixture as persisted.</summary>
    /// <returns>All historical keys and exact text.</returns>
    IReadOnlyDictionary<string, string>? ReadHistory();
    /// <summary>Closes and reopens the fixture so aliases cannot be satisfied by transient import state.</summary>
    void Reopen();
}

/// <summary>Reusable requirements-derived scenarios for partial Phase0 identity and isolation design.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds, ac-LegacyIdsRemainValid;
/// FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
/// No per-family ID grammar, full entity/writer inventory, context topology or real persistence is accepted here.
/// </remarks>
internal static class Phase0IdentityIsolationContract
{
    /// <summary>Checks each operational write persists while the versioned working set stays untouched.</summary>
    /// <param name="probe">Scripted or later disposable real writer boundary.</param>
    internal static void AssertBackgroundIsolation(IPhase0OperationalWriterProbe probe)
    {
        probe.Prepare();
        Assert.Equal("C0", probe.ReadHead());
        var fingerprint = probe.ReadWorkingSetFingerprint();
        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<Phase0OperationalRow>>(probe.ReadOperationalRows()));
        Assert.Null(probe.ReadEditingLease());
        var expected = new List<Phase0OperationalRow>();
        // Source-traced indirect writers retain proposals/plans, never apply project content automatically.
        foreach (var kind in new[] { "heartbeat", "audit", "automatic-session", "triage-proposal", "handoff-result", "consolidation-proposal", "yaml-bootstrap-plan", "legacy-import-plan" })
        {
            var row = new Phase0OperationalRow("op-" + (expected.Count + 1), kind, "payload:" + kind);
            probe.RunWriter(row.Id, row.Kind, row.Payload);
            expected.Add(row);
            Assert.Equal("C0", probe.ReadHead());
            Assert.Equal(fingerprint, probe.ReadWorkingSetFingerprint());
            Assert.Null(probe.ReadEditingLease());
            var actual = Assert.IsAssignableFrom<IReadOnlyList<Phase0OperationalRow>>(probe.ReadOperationalRows());
            Assert.Equal(expected.OrderBy(item => item.Id), actual.OrderBy(item => item.Id));
        }
    }

    /// <summary>Checks two independent sequence-one creations retain distinct identities and complete data.</summary>
    /// <param name="probe">Scripted or later disposable real node boundary.</param>
    internal static void AssertDistinctCreators(IPhase0IdentityProbe probe)
    {
        probe.Prepare();
        var first = probe.CreateTodo("LA", "LAB task");
        var second = probe.CreateTodo("LG", "LEGION task");
        AssertCreated(first, "LA", "LAB task");
        AssertCreated(second, "LG", "LEGION task");
        Assert.NotEqual(first!.Id, second!.Id);
        probe.Reconcile();
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Phase0IdentityRow>>(probe.ReadTodos());
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { first, second }.OrderBy(row => row.Id), rows.OrderBy(row => row.Id));
    }

    /// <summary>Checks permanent scoped aliases and all historical text before and after reopen.</summary>
    /// <param name="probe">Scripted or later disposable real alias/import boundary.</param>
    internal static void AssertLegacyPreservation(IPhase0LegacyAliasProbe probe)
    {
        Phase0LegacyAlias[] aliases =
        [
            new("Atlas", "LA", "Todo", "IMPL-UNITGREEN-001", new("fixture-canonical-a", "LA", 1, "human task")),
            new("Atlas", "LG", "Todo", "TODO-7", new("fixture-canonical-b", "LG", 1, "legacy task"))
        ];
        var history = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kind in new[] { "commit", "pr", "session", "memory", "document", "todo-reference" })
            history[kind] = kind + ": IMPL-UNITGREEN-001 -> TODO-7\r\nUnicode: café 雪\n";
        // Give the adapter copies so mutation cannot change the expected fixture values.
        probe.Import(aliases.ToArray(), new Dictionary<string, string>(history, StringComparer.Ordinal));
        for (var checkpoint = 0; checkpoint < 2; checkpoint++)
        {
            foreach (var alias in aliases)
                Assert.Equal(alias.Target, probe.Resolve(alias.Project, alias.Origin, alias.Kind, alias.LegacyId));
            var actual = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(probe.ReadHistory());
            Assert.Equal(history.OrderBy(pair => pair.Key), actual.OrderBy(pair => pair.Key));
            if (checkpoint == 0) probe.Reopen();
        }
    }

    /// <summary>Checks creator identity and sequence while leaving the pending ID syntax unselected.</summary>
    /// <param name="row">Actual created TODO.</param>
    /// <param name="origin">Expected creator tag.</param>
    /// <param name="title">Expected persisted title.</param>
    private static void AssertCreated(Phase0IdentityRow? row, string origin, string title)
    {
        Assert.NotNull(row);
        Assert.False(string.IsNullOrWhiteSpace(row.Id));
        Assert.Equal(origin, row.Origin);
        Assert.Equal(1, row.Sequence);
        Assert.Equal(title, row.Title);
    }
}
