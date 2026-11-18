namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Complete subscription observation for the scoped fixture, not a production DTO.</summary>
/// <param name="Node">Subscriber identity.</param><param name="Project">Logical project.</param><param name="Branch">Branch.</param>
/// <param name="HubEpoch">Current hub epoch.</param><param name="PublishedHead">Observed published head.</param>
/// <param name="PublishedGeneration">Published generation.</param><param name="Desired">Requested generation.</param>
/// <param name="Reported">Acknowledged generation.</param><param name="WorkerEpoch">Current worker fence.</param>
/// <param name="AcceptedHead">Last acknowledged head.</param>
/// <remarks>2026-11-17: FR-MCP-179/TR-MCP-FEDERATION-006/TEST-MCP-227; the three subscription-state criteria.</remarks>
internal sealed record Phase0SubscriptionRow(string Node, string Project, string Branch, string HubEpoch,
    string PublishedHead, long PublishedGeneration, long Desired, long Reported, long WorkerEpoch, string AcceptedHead);

/// <summary>Independent state, refs and mutation observations returned by a disposable probe.</summary>
/// <param name="Rows">All three fixture subscriptions.</param><param name="Heads">Complete local/public fixture refs.</param>
/// <param name="HistoryCommits">History commits created by the control operation.</param>
/// <param name="ControlMutations">Actual control-store writes, including transient writes.</param>
/// <param name="ControlFingerprint">Fingerprint including control fields omitted from the typed observation.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179; TEST-MCP-227.</remarks>
internal sealed record Phase0SubscriptionState(IReadOnlyList<Phase0SubscriptionRow>? Rows,
    IReadOnlyDictionary<string, string>? Heads, int HistoryCommits, int ControlMutations, string? ControlFingerprint);

/// <summary>Selected response fields asserted independently from durable subscription reads.</summary>
/// <param name="Node">Subscriber.</param><param name="Project">Project.</param><param name="Branch">Branch.</param>
/// <param name="HubEpoch">Hub epoch.</param><param name="Desired">Requested generation.</param>
/// <param name="Reported">Reported generation.</param><param name="WorkerEpoch">Worker epoch.</param>
/// <param name="Status">HTTP-style result.</param>
/// <remarks>2026-11-17: FR-MCP-179 ac-PollReadDoesNotAcknowledge/ac-AckOldGenerationPreservesNewRequest.</remarks>
internal sealed record Phase0SubscriptionReply(string Node, string Project, string Branch, string HubEpoch,
    long Desired, long Reported, long WorkerEpoch, int Status);

/// <summary>Control-state boundary; real bindings must coordinate an actual staged report before release.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007; FR-MCP-179; TR-MCP-FEDERATION-006; TEST-MCP-227.</remarks>
internal interface IPhase0SubscriptionStateProbe
{
    /// <summary>Initializes one independent poll, acknowledgement or missed-notification fixture.</summary><param name="scenario">poll, ack or reconcile.</param>
    void Prepare(string scenario);
    /// <summary>Reads actual durable state and refs independently of replies.</summary><returns>Complete fixture observation.</returns>
    Phase0SubscriptionState? ReadState();
    /// <summary>Polls without acknowledging work.</summary><returns>Selected response fields.</returns>
    Phase0SubscriptionReply? Poll();
    /// <summary>Holds report7 in flight before applying its acknowledgement.</summary>
    void StageReport7();
    /// <summary>Advances the independent publisher to A2 with generation8 while report7 waits.</summary>
    void AdvanceDesired8();
    /// <summary>Releases the staged report7 and waits for its durable acknowledgement.</summary><returns>Report response fields.</returns>
    Phase0SubscriptionReply? CompleteReport7();
    /// <summary>Reconciles remote A1 against stale control C0 without making a history commit.</summary>
    void Reconcile();
    /// <summary>Reopens persistence before the next independent read.</summary>
    void Reopen();
}

/// <summary>Reusable assertions for the three currently uncovered FR179 control-state criteria.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179 ac-PollReadDoesNotAcknowledge,
/// ac-AckOldGenerationPreservesNewRequest, ac-MissedNotificationReconciles; TR-MCP-FEDERATION-006; TEST-MCP-227.
/// Scripted evidence is not actual scheduling, durable control persistence or B11 acceptance.
/// </remarks>
internal static class Phase0SubscriptionStateContract
{
    private const string Epoch = "0fe46144-51d8-43d7-9544-ddca4ae15f84";

    /// <summary>Checks the two read-only polls and reopen against an independent complete baseline.</summary><param name="probe">Disposable boundary.</param>
    internal static void AssertPoll(IPhase0SubscriptionStateProbe probe)
    {
        probe.Prepare("poll");
        var before = AssertState(probe.ReadState(), "poll", 0);
        Assert.Equal(0, before.ControlMutations);
        for (var poll = 0; poll < 2; poll++)
        {
            Assert.Equal(new Phase0SubscriptionReply("A", "Atlas", "main", Epoch, 7, 6, 4, 200), probe.Poll());
            var after = AssertState(probe.ReadState(), "poll", poll + 1);
            Assert.Equal(before.ControlFingerprint, after.ControlFingerprint);
            Assert.Equal(0, after.ControlMutations);
        }
        probe.Reopen();
        var reopened = AssertState(probe.ReadState(), "poll", 3);
        Assert.Equal(before.ControlFingerprint, reopened.ControlFingerprint);
        Assert.Equal(0, reopened.ControlMutations);
    }

