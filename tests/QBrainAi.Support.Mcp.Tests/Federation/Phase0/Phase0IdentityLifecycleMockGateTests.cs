namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripts prove lifecycle assertions discriminate corrupt outcomes without a production allocator.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; FR-MCP-182 ac-BranchRevertDoesNotReuseSequence,
/// ac-SimultaneousHumanIdReservation, ac-OfflineHumanIdPublicationDenied;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. Mock evidence does not establish real Dolt or hub behavior.
/// </remarks>
public sealed class Phase0IdentityLifecycleMockGateTests
{
    /// <summary>Revert retains high water, and valid gaps do not invent a consecutive-allocation requirement.</summary>
    /// <param name="sequence">Independent valid next allocation.</param>
    [Theory]
    [InlineData(43L)]
    [InlineData(47L)]
    public void CorrectBranchRevertPasses(long sequence)
    {
        var probe = new AllocatorProbe(sequence: sequence);
        Phase0IdentityLifecycleContract.AssertBranchRevert(probe);
        Assert.Equal(new[] { "prepare42", "highwater", "revert-switch", "highwater", "create", "highwater", "rows" }, probe.Calls);
    }

    /// <summary>Either node can win; both initial requests arrive before either reply is released.</summary>
    /// <param name="winner">Independent winning request index.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CorrectConcurrentReservationPasses(int winner)
    {
        var probe = new ReservationProbe(winner);
        await Phase0IdentityLifecycleContract.AssertConcurrentReservationAsync(probe);
        Assert.Equal(3, probe.Requests);
        Assert.True(probe.BothInitialRequestsArrived);
        Assert.Equal(3, probe.RowReads);
    }

    /// <summary>Rejected publication retains Unicode local content, remote head and unreserved status.</summary>
    [Fact]
    public void CorrectOfflinePublicationPasses()
    {
        var probe = new OfflineProbe();
        Phase0IdentityLifecycleContract.AssertOfflineHumanPublication(probe);
        Assert.Equal(1, probe.Publications);
        Assert.Equal(2, probe.LocalReads);
        Assert.Equal(2, probe.HeadReads);
        Assert.Equal(2, probe.ReservationReads);
    }

