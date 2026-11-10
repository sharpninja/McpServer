namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a
/// background callback in the code under test makes"): a settable test clock whose timers never fire on their own.
/// The code under test may create timers through <see cref="TimeProvider.CreateTimer"/>; each one is recorded with its
/// callback, state, due time and period, and its callback runs only when the test calls <see cref="TriggeredTimer.Fire"/>
/// or <see cref="FireAll"/>. So a background cleanup, poll or deadline in the code under test runs exactly where the test
/// places it in the scripted trace, and neither real elapsed time nor a wall-clock change can run it.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): added because hostile validation round 13 finding R13-F1 found that VoiceConversationService arms a real 60-second cleanup timer whose idle-age decision could remove a session between two creates of a unit test.</description></item>
/// </list>
/// </remarks>
internal sealed class TriggeredTimerTimeProvider : SettableTimeProvider
{
    /// <summary>TR-MCP-USECASE-018 AC-006: every timer the code under test created, in creation order.</summary>
    private readonly List<TriggeredTimer> _timers = [];

    /// <summary>Creates a provider at <paramref name="utcNow"/> with no timers.</summary>
    /// <param name="utcNow">The instant, with a zero offset.</param>
    /// <exception cref="ArgumentException">The instant has a non-zero offset.</exception>
    internal TriggeredTimerTimeProvider(DateTimeOffset utcNow)
        : base(utcNow)
    {
    }

    /// <summary>Every timer the code under test created, in creation order, disposed ones included.</summary>
    internal IReadOnlyList<TriggeredTimer> Timers => _timers;

    /// <summary>
    /// Records a timer that never fires on its own: its callback runs only when the test fires it.
    /// </summary>
    /// <param name="callback">The timer callback.</param>
    /// <param name="state">The callback state.</param>
    /// <param name="dueTime">The due time the code under test asked for (recorded, never waited on).</param>
    /// <param name="period">The period the code under test asked for (recorded, never waited on).</param>
    /// <returns>The recorded timer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var timer = new TriggeredTimer(callback, state, dueTime, period);
        _timers.Add(timer);
        return timer;
    }

    /// <summary>Fires every timer that is not disposed, once each, in creation order.</summary>
    /// <returns>How many timers fired.</returns>
    internal int FireAll()
    {
        var fired = 0;
        foreach (var timer in _timers.ToArray())
        {
            if (timer.IsDisposed)
                continue;
            timer.Fire();
            fired++;
        }

        return fired;
    }
}

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended 2026-11-06): a timer created through <see cref="TriggeredTimerTimeProvider"/>.
/// It keeps the schedule the code under test asked for, never waits, and runs its callback only on <see cref="Fire"/>.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): added with <see cref="TriggeredTimerTimeProvider"/> for hostile validation round 13 finding R13-F1.</description></item>
/// </list>
/// </remarks>
internal sealed class TriggeredTimer : ITimer
{
    /// <summary>TR-MCP-USECASE-018 AC-006: the callback the code under test supplied.</summary>
    private readonly TimerCallback _callback;

    /// <summary>TR-MCP-USECASE-018 AC-006: the state the callback receives.</summary>
    private readonly object? _state;

    /// <summary>Creates a timer that keeps the given schedule and never fires on its own.</summary>
    /// <param name="callback">The timer callback.</param>
    /// <param name="state">The callback state.</param>
    /// <param name="dueTime">The due time asked for.</param>
    /// <param name="period">The period asked for.</param>
    internal TriggeredTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _callback = callback;
        _state = state;
        DueTime = dueTime;
        Period = period;
    }

    /// <summary>The due time last asked for, by creation or by <see cref="Change"/>.</summary>
    internal TimeSpan DueTime { get; private set; }

    /// <summary>The period last asked for, by creation or by <see cref="Change"/>.</summary>
    internal TimeSpan Period { get; private set; }

    /// <summary>How many times the test fired the timer.</summary>
    internal int FireCount { get; private set; }

    /// <summary>Whether the code under test disposed the timer.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Runs the callback once, now, on the calling thread.</summary>
    /// <exception cref="InvalidOperationException">The timer is disposed: a disposed timer must not fire.</exception>
    internal void Fire()
    {
        if (IsDisposed)
            throw new InvalidOperationException("A disposed timer cannot fire.");
        FireCount++;
        _callback(_state);
    }

    /// <summary>Records a new schedule; the timer still fires only when the test fires it.</summary>
    /// <param name="dueTime">The new due time.</param>
    /// <param name="period">The new period.</param>
    /// <returns>True unless the timer is disposed.</returns>
    public bool Change(TimeSpan dueTime, TimeSpan period)
    {
        if (IsDisposed)
            return false;
        DueTime = dueTime;
        Period = period;
        return true;
    }

    /// <summary>Marks the timer disposed; it can no longer fire.</summary>
    public void Dispose() => IsDisposed = true;

    /// <summary>Marks the timer disposed; it can no longer fire.</summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
