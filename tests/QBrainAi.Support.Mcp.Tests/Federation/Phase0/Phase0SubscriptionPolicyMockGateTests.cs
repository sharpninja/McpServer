namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Literal scheduling scripts and independent worker/branch observations prove policy assertion discrimination.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179/TR-MCP-FEDERATION-006/TEST-MCP-227 scheduling body;
/// FR-MCP-181/TR-MCP-FEDERATION-008/TEST-MCP-229 ac-OldWorkerEpochCannotReportSuccess;
/// FR-MCP-183/TR-MCP-FEDERATION-010/TEST-MCP-231 ac-StaleBranchReportedNotDeleted. No real timer, transport, database or plugin.</remarks>
public sealed class Phase0SubscriptionPolicyMockGateTests
{
    /// <summary>Correct scripts preserve policy and protected state, including two distinct old-observation ages.</summary>
    /// <param name="kind">Policy fixture.</param><param name="age">Controlled age of the stale observation.</param>
    [Theory]
    [InlineData("timing", 0L)] [InlineData("worker", 0L)] [InlineData("stale", 86401L)] [InlineData("stale", 172800L)]
    public void CorrectSubscriptionPolicyPasses(string kind, long age)
    {
        var probe = new PolicyProbe(kind, age);
        Run(kind, age, probe);
        Assert.Equal(kind == "timing" ? 17 : 0, probe.DelayReads);
        Assert.Equal(kind == "worker" ? 3 : 0, probe.WorkerReads);
        Assert.Equal(kind == "stale" ? 3 : 0, probe.BranchReads);
        Assert.Equal(kind == "stale" ? 2 : 0, probe.ClockReads);
    }