    /// <summary>Gets every allocator checkpoint, creation field and independent persisted-row fault.</summary>
    public static IEnumerable<object[]> AllocatorFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 3; checkpoint++)
                yield return ["HighWater:" + checkpoint];
            foreach (var fault in new[] { "Missing", "EmptyId", "WhitespaceId", "Origin", "ReusedSequence", "LowerSequence", "Title" })
                yield return ["Create:" + fault];
            foreach (var fault in new[] { "Missing", "Empty", "Extra", "Id", "Origin", "Sequence", "Title" })
                yield return ["Rows:" + fault];
        }
    }

    /// <summary>Gets individual concurrent replies, receipt fields and durable read faults, for either winning node.</summary>
    public static IEnumerable<object[]> ReservationFaults
    {
        get
        {
            for (var winner = 0; winner < 2; winner++)
            {
                foreach (var fault in new[] { "Missing", "Status", "Code", "Reservation", "Project", "Kind", "HumanId", "Node", "RequestId", "ReceiptId", "WhitespaceReceipt" })
                    yield return [winner, "Winner:" + fault];
                foreach (var fault in new[] { "Missing", "Status", "Code" })
                    yield return [winner, "Loser:" + fault];
                foreach (var fault in new[] { "Missing", "Status", "Code", "Reservation", "Project", "Kind", "HumanId", "Node", "RequestId", "ReceiptId", "WhitespaceReceipt" })
                    yield return [winner, "Retry:" + fault];
                for (var checkpoint = 0; checkpoint < 3; checkpoint++)
                {
                    yield return [winner, "Rows:" + checkpoint + ":Missing"];
                    yield return [winner, "Rows:" + checkpoint + ":Extra"];
                    if (checkpoint == 0) continue;
                    foreach (var fault in new[] { "Empty", "Project", "Kind", "HumanId", "Node", "RequestId", "ReceiptId", "WhitespaceReceipt" })
                        yield return [winner, "Rows:" + checkpoint + ":" + fault];
                }
            }
        }
    }

    /// <summary>Gets every pre/post local field and remote state fault, plus missing and incorrect rejection responses.</summary>
    public static IEnumerable<object[]> OfflineFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 2; checkpoint++)
            {
                foreach (var fault in new[] { "Missing", "CanonicalId", "RequestedHumanId", "Title", "Origin" })
                    yield return ["Local:" + checkpoint + ":" + fault];
                foreach (var fault in new[] { "Missing", "Wrong" })
                    yield return ["Head:" + checkpoint + ":" + fault];
                foreach (var fault in new[] { "Missing", "Reserved" })
                    yield return ["Reservations:" + checkpoint + ":" + fault];
            }
            foreach (var fault in new[] { "Missing", "Status", "Code" })
                yield return ["Response:" + fault];
        }
    }

    /// <summary>Sequence rollback, corrupt creation and mismatched durable content must fail assertions.</summary>
    /// <param name="fault">Single independent defect.</param>
    [Theory, MemberData(nameof(AllocatorFaults))]
    public void WrongAllocatorOutcomeIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityLifecycleContract.AssertBranchRevert(new AllocatorProbe(fault)));

    /// <summary>Concurrent outcomes, persisted uniqueness and winning retry must all be checked.</summary>
    /// <param name="winner">Fixture winner index.</param><param name="fault">Single independent defect.</param>
    [Theory, MemberData(nameof(ReservationFaults))]
    public async Task WrongReservationOutcomeIsRejected(int winner, string fault)
        => await Assert.ThrowsAnyAsync<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityLifecycleContract.AssertConcurrentReservationAsync(new ReservationProbe(winner, fault)));

    /// <summary>Publication rejection must not conceal local data loss, a changed remote or an implicit reservation.</summary>
    /// <param name="fault">Single independent defect.</param>
    [Theory, MemberData(nameof(OfflineFaults))]
    public void WrongOfflineOutcomeIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityLifecycleContract.AssertOfflineHumanPublication(new OfflineProbe(fault)));

    /// <summary>Independent revert and allocation script; no real counter or branch implementation.</summary>
    private sealed class AllocatorProbe(string fault = "", long sequence = 43) : IPhase0AllocatorLifecycleProbe
    {
        private int reads;
        private bool created;
        /// <summary>Gets actual calls to verify that independent readbacks were exercised.</summary>
        internal List<string> Calls { get; } = [];
        private Phase0AllocatedIdentity Row => new("fixture-after-revert", "LA", sequence, "task after revert");
        /// <inheritdoc />
        public void PrepareAllocated42() { Calls.Add("prepare42"); reads = 0; created = false; }
        /// <inheritdoc />
        public long ReadHighWater()
        {
            Calls.Add("highwater");
            var checkpoint = reads++;
            if (fault == "HighWater:" + checkpoint) return checkpoint == 2 ? 42 : 0;
            return created ? sequence : 42;
        }
        /// <inheritdoc />
        public void RevertAndSwitchBranch() => Calls.Add("revert-switch");
        /// <inheritdoc />
        public Phase0AllocatedIdentity? CreateTodo(string requestId, string title)
        {
            Calls.Add("create");
            Assert.Equal("create-after-revert", requestId);
            Assert.Equal("task after revert", title);
            created = true;
            return fault switch
            {
                "Create:Missing" => null,
                "Create:EmptyId" => Row with { Id = "" },
                "Create:WhitespaceId" => Row with { Id = " " },
                "Create:Origin" => Row with { Origin = "LG" },
                "Create:ReusedSequence" => Row with { Sequence = 42 },
                "Create:LowerSequence" => Row with { Sequence = 1 },
                "Create:Title" => Row with { Title = "wrong" },
                _ => Row
            };
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0AllocatedIdentity>? ReadTodos()
        {
            Calls.Add("rows");
            return fault switch
            {
                "Rows:Missing" => null, "Rows:Empty" => [], "Rows:Extra" => [Row, Row],
                "Rows:Id" => [Row with { Id = "other" }],
                "Rows:Origin" => [Row with { Origin = "LG" }],
                "Rows:Sequence" => [Row with { Sequence = 42 }],
                "Rows:Title" => [Row with { Title = "wrong" }],
                _ => [Row]
            };
        }
    }

    /// <summary>Independent simultaneous response script with a two-arrival barrier and stable stored receipt.</summary>
    private sealed class ReservationProbe(int winner, string fault = "") : IPhase0HumanReservationProbe
    {
        private readonly TaskCompletionSource<Phase0ReservationReply?>[] initial =
            [new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously)];
        /// <summary>Counts reservation calls.</summary>
        internal int Requests { get; private set; }
        /// <summary>Counts independent persisted-row observations.</summary>
        internal int RowReads { get; private set; }
        /// <summary>Records whether both initial calls arrived before either response was released.</summary>
        internal bool BothInitialRequestsArrived { get; private set; }
        private Phase0HumanReservation Stored => winner == 0
            ? new("Atlas", "Todo", "IMPL-UNITGREEN-001", "LA", "reserve-LA", "receipt-alpha")
            : new("Atlas", "Todo", "IMPL-UNITGREEN-001", "LG", "reserve-LG", "receipt-beta");
        /// <inheritdoc />
        public void Prepare() { Requests = 0; RowReads = 0; BothInitialRequestsArrived = false; }
        /// <inheritdoc />
        public Task<Phase0ReservationReply?> ReserveAsync(string project, string kind, string humanId, string node, string requestId)
        {
            Assert.Equal("Atlas", project);
            Assert.Equal("Todo", kind);
            Assert.Equal("IMPL-UNITGREEN-001", humanId);
            var index = Requests++;
            Assert.InRange(index, 0, 2);
            Assert.Equal(index == 2 ? Stored.Node : index == 0 ? "LA" : "LG", node);
            Assert.Equal(index == 2 ? Stored.RequestId : index == 0 ? "reserve-LA" : "reserve-LG", requestId);
            if (index == 2) return Task.FromResult(Reply("Retry", new(200, null, Stored)));
            if (index == 1)
            {
                BothInitialRequestsArrived = true;
                for (var reply = 0; reply < 2; reply++)
                    initial[reply].SetResult(reply == winner
                        ? Reply("Winner", new(201, null, Stored))
                        : Reply("Loser", new(409, "human_id_reserved", null)));
            }
            return initial[index].Task;
        }
        /// <summary>Corrupts exactly one response, independently of durable row reads.</summary>
        private Phase0ReservationReply? Reply(string phase, Phase0ReservationReply reply)
        {
            if (!fault.StartsWith(phase + ":", StringComparison.Ordinal)) return reply;
            var field = fault[(phase.Length + 1)..];
            return field switch
            {
                "Missing" => null,
                "Status" => reply with { Status = 500 },
                "Code" => reply with { Code = "wrong" },
                "Reservation" => reply with { Reservation = null },
                _ => reply with { Reservation = Corrupt(Stored, field) }
            };
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0HumanReservation>? ReadReservations()
        {
            var checkpoint = RowReads++;
            var rows = Requests == 0 ? new List<Phase0HumanReservation>() : [Stored];
            var prefix = "Rows:" + checkpoint + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return rows;
            var field = fault[prefix.Length..];
            if (field == "Missing") return null;
            if (field == "Extra") { rows.Add(Stored); return rows; }
            if (field == "Empty") return [];
            return [Corrupt(Stored, field)];
        }
        /// <summary>Returns one independently corrupted reservation field.</summary>
        private static Phase0HumanReservation Corrupt(Phase0HumanReservation row, string field) => field switch
        {
            "Project" => row with { Project = "other" }, "Kind" => row with { Kind = "Memory" },
            "HumanId" => row with { HumanId = "other" }, "Node" => row with { Node = "XX" },
            "RequestId" => row with { RequestId = "other" }, "ReceiptId" => row with { ReceiptId = "changed-receipt" },
            "WhitespaceReceipt" => row with { ReceiptId = " " }, _ => throw new InvalidOperationException(field)
        };
    }

    /// <summary>Independent disconnected content and publication readbacks, not a real hub or storage provider.</summary>
    private sealed class OfflineProbe(string fault = "") : IPhase0OfflineHumanIdentityProbe
    {
        private static Phase0ProvisionalTodo Expected => new("fixture-provisional-LA-1", "IMPL-UNITGREEN-001", "offline café 雪", "LA");
        /// <summary>Counts attempted publications.</summary>
        internal int Publications { get; private set; }
        /// <summary>Counts durable local reads.</summary>
        internal int LocalReads { get; private set; }
        /// <summary>Counts remote head reads.</summary>
        internal int HeadReads { get; private set; }
        /// <summary>Counts reservation reads.</summary>
        internal int ReservationReads { get; private set; }
        /// <inheritdoc />
        public void Prepare(Phase0ProvisionalTodo todo) => Assert.Equal(Expected, todo);
        /// <inheritdoc />
        public Phase0IdentityPublishReply? ReconnectAndPublish()
        {
            Publications++;
            return fault switch
            {
                "Response:Missing" => null, "Response:Status" => new(201, "human_id_unreserved"),
                "Response:Code" => new(409, "wrong"), _ => new(409, "human_id_unreserved")
            };
        }
        /// <inheritdoc />
        public Phase0ProvisionalTodo? ReadLocalTodo()
        {
            var prefix = "Local:" + LocalReads++ + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return Expected;
            return fault[prefix.Length..] switch
            {
                "Missing" => null, "CanonicalId" => Expected with { CanonicalId = "changed" },
                "RequestedHumanId" => Expected with { RequestedHumanId = "changed" },
                "Title" => Expected with { Title = "offline cafe" }, "Origin" => Expected with { Origin = "LG" },
                _ => throw new InvalidOperationException(fault)
            };
        }
        /// <inheritdoc />
        public string? ReadRemoteHead()
        {
            var prefix = "Head:" + HeadReads++ + ":";
            return fault == prefix + "Missing" ? null : fault == prefix + "Wrong" ? "C1" : "C0";
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0HumanReservation>? ReadReservations()
        {
            var prefix = "Reservations:" + ReservationReads++ + ":";
            return fault == prefix + "Missing" ? null : fault == prefix + "Reserved"
                ? [new("Atlas", "Todo", "IMPL-UNITGREEN-001", "LA", "implicit", "unexpected")] : [];
        }
    }
}
