namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended 2026-11-05, "The same holds for every age, expiry or staleness decision a unit
/// test checks"): a test-owned <see cref="TimeProvider"/> whose <see cref="GetUtcNow"/> returns the instant the test set,
/// for any number of reads, until the test sets or advances it, so an age, expiry or lease decision inside the code under
/// test is decided by the test's instant alone. The stopwatch timestamp and timers are refused, so the provider never
/// reads or waits on the real clock. Its local time zone is UTC.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): added as the one settable test clock of this project, because hostile validation round 12 finding R12-F2 and its sibling sweep found unit cases whose age, expiry, lease and lock decisions ran on the live clock, and the private fixed clocks of HandoffIngestionServiceTests, HandoffDurabilityTests and TriageServiceTests each repeat the same type.</description></item>
/// <item><term>2026-11-06</term><description>TR-MCP-USECASE-018 AC-006 (appended 2026-11-06, "This includes an age, expiry or staleness decision that a background callback in the code under test makes"): no longer sealed, so <see cref="TriggeredTimerTimeProvider"/> reuses its settable instant and replaces only the timer refusal with timers the test fires, because hostile validation round 13 finding R13-F1 found a production cleanup timer that a unit test could not control. This provider's own behavior is unchanged: it still refuses timers.</description></item>
/// </list>
/// </remarks>
internal class SettableTimeProvider : TimeProvider
{
    /// <summary>TR-MCP-USECASE-018 AC-006: the instant every read returns until the test sets or advances it.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added in slice Z4 with the class (hostile validation round 12 finding R12-F2), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private DateTimeOffset _utcNow;

    /// <summary>Creates a provider that returns <paramref name="utcNow"/>.</summary>
    /// <param name="utcNow">The instant, with a zero offset.</param>
    /// <exception cref="ArgumentException">The instant has a non-zero offset.</exception>
    internal SettableTimeProvider(DateTimeOffset utcNow) => Set(utcNow);

    /// <summary>Sets the instant every later read returns.</summary>
    /// <param name="utcNow">The instant, with a zero offset.</param>
    /// <exception cref="ArgumentException">The instant has a non-zero offset.</exception>
    internal void Set(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
            throw new ArgumentException("A settable time provider returns UTC instants; the time needs a zero offset.", nameof(utcNow));
        _utcNow = utcNow;
    }

    /// <summary>Moves the instant forward (or back, for a negative value).</summary>
    /// <param name="value">The amount to add.</param>
    internal void Advance(TimeSpan value) => _utcNow += value;

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    /// <summary>Returns the instant the test set.</summary>
    /// <returns>The instant.</returns>
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>Refused: a settable provider supplies wall-clock instants only and never reads the stopwatch.</summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override long GetTimestamp() =>
        throw new NotSupportedException("The settable time provider supplies wall-clock instants only.");

    /// <summary>Refused: a settable provider never starts a real timer.</summary>
    /// <param name="callback">The timer callback.</param>
    /// <param name="state">The callback state.</param>
    /// <param name="dueTime">The due time.</param>
    /// <param name="period">The period.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        throw new NotSupportedException("The settable time provider never starts a real timer.");
}
