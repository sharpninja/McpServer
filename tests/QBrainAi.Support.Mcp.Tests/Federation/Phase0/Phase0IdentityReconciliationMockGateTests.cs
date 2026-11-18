namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent correct/corrupt scripts prove reviewed graph and duplicate assertions discriminate failures.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyCollisionPreservesReferences,
/// ac-DifferentIdsSameTodoDetected; TR-MCP-FEDERATION-009; TEST-MCP-230. No production importer is used.</remarks>
public sealed class Phase0IdentityReconciliationMockGateTests
{
    /// <summary>Both source orderings preserve every reviewed relationship, alias, source and historical value.</summary>
    /// <param name="reverse">Permutes observations without changing their meaning.</param>
    [Theory, InlineData(false), InlineData(true)]
    public void CorrectReviewedCollisionPasses(bool reverse)
    {
        var probe = new Probe(reverse: reverse);
        Phase0IdentityReconciliationContract.AssertReviewedCollision(probe);
        Assert.Equal(new[] { "collision", "state", "apply", "state", "resolve", "ambiguous", "reopen", "state", "resolve", "ambiguous", "state" }, probe.Calls);
        Assert.Equal(12, probe.Resolutions);
    }

    /// <summary>Finite diagnostic scores and dependency order do not create an invented automatic-merge threshold.</summary>
    /// <param name="score">Independent valid diagnostic score.</param>
    [Theory, InlineData(0.73), InlineData(1.0)]
    public void CorrectDuplicatePlanningPasses(double score)
    {
        var probe = new Probe(score: score);
        Phase0IdentityReconciliationContract.AssertDuplicatePlanning(probe);
        Assert.Equal(new[] { "duplicates", "state", "plan", "state", "reopen", "state" }, probe.Calls);
    }

    /// <summary>Gets one corrupt persisted observation at every independently read checkpoint.</summary>
    public static IEnumerable<object[]> StateFaults
    {
        get
        {
            foreach (var scenario in new[] { "C", "D" })
            for (var checkpoint = 0; checkpoint < (scenario == "C" ? 4 : 3); checkpoint++)
            {
                foreach (var facet in new[] { "Missing", "RowsNull", "RowsExtra", "AliasesNull", "AliasesExtra", "EdgesNull", "EdgesExtra", "HistoryNull", "HistoryExtra", "FingerprintNull", "FingerprintEmpty" })
                    yield return [scenario, $"{scenario}:{checkpoint}:{facet}:0"];
                if (checkpoint > 0) yield return [scenario, $"{scenario}:{checkpoint}:Fingerprint:0"];
                var rowCount = scenario == "D" ? 2 : checkpoint == 0 ? 0 : 6;
                for (var index = 0; index < rowCount; index++)
                    foreach (var facet in new[] { "RowMissing", "RowId", "RowTitle" }) yield return [scenario, $"{scenario}:{checkpoint}:{facet}:{index}"];
                if (scenario == "C" && checkpoint > 0)
                {
                    for (var index = 0; index < 6; index++)
                        foreach (var facet in new[] { "AliasMissing", "AliasProject", "AliasOrigin", "AliasKind", "AliasLegacy", "AliasTarget" }) yield return [scenario, $"C:{checkpoint}:{facet}:{index}"];
                    for (var index = 0; index < 5; index++)
                        foreach (var facet in new[] { "EdgeMissing", "EdgeId", "EdgeSource", "EdgeField", "EdgeTarget" }) yield return [scenario, $"C:{checkpoint}:{facet}:{index}"];
                }
                var historyCount = scenario == "D" ? 1 : checkpoint == 0 ? 0 : 6;
                for (var index = 0; index < historyCount; index++)
                    foreach (var facet in new[] { "HistoryMissing", "HistoryKey", "HistoryValue" }) yield return [scenario, $"{scenario}:{checkpoint}:{facet}:{index}"];
            }
        }
    }

