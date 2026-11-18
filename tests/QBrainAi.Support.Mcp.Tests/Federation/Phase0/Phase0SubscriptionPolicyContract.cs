namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Scheduling effects read independently from selected delays.</summary>
/// <param name="HistoryCommits">Unexpected Dolt history commits.</param><param name="BusinessWrites">Unexpected project-data mutations.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179/TR-MCP-FEDERATION-006/TEST-MCP-227.</remarks>
internal sealed record Phase0TimingEffects(int HistoryCommits, int BusinessWrites);

/// <summary>Protected takeover state; the fingerprint covers the complete current receipt and desired-state projection.</summary>
/// <param name="WorkerEpoch">Current worker fence.</param><param name="Desired">Outstanding generation.</param>
/// <param name="Receipt">Current receipt identity.</param><param name="Fingerprint">Independent protected-state fingerprint.</param>
/// <remarks>2026-11-17: FR-MCP-181 ac-OldWorkerEpochCannotReportSuccess; TR-MCP-FEDERATION-008; TEST-MCP-229.</remarks>
internal sealed record Phase0WorkerReportState(long WorkerEpoch, long Desired, string Receipt, string? Fingerprint);

/// <summary>Stale-worker refusal fields, independently checked against durable state.</summary>
/// <param name="Status">HTTP-style status.</param><param name="Code">Stable failure code.</param><param name="Retryable">Automatic retry permission.</param>
/// <remarks>2026-11-17: FR-MCP-181 ac-OldWorkerEpochCannotReportSuccess; TEST-MCP-229.</remarks>
internal sealed record Phase0WorkerReportReply(int Status, string? Code, bool Retryable);

/// <summary>Selected generic stale-branch display observation, not a production wire DTO.</summary>
/// <param name="Project">Selected project.</param><param name="Branch">Selected branch.</param>
/// <param name="Stale">Visible staleness flag.</param><param name="ObservedUtc">Unchanged original observation time.</param>
/// <remarks>2026-11-17: FR-MCP-183 ac-StaleBranchReportedNotDeleted; TR-MCP-FEDERATION-010; TEST-MCP-231.</remarks>
internal sealed record Phase0StaleBranchStatus(string Project, string Branch, bool Stale, DateTimeOffset ObservedUtc);

/// <summary>Independent branch, draft and ref preservation observation.</summary>
/// <param name="Branch">Retained branch.</param><param name="Draft">Retained draft.</param><param name="Head">Retained ref.</param>
/// <param name="Fingerprint">Complete protected branch/draft/ref projection fingerprint.</param>
/// <remarks>2026-11-17: FR-MCP-183 ac-StaleBranchReportedNotDeleted; TEST-MCP-231.</remarks>
internal sealed record Phase0StaleBranchState(string Branch, string Draft, string Head, string? Fingerprint);

/// <summary>Test-only timing-policy, worker-fence and selected stale-status boundary.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179/181/183; no actual timer, database or plugin is implied.</remarks>
internal interface IPhase0SubscriptionPolicyProbe
{
    /// <summary>Prepares an independent fixture with a controlled clock.</summary><param name="kind">timing, worker or stale.</param><param name="ageSeconds">Stale fixture age.</param>
    void Prepare(string kind, long ageSeconds);
    /// <summary>Selects the next normal poll delay using the injected sample.</summary><param name="sample">Fixture sample in[0,1].</param><returns>Seconds, or missing observation.</returns>
    double? PollDelay(double sample);
    /// <summary>Records one more transient failure.</summary><returns>Selected retry delay in seconds.</returns>
    double? TransientFailureDelay();
    /// <summary>Requests an immediate wake while the retry delay is capped.</summary><param name="trigger">commit, reconnect or on-demand.</param><returns>Selected delay in seconds.</returns>
    double? WakeDelay(string trigger);
    /// <summary>Reads scheduling side effects independently.</summary><returns>Complete fixture counters.</returns>
    Phase0TimingEffects? ReadTimingEffects();
    /// <summary>Reads the current worker's protected receipt and desired state.</summary><returns>Durable protected observation.</returns>
    Phase0WorkerReportState? ReadWorkerState();
    /// <summary>Reports success using old worker4 after takeover5.</summary><returns>Refusal response.</returns>
    Phase0WorkerReportReply? ReportOldWorker4();
    /// <summary>Reads stale visibility through the selected surface.</summary><returns>Displayed stale observation.</returns>
    Phase0StaleBranchStatus? ReadStaleStatus();
    /// <summary>Reads the actual controlled clock, including after reopen.</summary><returns>Current UTC fixture time.</returns>
    DateTimeOffset? ReadClockUtc();
    /// <summary>Reads retained branch, draft and refs independently of status.</summary><returns>Protected fixture observation.</returns>
    Phase0StaleBranchState? ReadStaleBranch();
    /// <summary>Reopens persistence; does not reset the controlled clock or protected state.</summary>
    void Reopen();
}