    /// <summary>Checks a real interleaving: report7 cannot erase the newer desired8 publication.</summary><param name="probe">Disposable boundary.</param>
    internal static void AssertLateReport(IPhase0SubscriptionStateProbe probe)
    {
        probe.Prepare("ack");
        AssertState(probe.ReadState(), "ack", 0);
        probe.StageReport7();
        probe.AdvanceDesired8();
        AssertState(probe.ReadState(), "ack", 1);
        Assert.Equal(new Phase0SubscriptionReply("A", "Atlas", "main", Epoch, 8, 7, 4, 200), probe.CompleteReport7());
        var after = AssertState(probe.ReadState(), "ack", 2);
        var row = Assert.Single(after.Rows!, item => item.Project == "Atlas" && item.Branch == "main");
        Assert.True(row.Reported < row.Desired);
        probe.Reopen();
        var reopened = AssertState(probe.ReadState(), "ack", 3);
        Assert.Equal(after.ControlFingerprint, reopened.ControlFingerprint);
    }

    /// <summary>Checks recovered published state, idempotent repeat and reopen without any new history.</summary><param name="probe">Disposable boundary.</param>
    internal static void AssertMissedNotification(IPhase0SubscriptionStateProbe probe)
    {
        probe.Prepare("reconcile");
        AssertState(probe.ReadState(), "reconcile", 0);
        probe.Reconcile();
        var after = AssertState(probe.ReadState(), "reconcile", 1);
        probe.Reconcile();
        var repeated = AssertState(probe.ReadState(), "reconcile", 2);
        Assert.Equal(after.Rows!.OrderBy(Key), repeated.Rows!.OrderBy(Key));
        Assert.Equal(after.ControlFingerprint, repeated.ControlFingerprint);
        probe.Reopen();
        var reopened = AssertState(probe.ReadState(), "reconcile", 3);
        Assert.Equal(after.Rows!.OrderBy(Key), reopened.Rows!.OrderBy(Key));
        Assert.Equal(after.ControlFingerprint, reopened.ControlFingerprint);
    }

    private static Phase0SubscriptionState AssertState(Phase0SubscriptionState? state, string scenario, int checkpoint)
    {
        Assert.NotNull(state);
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Phase0SubscriptionRow>>(state.Rows);
        Assert.Equal(3, rows.Count);
        Assert.Equal(new Phase0SubscriptionRow("A", "Atlas", "feature/search", Epoch, "F1", 3, 3, 3, 2, "F1"),
            Assert.Single(rows, row => row.Project == "Atlas" && row.Branch == "feature/search"));
        Assert.Equal(new Phase0SubscriptionRow("B", "Other", "main", Epoch, "X1", 11, 11, 10, 9, "X0"),
            Assert.Single(rows, row => row.Project == "Other"));
        var main = Assert.Single(rows, row => row.Project == "Atlas" && row.Branch == "main");
        if (scenario == "reconcile" && checkpoint > 0)
        {
            Assert.True(main.PublishedGeneration > 6);
            Assert.True(main.Desired > 6);
            Assert.Equal(new Phase0SubscriptionRow("A", "Atlas", "main", Epoch, "A1", main.PublishedGeneration, main.Desired, 6, 4, "C0"), main);
        }
        else
        {
            var initialReconcile = scenario == "reconcile";
            var advanced = scenario == "ack" && checkpoint > 0;
            Assert.Equal(new Phase0SubscriptionRow("A", "Atlas", "main", Epoch,
                initialReconcile ? "C0" : advanced ? "A2" : "A1", initialReconcile ? 6 : advanced ? 8 : 7,
                initialReconcile ? 6 : advanced ? 8 : 7, scenario == "ack" && checkpoint > 1 ? 7 : 6, 4,
                scenario == "ack" && checkpoint > 1 ? "A1" : "C0"), main);
        }
        var heads = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(state.Heads);
        var expected = new Dictionary<string, string>
        {
            ["local:Atlas/main"] = "C0", ["remote:Atlas/main"] = scenario == "ack" && checkpoint > 0 ? "A2" : "A1",
            ["remote:Atlas/feature/search"] = "F1", ["remote:Other/main"] = "X1"
        };
        Assert.Equal(expected.OrderBy(item => item.Key), heads.OrderBy(item => item.Key));
        Assert.Equal(0, state.HistoryCommits);
        Assert.False(string.IsNullOrWhiteSpace(state.ControlFingerprint));
        return state;
    }

    private static string Key(Phase0SubscriptionRow row) => row.Node + "/" + row.Project + "/" + row.Branch;
}