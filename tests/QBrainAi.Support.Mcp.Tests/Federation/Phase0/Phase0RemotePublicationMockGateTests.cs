namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent committed-history scripts and deliberate corruptions prove the publication assertions discriminate.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Supplemental Ph0 feasibility: FR-MCP-178 ac-HubDraftDoesNotBlockRemote; FR-MCP-181 ac-LostPushAckDoesNotDuplicateCommit;
/// TR-MCP-FEDERATION-005/008; TEST-MCP-226/229. No native processes, remote, gateway or durable production stores are exercised.</remarks>
public sealed class Phase0RemotePublicationMockGateTests
{
    private static readonly string[] FaultPoints = ["None", "DropReply", "BeforeRef", "AfterRef", "AfterReceipt"];
    private static readonly string[] Checkpoints = ["Before", "Uploaded", "Attempted", "Restarted", "Recovered", "Retried", "Reopened", "Conflict"];
    private static readonly Guid Epoch = Guid.Parse("44444444-4444-4444-8444-444444444444");

    /// <summary>Gets all twenty independent clean/dirty and interruption combinations.</summary>
    public static IEnumerable<object[]> Scenarios
    {
        get { foreach (var source in new[] { false, true }) foreach (var hub in new[] { false, true }) foreach (var point in FaultPoints) yield return [source, hub, point]; }
    }

    /// <summary>Gets bounded reporting groups with their exact nested negative-control denominators.</summary>
    public static IEnumerable<object[]> StateGroups
    {
        get
        {
            foreach (var row in Scenarios)
            foreach (var checkpoint in Checkpoints)
                yield return [row[0], row[1], row[2], checkpoint, StateFaults(Snapshot((bool)row[0], (bool)row[1], (string)row[2], checkpoint)).Count()];
        }
    }

    /// <summary>Gets response and process-restart boundary groups with exact rejection counts.</summary>
    public static IEnumerable<object[]> BoundaryGroups
    {
        get
        {
            foreach (var row in Scenarios)
            {
                foreach (var label in new[] { "First", "Recover", "Retry", "Conflict" })
                    yield return [row[0], row[1], row[2], label, ReplyFaults(Reply((string)row[2], label)).Count()];
                foreach (var label in new[] { "Restart1", "Restart2" }) yield return [row[0], row[1], row[2], label, 6];
            }
        }
    }

    /// <summary>Every clean/dirty and interruption case passes and exercises all durable readbacks in order.</summary>
    /// <param name="sourceDirty">Source draft flag.</param><param name="hubDirty">Hub draft flag.</param><param name="point">Fault boundary.</param>
    [Theory, MemberData(nameof(Scenarios))]
    public void CorrectPublicationAndRecoveryPass(bool sourceDirty, bool hubDirty, string point)
    {
        var probe = new ScriptedProbe(sourceDirty, hubDirty, point);
        Phase0RemotePublicationContract.AssertPublication(probe, sourceDirty, hubDirty, point);
        Assert.Equal(new[] { "Prepare", "Before", "Upload", "Uploaded", "First", "Attempted", "Restart1", "Restarted", "Recover", "Recovered",
            "Retry", "Retried", "Restart2", "Reopened", "ConflictRequest", "Conflict" }, probe.Calls);
    }

    /// <summary>History and dictionary order, generation gaps, extra recovery roots and pre-receipt generation lag are valid.</summary>
    /// <param name="sourceDirty">Source draft flag.</param><param name="hubDirty">Hub draft flag.</param><param name="point">Fault boundary.</param>
    [Theory, MemberData(nameof(Scenarios))]
    public void ValidObservationVariationsPass(bool sourceDirty, bool hubDirty, string point)
        => Phase0RemotePublicationContract.AssertPublication(new ScriptedProbe(sourceDirty, hubDirty, point, variation: true), sourceDirty, hubDirty, point);

