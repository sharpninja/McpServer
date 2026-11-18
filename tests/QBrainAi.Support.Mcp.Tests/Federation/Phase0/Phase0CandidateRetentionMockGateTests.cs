namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent fixed object scripts prove the candidate-retention assertions discriminate corruption.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007 B6/B12; FR-MCP-180/TR-MCP-FEDERATION-007/TEST-MCP-228 ac-SameCellConflictNeedsReview/ac-DeleteUpdateNeedsReview; schema/constraint supplement. No Dolt process, object store, transfer or GC implementation.</remarks>
public sealed class Phase0CandidateRetentionMockGateTests
{
    /// <summary>Gets the four independently specified unresolved fixture kinds.</summary>
    public static IEnumerable<object[]> ValidFixtures => from kind in new[] { "cell", "delete", "schema", "constraint" } from reverse in new[] { false, true } select new object[] { kind, reverse };

    /// <summary>Correct literal scripts preserve complete evidence across all seven checkpoints.</summary><param name="kind">Fixture.</param><param name="reverse">Ref enumeration order variation.</param>
    [Theory, MemberData(nameof(ValidFixtures))]
    public void CorrectCandidateRetentionPasses(string kind, bool reverse)
    {
        var probe = new RetentionProbe(kind, reverse);
        Phase0CandidateRetentionContract.AssertRetention(probe, kind);
        Assert.Equal(6, probe.Phase); Assert.Equal(7, probe.ReviewReads); Assert.Equal(56, probe.ObjectReads); Assert.Equal(14, probe.RefReads); Assert.Equal(8, probe.SafetyReads); Assert.Equal(7, probe.OverrideReads);
    }

