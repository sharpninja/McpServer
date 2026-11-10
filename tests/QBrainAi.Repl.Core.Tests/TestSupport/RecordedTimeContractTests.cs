using System.Globalization;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// TR-MCP-USECASE-018 AC-006 (appended "A unit test that checks a recorded time ... takes the clock values from
/// test-owned scripted inputs and asserts them exactly"), BDPv4 mock gate: a clock handed to a test fake returns the
/// scripted times in order, one per read, and a recorded time is asserted exactly, instant and offset together. Fixed
/// rows and an independent scripted reference clock; wrong clocks (a fixed read-time instant, a stale clock that
/// repeats its first time, one that swaps two times, and one shifted by a tick) are rejected by the same assertions
/// inside this passing harness. No real clock is read and nothing sleeps.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended): added the recorded-time contract, because hostile validation round 10 finding R10-F3 showed six unit cases compared fake timestamps with the live clock and slept to separate them (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
public sealed class RecordedTimeContractTests
{
    /// <summary>The first scripted time of every row: a fixed instant in the past, so no row depends on the date it runs.</summary>
    internal static readonly DateTimeOffset First = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A fixed instant standing in for "the time of the read", for the wrong clock that reports it.</summary>
    internal static readonly DateTimeOffset ReadTime = new(2026, 11, 3, 8, 0, 0, TimeSpan.Zero);

    /// <summary>The rows: a name and the times a clock must return, in order.</summary>
    internal static readonly (string Name, DateTimeOffset[] Times)[] Rows =
    [
        ("one recorded time", [First]),
        ("open, begin, update and complete", [First, First.AddSeconds(1), First.AddSeconds(2), First.AddSeconds(3)]),
        ("an offset is kept", [new DateTimeOffset(2026, 1, 15, 3, 0, 0, TimeSpan.FromHours(-6)), First.AddMinutes(5)]),
    ];

    /// <summary>The scripted reference clock satisfies every row.</summary>
    [Fact]
    public void ScriptedReferenceClock_SatisfiesEveryRow() => AssertClock(ScriptedReference);

    /// <summary>Each wrong clock fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("read time")]
    [InlineData("stale")]
    [InlineData("swapped")]
    [InlineData("shifted")]
    public void WrongClocks_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertClock(times => WrongClock(fault, times)));

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: asserts every row against clocks made for it: the clock is read once per scripted time
    /// and each read must equal the scripted time exactly.
    /// </summary>
    /// <param name="clockFor">Makes the clock under test for one row's times.</param>
    internal static void AssertClock(Func<DateTimeOffset[], Func<DateTimeOffset>> clockFor)
    {
        ArgumentNullException.ThrowIfNull(clockFor);
        foreach (var row in Rows)
        {
            var clock = clockFor(row.Times);
            AssertRecordedTimes(row.Times, row.Times.Select(_ => clock()).ToArray(), row.Name);
        }
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: asserts that recorded times equal the expected times exactly and in order.</summary>
    /// <param name="expected">The scripted times.</param>
    /// <param name="recorded">The times the code under test recorded.</param>
    /// <param name="context">What is being checked, for the failure message.</param>
    internal static void AssertRecordedTimes(IReadOnlyList<DateTimeOffset> expected, IReadOnlyList<DateTimeOffset> recorded, string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(recorded);
        Assert.True(expected.Count == recorded.Count, $"{context}: {recorded.Count} recorded times, {expected.Count} expected");
        for (var index = 0; index < expected.Count; index++)
            Assert.True(Exact(expected[index]) == Exact(recorded[index]), $"{context}: time {index} recorded '{Exact(recorded[index])}', expected '{Exact(expected[index])}'");
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: the round-trip text of a time, so instant and offset are compared together.</summary>
    /// <param name="value">The time.</param>
    /// <returns>The round-trip text.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006: added for hostile validation round 10 finding R10-F3.</remarks>
    private static string Exact(DateTimeOffset value) => value.ToString("o", CultureInfo.InvariantCulture);

    /// <summary>TR-MCP-USECASE-018 AC-006 scripted reference: returns the row's times in order.</summary>
    /// <param name="times">The row's times.</param>
    /// <returns>The clock.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006: added for hostile validation round 10 finding R10-F3.</remarks>
    private static Func<DateTimeOffset> ScriptedReference(DateTimeOffset[] times)
    {
        var next = 0;
        return () => times[next++];
    }

    /// <summary>TR-MCP-USECASE-018 AC-006 wrong clocks: a fixed read-time instant, stale, swapped and shifted.</summary>
    /// <param name="fault">The deliberate fault.</param>
    /// <param name="times">The row's times.</param>
    /// <returns>The wrong clock.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006: added for hostile validation round 10 finding R10-F3.</remarks>
    private static Func<DateTimeOffset> WrongClock(string fault, DateTimeOffset[] times)
    {
        var next = 0;
        return fault switch
        {
            "read time" => () => ReadTime,
            "stale" => () => times[0],
            "swapped" => () =>
            {
                var index = next++;
                return times.Length > 2 && index is 1 or 2 ? times[3 - index] : times[index];
            },
            "shifted" => () => times[next++].AddTicks(1),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };
    }
}