    /// <summary>Rejects every independently corrupted state and proves each corruption was actually injected.</summary>
    /// <param name="sourceDirty">Source draft flag.</param><param name="hubDirty">Hub draft flag.</param><param name="point">Fault boundary.</param>
    /// <param name="checkpoint">Independent readback.</param><param name="negativeControls">Expected exact rejection denominator.</param>
    [Theory, MemberData(nameof(StateGroups))]
    public void WrongDurableStatesAreRejected(bool sourceDirty, bool hubDirty, string point, string checkpoint, int negativeControls)
    {
        var rejected = 0;
        foreach (var fault in StateFaults(Snapshot(sourceDirty, hubDirty, point, checkpoint)))
        {
            var probe = new ScriptedProbe(sourceDirty, hubDirty, point, "State:" + checkpoint, fault.Name);
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RemotePublicationContract.AssertPublication(probe, sourceDirty, hubDirty, point));
            Assert.True(probe.Injected, fault.Name); rejected++;
        }
        Assert.Equal(negativeControls, rejected);
    }

    /// <summary>Rejects delivered-response, receipt and native lifecycle contradictions at every operation boundary.</summary>
    /// <param name="sourceDirty">Source draft flag.</param><param name="hubDirty">Hub draft flag.</param><param name="point">Fault boundary.</param>
    /// <param name="label">Response or restart boundary.</param><param name="negativeControls">Expected exact rejection denominator.</param>
    [Theory, MemberData(nameof(BoundaryGroups))]
    public void WrongResponsesAndRestartsAreRejected(bool sourceDirty, bool hubDirty, string point, string label, int negativeControls)
    {
        var faults = label.StartsWith("Restart", StringComparison.Ordinal)
            ? new[] { "Null", "OldPid", "NewPid", "SamePid", "OldExited", "StorageReopened" }
            : ReplyFaults(Reply(point, label)).Select(pair => pair.Name);
        var rejected = 0;
        foreach (var fault in faults)
        {
            var probe = new ScriptedProbe(sourceDirty, hubDirty, point, label.StartsWith("Restart", StringComparison.Ordinal) ? label : "Reply:" + label, fault);
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RemotePublicationContract.AssertPublication(probe, sourceDirty, hubDirty, point));
            Assert.True(probe.Injected, fault); rejected++;
        }
        Assert.Equal(negativeControls, rejected);
    }

    /// <summary>Baseline collections must be frozen before a probe can mutate shared input aliases.</summary>
    [Fact]
    public void MutableBaselineCannotRewriteExpectedHistory()
    {
        var probe = new ScriptedProbe(false, false, "None", "Alias", "Baseline");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RemotePublicationContract.AssertPublication(probe, false, false, "None"));
        Assert.True(probe.Injected);
    }

    /// <summary>Independent fixed request fixture; no assertion implementation is called to derive expected replies.</summary>
    private static Phase0PublicationRequest Request(string candidate = "A1") => new(
        Guid.Parse("33333333-3333-4333-8333-333333333333"), Guid.Parse("22222222-2222-4222-8222-222222222222"),
        Guid.Parse("11111111-1111-4111-8111-111111111111"), "main", Epoch, candidate, "C0");

    /// <summary>Independent fixed durable receipt, permitting a valid published-generation gap.</summary>
    private static Phase0PublicationReceipt Receipt(bool variation = false) => new("receipt-R1", Request(), "A1", variation ? 213 : 13, true);

    /// <summary>Independent original committed ancestry; the init root is retained.</summary>
    private static Phase0HistoryPoint[] OriginalHistory() => [new("root", []), new("C0", ["root"])];

    /// <summary>Independent committed candidate ancestry, with no draft/history creation during upload.</summary>
    private static Phase0HistoryPoint[] CandidateHistory() => [new("root", []), new("C0", ["root"]), new("A1", ["C0"])];

    /// <summary>Scripted full state at a named checkpoint; no publication implementation runs.</summary>
    private static Phase0RemotePublicationState Snapshot(bool sourceDirty, bool hubDirty, string point, string checkpoint, bool variation = false)
    {
        var final = checkpoint is "Recovered" or "Retried" or "Reopened" or "Conflict";
        var attempted = checkpoint is "Attempted" or "Restarted";
        var uploaded = checkpoint != "Before";
        var published = final || (attempted && point != "BeforeRef");
        var receipt = final || (attempted && point is "None" or "DropReply" or "AfterReceipt");
        var completed = final || (attempted && point == "None");
        var generation = published && !(variation && attempted && point == "AfterRef") ? (variation ? 213 : 13) : 7;
        var result = new Phase0RemotePublicationState(Epoch, "A1", "published", sourceDirty ? "source-draft" : "published",
            CandidateHistory(), "C0", hubDirty ? "hub-draft" : "original",
            new Dictionary<string, string> { ["main"] = published ? "A1" : "C0", ["feature"] = "F0", ["other"] = "X0" },
            published ? "published" : "original", published ? CandidateHistory() : OriginalHistory(), generation,
            uploaded ? "A1" : null, uploaded ? ["A1"] : [], Request(), completed, receipt ? [Receipt(variation)] : [], 0);
        return !variation ? result : result with
        {
            SourceHistory = result.SourceHistory.Reverse().ToArray(), PublicHistory = result.PublicHistory.Reverse().ToArray(),
            PublicRefs = result.PublicRefs.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value),
            RecoveryRoots = uploaded ? ["another-retained-root", "A1"] : []
        };
    }

    /// <summary>Independent delivered response script, including honest uncertain transport outcomes.</summary>
    private static Phase0PublicationReply Reply(string point, string label, bool variation = false)
    {
        if (label == "Conflict") return new("Http", 409, "request_id_conflict", null, "None", false);
        if (label == "First" && point != "None") return new("TransportFailure", null, null, null, point, point is "BeforeRef" or "AfterRef" or "AfterReceipt");
        return new("Http", 200, null, Receipt(variation), "None", false);
    }

    /// <summary>Every single receipt identity, binding, result and completion corruption.</summary>
    private static IEnumerable<(string Name, Phase0PublicationReceipt Value)> ReceiptFaults(Phase0PublicationReceipt row)
    {
        yield return ("Id", row with { Id = "" }); yield return ("Result", row with { ResultHead = "C0" });
        yield return ("Generation", row with { Generation = -1 }); yield return ("Completed", row with { Completed = false });
        foreach (var pair in RequestFaults(row.Request)) yield return ("Request:" + pair.Name, row with { Request = pair.Value });
    }

    /// <summary>Every immutable request binding corruption, including authenticated node and branch.</summary>
    private static IEnumerable<(string Name, Phase0PublicationRequest Value)> RequestFaults(Phase0PublicationRequest row)
    {
        yield return ("RequestId", row with { RequestId = Guid.Empty }); yield return ("NodeId", row with { NodeId = Guid.Empty });
        yield return ("ProjectId", row with { ProjectId = Guid.Empty }); yield return ("Branch", row with { Branch = "feature" });
        yield return ("HubEpoch", row with { HubEpoch = Guid.Empty }); yield return ("Candidate", row with { CandidateHead = "wrong" });
        yield return ("ExpectedHead", row with { ExpectedPublishedHead = "wrong" });
    }

    /// <summary>Complete response envelope corruptions, preserving safe opaque aliases.</summary>
    private static IEnumerable<(string Name, Phase0PublicationReply? Value)> ReplyFaults(Phase0PublicationReply row)
    {
        yield return ("Null", null); yield return ("Kind", row with { Kind = "wrong" });
        yield return ("Status", row with { Status = row.Status is null ? 200 : 418 });
        yield return ("Code", row with { Code = "wrong" }); yield return ("Fault", row with { ReachedFault = "wrong" });
        yield return ("Terminated", row with { ProcessTerminated = !row.ProcessTerminated });
        yield return ("ReceiptPresence", row with { Receipt = row.Receipt is null ? Receipt() : null });
        if (row.Receipt is not null) foreach (var pair in ReceiptFaults(row.Receipt)) yield return ("Receipt:" + pair.Name, row with { Receipt = pair.Value });
    }

    /// <summary>All complete state corruptions, kept independent from the assertion source.</summary>
    private static IEnumerable<(string Name, Phase0RemotePublicationState? Value)> StateFaults(Phase0RemotePublicationState row)
    {
        yield return ("Null", null); yield return ("Epoch", row with { Epoch = Guid.Empty });
        yield return ("SourceHead", row with { SourceHead = "wrong" }); yield return ("CommittedTitle", row with { SourceCommittedTitle = "draft" });
        yield return ("WorkingTitle", row with { SourceWorkingTitle = "wrong" }); yield return ("HubHead", row with { HubHead = "wrong" });
        yield return ("HubTitle", row with { HubWorkingTitle = "wrong" }); yield return ("PublicTitle", row with { PublicTitle = "wrong" });
        yield return ("Generation", row with { Generation = -1 }); yield return ("Upload", row with { UploadHead = "wrong" });
        yield return ("RootsNull", row with { RecoveryRoots = null! });
        yield return ("RootsMissingOrExtra", row with { RecoveryRoots = row.UploadHead is null ? ["unexpected"] : [] });
        yield return ("RootsDuplicate", row with { RecoveryRoots = ["A1", "A1"] }); yield return ("RootsBlank", row with { RecoveryRoots = ["A1", ""] });
        yield return ("IntentNull", row with { Intent = null! }); yield return ("IntentCompleted", row with { IntentCompleted = !row.IntentCompleted });
        foreach (var pair in RequestFaults(row.Intent)) yield return ("Intent:" + pair.Name, row with { Intent = pair.Value });
        yield return ("ReceiptsNull", row with { Receipts = null! });
        yield return ("ReceiptCount", row with { Receipts = row.Receipts.Count == 0 ? [Receipt()] : [] });
        if (row.Receipts.Count != 0)
        {
            yield return ("ReceiptNull", row with { Receipts = [null!] });
            yield return ("ReceiptDuplicate", row with { Receipts = [row.Receipts[0], row.Receipts[0]] });
            foreach (var pair in ReceiptFaults(row.Receipts[0])) yield return ("Receipt:" + pair.Name, row with { Receipts = [pair.Value] });
        }
        yield return ("Force", row with { ForceUpdates = 1 }); yield return ("RefsNull", row with { PublicRefs = null! });
        yield return ("RefsMissing", row with { PublicRefs = row.PublicRefs.Where(pair => pair.Key != "feature").ToDictionary(pair => pair.Key, pair => pair.Value) });
        yield return ("RefsExtra", row with { PublicRefs = row.PublicRefs.Append(new KeyValuePair<string, string>("unexpected", "U0")).ToDictionary(pair => pair.Key, pair => pair.Value) });
        foreach (var key in new[] { "main", "feature", "other" })
        {
            var refs = row.PublicRefs.ToDictionary(pair => pair.Key, pair => pair.Value); refs[key] = "";
            yield return ("Ref:" + key, row with { PublicRefs = refs });
        }
        foreach (var source in new[] { true, false })
        {
            var history = source ? row.SourceHistory : row.PublicHistory;
            foreach (var pair in HistoryFaults(history))
                yield return ((source ? "SourceHistory:" : "PublicHistory:") + pair.Name,
                    source ? row with { SourceHistory = pair.Value! } : row with { PublicHistory = pair.Value! });
        }
    }

    /// <summary>Missing, duplicate, invented and corrupt-parent ancestry observations.</summary>
    private static IEnumerable<(string Name, IReadOnlyList<Phase0HistoryPoint>? Value)> HistoryFaults(IReadOnlyList<Phase0HistoryPoint> history)
    {
        yield return ("Null", null); yield return ("Empty", []); yield return ("NullPoint", [null!]);
        yield return ("Missing", history.Take(history.Count - 1).ToArray());
        yield return ("Extra", history.Append(new Phase0HistoryPoint("invented", ["C0"])).ToArray());
        yield return ("Duplicate", history.Append(history[0]).ToArray());
        yield return ("ParentsNull", history.Select((point, i) => i == 0 ? point with { Parents = null! } : point).ToArray());
        yield return ("Parent", history.Select((point, i) => i == 0 ? point with { Parents = ["wrong"] } : point).ToArray());
    }

    /// <summary>Independent scripted states with exact input/call validation and one targeted corruption per run.</summary>
    private sealed class ScriptedProbe(bool sourceDirty, bool hubDirty, string point, string target = "", string fault = "", bool variation = false)
        : IPhase0RemotePublicationProbe
    {
        private int publications;
        private int restarts;
        private Phase0RemotePublicationState? aliased;
        /// <summary>Gets the observed seam call sequence.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Gets whether the named wrong observation actually reached the assertion boundary.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc />
        public void Prepare(bool requestedSourceDirty, bool requestedHubDirty)
        { Assert.Equal(sourceDirty, requestedSourceDirty); Assert.Equal(hubDirty, requestedHubDirty); Calls.Add("Prepare"); }
        /// <inheritdoc />
        public Phase0RemotePublicationState? Read(string checkpoint)
        {
            Calls.Add(checkpoint);
            var state = Snapshot(sourceDirty, hubDirty, point, checkpoint, variation);
            if (target == "Alias" && checkpoint == "Before") aliased = state;
            if (target == "Alias" && Injected)
            {
                Assert.NotNull(aliased);
                var refs = state.PublicRefs.ToDictionary(pair => pair.Key, pair => pair.Value);
                refs["feature"] = aliased.PublicRefs["feature"];
                return state with { PublicRefs = refs, SourceHistory = aliased.SourceHistory,
                    PublicHistory = refs["main"] == "A1" ? aliased.SourceHistory : state.PublicHistory };
            }
            if (target != "State:" + checkpoint) return state;
            Injected = true;
            return StateFaults(state).Single(pair => pair.Name == fault).Value;
        }
        /// <inheritdoc />
        public void Upload(Phase0PublicationRequest request)
        {
            Assert.Equal(Request(), request); Calls.Add("Upload");
            if (target == "Alias")
            {
                Assert.NotNull(aliased);
                ((Dictionary<string, string>)aliased.PublicRefs)["feature"] = "rewritten-baseline";
                ((Phase0HistoryPoint[])aliased.SourceHistory)[0] = new("rewritten-baseline", []);
                Injected = true;
            }
        }
        /// <inheritdoc />
        public Phase0PublicationReply? Publish(Phase0PublicationRequest request, string requestedFault)
        {
            var label = publications++ switch { 0 => "First", 1 => "Retry", _ => "Conflict" };
            Assert.Equal(label == "Conflict" ? Request("different-candidate") : Request(), request);
            Assert.Equal(label == "First" ? point : "None", requestedFault);
            Calls.Add(label == "Conflict" ? "ConflictRequest" : label);
            return GetReply(label);
        }
        /// <inheritdoc />
        public Phase0PublicationReply? Recover(Phase0PublicationRequest request)
        { Assert.Equal(Request(), request); Calls.Add("Recover"); return GetReply("Recover"); }
        /// <inheritdoc />
        public Phase0PublicationRestart? Restart()
        {
            var label = "Restart" + ++restarts; Calls.Add(label);
            var result = new Phase0PublicationRestart(300 + restarts, 301 + restarts, true, true);
            if (target != label) return result;
            Injected = true;
            return fault switch
            {
                "Null" => null, "OldPid" => result with { OldPid = 0 }, "NewPid" => result with { NewPid = 0 },
                "SamePid" => result with { NewPid = result.OldPid }, "OldExited" => result with { OldExited = false },
                "StorageReopened" => result with { StorageReopened = false }, _ => throw new InvalidOperationException(fault)
            };
        }
        /// <summary>Returns the independent response, optionally replacing one targeted observation.</summary>
        private Phase0PublicationReply? GetReply(string label)
        {
            var reply = Reply(point, label, variation);
            if (target != "Reply:" + label) return reply;
            Injected = true;
            return ReplyFaults(reply).Single(pair => pair.Name == fault).Value;
        }
    }
}