    /// <summary>Gets each qualified alias response and ambiguous lookup corruption before and after reopen.</summary>
    public static IEnumerable<object[]> ResolutionFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 2; checkpoint++)
            {
                for (var index = 0; index < 6; index++)
                    foreach (var facet in new[] { "Missing", "Id", "Title" }) yield return [$"Resolve:{checkpoint}:{facet}:{index}"];
                foreach (var facet in new[] { "Status", "Code" }) yield return [$"Ambiguous:{checkpoint}:{facet}:0"];
            }
        }
    }

    /// <summary>Gets missing/extra candidates, mis-scoped identities, invalid scores and missing/wrong evidence.</summary>
    public static IEnumerable<object[]> CandidateFaults
    {
        get
        {
            foreach (var facet in new[] { "Null", "Empty", "Extra", "NaN", "Infinity", "TitleNull", "Title", "DescriptionNull", "Description", "DependenciesNull", "DependenciesEmpty", "DependenciesExtra", "DependenciesWrong", "SameIdentity" }) yield return [facet];
            foreach (var side in new[] { "First", "Second" })
                foreach (var facet in new[] { "Project", "Origin", "Kind", "Legacy" }) yield return [side + facet];
        }
    }

    /// <summary>Every bad snapshot must reach a real assertion rejection, never a generic script exception.</summary>
    /// <param name="scenario">Collision or duplicate scenario.</param><param name="fault">Single observation defect.</param>
    [Theory, MemberData(nameof(StateFaults))]
    public void WrongStateIsRejected(string scenario, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
        { if (scenario == "C") Phase0IdentityReconciliationContract.AssertReviewedCollision(new Probe(fault));
          else Phase0IdentityReconciliationContract.AssertDuplicatePlanning(new Probe(fault)); });

    /// <summary>Qualified and ambiguous lookups must not silently resolve the other origin.</summary>
    /// <param name="fault">Single lookup defect.</param>
    [Theory, MemberData(nameof(ResolutionFaults))]
    public void WrongResolutionIsRejected(string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
        Phase0IdentityReconciliationContract.AssertReviewedCollision(new Probe(fault)));

    /// <summary>Candidate shape and evidence are checked independently of whether the planner returns success.</summary>
    /// <param name="fault">Single candidate defect.</param>
    [Theory, MemberData(nameof(CandidateFaults))]
    public void WrongCandidateIsRejected(string fault) => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
        Phase0IdentityReconciliationContract.AssertDuplicatePlanning(new Probe("Candidate:" + fault)));

    /// <summary>Independent fixture script with no reference to contract expected-state helpers.</summary>
    private sealed class Probe(string fault = "", bool reverse = false, double score = 0.73) : IPhase0IdentityReconciliationProbe
    {
        private bool imported;
        private bool duplicates;
        private int stateReads;
        private int ambiguousReads;
        /// <summary>Gets compact operation ordering; individual resolutions are counted separately.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Gets qualified read count across reopening.</summary>
        internal int Resolutions { get; private set; }
        /// <inheritdoc />
        public void PrepareCollision() { Calls.Add("collision"); }
        /// <inheritdoc />
        public void PrepareDuplicates() { Calls.Add("duplicates"); duplicates = true; }
        /// <inheritdoc />
        public void ApplyReviewedMapping(IReadOnlyDictionary<Phase0ReconciliationKey, string> mapping)
        {
            Calls.Add("apply");
            var expected = ScriptAliases();
            Assert.Equal(expected.Count, mapping.Count);
            foreach (var pair in expected) Assert.Equal(pair.Value, mapping[pair.Key]);
            imported = true;
        }
        /// <inheritdoc />
        public void Reopen() => Calls.Add("reopen");
        /// <inheritdoc />
        public Phase0ReconciliationRow? Resolve(Phase0ReconciliationKey key)
        {
            var index = Resolutions++;
            if (index % 6 == 0) Calls.Add("resolve");
            var row = ScriptRows().Single(item => item.Id == ScriptAliases()[key]);
            return fault == $"Resolve:{index / 6}:Missing:{index % 6}" ? null
                : fault == $"Resolve:{index / 6}:Id:{index % 6}" ? row with { Id = "wrong" }
                : fault == $"Resolve:{index / 6}:Title:{index % 6}" ? row with { Title = "wrong" } : row;
        }
        /// <inheritdoc />
        public (int Status, string? Code) ResolveAmbiguous()
        {
            Calls.Add("ambiguous"); var checkpoint = ambiguousReads++;
            return (fault == $"Ambiguous:{checkpoint}:Status:0" ? 200 : 409,
                fault == $"Ambiguous:{checkpoint}:Code:0" ? "not_found" : "ambiguous_legacy_id");
        }
        /// <inheritdoc />
        public Phase0ReconciliationState? ReadState()
        {
            Calls.Add("state"); var checkpoint = stateReads++;
            var rows = duplicates ? new List<Phase0ReconciliationRow> { new("TODO-7", "Fix login"), new("TODO-91", "Fix login") } : imported ? ScriptRows() : [];
            var aliases = imported ? ScriptAliases() : new Dictionary<Phase0ReconciliationKey, string>();
            var edges = imported ? ScriptEdges() : [];
            var history = duplicates ? new Dictionary<string, string> { ["source"] = "TODO-7 / TODO-91\r\n" } : imported ? ScriptHistory() : new Dictionary<string, string>();
            var fingerprint = duplicates ? "duplicate-source-v1" : "collision-source-v1";
            if (reverse) { rows.Reverse(); edges.Reverse(); }
            var prefix = (duplicates ? "D:" : "C:") + checkpoint + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return new(rows, aliases, edges, history, fingerprint);
            var parts = fault.Split(':'); var facet = parts[2]; var index = int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
            if (facet == "Missing") return null;
            if (facet == "RowsExtra") rows.Add(new("unexpected", "unexpected"));
            if (facet == "RowMissing") rows.RemoveAt(index);
            if (facet == "RowId") rows[index] = rows[index] with { Id = "wrong" };
            if (facet == "RowTitle") rows[index] = rows[index] with { Title = "wrong" };
            if (facet == "AliasesExtra") aliases.Add(new("Other", "OTHER", "Todo", "foreign"), "unexpected");
            if (facet.StartsWith("Alias", StringComparison.Ordinal) && facet is not "AliasesNull" and not "AliasesExtra")
            {
                var pair = aliases.ElementAt(index); aliases.Remove(pair.Key);
                if (facet != "AliasMissing") aliases.Add(pair.Key with {
                    Project = facet == "AliasProject" ? "wrong" : pair.Key.Project,
                    Origin = facet == "AliasOrigin" ? "wrong" : pair.Key.Origin,
                    Kind = facet == "AliasKind" ? "wrong" : pair.Key.Kind,
                    LegacyId = facet == "AliasLegacy" ? "wrong" : pair.Key.LegacyId }, facet == "AliasTarget" ? "wrong" : pair.Value);
            }
            if (facet == "EdgesExtra") edges.Add(new("unexpected", "unexpected", "unexpected", "unexpected"));
            if (facet == "EdgeMissing") edges.RemoveAt(index);
            if (facet is "EdgeId" or "EdgeSource" or "EdgeField" or "EdgeTarget") edges[index] = edges[index] with {
                Id = facet == "EdgeId" ? "wrong" : edges[index].Id, Source = facet == "EdgeSource" ? "wrong" : edges[index].Source,
                Field = facet == "EdgeField" ? "wrong" : edges[index].Field, Target = facet == "EdgeTarget" ? "wrong" : edges[index].Target };
            if (facet == "HistoryExtra") history.Add("unexpected", "unexpected");
            if (facet is "HistoryMissing" or "HistoryKey" or "HistoryValue")
            {
                var pair = history.ElementAt(index); history.Remove(pair.Key);
                if (facet != "HistoryMissing") history.Add(facet == "HistoryKey" ? "wrong" : pair.Key, facet == "HistoryValue" ? pair.Value.Replace("\r\n", "\n", StringComparison.Ordinal) : pair.Value);
            }
            return new(facet == "RowsNull" ? null : rows, facet == "AliasesNull" ? null : aliases,
                facet == "EdgesNull" ? null : edges, facet == "HistoryNull" ? null : history,
                facet == "FingerprintNull" ? null : facet == "FingerprintEmpty" ? " " : facet == "Fingerprint" ? "changed" : fingerprint);
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0DuplicateEvidence>? PlanDuplicates()
        {
            Calls.Add("plan"); var facet = fault.StartsWith("Candidate:", StringComparison.Ordinal) ? fault[10..] : "";
            var first = new Phase0ReconciliationKey("Atlas", "LA", "Todo", "TODO-7");
            var second = new Phase0ReconciliationKey("Atlas", "LG", "Todo", "TODO-91");
            Phase0ReconciliationKey Mutate(Phase0ReconciliationKey key, string side) => key with {
                Project = facet == side + "Project" ? "wrong" : key.Project, Origin = facet == side + "Origin" ? "wrong" : key.Origin,
                Kind = facet == side + "Kind" ? "wrong" : key.Kind, LegacyId = facet == side + "Legacy" ? "wrong" : key.LegacyId };
            IReadOnlyList<string>? dependencies = facet switch { "DependenciesNull" => null, "DependenciesEmpty" => [],
                "DependenciesExtra" => ["DEP-A", "DEP-B", "DEP-C"], "DependenciesWrong" => ["DEP-A", "DEP-C"], _ => ["DEP-B", "DEP-A"] };
            // Candidate ordering is not an AC: the positive score1 case reverses it.
            var left = Mutate(first, "First"); var right = Mutate(second, "Second");
            if (score == 1.0) (left, right) = (right, left);
            if (facet == "SameIdentity") right = left;
            var candidate = new Phase0DuplicateEvidence(left, right,
                facet == "NaN" ? double.NaN : facet == "Infinity" ? double.PositiveInfinity : score,
                facet == "TitleNull" ? null : facet == "Title" ? "wrong" : "Fix login",
                facet == "DescriptionNull" ? null : facet == "Description" ? "wrong" : "Repair authentication. café 雪", dependencies);
            return facet switch { "Null" => null, "Empty" => [], "Extra" => [candidate, candidate], _ => [candidate] };
        }
        /// <summary>Independent rows for source-verified TodoItem, TodoItemTask and TodoItemListItem entities.</summary>
        private static List<Phase0ReconciliationRow> ScriptRows() => [
            new("reviewed-LA-todo", "LA distinct TODO"), new("reviewed-LA-task", "LA task"), new("reviewed-LA-list", "DependsOn"),
            new("reviewed-LG-todo", "LG distinct TODO"), new("reviewed-LG-task", "LG task"), new("reviewed-LG-list", "Description") ];
        /// <summary>Independent scoped aliases for the two TODO identities and their numeric-key task/list children.</summary>
        private static Dictionary<Phase0ReconciliationKey, string> ScriptAliases() => new() {
            [new("Atlas", "LA", "Todo", "TODO-7")] = "reviewed-LA-todo", [new("Atlas", "LA", "task", "7")] = "reviewed-LA-task",
            [new("Atlas", "LA", "list", "7")] = "reviewed-LA-list", [new("Atlas", "LG", "Todo", "TODO-7")] = "reviewed-LG-todo",
            [new("Atlas", "LG", "task", "7")] = "reviewed-LG-task", [new("Atlas", "LG", "list", "7")] = "reviewed-LG-list" };
        /// <summary>Actual task/list TodoId foreign keys plus the depends-on list Value reference.</summary>
        private static List<Phase0ReconciliationEdge> ScriptEdges() => [
            new("LA:task", "reviewed-LA-task", "TodoId", "reviewed-LA-todo"), new("LA:list", "reviewed-LA-list", "TodoId", "reviewed-LA-todo"),
            new("LG:task", "reviewed-LG-task", "TodoId", "reviewed-LG-todo"), new("LG:list", "reviewed-LG-list", "TodoId", "reviewed-LG-todo"),
            new("cross-origin", "reviewed-LA-list", "Value", "reviewed-LG-todo") ];
        /// <summary>Independent historical byte fixtures, never generated by the contract.</summary>
        private static Dictionary<string, string> ScriptHistory() => new(StringComparer.Ordinal) {
            ["commit"] = "commit: TODO-7 -> TODO-7\r\nUnicode: café 雪\n", ["pr"] = "pr: TODO-7 -> TODO-7\r\nUnicode: café 雪\n",
            ["session"] = "session: TODO-7 -> TODO-7\r\nUnicode: café 雪\n", ["memory"] = "memory: TODO-7 -> TODO-7\r\nUnicode: café 雪\n",
            ["document"] = "document: TODO-7 -> TODO-7\r\nUnicode: café 雪\n", ["todo-reference"] = "todo-reference: TODO-7 -> TODO-7\r\nUnicode: café 雪\n" };
    }
}
