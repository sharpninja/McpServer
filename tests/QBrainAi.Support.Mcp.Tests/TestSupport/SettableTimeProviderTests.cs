namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the settable test clock (TR-MCP-USECASE-018 AC-006, appended 2026-11-05, "The same holds for every age, expiry
/// or staleness decision a unit test checks"): it returns the instant the test set for every read, moves only when the
/// test sets or advances it, accepts UTC instants only, and refuses the stopwatch timestamp and timers, so it never reads
/// or waits on the real clock. Test data: fixed instants in 2001 and 2041; no case reads the system clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added with <see cref="SettableTimeProvider"/> for hostile validation round 12 finding R12-F2.</description></item>
/// </list>
/// </remarks>
public sealed class SettableTimeProviderTests
{
    private static readonly DateTimeOffset Early = new(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);

    /// <summary>Every read returns the set instant until the test sets or advances it.</summary>
    [Fact]
    public void Reads_ReturnTheSetInstantUntilChanged()
    {
        var clock = new SettableTimeProvider(Early);
        Assert.Equal(Early, clock.GetUtcNow());
        Assert.Equal(Early, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromSeconds(31));
        Assert.Equal(Early.AddSeconds(31), clock.GetUtcNow());
        var late = new DateTimeOffset(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);
        clock.Set(late);
        Assert.Equal(late, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromHours(-2));
        Assert.Equal(late.AddHours(-2), clock.GetUtcNow());
        Assert.Equal(TimeZoneInfo.Utc, clock.LocalTimeZone);
    }

    /// <summary>An instant with a non-zero offset is refused, at creation and when set.</summary>
    [Fact]
    public void NonUtcInstants_AreRefused()
    {
        var offset = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.FromHours(-6));
        Assert.Throws<ArgumentException>(() => new SettableTimeProvider(offset));
        var clock = new SettableTimeProvider(Early);
        Assert.Throws<ArgumentException>(() => clock.Set(offset));
        Assert.Equal(Early, clock.GetUtcNow());
    }

    /// <summary>The stopwatch timestamp and timers are refused, so the provider never reads or waits on the real clock.</summary>
    [Fact]
    public void StopwatchAndTimers_AreRefused()
    {
        var clock = new SettableTimeProvider(Early);
        Assert.Throws<NotSupportedException>(() => clock.GetTimestamp());
        Assert.Throws<NotSupportedException>(() => clock.CreateTimer(_ => { }, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan));
    }
}
