namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Test-owned allocation receipt with scope and immutable canonical identity.</summary>
/// <param name="RequestId">Allocation request.</param><param name="Project">Project.</param><param name="Tag">Creator.</param>
/// <param name="Kind">Entity kind.</param><param name="Sequence">Durable sequence.</param><param name="Id">Exact generated identity.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Supporting crash/concurrency design, not a public wire DTO.</remarks>
internal sealed record Phase0SafetyAllocation(string RequestId, string Project, string Tag, string Kind, long Sequence, string Id);

/// <summary>Independent durable allocator row.</summary>
/// <param name="Project">Project.</param><param name="Tag">Creator.</param><param name="Kind">Kind.</param><param name="HighWater">High water.</param>
/// <remarks>2026-11-19: TR-MCP-FEDERATION-009; branch-independent allocation design.</remarks>
internal sealed record Phase0SafetyCounter(string Project, string Tag, string Kind, long HighWater);

/// <summary>Complete readback, independent of an allocation response.</summary>
/// <param name="Safe">Whether old-tag authority was established.</param><param name="Counters">All fixture counters.</param>
/// <param name="Receipts">All durable allocation receipts.</param><param name="Content">All project rows.</param>
/// <param name="History">Byte-exact historical reference text.</param>
/// <remarks>2026-11-19: FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.</remarks>
internal sealed record Phase0SafetyState(bool Safe, IReadOnlyList<Phase0SafetyCounter>? Counters,
    IReadOnlyList<Phase0SafetyAllocation>? Receipts, IReadOnlyList<string>? Content, string History);

/// <summary>Normalized local observation. Refused means no identity was issued; it assigns no HTTP code.</summary>
/// <param name="Outcome">Issued, ReplyLost, Refused, ContentRejected or Applied.</param><param name="Allocation">Issued receipt only.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; later public mapping remains with the existing typed-contract owner.</remarks>
internal sealed record Phase0SafetyReply(string Outcome, Phase0SafetyAllocation? Allocation);

/// <summary>Exact test-owned command input, not a new product endpoint.</summary>
/// <param name="Action">Fixture action.</param><param name="RequestId">Stable request.</param><param name="Tag">Creator.</param>
/// <param name="HighWater">Authoritative recovery evidence, when applicable.</param>
/// <param name="Project">Explicit immutable project scope.</param><param name="Kind">Explicit allocator entity kind.</param>
/// <remarks>2026-11-19: TR-MCP-FEDERATION-009; inputs retain allocator key Atlas/Todo explicitly in the adapter contract.</remarks>
internal sealed record Phase0SafetyCommand(string Action, string RequestId, string Tag = "LA", long HighWater = 0,
    string Project = "Atlas", string Kind = "Todo");

/// <summary>Disposable control/project adapter boundary; no implementation ships in this mock slice.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal interface IPhase0AllocatorSafetyProbe
{
    /// <summary>Seeds Atlas/Todo plus an unrelated Other/LA/Todo counter and legacy history.</summary>
    /// <param name="scenario">Concurrent, LostContent, Restore or Exhausted.</param>
    void Prepare(string scenario);
    /// <summary>Executes one exact test-owned command; concurrent initial allocations must overlap.</summary>
    /// <param name="command">Captured input.</param><returns>Actual observation; no fabricated public status.</returns>
    Task<Phase0SafetyReply?> ExecuteAsync(Phase0SafetyCommand command);
    /// <summary>Reads complete fixture counters, receipts, project content and history through a separate connection.</summary>
    /// <returns>Actual durable state.</returns>
    Phase0SafetyState? ReadState();
    /// <summary>Reopens control and project readers without resetting either store.</summary>
    void Reopen();
}

/// <summary>Requirement-derived assertions shared by scripted observations and later disposable native bindings.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. No new public code, contiguous-sequence requirement or cross-store atomicity.</remarks>
internal static class Phase0AllocatorSafetyContract
{
    private const string Historical = "commit IMPL-UNITGREEN-001\nPR TODO-7\nsession/memory/document TODO-7 café 雪";

