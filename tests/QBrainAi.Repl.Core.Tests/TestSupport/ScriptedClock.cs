namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended "A unit test that checks a recorded time ... takes the clock values from
/// test-owned scripted inputs and asserts them exactly"): a test-owned clock for the test fakes. It returns its scripted
/// times in order, one per read, and refuses a read past the script; <see cref="Stepping"/> gives fakes an unbounded
/// default that starts at a fixed instant and steps by a fixed interval. It never reads the real clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended): added for the session-state fake and the auth-rotation stub, because hostile validation round 10 finding R10-F3 showed six unit cases compared fake timestamps with the live clock and slept to separate them (operator 2026-11-03: "don't gate on me between rounds"; contract RecordedTimeContractTests, mock gate ug-r11-mockgate-u-1).</description></item>
/// </list>
/// </remarks>
internal sealed class ScriptedClock
{
    /// <summary>TR-MCP-USECASE-018 AC-006: the scripted times, returned in order.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added on 2026-11-03 with the class (hostile validation round 10 finding R10-F3), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private readonly DateTimeOffset[] _times;

    /// <summary>TR-MCP-USECASE-018 AC-006: the index of the next scripted time, and so the number of reads so far.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added on 2026-11-03 with the class (hostile validation round 10 finding R10-F3), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private int _next;

    /// <summary>Creates a clock that returns <paramref name="times"/> in order.</summary>
    /// <param name="times">The scripted times; at least one.</param>
    internal ScriptedClock(params DateTimeOffset[] times)
    {
        ArgumentNullException.ThrowIfNull(times);
        if (times.Length == 0)
            throw new ArgumentException("A scripted clock needs at least one time.", nameof(times));
        _times = times;
    }

    /// <summary>The number of reads so far.</summary>
    internal int Reads => _next;

    /// <summary>Returns the next scripted time.</summary>
    /// <returns>The next time.</returns>
    /// <exception cref="InvalidOperationException">Every scripted time has been read.</exception>
    internal DateTimeOffset Next() =>
        _next < _times.Length
            ? _times[_next++]
            : throw new InvalidOperationException($"The scripted clock has no time left after {_times.Length} reads.");

    /// <summary>An unbounded test-owned clock: <paramref name="first"/>, then one <paramref name="step"/> later on each read.</summary>
    /// <param name="first">The first time.</param>
    /// <param name="step">The interval between reads.</param>
    /// <returns>The clock.</returns>
    internal static Func<DateTimeOffset> Stepping(DateTimeOffset first, TimeSpan step)
    {
        var reads = 0L;
        return () => first.AddTicks(step.Ticks * reads++);
    }
}