    /// <summary>Gets independently missing/nonfinite/wrong delays, effects, refusals, clocks, displays and protected states.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            for (var index = 0; index < 17; index++)
                foreach (var fault in new[] { "Missing", "NaN", "Infinity", "Wrong" }) yield return ["timing", 0L, "delay:" + index + ":" + fault];
            for (var index = 0; index < 2; index++)
                foreach (var fault in new[] { "Missing", "History", "Business" }) yield return ["timing", 0L, "effects:" + index + ":" + fault];
            for (var index = 0; index < 3; index++)
            {
                foreach (var fault in new[] { "Missing", "Epoch", "Desired", "Receipt", "EmptyFingerprint" }) yield return ["worker", 0L, "worker:" + index + ":" + fault];
                foreach (var fault in new[] { "Missing", "Branch", "Draft", "Head", "EmptyFingerprint" }) yield return ["stale", 86401L, "branch:" + index + ":" + fault];
                if (index > 0)
                {
                    yield return ["worker", 0L, "worker:" + index + ":ChangedFingerprint"];
                    yield return ["stale", 86401L, "branch:" + index + ":ChangedFingerprint"];
                }
            }
            foreach (var fault in new[] { "Missing", "Status", "Code", "Retryable" }) yield return ["worker", 0L, "reply:0:" + fault];
            for (var index = 0; index < 2; index++)
            {
                foreach (var fault in new[] { "Missing", "Project", "Branch", "Stale", "Observed" }) yield return ["stale", 86401L, "status:" + index + ":" + fault];
                foreach (var fault in new[] { "Missing", "Wrong" }) yield return ["stale", 86401L, "clock:" + index + ":" + fault];
            }
        }
    }

    /// <summary>Every declared corrupt observation must fail through the shared assertion path.</summary>
    /// <param name="kind">Policy fixture.</param><param name="age">Controlled stale age.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongSubscriptionPolicyIsRejected(string kind, long age, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Run(kind, age, new PolicyProbe(kind, age, fault)));

    private static void Run(string kind, long age, IPhase0SubscriptionPolicyProbe probe)
    {
        switch (kind)
        {
            case "timing": Phase0SubscriptionPolicyContract.AssertTiming(probe); break;
            case "worker": Phase0SubscriptionPolicyContract.AssertWorkerFence(probe); break;
            case "stale": Phase0SubscriptionPolicyContract.AssertStaleBranch(probe, age); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    /// <summary>Independent literal responses; all expected values are authored before production scheduling or control code.</summary>
    private sealed class PolicyProbe(string kind, long age, string fault = "") : IPhase0SubscriptionPolicyProbe
    {
        private int effectsRead;
        private int statusRead;
        /// <summary>Gets scalar delay reads.</summary>
        internal int DelayReads { get; private set; }
        /// <summary>Gets independent worker reads.</summary>
        internal int WorkerReads { get; private set; }
        /// <summary>Gets independent branch reads.</summary>
        internal int BranchReads { get; private set; }
        /// <summary>Gets independent controlled-clock reads.</summary>
        internal int ClockReads { get; private set; }
        /// <inheritdoc />
        public void Prepare(string value, long ageSeconds) { Assert.Equal(kind, value); Assert.Equal(age, ageSeconds); DelayReads = WorkerReads = BranchReads = ClockReads = effectsRead = statusRead = 0; }
        /// <inheritdoc />
        public double? PollDelay(double sample)
        {
            Assert.Equal(new[] { 0d, 0.25, 0.5, 0.75, 1 }[DelayReads], sample);
            return Delay(new[] { 24d, 27, 30, 33, 36 }[DelayReads]);
        }
        /// <inheritdoc />
        public double? TransientFailureDelay() => Delay(new[] { 1d, 2, 4, 8, 16, 32, 60, 60, 60 }[DelayReads - 5]);
        /// <inheritdoc />
        public double? WakeDelay(string trigger) { Assert.Equal(new[] { "commit", "reconnect", "on-demand" }[DelayReads - 14], trigger); return Delay(0); }
        /// <inheritdoc />
        public Phase0TimingEffects? ReadTimingEffects() => Active("effects", effectsRead++) switch
        {
            "Missing" => null, "History" => new(1, 0), "Business" => new(0, 1), _ => new(0, 0)
        };
        /// <inheritdoc />
        public Phase0WorkerReportState? ReadWorkerState() => Active("worker", WorkerReads++) switch
        {
            "Missing" => null, "Epoch" => new(4, 8, "R-current", "worker-protected"), "Desired" => new(5, 7, "R-current", "worker-protected"),
            "Receipt" => new(5, 8, "R-old", "worker-protected"), "EmptyFingerprint" => new(5, 8, "R-current", null),
            "ChangedFingerprint" => new(5, 8, "R-current", "changed"), _ => new(5, 8, "R-current", "worker-protected")
        };
        /// <inheritdoc />
        public Phase0WorkerReportReply? ReportOldWorker4() => Active("reply", 0) switch
        {
            "Missing" => null, "Status" => new(200, "stale_worker_epoch", false), "Code" => new(409, "other", false),
            "Retryable" => new(409, "stale_worker_epoch", true), _ => new(409, "stale_worker_epoch", false)
        };
        /// <inheritdoc />
        public Phase0StaleBranchStatus? ReadStaleStatus()
        {
            var observed = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            return Active("status", statusRead++) switch
            {
                "Missing" => null, "Project" => new("Other", "feature/search", true, observed),
                "Branch" => new("Atlas", "main", true, observed), "Stale" => new("Atlas", "feature/search", false, observed),
                "Observed" => new("Atlas", "feature/search", true, observed.AddSeconds(1)), _ => new("Atlas", "feature/search", true, observed)
            };
        }
        /// <inheritdoc />
        public DateTimeOffset? ReadClockUtc() => Active("clock", ClockReads++) switch
        {
            "Missing" => null, "Wrong" => new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(age)
        };
        /// <inheritdoc />
        public Phase0StaleBranchState? ReadStaleBranch() => Active("branch", BranchReads++) switch
        {
            "Missing" => null, "Branch" => new("main", "draft", "F1", "branch-protected"), "Draft" => new("feature/search", "lost", "F1", "branch-protected"),
            "Head" => new("feature/search", "draft", "C0", "branch-protected"), "EmptyFingerprint" => new("feature/search", "draft", "F1", null),
            "ChangedFingerprint" => new("feature/search", "draft", "F1", "changed"), _ => new("feature/search", "draft", "F1", "branch-protected")
        };
        /// <inheritdoc />
        public void Reopen() { Assert.True(kind == "worker" ? WorkerReads == 2 : BranchReads == 2); }

        private string Active(string channel, int index)
            => fault.StartsWith(channel + ":" + index + ":", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
        private double? Delay(double correct)
            => Active("delay", DelayReads++) switch { "Missing" => null, "NaN" => double.NaN, "Infinity" => double.PositiveInfinity, "Wrong" => correct + 1, _ => correct };
    }
}