/// <summary>Reusable B11 policy assertions, separately binding the approved stale-worker and stale-branch criteria.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-007/B11; FR-MCP-179/TR-MCP-FEDERATION-006/TEST-MCP-227 scheduling body;
/// FR-MCP-181/TR-MCP-FEDERATION-008/TEST-MCP-229 ac-OldWorkerEpochCannotReportSuccess;
/// FR-MCP-183/TR-MCP-FEDERATION-010/TEST-MCP-231 ac-StaleBranchReportedNotDeleted.
/// Jitter mapping is the proposed contract-step algorithm, not runtime evidence or an additional SLA.
/// </remarks>
internal static class Phase0SubscriptionPolicyContract
{
    /// <summary>Checks literal bounded jitter, capped transient retry and immediate wake delays without project effects.</summary><param name="probe">Policy boundary.</param>
    internal static void AssertTiming(IPhase0SubscriptionPolicyProbe probe)
    {
        probe.Prepare("timing", 0);
        Assert.Equal(new Phase0TimingEffects(0, 0), probe.ReadTimingEffects());
        var samples = new[] { 0d, 0.25, 0.5, 0.75, 1 };
        var delays = new[] { 24d, 27, 30, 33, 36 };
        for (var index = 0; index < samples.Length; index++) Assert.Equal(delays[index], probe.PollDelay(samples[index]));
        foreach (var delay in new[] { 1d, 2, 4, 8, 16, 32, 60, 60, 60 }) Assert.Equal(delay, probe.TransientFailureDelay());
        foreach (var trigger in new[] { "commit", "reconnect", "on-demand" }) Assert.Equal(0d, probe.WakeDelay(trigger));
        Assert.Equal(new Phase0TimingEffects(0, 0), probe.ReadTimingEffects());
    }

    /// <summary>Checks the old worker cannot overwrite current receipt or desired state, including after reopen.</summary><param name="probe">Worker report boundary.</param>
    internal static void AssertWorkerFence(IPhase0SubscriptionPolicyProbe probe)
    {
        probe.Prepare("worker", 0);
        var before = probe.ReadWorkerState();
        Assert.NotNull(before); Assert.False(string.IsNullOrWhiteSpace(before.Fingerprint));
        Assert.Equal(new Phase0WorkerReportState(5, 8, "R-current", before.Fingerprint), before);
        Assert.Equal(new Phase0WorkerReportReply(409, "stale_worker_epoch", false), probe.ReportOldWorker4());
        Assert.Equal(before, probe.ReadWorkerState());
        probe.Reopen();
        Assert.Equal(before, probe.ReadWorkerState());
    }

    /// <summary>Checks older-than24h status preserves original observed time and every protected branch artifact.</summary>
    /// <param name="probe">Selected status boundary.</param><param name="ageSeconds">86401or172800 fixture age.</param>
    internal static void AssertStaleBranch(IPhase0SubscriptionPolicyProbe probe, long ageSeconds)
    {
        Assert.True(ageSeconds > 86400);
        probe.Prepare("stale", ageSeconds);
        var before = probe.ReadStaleBranch();
        Assert.NotNull(before); Assert.False(string.IsNullOrWhiteSpace(before.Fingerprint));
        Assert.Equal(new Phase0StaleBranchState("feature/search", "draft", "F1", before.Fingerprint), before);
        var observed = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(observed.AddSeconds(ageSeconds), probe.ReadClockUtc());
        Assert.Equal(new Phase0StaleBranchStatus("Atlas", "feature/search", true, observed), probe.ReadStaleStatus());
        Assert.Equal(before, probe.ReadStaleBranch());
        probe.Reopen();
        Assert.Equal(observed.AddSeconds(ageSeconds), probe.ReadClockUtc());
        Assert.Equal(new Phase0StaleBranchStatus("Atlas", "feature/search", true, observed), probe.ReadStaleStatus());
        Assert.Equal(before, probe.ReadStaleBranch());
    }
}