    /// <summary>Gets missing or corrupt observations at each required persistence boundary.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            for (var phase = 0; phase < 7; phase++)
            {
                foreach (var member in new[] { "Missing", "Id", "Project", "Branch", "Base", "Local", "Incoming", "Candidate", "State" }) yield return ["cell", $"review:{phase}:{member}"];
                foreach (var side in new[] { "local", "remote" })
                {
                    foreach (var member in new[] { "Missing", "Extra", "Missingbase", "Missinglocal", "Missingincoming", "Missingcandidate", "Wrongbase", "Wronglocal", "Wrongincoming", "Wrongcandidate" }) yield return ["cell", $"refs:{phase}:{side}:{member}"];
                    foreach (var hash in new[] { "C0", "B1", "A1", "K1" })
                        foreach (var member in new[] { "Missing", "Hash", "Schema", "Rows", "ArtifactsMissing", "ArtifactsChanged" }) yield return ["cell", $"object:{phase}:{side}:{hash}:{member}"];
                    foreach (var kind in new[] { "cell", "delete", "schema", "constraint" })
                        foreach (var member in new[] { "Table", "Key", "Kind", "Base", "Local", "Incoming", "BaseDeleted", "LocalDeleted", "IncomingDeleted", "Error" }) yield return [kind, $"artifact:{phase}:{side}:{member}"];
                }
                foreach (var member in new[] { "Missing", "Transfers", "Reopens", "LocalGc", "RemoteGc", "CleanupAttempts", "OwningSessions" }) yield return ["cell", $"operation:{phase}:{member}"];
                foreach (var member in new[] { "Missing", "Local", "Public", "Feature", "Other", "Rows", "Fingerprint", "Conflicts", "Publications" }) yield return ["cell", $"safety:{phase + 1}:{member}"];
                foreach (var member in new[] { "Missing", "Extra", "MissingKey", "global.automatic", "global.allowConflicts", "global.forceTransaction", "live.automatic", "live.allowConflicts", "live.forceTransaction", "fresh.automatic", "fresh.allowConflicts", "fresh.forceTransaction" }) yield return ["cell", $"override:{phase}:{member}"];
            }
            foreach (var member in new[] { "Missing", "Local", "Public", "Feature", "Other", "Rows", "EmptyFingerprint", "Conflicts", "Publications" }) yield return ["cell", "safety:0:" + member];
        }
    }

    /// <summary>Each corrupt observation must be rejected by the unchanged shared assertions.</summary><param name="kind">Fixture.</param><param name="fault">One independent corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongCandidateRetentionIsRejected(string kind, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0CandidateRetentionContract.AssertRetention(new RetentionProbe(kind, false, fault), kind));

    /// <summary>Literal mock objects and mutation observations, authored independently of the assertion oracle.</summary>
    private sealed class RetentionProbe(string kind, bool reverse, string fault = "") : IPhase0CandidateRetentionProbe
    {
        /// <summary>Gets the last operation boundary reached.</summary>
        internal int Phase { get; private set; }
        /// <summary>Gets review reads.</summary>
        internal int ReviewReads { get; private set; }
        /// <summary>Gets physical object reads.</summary>
        internal int ObjectReads { get; private set; }
        /// <summary>Gets physical ref reads.</summary>
        internal int RefReads { get; private set; }
        /// <summary>Gets independent safety reads.</summary>
        internal int SafetyReads { get; private set; }
        /// <summary>Gets independent settings reads.</summary>
        internal int OverrideReads { get; private set; }
        /// <inheritdoc />
        public void Prepare(string value) { Assert.Equal(kind, value); Phase = ReviewReads = ObjectReads = RefReads = SafetyReads = OverrideReads = 0; }
        /// <inheritdoc />
        public Phase0RetentionReview? ReadReview()
        {
            ReviewReads++;
            var value = new Phase0RetentionReview(Guid.Parse("35ce4ee2-297b-47e1-9df4-84555d8bc118"), "Atlas", "main", "C0", "B1", "A1", "K1", "needs_review");
            return Active($"review:{Phase}:") switch
            {
                "Missing" => null, "Id" => value with { Id = Guid.Empty }, "Project" => value with { Project = "Other" }, "Branch" => value with { Branch = "feature" },
                "Base" => value with { Base = "C9" }, "Local" => value with { Local = "B9" }, "Incoming" => value with { Incoming = "A9" }, "Candidate" => value with { Candidate = "K9" }, "State" => value with { State = "resolved" }, _ => value
            };
        }
        /// <inheritdoc />
        public IReadOnlyDictionary<string, string>? ReadRefs(string side)
        {
            RefReads++;
            var prefix = "refs/heads/review/35ce4ee2-297b-47e1-9df4-84555d8bc118/";
            var pairs = new[] { ("base", "C0"), ("local", "B1"), ("incoming", "A1"), ("candidate", "K1") };
            var result = (reverse ? pairs.Reverse() : pairs).ToDictionary(pair => prefix + pair.Item1, pair => pair.Item2);
            var active = Active($"refs:{Phase}:{side}:");
            if (active == "Missing") return null;
            if (active == "Extra") result[prefix + "unbound"] = "Z9";
            if (active.StartsWith("Missing", StringComparison.Ordinal)) result.Remove(prefix + active[7..]);
            if (active.StartsWith("Wrong", StringComparison.Ordinal)) result[prefix + active[5..]] = "Z9";
            return result;
        }
        /// <inheritdoc />
        public Phase0RetainedObject? ReadObject(string side, string hash)
        {
            ObjectReads++;
            var schema = "title:VARCHAR(64)";
            var rows = hash switch { "C0" => "T1:original:low", "B1" => "T1:edited:medium", "A1" => "T1:original:high", "K1" => "T1:edited:medium", _ => throw new ArgumentOutOfRangeException(nameof(hash)) };
            Phase0RetainedArtifact artifact = new("Todo", "T1.priority", "cell", "low", "medium", "high", false, false, false, null);
            if (kind == "delete")
            { if (hash == "A1") rows = ""; artifact = new("Todo", "T1.title", "delete_update", "original", "edited", null, false, false, true, null); }
            if (kind == "schema")
            { if (hash is "B1" or "K1") schema = "title:VARCHAR(128)"; if (hash == "A1") { schema = "title:INT"; rows = "T1:17:low"; } artifact = new("Todo", "title.type", "schema", "VARCHAR(64)", "VARCHAR(128)", "INT", false, false, false, null); }
            if (kind == "constraint")
            { schema = "code:VARCHAR(8):UNIQUE"; rows = hash switch { "C0" => "T1:S", "B1" => "T1:S;T2:D", "A1" => "T1:S;T3:D", _ => "T1:S;T2:D;T3:D" }; artifact = new("Todo", "UX_Code:D", "unique", null, "T2:D", "T3:D", false, false, false, "unique index UX_Code duplicated D"); }
            if (hash == "K1") artifact = CorruptArtifact(artifact, Active($"artifact:{Phase}:{side}:"));
            var value = new Phase0RetainedObject(hash, schema, rows, hash == "K1" ? [artifact] : []);
            return Active($"object:{Phase}:{side}:{hash}:") switch
            {
                "Missing" => null, "Hash" => value with { Hash = "Z9" }, "Schema" => value with { Schema = "changed" }, "Rows" => value with { Rows = "changed" }, "ArtifactsMissing" => value with { Artifacts = null },
                "ArtifactsChanged" => value with { Artifacts = hash == "K1" ? [] : [artifact] }, _ => value
            };
        }
        /// <inheritdoc />
        public Phase0RetentionSafety? ReadSafety()
        {
            var active = Active($"safety:{SafetyReads++}:");
            var value = new Phase0RetentionSafety("B1", "A1", "F1", "X0", kind == "constraint" ? "T1:S;T2:D" : "T1:edited:medium", "independent-live-working-set-history", 0, 0);
            return active switch
            {
                "Missing" => null, "Local" => value with { Local = "K1" }, "Public" => value with { Public = "K1" }, "Feature" => value with { Feature = "F9" }, "Other" => value with { Other = "X9" },
                "Rows" => value with { Rows = "changed" }, "EmptyFingerprint" => value with { Fingerprint = null }, "Fingerprint" => value with { Fingerprint = "changed" }, "Conflicts" => value with { Conflicts = 1 }, "Publications" => value with { Publications = 1 }, _ => value
            };
        }
        /// <inheritdoc />
        public IReadOnlyDictionary<string, int>? ReadOverrides()
        {
            OverrideReads++;
            var values = new Dictionary<string, int> { ["global.automatic"] = 0, ["global.allowConflicts"] = 0, ["global.forceTransaction"] = 0, ["live.automatic"] = 0, ["live.allowConflicts"] = 0, ["live.forceTransaction"] = 0, ["fresh.automatic"] = 0, ["fresh.allowConflicts"] = 0, ["fresh.forceTransaction"] = 0 };
            var active = Active($"override:{Phase}:");
            if (active == "Missing") return null;
            if (active == "Extra") values["other"] = 0;
            if (active == "MissingKey") values.Remove("fresh.allowConflicts");
            if (values.ContainsKey(active)) values[active] = 1;
            return values;
        }
        /// <inheritdoc />
        public Phase0RetentionOperations? ReadOperations()
        {
            var value = new Phase0RetentionOperations(Phase > 0 ? 1 : 0, Phase >= 6 ? 3 : Phase >= 4 ? 2 : Phase >= 2 ? 1 : 0, Phase >= 3 ? 1 : 0, Phase >= 3 ? 1 : 0, Phase >= 5 ? 1 : 0, 0);
            return Active($"operation:{Phase}:") switch
            {
                "Missing" => null, "Transfers" => value with { Transfers = value.Transfers + 1 }, "Reopens" => value with { Reopens = value.Reopens + 1 }, "LocalGc" => value with { LocalGc = value.LocalGc + 1 },
                "RemoteGc" => value with { RemoteGc = value.RemoteGc + 1 }, "CleanupAttempts" => value with { CleanupAttempts = value.CleanupAttempts + 1 }, "OwningSessions" => value with { OwningSessions = 1 }, _ => value
            };
        }
        /// <inheritdoc />
        public void TransferRoundTrip() { Assert.Equal(0, Phase); Phase = 1; }
        /// <inheritdoc />
        public void Reopen() { Assert.Contains(Phase, new[] { 1, 3, 5 }); Phase++; }
        /// <inheritdoc />
        public void CollectBoth() { Assert.Equal(2, Phase); Phase = 3; }
        /// <inheritdoc />
        public void AttemptUnresolvedCleanup() { Assert.Equal(4, Phase); Phase = 5; }
        /// <summary>Selects only the explicitly targeted read corruption.</summary><param name="prefix">Read identity.</param><returns>Fault field or empty.</returns>
        private string Active(string prefix) => fault.StartsWith(prefix, StringComparison.Ordinal) ? fault[prefix.Length..] : "";
        /// <summary>Independently corrupts one retained artifact field.</summary><param name="value">Literal fixture artifact.</param><param name="field">One wrong field.</param><returns>Corrupted artifact.</returns>
        private static Phase0RetainedArtifact CorruptArtifact(Phase0RetainedArtifact value, string field) => field switch
        {
            "Table" => value with { Table = "Other" }, "Key" => value with { Key = "other" }, "Kind" => value with { Kind = "other" }, "Base" => value with { Base = "wrong" }, "Local" => value with { Local = "wrong" }, "Incoming" => value with { Incoming = "wrong" },
            "BaseDeleted" => value with { BaseDeleted = !value.BaseDeleted }, "LocalDeleted" => value with { LocalDeleted = !value.LocalDeleted }, "IncomingDeleted" => value with { IncomingDeleted = !value.IncomingDeleted }, "Error" => value with { Error = value.Error == null ? "unexpected" : null }, _ => value
        };
    }
}
