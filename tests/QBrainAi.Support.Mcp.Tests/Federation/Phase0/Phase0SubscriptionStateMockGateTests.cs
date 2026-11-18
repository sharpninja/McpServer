namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripts discriminate reply, durable subscription, ref and history defects.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179 ac-PollReadDoesNotAcknowledge,
/// ac-AckOldGenerationPreservesNewRequest, ac-MissedNotificationReconciles; TR-MCP-FEDERATION-006; TEST-MCP-227.
/// This validates assertions with mocks only; no database, actual publisher, scheduler or installed plugin is bound.
/// </remarks>
public sealed class Phase0SubscriptionStateMockGateTests
{
    /// <summary>Literal correct scripts allow either row order and generation gaps when only advancement is required.</summary>
    /// <param name="scenario">Criterion fixture.</param><param name="reverse">Valid reverse row order.</param>
    /// <param name="gap">Valid nonconsecutive reconciliation generation.</param>
    [Theory]
    [InlineData("poll", false, false)] [InlineData("poll", true, false)]
    [InlineData("ack", false, false)] [InlineData("ack", true, false)]
    [InlineData("reconcile", false, false)] [InlineData("reconcile", true, false)]
    [InlineData("reconcile", false, true)] [InlineData("reconcile", true, true)]
    public void CorrectSubscriptionStatePasses(string scenario, bool reverse, bool gap)
    {
        var probe = new SubscriptionProbe(scenario, reverse: reverse, gap: gap);
        Run(scenario, probe);
        Assert.Equal(scenario switch
        {
            "poll" => new[] { "prepare:poll", "state:0", "poll:0", "state:1", "poll:1", "state:2", "reopen", "state:3" },
            "ack" => ["prepare:ack", "state:0", "stage:7", "advance:8", "state:1", "complete:7", "state:2", "reopen", "state:3"],
            _ => ["prepare:reconcile", "state:0", "reconcile", "state:1", "reconcile", "state:2", "reopen", "state:3"]
        }, probe.Calls);
    }

