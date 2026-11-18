namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Origin-scoped legacy reference, independent of the pending generated-ID grammar.</summary>
/// <param name="Project">Immutable project scope.</param>
/// <param name="Origin">Original node identity.</param>
/// <param name="Kind">Entity family.</param>
/// <param name="LegacyId">Exact original string identity.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-LegacyCollisionPreservesReferences; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal sealed record Phase0ReconciliationKey(string Project, string Origin, string Kind, string LegacyId);

/// <summary>Independently read canonical row after reviewed import.</summary>
/// <param name="Id">Canonical target identity selected by review, not guessed by the adapter.</param>
/// <param name="Title">Unmodified authored value.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-LegacyCollisionPreservesReferences; TEST-MCP-230.</remarks>
internal sealed record Phase0ReconciliationRow(string Id, string Title);

/// <summary>One persisted relationship, including optional null references.</summary>
/// <param name="Id">Stable fixture edge identity.</param>
/// <param name="Source">Canonical child or parent identity.</param>
/// <param name="Field">Declared reference property.</param>
/// <param name="Target">Canonical reference or a preserved optional null.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-LegacyCollisionPreservesReferences; TEST-MCP-230.</remarks>
internal sealed record Phase0ReconciliationEdge(string Id, string Source, string Field, string? Target);

/// <summary>Safe observations of all imported rows, aliases, references and historical bytes.</summary>
/// <param name="Rows">Complete fixture rows.</param>
/// <param name="Aliases">Complete permanent scoped aliases.</param>
/// <param name="Edges">Complete declared relationship manifest.</param>
/// <param name="History">Historical text with its original line endings and Unicode.</param>
/// <param name="SourceFingerprint">Independent immutable-source read fingerprint.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-LegacyCollisionPreservesReferences; TEST-MCP-230.</remarks>
internal sealed record Phase0ReconciliationState(
    IReadOnlyList<Phase0ReconciliationRow>? Rows,
    IReadOnlyDictionary<Phase0ReconciliationKey, string>? Aliases,
    IReadOnlyList<Phase0ReconciliationEdge>? Edges,
    IReadOnlyDictionary<string, string>? History,
    string? SourceFingerprint);

/// <summary>Duplicate evidence returned by the planner, never authority to merge records.</summary>
/// <param name="First">First original scoped identity.</param>
/// <param name="Second">Second original scoped identity.</param>
/// <param name="Score">Finite diagnostic similarity score; no threshold or score scale is selected here.</param>
/// <param name="Title">Normalized title evidence.</param>
/// <param name="Description">Normalized description evidence.</param>
/// <param name="Dependencies">Normalized dependency-set evidence.</param>
/// <remarks>2026-11-17: FR-MCP-182 ac-DifferentIdsSameTodoDetected; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal sealed record Phase0DuplicateEvidence(Phase0ReconciliationKey First, Phase0ReconciliationKey Second,
    double Score, string? Title, string? Description, IReadOnlyList<string>? Dependencies);

/// <summary>Disposable reviewed-import and duplicate-planning seam.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyCollisionPreservesReferences,
/// ac-DifferentIdsSameTodoDetected; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// The future real adapter seeds the documented source graph and independently reads all target relationships.
/// </remarks>
internal interface IPhase0IdentityReconciliationProbe
{
    /// <summary>Seeds the two origin stores and an empty import target for the documented collision fixture.</summary>
    void PrepareCollision();
    /// <summary>Applies only the supplied reviewed mapping to the complete documented source graph.</summary>
    /// <param name="mapping">Explicit origin-scoped canonical targets, including children.</param>
    void ApplyReviewedMapping(IReadOnlyDictionary<Phase0ReconciliationKey, string> mapping);
    /// <summary>Reads complete persisted target state plus the independently read source fingerprint.</summary>
    /// <returns>All fixture observations; a missing observation fails the contract.</returns>
    Phase0ReconciliationState? ReadState();
    /// <summary>Closes and reopens target storage so transient mapping state cannot satisfy the contract.</summary>
    void Reopen();
    /// <summary>Resolves the supplied original scope after import.</summary>
    /// <param name="key">Complete original identity.</param>
    /// <returns>Canonical row, or null for a missing alias.</returns>
    Phase0ReconciliationRow? Resolve(Phase0ReconciliationKey key);
    /// <summary>Looks up TODO-7 without origin context in Atlas.</summary>
    /// <returns>Native status and stable error code.</returns>
    (int Status, string? Code) ResolveAmbiguous();
    /// <summary>Seeds LAB TODO-7 and LEGION TODO-91 with matching normalized title, description and dependencies.</summary>
    void PrepareDuplicates();
    /// <summary>Plans duplicates without approval and without changing either row or its aliases.</summary>
    /// <returns>All candidate pairs and their diagnostic evidence.</returns>
    IReadOnlyList<Phase0DuplicateEvidence>? PlanDuplicates();
}

