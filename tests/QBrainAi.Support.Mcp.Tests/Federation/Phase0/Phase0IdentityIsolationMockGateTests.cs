namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent fixtures prove the identity and writer-isolation assertions reject corrupted evidence.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds, ac-LegacyIdsRemainValid;
/// FR-MCP-184 ac-BackgroundWritersDoNotDirtyVersionedBranch; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.
/// No real database, migration, allocator grammar or production adapter is implemented or accepted here.
/// </remarks>
public sealed class Phase0IdentityIsolationMockGateTests
{
    /// <summary>Correct operational persistence preserves the versioned working set after every writer.</summary>
    [Fact]
    public void CorrectBackgroundIsolationPasses()
    {
        var probe = new WriterProbe();
        Phase0IdentityIsolationContract.AssertBackgroundIsolation(probe);
        Assert.Equal(3, probe.Writes);
        Assert.Equal(4, probe.FingerprintReads);
        Assert.Equal(4, probe.HeadReads);
        Assert.Equal(4, probe.RowReads);
    }

    /// <summary>Both sequence-one node records survive reconciliation with all content and provenance.</summary>
    [Fact]
    public void CorrectDistinctCreatorsPass()
    {
        var probe = new IdentityProbe();
        Phase0IdentityIsolationContract.AssertDistinctCreators(probe);
        Assert.Equal(new[] { "prepare", "create:LA", "create:LG", "reconcile", "read" }, probe.Calls);
    }

    /// <summary>Original scoped references resolve before/after reopen and all six historical text kinds survive.</summary>
    [Fact]
    public void CorrectLegacyPreservationPasses()
    {
        var probe = new AliasProbe();
        Phase0IdentityIsolationContract.AssertLegacyPreservation(probe);
        Assert.Equal(4, probe.Resolutions);
        Assert.Equal(2, probe.HistoryReads);
        Assert.Equal(1, probe.Reopens);
    }

    /// <summary>No record-order requirement is invented for unordered persistence readbacks.</summary>
    [Fact]
    public void UnorderedReadbacksRemainValid()
    {
        Phase0IdentityIsolationContract.AssertBackgroundIsolation(new WriterProbe(fault: "Reverse"));
        Phase0IdentityIsolationContract.AssertDistinctCreators(new IdentityProbe(fault: "Reverse"));
        Phase0IdentityIsolationContract.AssertLegacyPreservation(new AliasProbe(fault: "Reverse"));
    }

