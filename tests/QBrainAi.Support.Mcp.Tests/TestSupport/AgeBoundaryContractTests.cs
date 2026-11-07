using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the contract every age, expiry, lease and lock decision a unit test checks is bound to (TR-MCP-USECASE-018
/// AC-006, appended 2026-11-05, "The same holds for every age, expiry or staleness decision a unit test checks"): the
/// test supplies a reference time as a scripted clock and one recorded time, and the decision is determined by those
/// two inputs alone, immediately before, at and after the threshold. The reference times lie far before and far after
/// the real clock, so a decision that reads any clock other than the supplied one gives the wrong verdict on some row.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 (and TR-MCP-USECASE-019 AC-008, which states these rules once with
/// it). Test data: the decisions of <see cref="Decisions"/>, each with the threshold, direction and at-threshold verdict
/// pinned from the production comparison it binds (cited per decision), the two reference times, and recorded times one
/// second before, at and one second after each threshold. The mock gate runs the rows against an independent scripted
/// decision and rejects, inside a passing harness, a decision that reads another clock, one with the boundary swapped,
/// one with the comparison inverted and one that ignores a non-zero threshold. Each production decision is then bound,
/// from its own test class, through <see cref="AssertDecision"/> with the same rows. No case reads the system clock.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added the shared age-boundary contract and its mock gate, because hostile validation round 12 finding R12-F2 and its sibling sweep found 37 unit cases whose stale-turn, temporary-file, hygiene, envelope, manifest, lease and lock decisions ran on the live clock.</description></item>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): added decisions D8 (the voice idle-session cleanup that the service's cleanup timer runs) and D9 (the voice device-session idle skip), because hostile validation round 13 finding R13-F1 found that the cleanup decision ran on the live clock from a real timer, and the R12-F2 sweep had excluded it on elapsed time alone (entry N35). The earlier decisions and rows are unchanged.</description></item>
/// <item><term>2026-11-07</term><description>TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the reference times (References) moved unchanged into the abstract base class AgeBoundaryContractTestsBase in TestSupport/AgeBoundaryContractTestsBase.cs beside this file, which this class now derives from, so every unit class that names AgeBoundaryContractTests.References still does, and the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests links the base for the clock that both halves of WorkspaceHygieneG5OverlayTests share (slice S8, hostile validation finding R14-F4; DRY); the rows, decisions and cases are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class AgeBoundaryContractTests : AgeBoundaryContractTestsBase
{
    /// <summary>The distance of the rows before and after each threshold.</summary>
    internal static readonly TimeSpan Step = TimeSpan.FromSeconds(1);

    /// <summary>The instant the "another clock" negative control reads instead of the supplied reference.</summary>
    private static readonly DateTimeOffset AnotherClock = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The decisions under the contract. Older: the recorded time is selected (stale, expired, taken over or deleted)
    /// when it lies more than the threshold before the reference; Newer: when it lies more than the threshold after it.
    /// AtThreshold is the verdict exactly at the threshold, pinned from the production comparison named in each comment.
    /// </summary>
    internal static readonly AgeDecision[] Decisions =
    [
        // SessionLogService.QueryAsync and TurnMatchesStaleQuery: listed unless Timestamp > now - N hours.
        new("D1 stale in-progress turn query", TimeSpan.FromHours(24), AgeDirection.Older, AtThreshold: true),
        // GraphRagService.CleanupStaleArtifacts and TryDeleteIfStale: deleted when now - last write > 2 hours.
        new("D2 temporary artifact cleanup", TimeSpan.FromHours(2), AgeDirection.Older, AtThreshold: false),
        // WorkspaceValidationService.CollectSessionFindingsAsync: a finding when Timestamp < now - threshold.
        new("D3 hygiene stale turn, default threshold", TimeSpan.FromHours(48), AgeDirection.Older, AtThreshold: false),
        new("D3 hygiene stale turn, override threshold", TimeSpan.FromHours(8), AgeDirection.Older, AtThreshold: false),
        // FederationEnvelopeSigner.Verify: expired when ExpiresAtUtc < now; future when IssuedAtUtc > now + 5 minutes.
        new("D4 federation envelope expiry", TimeSpan.Zero, AgeDirection.Older, AtThreshold: false),
        new("D4 federation envelope future issue", TimeSpan.FromMinutes(5), AgeDirection.Newer, AtThreshold: false),
        // InMemoryKeyServerService.VerifyManifestAsync: expired when ExpiresAtUtc < now; future when IssuedAtUtc > now + skew.
        new("D5 transaction manifest expiry", TimeSpan.Zero, AgeDirection.Older, AtThreshold: false),
        new("D5 transaction manifest future issue", TimeSpan.FromSeconds(300), AgeDirection.Newer, AtThreshold: false),
        // HandoffIngestionService.ResolveExistingReservationAsync: another owner's lease is live while expires >= now.
        new("D6 handoff processing lease takeover", TimeSpan.Zero, AgeDirection.Older, AtThreshold: false),
        // MemoryConsolidateOperations.TryAcquireLock: a forced job takes a held lock over when now - acquired > 30 seconds.
        new("D7 consolidation lock takeover", TimeSpan.FromSeconds(30), AgeDirection.Older, AtThreshold: false),
        // VoiceConversationService.CleanupIdleSessionsAsync (the idle-cleanup timer callback): an idle session is deleted
        // when now - LastUpdatedUtc > SessionIdleTimeoutMinutes; the binding fixture configures 15 minutes.
        new("D8 voice idle-session cleanup", TimeSpan.FromMinutes(15), AgeDirection.Older, AtThreshold: false),
        // VoiceConversationService.FindSessionByDevice: a device's session is skipped when now - LastUpdatedUtc > the same timeout.
        new("D9 voice device-session idle skip", TimeSpan.FromMinutes(15), AgeDirection.Older, AtThreshold: false),
    ];

    /// <summary>Which side of the reference a selected recorded time lies on.</summary>
    internal enum AgeDirection
    {
        /// <summary>Selected when the recorded time is more than the threshold before the reference.</summary>
        Older,

        /// <summary>Selected when the recorded time is more than the threshold after the reference.</summary>
        Newer,
    }

    /// <summary>One decision under the contract.</summary>
    /// <param name="Name">The decision's name, as each binding names it.</param>
    /// <param name="Threshold">The threshold.</param>
    /// <param name="Direction">Which side of the reference is selected.</param>
    /// <param name="AtThreshold">Whether a recorded time exactly at the threshold is selected.</param>
    internal sealed record AgeDecision(string Name, TimeSpan Threshold, AgeDirection Direction, bool AtThreshold);

    /// <summary>The named decision of <see cref="Decisions"/>.</summary>
    /// <param name="name">The decision's name.</param>
    /// <returns>The decision.</returns>
    internal static AgeDecision Decision(string name) => Decisions.Single(decision => decision.Name == name);

    /// <summary>
    /// The rows of a decision at one reference: the recorded time one step short of the threshold, exactly at it, and one
    /// step past it, each with its expected verdict.
    /// </summary>
    /// <param name="decision">The decision.</param>
    /// <param name="reference">The reference time.</param>
    /// <returns>The rows.</returns>
    internal static (string Row, DateTimeOffset Recorded, bool Selected)[] Rows(AgeDecision decision, DateTimeOffset reference)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return decision.Direction == AgeDirection.Older
            ?
            [
                ("one step past the threshold", reference - decision.Threshold - Step, true),
                ("exactly at the threshold", reference - decision.Threshold, decision.AtThreshold),
                ("one step short of the threshold", reference - decision.Threshold + Step, false),
            ]
            :
            [
                ("one step short of the threshold", reference + decision.Threshold - Step, false),
                ("exactly at the threshold", reference + decision.Threshold, decision.AtThreshold),
                ("one step past the threshold", reference + decision.Threshold + Step, true),
            ];
    }

    /// <summary>
    /// Asserts that <paramref name="selected"/> gives every row's verdict at both references for the named decision.
    /// </summary>
    /// <param name="name">The decision's name.</param>
    /// <param name="selected">The decision under test: the reference time and one recorded time to whether it is selected.</param>
    internal static void AssertDecision(string name, Func<DateTimeOffset, DateTimeOffset, bool> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        var decision = Decision(name);
        foreach (var reference in References)
        {
            foreach (var (row, recorded, expected) in Rows(decision, reference))
                Assert.True(selected(reference, recorded) == expected, $"{decision.Name} at {reference:O}, recorded {recorded:O} ({row}): expected selected={expected}.");
        }
    }

    /// <summary>
    /// Asynchronous form of <see cref="AssertDecision"/> for decisions reached through asynchronous production code.
    /// </summary>
    /// <param name="name">The decision's name.</param>
    /// <param name="selected">The decision under test.</param>
    /// <returns>A task that completes when every row has been asserted.</returns>
    internal static async Task AssertDecisionAsync(string name, Func<DateTimeOffset, DateTimeOffset, Task<bool>> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        var decision = Decision(name);
        foreach (var reference in References)
        {
            foreach (var (row, recorded, expected) in Rows(decision, reference))
            {
                var actual = await selected(reference, recorded).ConfigureAwait(true);
                Assert.True(actual == expected, $"{decision.Name} at {reference:O}, recorded {recorded:O} ({row}): expected selected={expected}.");
            }
        }
    }

    /// <summary>The names of every decision, for the theories below.</summary>
    /// <returns>One row per decision.</returns>
    public static TheoryData<string> DecisionNames()
    {
        var data = new TheoryData<string>();
        foreach (var decision in Decisions)
            data.Add(decision.Name);
        return data;
    }

    /// <summary>The names of every decision with a non-zero threshold, for the ignored-threshold control.</summary>
    /// <returns>One row per decision.</returns>
    public static TheoryData<string> ThresholdDecisionNames()
    {
        var data = new TheoryData<string>();
        foreach (var decision in Decisions.Where(decision => decision.Threshold > TimeSpan.Zero))
            data.Add(decision.Name);
        return data;
    }

    /// <summary>Mock gate positive control: the independent scripted decision satisfies every decision's rows.</summary>
    /// <param name="name">The decision's name.</param>
    [Theory]
    [MemberData(nameof(DecisionNames))]
    public void ScriptedDecision_SatisfiesTheContract(string name) =>
        AssertDecision(name, (reference, recorded) => ScriptedSelected(Decision(name), reference, recorded));

    /// <summary>Mock gate negative control: a decision that reads another clock instead of the supplied reference is rejected.</summary>
    /// <param name="name">The decision's name.</param>
    [Theory]
    [MemberData(nameof(DecisionNames))]
    public void DecisionOnAnotherClock_IsRejected(string name) =>
        Assert.ThrowsAny<XunitException>(() => AssertDecision(name, (_, recorded) => ScriptedSelected(Decision(name), AnotherClock, recorded)));

    /// <summary>Mock gate negative control: a decision with the at-threshold verdict swapped is rejected.</summary>
    /// <param name="name">The decision's name.</param>
    [Theory]
    [MemberData(nameof(DecisionNames))]
    public void DecisionWithTheBoundarySwapped_IsRejected(string name) =>
        Assert.ThrowsAny<XunitException>(() => AssertDecision(name, (reference, recorded) =>
            ScriptedSelected(Decision(name) with { AtThreshold = !Decision(name).AtThreshold }, reference, recorded)));

    /// <summary>Mock gate negative control: a decision with the comparison inverted (the other side selected) is rejected.</summary>
    /// <param name="name">The decision's name.</param>
    [Theory]
    [MemberData(nameof(DecisionNames))]
    public void DecisionWithTheComparisonInverted_IsRejected(string name) =>
        Assert.ThrowsAny<XunitException>(() => AssertDecision(name, (reference, recorded) =>
            ScriptedSelected(Decision(name) with { Direction = Decision(name).Direction == AgeDirection.Older ? AgeDirection.Newer : AgeDirection.Older }, reference, recorded)));

    /// <summary>Mock gate negative control: a decision that ignores its non-zero threshold is rejected.</summary>
    /// <param name="name">The decision's name.</param>
    [Theory]
    [MemberData(nameof(ThresholdDecisionNames))]
    public void DecisionIgnoringTheThreshold_IsRejected(string name) =>
        Assert.ThrowsAny<XunitException>(() => AssertDecision(name, (reference, recorded) =>
            ScriptedSelected(Decision(name) with { Threshold = TimeSpan.Zero }, reference, recorded)));

    /// <summary>
    /// Mock gate: the asynchronous assertion gives the same verdicts as the synchronous one, accepting the scripted
    /// decision and rejecting a decision on another clock.
    /// </summary>
    /// <returns>A task that completes when the checks finish.</returns>
    [Fact]
    public async Task AsynchronousAssertion_MatchesTheSynchronousOne()
    {
        foreach (var decision in Decisions)
        {
            await AssertDecisionAsync(decision.Name, (reference, recorded) => Task.FromResult(ScriptedSelected(decision, reference, recorded))).ConfigureAwait(true);
            await Assert.ThrowsAnyAsync<XunitException>(() =>
                AssertDecisionAsync(decision.Name, (_, recorded) => Task.FromResult(ScriptedSelected(decision, AnotherClock, recorded)))).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Mock gate: the rows sit where the contract says, so a row cannot drift: each decision has three rows per
    /// reference, the at-threshold row is exactly the threshold from the reference, and the step is one second.
    /// </summary>
    [Fact]
    public void Rows_SitAtTheThresholdAndOneStepEitherSide()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), Step);
        foreach (var decision in Decisions)
        {
            foreach (var reference in References)
            {
                var rows = Rows(decision, reference);
                Assert.Equal(3, rows.Length);
                var at = Assert.Single(rows, row => row.Row == "exactly at the threshold");
                Assert.Equal(decision.Direction == AgeDirection.Older ? reference - decision.Threshold : reference + decision.Threshold, at.Recorded);
                Assert.Equal(decision.AtThreshold, at.Selected);
                Assert.Equal(2, rows.Count(row => (row.Recorded - at.Recorded).Duration() == Step));
            }
        }
    }

    /// <summary>
    /// The independent scripted decision: Older selects a recorded time before the reference minus the threshold (at it
    /// too when AtThreshold); Newer selects one after the reference plus the threshold (at it too when AtThreshold).
    /// </summary>
    /// <param name="decision">The decision.</param>
    /// <param name="reference">The reference time.</param>
    /// <param name="recorded">The recorded time.</param>
    /// <returns>Whether the recorded time is selected.</returns>
    private static bool ScriptedSelected(AgeDecision decision, DateTimeOffset reference, DateTimeOffset recorded)
    {
        if (decision.Direction == AgeDirection.Older)
        {
            var bound = reference - decision.Threshold;
            return decision.AtThreshold ? recorded <= bound : recorded < bound;
        }

        var upper = reference + decision.Threshold;
        return decision.AtThreshold ? recorded >= upper : recorded > upper;
    }
}