    /// <summary>Checks uniqueness, atomic receipt/high-water visibility and durability with concurrent requests.</summary>
    /// <param name="probe">Independent adapter.</param><returns>Completion after reopen readback.</returns>
    internal static async Task AssertConcurrentAsync(IPhase0AllocatorSafetyProbe probe)
    {
        probe.Prepare("Concurrent"); CheckState(probe.ReadState(), true, 42, []);
        var a = probe.ExecuteAsync(new("Allocate", "a"));
        var b = probe.ExecuteAsync(new("Allocate", "b"));
        var replies = await Task.WhenAll(a, b);
        var first = Issued(replies[0], "a", "LA", 42);
        var second = Issued(replies[1], "b", "LA", 42);
        Assert.NotEqual(first.Sequence, second.Sequence); Assert.NotEqual(first.Id, second.Id);
        var receipts = new[] { first, second };
        CheckState(probe.ReadState(), true, Math.Max(first.Sequence, second.Sequence), receipts);
        probe.Reopen(); CheckState(probe.ReadState(), true, Math.Max(first.Sequence, second.Sequence), receipts);
    }

    /// <summary>Lost reply retries reuse the durable allocation; a failed content write consumes its number.</summary>
    /// <param name="probe">Separate control/project boundary.</param><returns>Completion.</returns>
    internal static async Task AssertLostContentAsync(IPhase0AllocatorSafetyProbe probe)
    {
        probe.Prepare("LostContent"); CheckState(probe.ReadState(), true, 42, []);
        Reply(await probe.ExecuteAsync(new("AllocateLoseReply", "a")), "ReplyLost");
        var captured = Assert.IsType<Phase0SafetyState>(probe.ReadState());
        var stored = Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<Phase0SafetyAllocation>>(captured.Receipts));
        Allocation(stored, "a", "LA", 42); CheckState(captured, true, stored.Sequence, [stored]);
        var retry = Issued(await probe.ExecuteAsync(new("Allocate", "a")), "a", "LA", 42);
        Assert.Equal(stored, retry); CheckState(probe.ReadState(), true, stored.Sequence, [stored]);
        Reply(await probe.ExecuteAsync(new("FailProjectContent", "a")), "ContentRejected");
        CheckState(probe.ReadState(), true, stored.Sequence, [stored]);
        var next = Issued(await probe.ExecuteAsync(new("Allocate", "b")), "b", "LA", stored.Sequence);
        Assert.NotEqual(stored.Id, next.Id);
        CheckState(probe.ReadState(), true, next.Sequence, [stored, next]);
        probe.Reopen(); CheckState(probe.ReadState(), true, next.Sequence, [stored, next]);
    }

    /// <summary>Unsafe restored old tag refuses issuance; authoritative high water restores safe creation.</summary>
    /// <param name="probe">Restored control/project fixture.</param><returns>Completion.</returns>
    internal static async Task AssertRestoreAsync(IPhase0AllocatorSafetyProbe probe)
    {
        probe.Prepare("Restore"); CheckState(probe.ReadState(), false, 42, []);
        Reply(await probe.ExecuteAsync(new("Allocate", "blocked")), "Refused");
        CheckState(probe.ReadState(), false, 42, []);
        Reply(await probe.ExecuteAsync(new("EstablishAuthoritativeHighWater", "recover", HighWater: 100)), "Applied");
        CheckState(probe.ReadState(), true, 100, []);
        var row = Issued(await probe.ExecuteAsync(new("Allocate", "a")), "a", "LA", 100);
        CheckState(probe.ReadState(), true, row.Sequence, [row]);
        probe.Reopen(); CheckState(probe.ReadState(), true, row.Sequence, [row]);
    }

    /// <summary>Int64 exhaustion cannot wrap; a fresh never-reused tag starts independently without rewriting originals.</summary>
    /// <param name="probe">Exhausted old-tag fixture.</param><returns>Completion.</returns>
    internal static async Task AssertExhaustedAsync(IPhase0AllocatorSafetyProbe probe)
    {
        probe.Prepare("Exhausted"); CheckState(probe.ReadState(), true, long.MaxValue, []);
        Reply(await probe.ExecuteAsync(new("Allocate", "blocked")), "Refused");
        CheckState(probe.ReadState(), true, long.MaxValue, []);
        Reply(await probe.ExecuteAsync(new("EnrollFreshNeverReusedTag", "enroll", "LG")), "Applied");
        CheckState(probe.ReadState(), true, long.MaxValue, [], 0);
        var row = Issued(await probe.ExecuteAsync(new("Allocate", "a", "LG")), "a", "LG", 0);
        Assert.Equal(1, row.Sequence);
        CheckState(probe.ReadState(), true, long.MaxValue, [row], 1);
        probe.Reopen(); CheckState(probe.ReadState(), true, long.MaxValue, [row], 1);
    }

    private static void Reply(Phase0SafetyReply? reply, string outcome)
    { Assert.NotNull(reply); Assert.Equal(outcome, reply.Outcome); Assert.Null(reply.Allocation); }

    private static Phase0SafetyAllocation Issued(Phase0SafetyReply? reply, string request, string tag, long previous)
    {
        Assert.NotNull(reply); Assert.Equal("Issued", reply.Outcome);
        var row = Assert.IsType<Phase0SafetyAllocation>(reply.Allocation); Allocation(row, request, tag, previous); return row;
    }

    private static void Allocation(Phase0SafetyAllocation row, string request, string tag, long previous)
    {
        Assert.Equal(request, row.RequestId); Assert.Equal("Atlas", row.Project); Assert.Equal("Todo", row.Kind);
        Assert.Equal(tag, row.Tag); Assert.True(row.Sequence > previous);
        Assert.Equal("EXEC-TODO-" + tag + "-" + row.Sequence.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), row.Id);
    }

    private static void CheckState(Phase0SafetyState? state, bool safe, long oldHighWater,
        IReadOnlyList<Phase0SafetyAllocation> expected, long? freshHighWater = null)
    {
        Assert.NotNull(state); Assert.Equal(safe, state.Safe); Assert.Equal(Historical, state.History);
        Assert.Equal(new[] { "legacy|IMPL-UNITGREEN-001|TODO-7|café 雪" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(state.Content));
        var counters = Assert.IsAssignableFrom<IReadOnlyList<Phase0SafetyCounter>>(state.Counters);
        var required = new List<Phase0SafetyCounter> { new("Atlas", "LA", "Todo", oldHighWater), new("Other", "LA", "Todo", 9) };
        if (freshHighWater.HasValue) required.Add(new("Atlas", "LG", "Todo", freshHighWater.Value));
        Assert.Equal(required.OrderBy(Key).ToArray(), counters.OrderBy(Key).ToArray());
        var rows = Assert.IsAssignableFrom<IReadOnlyList<Phase0SafetyAllocation>>(state.Receipts);
        Assert.Equal(expected.OrderBy(row => row.RequestId, StringComparer.Ordinal).ToArray(), rows.OrderBy(row => row.RequestId, StringComparer.Ordinal).ToArray());
    }

    private static string Key(Phase0SafetyCounter counter) => counter.Project + "/" + counter.Tag + "/" + counter.Kind;
}

