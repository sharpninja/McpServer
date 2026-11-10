namespace QBrainAi.Support.Mcp.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks, including the creation and
/// update times a service stamps on a record it stores: the test supplies the service's clock as scripted times"): a
/// test-owned <see cref="TimeProvider"/> handed to a service under test. Each <see cref="GetUtcNow"/> returns the next
/// scripted UTC instant, in order; a read past the script is refused, and so are the stopwatch timestamp and timers, so
/// the provider never reads or waits on the real clock. Its local time zone is UTC.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks"): added for the GraphRAG entity and memory timestamp cases, because the sibling sweep before round 11 of hostile validation round 10 finding R10-F3 found three unit cases that compared service-stamped times with the live clock or slept to separate them (operator 2026-11-03: "don't gate on me between rounds"; contract StampedTimeContractTests).</description></item>
/// </list>
/// </remarks>
internal sealed class ScriptedTimeProvider : TimeProvider
{
    /// <summary>TR-MCP-USECASE-018 AC-006: the scripted UTC instants, returned in order.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added on 2026-11-03 with the class (the sibling sweep of hostile validation round 10 finding R10-F3), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private readonly DateTimeOffset[] _times;

    /// <summary>TR-MCP-USECASE-018 AC-006: the index of the next scripted instant, and so the number of reads so far.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added on 2026-11-03 with the class (the sibling sweep of hostile validation round 10 finding R10-F3), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private int _next;

    /// <summary>Creates a provider that returns <paramref name="times"/> in order.</summary>
    /// <param name="times">The scripted instants; at least one, each with a zero offset.</param>
    /// <exception cref="ArgumentException">No time is given, or a time has a non-zero offset.</exception>
    internal ScriptedTimeProvider(params DateTimeOffset[] times)
    {
        ArgumentNullException.ThrowIfNull(times);
        if (times.Length == 0)
            throw new ArgumentException("A scripted time provider needs at least one time.", nameof(times));
        if (times.Any(time => time.Offset != TimeSpan.Zero))
            throw new ArgumentException("A scripted time provider returns UTC instants; every time needs a zero offset.", nameof(times));
        _times = times;
    }

    /// <summary>The number of reads so far.</summary>
    internal int Reads => _next;

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    /// <summary>Returns the next scripted instant.</summary>
    /// <returns>The next time.</returns>
    /// <exception cref="InvalidOperationException">Every scripted time has been read.</exception>
    public override DateTimeOffset GetUtcNow() =>
        _next < _times.Length
            ? _times[_next++]
            : throw new InvalidOperationException($"The scripted time provider has no time left after {_times.Length} reads.");

    /// <summary>Refused: a scripted provider supplies wall-clock instants only and never reads the stopwatch.</summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override long GetTimestamp() =>
        throw new NotSupportedException("The scripted time provider supplies wall-clock instants only.");

    /// <summary>Refused: a scripted provider never starts a real timer.</summary>
    /// <param name="callback">The timer callback.</param>
    /// <param name="state">The callback state.</param>
    /// <param name="dueTime">The due time.</param>
    /// <param name="period">The period.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        throw new NotSupportedException("The scripted time provider never starts a real timer.");
}