/// <summary>Requirements-derived assertions for reviewed identity graphs and nonmutating duplicate review.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyCollisionPreservesReferences,
/// ac-DifferentIdsSameTodoDetected; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// The small authored fixture is not the full B2 model manifest or evidence of real migration behavior.
/// </remarks>
internal static class Phase0IdentityReconciliationContract
{
    /// <summary>Checks every supplied relationship against the explicit reviewed mapping and preserves source/history.</summary>
    /// <param name="probe">Scripted or future disposable real import adapter.</param>
    internal static void AssertReviewedCollision(IPhase0IdentityReconciliationProbe probe)
    {
        var mapping = new Dictionary<Phase0ReconciliationKey, string>();
        var rows = new List<Phase0ReconciliationRow>();
        var edges = new List<Phase0ReconciliationEdge>();
        foreach (var origin in new[] { "LA", "LG" })
        {
            var todo = "reviewed-" + origin + "-todo";
            mapping.Add(new("Atlas", origin, "Todo", "TODO-7"), todo);
            rows.Add(new(todo, origin + " distinct TODO"));
            foreach (var child in new[] { "task", "list" })
            {
                var id = "reviewed-" + origin + "-" + child;
                mapping.Add(new("Atlas", origin, child, "7"), id);
                // Preserve non-reference content independently: Task text or the exact ListType discriminator.
                rows.Add(new(id, child == "list" ? origin == "LA" ? "DependsOn" : "Description" : origin + " task"));
                edges.Add(new(origin + ":" + child, id, "TodoId", todo));
            }
            // McpDbContext task/list composite foreign keys map Project scope plus TodoId.
            // No document-note/archive relationship is invented as a TodoId child.
        }
        // A cross-origin dependency must follow the reviewed target, not the equal legacy label.
        // TodoItemListItemEntity.Value holds the dependency when ListType is depends-on.
        edges.Add(new("cross-origin", "reviewed-LA-list", "Value", "reviewed-LG-todo"));
        var history = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kind in new[] { "commit", "pr", "session", "memory", "document", "todo-reference" })
            history.Add(kind, kind + ": TODO-7 -> TODO-7\r\nUnicode: café 雪\n");