    /// <summary>Gets independent faults at every writer readback, including omissions and wrong persisted values.</summary>
    public static IEnumerable<object[]> WriterFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 4; checkpoint++)
            {
                foreach (var fault in new[] { "Head", "Fingerprint", "FingerprintNull", "FingerprintEmpty", "RowsMissing", "RowsExtra" })
                    yield return [checkpoint, fault];
                if (checkpoint == 0) continue;
                yield return [checkpoint, "RowDropped"];
                for (var row = 0; row < checkpoint; row++)
                foreach (var corruptField in new[] { "Id", "Kind", "Payload" })
                    yield return [checkpoint, row + ":" + corruptField];
            }
        }
    }

    /// <summary>Gets every independently corrupted creation and reconciled row field.</summary>
    public static IEnumerable<object[]> IdentityFaults
    {
        get
        {
            for (var creation = 0; creation < 2; creation++)
            foreach (var fault in new[] { "Missing", "EmptyId", "WhitespaceId", "Origin", "Sequence", "Title", "DuplicateId" })
                yield return [creation, fault];
            foreach (var fault in new[] { "RowsMissing", "DropFirst", "DropSecond", "ExtraRow", "DuplicateRow" })
                yield return [-1, fault];
            for (var row = 0; row < 2; row++)
            foreach (var corruptField in new[] { "Id", "Origin", "Sequence", "Title" })
                yield return [-1, row + ":" + corruptField];
        }
    }

    /// <summary>Gets faults at all alias lookups and historical-text readbacks before/after reopen.</summary>
    public static IEnumerable<object[]> AliasFaults
    {
        get
        {
            for (var lookup = 0; lookup < 4; lookup++)
            foreach (var fault in new[] { "Missing", "Id", "Origin", "Sequence", "Title" })
                yield return [lookup, "Resolve:" + fault];
            for (var checkpoint = 0; checkpoint < 2; checkpoint++)
            {
                yield return [checkpoint, "History:Missing"];
                yield return [checkpoint, "History:Extra"];
                foreach (var key in HistoryKinds)
                foreach (var fault in new[] { "Drop", "Text", "Newline" })
                    yield return [checkpoint, "History:" + key + ":" + fault];
            }
        }
    }

    /// <summary>Each writer-state fault must fail the reusable assertions.</summary>
    /// <param name="checkpoint">Single corrupted readback.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(WriterFaults))]
    public void WrongWriterOutcomeIsRejected(int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityIsolationContract.AssertBackgroundIsolation(new WriterProbe(checkpoint, fault)));

    /// <summary>Each lost or corrupted identity must fail the reusable assertions.</summary>
    /// <param name="creation">Creation index, or minus one for reconciliation.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(IdentityFaults))]
    public void WrongIdentityOutcomeIsRejected(int creation, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityIsolationContract.AssertDistinctCreators(new IdentityProbe(creation, fault)));

    /// <summary>Each broken alias or historical rewrite must fail the reusable assertions.</summary>
    /// <param name="position">Lookup or historical read index.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(AliasFaults))]
    public void WrongAliasOutcomeIsRejected(int position, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityIsolationContract.AssertLegacyPreservation(new AliasProbe(position, fault)));

    private static readonly string[] HistoryKinds = ["commit", "pr", "session", "memory", "document", "todo-reference"];

    /// <summary>Independent operational readback script, not a production routing implementation.</summary>
    private sealed class WriterProbe(int checkpoint = -1, string fault = "") : IPhase0OperationalWriterProbe
    {
        internal int Writes { get; private set; }
        internal int FingerprintReads { get; private set; }
        internal int HeadReads { get; private set; }
        internal int RowReads { get; private set; }
        private static readonly string[] Kinds = ["heartbeat", "audit", "automatic-session"];
        /// <inheritdoc />
        public void Prepare() => Writes = 0;
        /// <inheritdoc />
        public string? ReadHead() => HeadReads++ == checkpoint && fault == "Head" ? "C1" : "C0";
        /// <inheritdoc />
        public string? ReadWorkingSetFingerprint()
        {
            if (FingerprintReads++ != checkpoint) return "fixture-working-set-C0";
            return fault switch { "Fingerprint" => "dirty", "FingerprintNull" => null, "FingerprintEmpty" => "", _ => "fixture-working-set-C0" };
        }
        /// <inheritdoc />
        public void RunWriter(string id, string kind, string payload)
        {
            Assert.Equal("op-" + (Writes + 1), id);
            Assert.Equal(Kinds[Writes], kind);
            Assert.Equal("payload:" + Kinds[Writes], payload);
            Writes++;
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0OperationalRow>? ReadOperationalRows()
        {
            var rows = Kinds.Take(Writes).Select((kind, index) => new Phase0OperationalRow("op-" + (index + 1), kind, "payload:" + kind)).ToList();
            if (fault == "Reverse") { RowReads++; rows.Reverse(); return rows; }
            if (RowReads++ != checkpoint) return rows;
            if (fault == "RowsMissing") return null;
            if (fault == "RowsExtra") { rows.Add(new("extra", "audit", "unexpected")); return rows; }
            if (fault == "RowDropped") { rows.RemoveAt(0); return rows; }
            var parts = fault.Split(':');
            if (parts.Length != 2) return rows;
            var row = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            rows[row] = parts[1] switch
            {
                "Id" => rows[row] with { Id = "wrong" }, "Kind" => rows[row] with { Kind = "wrong" },
                "Payload" => rows[row] with { Payload = "wrong" }, _ => throw new InvalidOperationException(fault)
            };
            return rows;
        }
    }

    /// <summary>Independent two-node creation and reconciliation readbacks with opaque fixture identities.</summary>
    private sealed class IdentityProbe(int creation = -2, string fault = "") : IPhase0IdentityProbe
    {
        internal List<string> Calls { get; } = [];
        private int created;
        private static Phase0IdentityRow[] Rows => [new("fixture-a", "LA", 1, "LAB task"), new("fixture-b", "LG", 1, "LEGION task")];
        /// <inheritdoc />
        public void Prepare() { Calls.Add("prepare"); created = 0; }
        /// <inheritdoc />
        public Phase0IdentityRow? CreateTodo(string origin, string title)
        {
            Calls.Add("create:" + origin);
            var row = Rows[created];
            Assert.Equal(row.Origin, origin); Assert.Equal(row.Title, title);
            if (created++ != creation) return row;
            return fault switch
            {
                "Missing" => null, "EmptyId" => row with { Id = "" }, "WhitespaceId" => row with { Id = " " },
                "DuplicateId" => row with { Id = created == 1 ? "fixture-b" : "fixture-a" },
                _ => Corrupt(row, fault)
            };
        }
        /// <inheritdoc />
        public void Reconcile() => Calls.Add("reconcile");
        /// <inheritdoc />
        public IReadOnlyList<Phase0IdentityRow>? ReadTodos()
        {
            Calls.Add("read"); var rows = Rows.ToList();
            if (fault == "Reverse") { rows.Reverse(); return rows; }
            if (creation != -1) return rows;
            if (fault == "RowsMissing") return null;
            if (fault == "DropFirst") { rows.RemoveAt(0); return rows; }
            if (fault == "DropSecond") { rows.RemoveAt(1); return rows; }
            if (fault == "ExtraRow") { rows.Add(new("extra", "LA", 2, "extra")); return rows; }
            if (fault == "DuplicateRow") return [rows[0], rows[0]];
            var parts = fault.Split(':');
            var index = int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            rows[index] = Corrupt(rows[index], parts[1]); return rows;
        }
    }

    /// <summary>Independent scoped alias readbacks and historical bytes across a simulated reopen.</summary>
    private sealed class AliasProbe(int position = -1, string fault = "") : IPhase0LegacyAliasProbe
    {
        internal int Resolutions { get; private set; }
        internal int HistoryReads { get; private set; }
        internal int Reopens { get; private set; }
        private static Phase0IdentityRow[] Targets => [new("fixture-canonical-a", "LA", 1, "human task"), new("fixture-canonical-b", "LG", 1, "legacy task")];
        /// <inheritdoc />
        public void Import(IReadOnlyList<Phase0LegacyAlias> aliases, IReadOnlyDictionary<string, string> history)
        {
            Assert.Equal(2, aliases.Count); Assert.Equal(6, history.Count);
            Assert.Equal(new Phase0LegacyAlias("Atlas", "LA", "Todo", "IMPL-UNITGREEN-001", Targets[0]), aliases[0]);
            Assert.Equal(new Phase0LegacyAlias("Atlas", "LG", "Todo", "TODO-7", Targets[1]), aliases[1]);
            foreach (var key in HistoryKinds) Assert.Equal(key + ": IMPL-UNITGREEN-001 -> TODO-7\r\nUnicode: café 雪\n", history[key]);
        }
        /// <inheritdoc />
        public Phase0IdentityRow? Resolve(string project, string origin, string kind, string legacyId)
        {
            var index = Resolutions % 2;
            Assert.Equal("Atlas", project); Assert.Equal(index == 0 ? "LA" : "LG", origin);
            Assert.Equal("Todo", kind); Assert.Equal(index == 0 ? "IMPL-UNITGREEN-001" : "TODO-7", legacyId);
            Assert.Equal(Resolutions >= 2 ? 1 : 0, Reopens);
            var row = Targets[index];
            if (Resolutions++ != position || !fault.StartsWith("Resolve:", StringComparison.Ordinal)) return row;
            return fault == "Resolve:Missing" ? null : Corrupt(row, fault[8..]);
        }
        /// <inheritdoc />
        public IReadOnlyDictionary<string, string>? ReadHistory()
        {
            var rows = HistoryKinds.ToDictionary(key => key, key => key + ": IMPL-UNITGREEN-001 -> TODO-7\r\nUnicode: café 雪\n", StringComparer.Ordinal);
            if (fault == "Reverse") { HistoryReads++; return rows.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal); }
            if (HistoryReads++ != position || !fault.StartsWith("History:", StringComparison.Ordinal)) return rows;
            if (fault == "History:Missing") return null;
            if (fault == "History:Extra") { rows.Add("unexpected", "text"); return rows; }
            var parts = fault.Split(':'); var key = parts[1];
            if (parts[2] == "Drop") rows.Remove(key);
            else rows[key] = parts[2] == "Newline" ? rows[key].Replace("\r\n", "\n", StringComparison.Ordinal) : "rewritten";
            return rows;
        }
        /// <inheritdoc />
        public void Reopen() => Reopens++;
    }

    /// <summary>Corrupts only one identity field without consulting the contract's expected values.</summary>
    /// <param name="row">Independent fixture row.</param><param name="corruptField">Field to corrupt.</param>
    /// <returns>Single-field corrupted row.</returns>
    private static Phase0IdentityRow Corrupt(Phase0IdentityRow row, string corruptField) => corruptField switch
    {
        "Id" => row with { Id = "wrong" }, "Origin" => row with { Origin = "XX" },
        "Sequence" => row with { Sequence = 99 }, "Title" => row with { Title = "wrong" },
        _ => throw new InvalidOperationException(corruptField)
    };
}