/// <summary>Independent finite scripts prove allocator assertions reject individual wrong observations.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-TwoNodesCreateDistinctIds/ac-LegacyIdsRemainValid;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Scripted snapshots are not a counter implementation or native proof.</remarks>
public sealed class Phase0AllocatorSafetyMockGateTests
{
    /// <summary>Gets the four finite scenarios.</summary>
    public static IEnumerable<object[]> Scenarios => new[] { "Concurrent", "LostContent", "Restore", "Exhausted" }.Select(value => new object[] { value });

    /// <summary>Gets a single corrupt reply or complete durable snapshot at every checkpoint.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var scenario in new[] { "Concurrent", "LostContent", "Restore", "Exhausted" })
            {
                var replies = scenario == "Concurrent" ? 2 : scenario == "LostContent" ? 4 : 3;
                var reads = scenario == "Concurrent" ? 3 : scenario == "LostContent" ? 6 : 5;
                for (var index = 0; index < replies; index++)
                {
                    foreach (var facet in new[] { "Missing", "Outcome", "Allocation" }) yield return [scenario, "Reply", index, facet];
                    var issued = scenario == "Concurrent" || (scenario == "LostContent" ? index is 1 or 3 : index == 2);
                    if (issued) foreach (var facet in new[] { "Request", "Project", "Tag", "Kind", "Sequence", "Id" })
                        yield return [scenario, "Reply", index, facet];
                }
                var receiptCounts = scenario switch { "Concurrent" => new[] { 0, 2, 2 }, "LostContent" => new[] { 0, 1, 1, 1, 2, 2 }, _ => new[] { 0, 0, 0, 1, 1 } };
                for (var index = 0; index < reads; index++)
                {
                    foreach (var facet in new[] { "Missing", "Safe", "HighWater", "Foreign", "MissingCounter", "ExtraCounter", "CounterScope", "MissingReceipts", "ExtraReceipt", "Content", "History" })
                        yield return [scenario, "State", index, facet];
                    for (var row = 0; row < receiptCounts[index]; row++)
                    foreach (var facet in new[] { "Request", "Project", "Tag", "Kind", "Sequence", "Id" })
                        yield return [scenario, "State", index, "Receipt/" + row + "/" + facet];
                }
            }
        }
    }

    /// <summary>Valid scripted receipts pass, including allowed gaps and either concurrent sequence ordering.</summary>
    /// <param name="scenario">Scenario.</param><returns>Completion.</returns>
    [Theory, MemberData(nameof(Scenarios))]
    public async Task CorrectObservationsPass(string scenario)
    {
        foreach (var gap in new long[] { 1, 5 })
        {
            var probe = new Probe(gap: gap); await Run(scenario, probe);
            Assert.True(probe.Reopened); Assert.Equal(scenario == "Concurrent" ? 2 : scenario == "LostContent" ? 4 : 3, probe.Commands);
            if (scenario == "Concurrent") Assert.True(probe.BothInitialRequestsArrived);
        }
    }

    /// <summary>Each wrong observation must reach and fail an xUnit assertion, not any arbitrary exception.</summary>
    /// <param name="scenario">Scenario.</param><param name="side">Reply or State.</param><param name="index">Checkpoint.</param>
    /// <param name="facet">Single corruption.</param><returns>Completion.</returns>
    [Theory, MemberData(nameof(Faults))]
    public async Task WrongObservationIsRejected(string scenario, string side, int index, string facet)
    {
        var probe = new Probe(side, index, facet);
        await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() => Run(scenario, probe));
        Assert.True(probe.Injected, "Requested negative control was not exercised.");
    }

    private static Task Run(string scenario, IPhase0AllocatorSafetyProbe probe) => scenario switch
    {
        "Concurrent" => Phase0AllocatorSafetyContract.AssertConcurrentAsync(probe),
        "LostContent" => Phase0AllocatorSafetyContract.AssertLostContentAsync(probe),
        "Restore" => Phase0AllocatorSafetyContract.AssertRestoreAsync(probe),
        "Exhausted" => Phase0AllocatorSafetyContract.AssertExhaustedAsync(probe),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario))
    };

    private sealed class Probe(string side = "", int faultIndex = 0, string facet = "", long gap = 1) : IPhase0AllocatorSafetyProbe
    {
        private string scenario = "";
        private int reads;
        private readonly TaskCompletionSource both = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Commands { get; private set; }
        internal bool Reopened { get; private set; }
        internal bool Injected { get; private set; }
        internal bool BothInitialRequestsArrived { get; private set; }
        public void Prepare(string value) { scenario = value; }
        public async Task<Phase0SafetyReply?> ExecuteAsync(Phase0SafetyCommand command)
        {
            var index = Commands++;
            Assert.Equal(ExpectedCommands()[index], command);
            if (scenario == "Concurrent")
            {
                if (Commands == 2) { BothInitialRequestsArrived = true; both.SetResult(); }
                await both.Task;
            }
            var correct = Replies()[index];
            if (side != "Reply" || index != faultIndex) return correct;
            Injected = true;
            return facet switch
            {
                "Missing" => null, "Outcome" => correct with { Outcome = "wrong" },
                "Allocation" => correct with { Allocation = correct.Allocation is null ? A("unexpected", "LA", 1) : null },
                "Request" or "Project" or "Tag" or "Kind" or "Sequence" or "Id" => correct with { Allocation = Corrupt(correct.Allocation!, facet) },
                _ => correct
            };
        }
        public Phase0SafetyState? ReadState()
        {
            var index = reads++; var correct = States()[index];
            if (side != "State" || index != faultIndex) return correct;
            Injected = true;
            if (facet.StartsWith("Receipt/", StringComparison.Ordinal))
            {
                var parts = facet.Split('/'); var rowIndex = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                return correct with { Receipts = correct.Receipts!.Select((row, position) => position == rowIndex ? Corrupt(row, parts[2]) : row).ToArray() };
            }
            return facet switch
            {
                "Missing" => null, "Safe" => correct with { Safe = !correct.Safe },
                "HighWater" => correct with { Counters = correct.Counters!.Select(row => row.Project == "Atlas" && row.Tag == "LA" ? row with { HighWater = row.HighWater == long.MaxValue ? 0 : row.HighWater + 1 } : row).ToArray() },
                "Foreign" => correct with { Counters = correct.Counters!.Select(row => row.Project == "Other" ? row with { HighWater = 10 } : row).ToArray() },
                "MissingCounter" => correct with { Counters = null },
                "ExtraCounter" => correct with { Counters = correct.Counters!.Append(new("Extra", "LA", "Todo", 7)).ToArray() },
                "CounterScope" => correct with { Counters = correct.Counters!.Select(row => row with { Kind = "Memory" }).ToArray() },
                "MissingReceipts" => correct with { Receipts = null },
                "ExtraReceipt" => correct with { Receipts = correct.Receipts!.Append(A("unexpected", "LA", 2)).ToArray() },
                "Content" => correct with { Content = ["lost or changed"] }, "History" => correct with { History = correct.History + "rewrite" }, _ => correct
            };
        }
        public void Reopen() { Assert.False(Reopened); Reopened = true; }
        private Phase0SafetyCommand[] ExpectedCommands() => scenario switch
        {
            "Concurrent" => [new("Allocate", "a"), new("Allocate", "b")],
            "LostContent" => [new("AllocateLoseReply", "a"), new("Allocate", "a"), new("FailProjectContent", "a"), new("Allocate", "b")],
            "Restore" => [new("Allocate", "blocked"), new("EstablishAuthoritativeHighWater", "recover", HighWater: 100), new("Allocate", "a")],
            _ => [new("Allocate", "blocked"), new("EnrollFreshNeverReusedTag", "enroll", "LG"), new("Allocate", "a", "LG")]
        };
        private Phase0SafetyReply[] Replies() => scenario switch
        {
            "Concurrent" => [new("Issued", A("a", "LA", 42 + gap + 1)), new("Issued", A("b", "LA", 42 + gap))],
            "LostContent" => [new("ReplyLost", null), new("Issued", A("a", "LA", 42 + gap)), new("ContentRejected", null), new("Issued", A("b", "LA", 43 + gap))],
            "Restore" => [new("Refused", null), new("Applied", null), new("Issued", A("a", "LA", 100 + gap))],
            _ => [new("Refused", null), new("Applied", null), new("Issued", A("a", "LG", 1))]
        };
        private Phase0SafetyState[] States() => scenario switch
        {
            "Concurrent" => [S(true, 42), S(true, 43 + gap, [A("a", "LA", 43 + gap), A("b", "LA", 42 + gap)]), S(true, 43 + gap, [A("a", "LA", 43 + gap), A("b", "LA", 42 + gap)])],
            "LostContent" => [S(true, 42), S(true, 42 + gap, [A("a", "LA", 42 + gap)]), S(true, 42 + gap, [A("a", "LA", 42 + gap)]), S(true, 42 + gap, [A("a", "LA", 42 + gap)]), S(true, 43 + gap, [A("a", "LA", 42 + gap), A("b", "LA", 43 + gap)]), S(true, 43 + gap, [A("a", "LA", 42 + gap), A("b", "LA", 43 + gap)])],
            "Restore" => [S(false, 42), S(false, 42), S(true, 100), S(true, 100 + gap, [A("a", "LA", 100 + gap)]), S(true, 100 + gap, [A("a", "LA", 100 + gap)])],
            _ => [S(true, long.MaxValue), S(true, long.MaxValue), S(true, long.MaxValue, [], 0), S(true, long.MaxValue, [A("a", "LG", 1)], 1), S(true, long.MaxValue, [A("a", "LG", 1)], 1)]
        };
        /// <summary>Changes one independently observed allocation field without deriving an expected value from it.</summary>
        private static Phase0SafetyAllocation Corrupt(Phase0SafetyAllocation row, string dimension) => dimension switch
        {
            "Request" => row with { RequestId = "wrong" }, "Project" => row with { Project = "Other" },
            "Tag" => row with { Tag = "ZZ" }, "Kind" => row with { Kind = "Memory" },
            "Sequence" => row with { Sequence = 0 }, "Id" => row with { Id = "EXEC-TODO-LA-000" }, _ => row
        };
        /// <summary>Independently authored scripted fixture allocation.</summary>
        private static Phase0SafetyAllocation A(string request, string tag, long sequence) => new(request, "Atlas", tag, "Todo", sequence,
            "EXEC-TODO-" + tag + "-" + sequence.ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
        private static Phase0SafetyState S(bool safe, long highWater, Phase0SafetyAllocation[]? receipts = null, long? fresh = null)
        {
            var counters = new List<Phase0SafetyCounter> { new("Other", "LA", "Todo", 9), new("Atlas", "LA", "Todo", highWater) };
            if (fresh.HasValue) counters.Add(new("Atlas", "LG", "Todo", fresh.Value));
            return new(safe, counters, receipts ?? [], ["legacy|IMPL-UNITGREEN-001|TODO-7|café 雪"], "commit IMPL-UNITGREEN-001\nPR TODO-7\nsession/memory/document TODO-7 café 雪");
        }
    }
}
