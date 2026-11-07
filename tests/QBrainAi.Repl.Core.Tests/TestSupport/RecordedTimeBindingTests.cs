namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended), BDPv4 binding: the test-owned <see cref="ScriptedClock"/> satisfies the same
/// recorded-time contract the mock gate proved on a scripted reference (RecordedTimeContractTests), and the stepping
/// clock the session-state fake and the auth-rotation stub use by default yields exact, strictly increasing times. No
/// real clock is read and nothing sleeps.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended): added for hostile validation round 10 finding R10-F3 (operator 2026-11-03: "don't gate on me between rounds"); a separate class, because TestGroups selects by class and the mock gate ran the contract alone.</description></item>
/// </list>
/// </remarks>
public sealed class RecordedTimeBindingTests
{
    /// <summary>The scripted clock satisfies every row of the recorded-time contract.</summary>
    [Fact]
    public void ScriptedClock_SatisfiesTheContract() =>
        RecordedTimeContractTests.AssertClock(times => new ScriptedClock(times).Next);

    /// <summary>The scripted clock refuses a read past its script instead of inventing a time.</summary>
    [Fact]
    public void ScriptedClock_RefusesAReadPastItsScript()
    {
        var clock = new ScriptedClock(RecordedTimeContractTests.First);
        Assert.Equal(RecordedTimeContractTests.First, clock.Next());
        Assert.Throws<InvalidOperationException>(() => clock.Next());
        Assert.Equal(1, clock.Reads);
    }

    /// <summary>The stepping clock yields the first time, then one step later on each read, exactly.</summary>
    [Fact]
    public void SteppingClock_YieldsExactSteps()
    {
        var clock = ScriptedClock.Stepping(RecordedTimeContractTests.First, TimeSpan.FromSeconds(1));
        var recorded = new[] { clock(), clock(), clock() };
        RecordedTimeContractTests.AssertRecordedTimes(
            [RecordedTimeContractTests.First, RecordedTimeContractTests.First.AddSeconds(1), RecordedTimeContractTests.First.AddSeconds(2)],
            recorded,
            "stepping clock");
    }
}
