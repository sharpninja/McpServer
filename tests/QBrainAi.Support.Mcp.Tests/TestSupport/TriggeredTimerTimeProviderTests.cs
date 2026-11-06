namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// Tests the test clock whose timers fire only when the test fires them (TR-MCP-USECASE-018 AC-006, appended 2026-11-06,
/// "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): it
/// keeps the settable instant of <see cref="SettableTimeProvider"/>, records each timer with the schedule asked for, never
/// runs a callback on its own, runs it once per fire on the calling thread, and refuses to fire a disposed timer. Test
/// data: fixed instants in 2001; no case reads the system clock or waits.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06): added with <see cref="TriggeredTimerTimeProvider"/> for hostile validation round 13 finding R13-F1.</description></item>
/// </list>
/// </remarks>
public sealed class TriggeredTimerTimeProviderTests
{
    /// <summary>TR-MCP-USECASE-018 AC-006: a fixed UTC instant in 2001 that the cases set the clock to.</summary>
    private static readonly DateTimeOffset Early = new(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);

    /// <summary>The settable instant is kept: reads return the set instant until the test sets or advances it.</summary>
    [Fact]
    public void Reads_ReturnTheSetInstantUntilChanged()
    {
        var clock = new TriggeredTimerTimeProvider(Early);
        Assert.Equal(Early, clock.GetUtcNow());
        clock.Advance(TimeSpan.FromMinutes(15));
        Assert.Equal(Early.AddMinutes(15), clock.GetUtcNow());
        Assert.Throws<NotSupportedException>(() => clock.GetTimestamp());
    }

    /// <summary>A created timer keeps the asked schedule and never runs its callback until the test fires it.</summary>
    [Fact]
    public void CreatedTimer_KeepsTheScheduleAndNeverFiresOnItsOwn()
    {
        var clock = new TriggeredTimerTimeProvider(Early);
        var calls = new List<object?>();
        var state = new object();
        using var timer = clock.CreateTimer(calls.Add, state, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

        clock.Advance(TimeSpan.FromDays(3));
        var recorded = Assert.Single(clock.Timers);
        Assert.Same(recorded, timer);
        Assert.Equal(TimeSpan.FromSeconds(60), recorded.DueTime);
        Assert.Equal(TimeSpan.FromSeconds(60), recorded.Period);
        Assert.Empty(calls);
        Assert.Equal(0, recorded.FireCount);

        recorded.Fire();
        Assert.Same(state, Assert.Single(calls));
        Assert.Equal(1, recorded.FireCount);
        Assert.Equal(1, clock.FireAll());
        Assert.Equal(2, calls.Count);
        Assert.Equal(2, recorded.FireCount);
    }

    /// <summary>Change records the new schedule but still never fires; a disposed timer cannot fire or change.</summary>
    [Fact]
    public void ChangeAndDispose_AreRecordedAndADisposedTimerCannotFire()
    {
        var clock = new TriggeredTimerTimeProvider(Early);
        var calls = 0;
        var timer = clock.CreateTimer(_ => calls++, null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
        var recorded = Assert.Single(clock.Timers);

        Assert.True(timer.Change(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(3)));
        Assert.Equal(TimeSpan.FromMinutes(2), recorded.DueTime);
        Assert.Equal(TimeSpan.FromMinutes(3), recorded.Period);
        Assert.Equal(0, calls);

        timer.Dispose();
        Assert.True(recorded.IsDisposed);
        Assert.False(timer.Change(TimeSpan.Zero, TimeSpan.Zero));
        Assert.Throws<InvalidOperationException>(recorded.Fire);
        Assert.Equal(0, clock.FireAll());
        Assert.Equal(0, calls);
    }

    /// <summary>FireAll fires every live timer once, in creation order, and skips disposed ones.</summary>
    /// <returns>A task that completes when the checks finish.</returns>
    [Fact]
    public async Task FireAll_FiresLiveTimersInCreationOrder()
    {
        var clock = new TriggeredTimerTimeProvider(Early);
        var order = new List<string>();
        var first = clock.CreateTimer(_ => order.Add("first"), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        var second = clock.CreateTimer(_ => order.Add("second"), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        var third = clock.CreateTimer(_ => order.Add("third"), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        await second.DisposeAsync().ConfigureAwait(true);

        Assert.Equal(2, clock.FireAll());
        Assert.Equal(new[] { "first", "third" }, order);
        first.Dispose();
        third.Dispose();
        Assert.Equal(0, clock.FireAll());
        Assert.Equal(3, clock.Timers.Count);
    }
}