        probe.PrepareCollision();
        var baseline = probe.ReadState();
        Assert.NotNull(baseline);
        var sourceFingerprint = baseline.SourceFingerprint;
        Assert.False(string.IsNullOrWhiteSpace(sourceFingerprint));
        AssertSnapshot(baseline, [], new Dictionary<Phase0ReconciliationKey, string>(), [],
            new Dictionary<string, string>(), sourceFingerprint);
        probe.ApplyReviewedMapping(new Dictionary<Phase0ReconciliationKey, string>(mapping));
        for (var checkpoint = 0; checkpoint < 2; checkpoint++)
        {
            AssertSnapshot(probe.ReadState(), rows, mapping, edges, history, sourceFingerprint);
            foreach (var pair in mapping)
                Assert.Equal(rows.Single(row => row.Id == pair.Value), probe.Resolve(pair.Key));
            var ambiguous = probe.ResolveAmbiguous();
            Assert.Equal(409, ambiguous.Status);
            Assert.Equal("ambiguous_legacy_id", ambiguous.Code);
            if (checkpoint == 0) probe.Reopen();
        }
        AssertSnapshot(probe.ReadState(), rows, mapping, edges, history, sourceFingerprint);
    }

    /// <summary>Checks both original IDs and feature evidence survive planning and reopening without automatic merge.</summary>
    /// <param name="probe">Scripted or future disposable real planner adapter.</param>
    internal static void AssertDuplicatePlanning(IPhase0IdentityReconciliationProbe probe)
    {
        var rows = new[] { new Phase0ReconciliationRow("TODO-7", "Fix login"), new Phase0ReconciliationRow("TODO-91", "Fix login") };
        probe.PrepareDuplicates();
        var baseline = probe.ReadState();
        Assert.NotNull(baseline);
        var sourceFingerprint = baseline.SourceFingerprint;
        Assert.False(string.IsNullOrWhiteSpace(sourceFingerprint));
        AssertDuplicateState(baseline, rows, sourceFingerprint);
        var candidates = Assert.IsAssignableFrom<IReadOnlyList<Phase0DuplicateEvidence>>(probe.PlanDuplicates());
        var candidate = Assert.Single(candidates);
        Phase0ReconciliationKey[] identities = [new("Atlas", "LA", "Todo", "TODO-7"), new("Atlas", "LG", "Todo", "TODO-91")];
        Assert.Contains(candidate.First, identities);
        Assert.Contains(candidate.Second, identities);
        Assert.NotEqual(candidate.First, candidate.Second);
        Assert.True(double.IsFinite(candidate.Score));
        Assert.Equal("Fix login", candidate.Title);
        Assert.Equal("Repair authentication. café 雪", candidate.Description);
        var dependencies = Assert.IsAssignableFrom<IReadOnlyList<string>>(candidate.Dependencies);
        Assert.Equal(new[] { "DEP-A", "DEP-B" }, dependencies.OrderBy(item => item, StringComparer.Ordinal));
        AssertDuplicateState(probe.ReadState(), rows, sourceFingerprint);
        probe.Reopen();
        AssertDuplicateState(probe.ReadState(), rows, sourceFingerprint);
    }

    /// <summary>Checks the planner did not merge/delete/rename rows, create aliases or change source and historical data.</summary>
    /// <param name="state">Actual read.</param>
    /// <param name="rows">Independent expected rows.</param>
    /// <param name="sourceFingerprint">Nonempty baseline fingerprint, compared across planning and reopen.</param>
    private static void AssertDuplicateState(Phase0ReconciliationState? state, IReadOnlyList<Phase0ReconciliationRow> rows, string sourceFingerprint) =>
        AssertSnapshot(state, rows, new Dictionary<Phase0ReconciliationKey, string>(), [],
            new Dictionary<string, string> { ["source"] = "TODO-7 / TODO-91\r\n" }, sourceFingerprint);

    /// <summary>Compares full sets, preventing missing/extra/rekeyed observations from hiding behind success flags.</summary>
    /// <param name="actual">Independently read complete snapshot.</param>
    /// <param name="rows">Expected canonical rows.</param>
    /// <param name="aliases">Expected exact scoped alias set.</param>
    /// <param name="edges">Expected complete fixture relationships.</param>
    /// <param name="history">Expected byte-preserved historical text.</param>
    /// <param name="sourceFingerprint">Expected immutable-source fingerprint.</param>
    private static void AssertSnapshot(Phase0ReconciliationState? actual, IReadOnlyList<Phase0ReconciliationRow> rows,
        IReadOnlyDictionary<Phase0ReconciliationKey, string> aliases, IReadOnlyList<Phase0ReconciliationEdge> edges,
        IReadOnlyDictionary<string, string> history, string sourceFingerprint)
    {
        Assert.NotNull(actual);
        var actualRows = Assert.IsAssignableFrom<IReadOnlyList<Phase0ReconciliationRow>>(actual.Rows);
        Assert.Equal(rows.OrderBy(row => row.Id, StringComparer.Ordinal), actualRows.OrderBy(row => row.Id, StringComparer.Ordinal));
        var actualAliases = Assert.IsAssignableFrom<IReadOnlyDictionary<Phase0ReconciliationKey, string>>(actual.Aliases);
        Assert.Equal(aliases.Count, actualAliases.Count);
        foreach (var pair in aliases)
        {
            Assert.True(actualAliases.TryGetValue(pair.Key, out var target));
            Assert.Equal(pair.Value, target);
        }
        var actualEdges = Assert.IsAssignableFrom<IReadOnlyList<Phase0ReconciliationEdge>>(actual.Edges);
        Assert.Equal(edges.OrderBy(edge => edge.Id, StringComparer.Ordinal), actualEdges.OrderBy(edge => edge.Id, StringComparer.Ordinal));
        var actualHistory = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(actual.History);
        Assert.Equal(history.OrderBy(pair => pair.Key, StringComparer.Ordinal), actualHistory.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.Equal(sourceFingerprint, actual.SourceFingerprint);
    }
}
