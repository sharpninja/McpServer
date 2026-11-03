using Xunit;

namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended "the test supplies the service's clock as scripted times"): tests the test-owned
/// <see cref="ScriptedTimeProvider"/> itself: it returns its scripted UTC instants in order and counts the reads, refuses
/// a read past the script, refuses a script time with a non-zero offset, reports UTC as its local time zone, and refuses
/// the stopwatch timestamp and timers, so it never reads or waits on the real clock. Fixed instants only.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks"): added with the provider for the sibling sweep before round 11 of hostile validation round 10 finding R10-F3 (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
public sealed class ScriptedTimeProviderTests
{
    /// <summary>The provider returns its script in order, and counts each read.</summary>
    [Fact]
    public void GetUtcNow_ReturnsTheScriptInOrder()
    {
        var clock = new ScriptedTimeProvider(StampedTimeContractTests.Created, StampedTimeContractTests.FirstUpdate);

        Assert.Equal(StampedTimeContractTests.Created, clock.GetUtcNow());
        Assert.Equal(1, clock.Reads);
        Assert.Equal(StampedTimeContractTests.FirstUpdate, clock.GetUtcNow());
        Assert.Equal(2, clock.Reads);
    }

    /// <summary>A read past the script is refused instead of falling back to the real clock.</summary>
    [Fact]
    public void GetUtcNow_PastTheScript_IsRefused()
    {
        var clock = new ScriptedTimeProvider(StampedTimeContractTests.Created);
        _ = clock.GetUtcNow();

        Assert.Throws<InvalidOperationException>(() => clock.GetUtcNow());
    }

    /// <summary>A script time with a non-zero offset is refused, since the provider returns UTC instants.</summary>
    [Fact]
    public void Constructor_NonUtcTime_IsRefused() =>
        Assert.Throws<ArgumentException>(() => new ScriptedTimeProvider(new DateTimeOffset(2026, 1, 15, 3, 0, 0, TimeSpan.FromHours(-6))));

    /// <summary>An empty script is refused.</summary>
    [Fact]
    public void Constructor_EmptyScript_IsRefused() =>
        Assert.Throws<ArgumentException>(() => new ScriptedTimeProvider());

    /// <summary>The local time zone is UTC, so a local reading equals the scripted instant.</summary>
    [Fact]
    public void LocalTimeZone_IsUtc() =>
        Assert.Equal(TimeZoneInfo.Utc, new ScriptedTimeProvider(StampedTimeContractTests.Created).LocalTimeZone);

    /// <summary>The stopwatch timestamp and timers are refused, so the provider never reads or waits on the real clock.</summary>
    [Fact]
    public void TimestampAndTimers_AreRefused()
    {
        var clock = new ScriptedTimeProvider(StampedTimeContractTests.Created);

        Assert.Throws<NotSupportedException>(() => clock.GetTimestamp());
        Assert.Throws<NotSupportedException>(() => clock.CreateTimer(_ => { }, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan));
        Assert.Equal(0, clock.Reads);
    }
}