    /// <summary>Gets each independently corrupted observation; expected negatives catch only assertion failures.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var scenario in new[] { "poll", "ack", "reconcile" })
            {
                for (var checkpoint = 0; checkpoint < 4; checkpoint++)
                {
                    foreach (var fault in new[] { "Null", "RowsNull", "MissingRow", "DuplicateRow", "ExtraRow", "Node", "Project", "Branch", "Epoch", "PublishedHead", "PublishedGeneration", "Desired", "Reported", "WorkerEpoch", "AcceptedHead", "Unrelated", "HeadsNull", "HeadMissing", "HeadExtra", "LocalHead", "RemoteHead", "Commits", "EmptyFingerprint" })
                        yield return [scenario, "state:" + checkpoint + ":" + fault];
                    if (scenario == "poll") yield return [scenario, "state:" + checkpoint + ":ControlMutations"];
                    if ((scenario == "poll" && checkpoint > 0) || (scenario == "ack" && checkpoint == 3) || (scenario == "reconcile" && checkpoint > 1))
                        yield return [scenario, "state:" + checkpoint + ":ChangedFingerprint"];
                }
                for (var response = 0; response < (scenario == "poll" ? 2 : scenario == "ack" ? 1 : 0); response++)
                    foreach (var fault in new[] { "Null", "Node", "Project", "Branch", "Epoch", "Desired", "Reported", "WorkerEpoch", "Status" })
                        yield return [scenario, "reply:" + response + ":" + fault];
            }
        }
    }

    /// <summary>Each defect must be rejected by the same assertions used by a future real adapter.</summary>
    /// <param name="scenario">Criterion fixture.</param><param name="fault">Single independent defect.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongSubscriptionStateIsRejected(string scenario, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Run(scenario, new SubscriptionProbe(scenario, fault)));

    private static void Run(string scenario, IPhase0SubscriptionStateProbe probe)
    {
        switch (scenario)
        {
            case "poll": Phase0SubscriptionStateContract.AssertPoll(probe); break;
            case "ack": Phase0SubscriptionStateContract.AssertLateReport(probe); break;
            case "reconcile": Phase0SubscriptionStateContract.AssertMissedNotification(probe); break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    /// <summary>Canned control responses and separate durable reads; no production algorithm supplies expectations.</summary>
    private sealed class SubscriptionProbe(string scenario, string fault = "", bool reverse = false, bool gap = false) : IPhase0SubscriptionStateProbe
    {
        private int stateRead;
        private int replyRead;
        /// <summary>Gets the ordered probe boundary calls.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(string value) { Assert.Equal(scenario, value); stateRead = replyRead = 0; Calls.Clear(); Calls.Add("prepare:" + value); }
        /// <inheritdoc />
        public Phase0SubscriptionState? ReadState()
        {
            var checkpoint = stateRead++; Calls.Add("state:" + checkpoint);
            var active = fault.StartsWith("state:" + checkpoint + ":", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
            var row = new Phase0SubscriptionRow("A", "Atlas", "main", "0fe46144-51d8-43d7-9544-ddca4ae15f84", "A1", 7, 7, 6, 4, "C0");
            var fingerprint = "control-7-6";
            if (scenario == "ack" && checkpoint > 0)
            {
                row = row with { PublishedHead = "A2", PublishedGeneration = 8, Desired = 8, Reported = checkpoint > 1 ? 7 : 6, AcceptedHead = checkpoint > 1 ? "A1" : "C0" };
                fingerprint = checkpoint > 1 ? "control-8-7" : "control-8-6";
            }
            if (scenario == "reconcile")
            {
                row = checkpoint == 0 ? row with { PublishedHead = "C0", PublishedGeneration = 6, Desired = 6 }
                    : row with { PublishedGeneration = gap ? 12 : 7, Desired = gap ? 15 : 7 };
                fingerprint = checkpoint == 0 ? "control-6-6" : "control-reconciled";
            }
            row = active switch
            {
                "Node" => row with { Node = "wrong" }, "Project" => row with { Project = "wrong" }, "Branch" => row with { Branch = "wrong" },
                "Epoch" => row with { HubEpoch = "wrong" }, "PublishedHead" => row with { PublishedHead = "wrong" },
                "PublishedGeneration" => row with { PublishedGeneration = scenario == "reconcile" && checkpoint > 0 ? 6 : row.PublishedGeneration + 1 },
                "Desired" => row with { Desired = scenario == "reconcile" && checkpoint > 0 ? 6 : row.Desired + 1 },
                "Reported" => row with { Reported = row.Reported + 1 }, "WorkerEpoch" => row with { WorkerEpoch = 5 },
                "AcceptedHead" => row with { AcceptedHead = "wrong" }, _ => row
            };
            var feature = new Phase0SubscriptionRow("A", "Atlas", "feature/search", "0fe46144-51d8-43d7-9544-ddca4ae15f84", "F1", 3, 3, 3, 2, "F1");
            var other = new Phase0SubscriptionRow("B", "Other", "main", "0fe46144-51d8-43d7-9544-ddca4ae15f84", "X1", 11, 11, 10, 9, "X0");
            if (active == "Unrelated") other = other with { Reported = 11 };
            IReadOnlyList<Phase0SubscriptionRow>? rows = active switch
            {
                "RowsNull" => null, "MissingRow" => [row, feature], "DuplicateRow" => [row, feature, row],
                "ExtraRow" => [row, feature, other, other], _ => reverse ? [other, feature, row] : [row, feature, other]
            };
            var heads = new Dictionary<string, string>
            {
                ["local:Atlas/main"] = "C0", ["remote:Atlas/main"] = scenario == "ack" && checkpoint > 0 ? "A2" : "A1",
                ["remote:Atlas/feature/search"] = "F1", ["remote:Other/main"] = "X1"
            };
            if (active == "HeadMissing") heads.Remove("remote:Other/main");
            if (active == "HeadExtra") heads["unexpected/main"] = "bad";
            if (active == "LocalHead") heads["local:Atlas/main"] = "changed";
            if (active == "RemoteHead") heads["remote:Atlas/main"] = "changed";
            return active == "Null" ? null : new Phase0SubscriptionState(rows, active == "HeadsNull" ? null : heads,
                active == "Commits" ? 1 : 0, active == "ControlMutations" ? 1 : 0,
                active == "EmptyFingerprint" ? null : active == "ChangedFingerprint" ? "changed" : fingerprint);
        }
        /// <inheritdoc />
        public Phase0SubscriptionReply? Poll() { Calls.Add("poll:" + replyRead); return Reply(); }
        /// <inheritdoc />
        public void StageReport7() => Calls.Add("stage:7");
        /// <inheritdoc />
        public void AdvanceDesired8() => Calls.Add("advance:8");
        /// <inheritdoc />
        public Phase0SubscriptionReply? CompleteReport7() { Calls.Add("complete:7"); return Reply(); }
        /// <inheritdoc />
        public void Reconcile() => Calls.Add("reconcile");
        /// <inheritdoc />
        public void Reopen() => Calls.Add("reopen");

        private Phase0SubscriptionReply? Reply()
        {
            var current = replyRead++;
            var active = fault.StartsWith("reply:" + current + ":", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
            var value = new Phase0SubscriptionReply("A", "Atlas", "main", "0fe46144-51d8-43d7-9544-ddca4ae15f84", scenario == "ack" ? 8 : 7, scenario == "ack" ? 7 : 6, 4, 200);
            return active switch
            {
                "Null" => null, "Node" => value with { Node = "wrong" }, "Project" => value with { Project = "wrong" },
                "Branch" => value with { Branch = "wrong" }, "Epoch" => value with { HubEpoch = "wrong" },
                "Desired" => value with { Desired = value.Desired + 1 }, "Reported" => value with { Reported = value.Reported + 1 },
                "WorkerEpoch" => value with { WorkerEpoch = 5 }, "Status" => value with { Status = 500 }, _ => value
            };
        }
    }
}