using System.Globalization;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Requirements;

/// <summary>
/// FR-MCP-040 ac-1 (BDPv4 mock gate, PR 87 slice P): the server reads a requirement's stored CreatedAtUtc and
/// UpdatedAtUtc text into the value it sends, keeping the stored offset, and reads a blank or unreadable stored value as
/// null; a mapping reports the earliest stored creation time of its links. A stored value without a complete date (its
/// year, month and day stated in the text) reads as null and is ignored for a mapping, so no part of a reported value
/// comes from the read clock or a parser default. Fixed rows and an independent scripted reader; wrong readers (read
/// time, always null, offset converted, latest link instead of earliest, a fixed local offset, a date filled from the
/// clock, the year found in the text) are rejected by the same assertions inside this passing harness. No store or
/// environment is read; the scripted reader reads no clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: added the stored-time reader contract (mock gate ug-r9-mockgate-p-1).</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1 (appended "A stored time written without an offset is read as UTC"): added the offset-less row and a wrong reader that applies a fixed local offset, because no row pinned that case (operator 2026-11-02: "UTC").</description></item>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1 (appended "A read never fills a missing date from the clock"): added rows for a time alone, a date without a year, a date alone and a complete date in another order, a mapping row with an incomplete link time, and the wrong reader "date filled from the clock" (the earlier production parse), because hostile validation finding R9-F4 showed a time alone took the read day's date (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// <item><term>2026-11-03</term><description>FR-MCP-040 ac-1 (appended "A readable stored time states its year, its month and its day as date components"): added rows for a year and month alone (numeric and in words), year digits only in a fraction and a complete date with the month in words, two mapping rows (an incomplete earlier-looking link, every link incomplete), and the wrong reader "year in the text" (the round 9 production reader), because hostile validation round 10 finding R10-F1 showed a year and month alone passed with day 1 (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
public sealed class RequirementStoredTimestampContractTests
{
    /// <summary>Stored text and the value the server must send.</summary>
    internal static readonly (string Name, string Stored, DateTimeOffset? Expected)[] Rows =
    [
        ("round-trip UTC", "2026-10-20T15:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
        ("stored offset kept", "2026-10-20T10:00:00.0000000-05:00", new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.FromHours(-5))),
        ("no offset reads as UTC", "2026-10-20T15:00:00.0000000", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
        ("empty", "", null),
        ("whitespace", "   ", null),
        ("unreadable", "not a time", null),
        ("time alone", "12:34:56", null),
        ("date without a year", "10-20 15:00:00", null),
        ("date alone reads as midnight UTC", "2026-10-20", new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero)),
        ("complete date in another order", "10/20/2026 15:00:00", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
        ("year and month alone", "2026-10", null),
        ("year and month in words", "October 2026", null),
        ("year digits only in a fraction", "12:34:56.2026", null),
        ("complete date with the month in words", "20 October 2026 15:00:00", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
    ];

    /// <summary>Stored link creation texts of one mapping and the creation time the mapping must report.</summary>
    internal static readonly (string Name, string[] Links, DateTimeOffset? Expected)[] MappingRows =
    [
        ("earliest of two", ["2026-10-21T08:00:00.0000000+00:00", "2026-10-20T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero)),
        ("blank link ignored", ["", "2026-10-22T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
        ("no readable link", ["not a time", ""], null),
        ("no links", [], null),
        ("incomplete link time ignored", ["12:34:56", "2026-10-22T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
        ("incomplete earlier-looking link ignored", ["2026-10", "2026-10-22T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
        ("every link incomplete", ["October 2026", "12:34:56"], null),
    ];

    /// <summary>The scripted readers satisfy every row.</summary>
    [Fact]
    public void ScriptedReaders_SatisfyEveryRow() => AssertReaders(ScriptedRead, ScriptedEarliest);

    /// <summary>Each wrong reader fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("read time")]
    [InlineData("always null")]
    [InlineData("offset converted to UTC")]
    [InlineData("latest link")]
    [InlineData("no offset read with a fixed local offset")]
    [InlineData("date filled from the clock")]
    [InlineData("year in the text")]
    public void WrongReaders_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertReaders(
            stored => fault switch
            {
                "read time" => DateTimeOffset.UtcNow,
                "always null" => null,
                "offset converted to UTC" => ScriptedRead(stored)?.ToUniversalTime(),
                "latest link" => ScriptedRead(stored),
                // A fixed -06:00 offset stands in for "the local zone", so the control does not depend on the host's zone.
                "no offset read with a fixed local offset" => stored == "2026-10-20T15:00:00.0000000"
                    ? new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.FromHours(-6))
                    : ScriptedRead(stored),
                // The unrestricted framework parse: a time alone takes the read day's date, which no row expects.
                "date filled from the clock" => ClockFillingRead(stored),
                // The round 9 production reader: a parse that fills a missing day from the parser's default is accepted
                // because the text holds the year; "2026-10" reads as 2026-10-01 on every day, so the control is fixed.
                "year in the text" => YearInTextRead(stored),
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            },
            links => fault switch
            {
                "latest link" => links.Select(ScriptedRead).Where(value => value is not null).OrderByDescending(value => value).FirstOrDefault(),
                "date filled from the clock" => links.Select(ClockFillingRead).Where(value => value is not null).Min(),
                "year in the text" => links.Select(YearInTextRead).Where(value => value is not null).Min(),
                _ => ScriptedEarliest(links),
            }));

    /// <summary>Asserts both readers against every row, comparing instant and offset.</summary>
    /// <param name="read">Reads one stored time.</param>
    /// <param name="earliest">Reads a mapping's creation time from its links' stored times.</param>
    internal static void AssertReaders(Func<string, DateTimeOffset?> read, Func<IReadOnlyList<string>, DateTimeOffset?> earliest)
    {
        foreach (var row in Rows)
            Assert.True(Exact(row.Expected) == Exact(read(row.Stored)), $"{row.Name}: read '{Exact(read(row.Stored))}'");
        foreach (var row in MappingRows)
            Assert.True(Exact(row.Expected) == Exact(earliest(row.Links)), $"{row.Name}: earliest '{Exact(earliest(row.Links))}'");
    }

    /// <summary>FR-MCP-040 ac-1: the round-trip text of a time, so instant and offset are compared together.</summary>
    /// <param name="value">The time, or null.</param>
    /// <returns>The round-trip text, or null.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static string? Exact(DateTimeOffset? value) => value?.ToString("o", CultureInfo.InvariantCulture);

    /// <summary>FR-MCP-040 ac-1 scripted reference: the stated meaning of each stored text, looked up from the rows.</summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The time the rows state for it.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static DateTimeOffset? ScriptedRead(string stored) => Rows.Concat(LinkRows()).First(row => row.Stored == stored).Expected;

    /// <summary>FR-MCP-040 ac-1 scripted reference: the stated creation time of a mapping, looked up from the rows.</summary>
    /// <param name="links">The links' stored creation texts.</param>
    /// <returns>The time the mapping rows state for them.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static DateTimeOffset? ScriptedEarliest(IReadOnlyList<string> links) =>
        MappingRows.Single(row => row.Links.SequenceEqual(links)).Expected;

    /// <summary>
    /// FR-MCP-040 ac-1 wrong reader: the unrestricted invariant parse that assumes UTC, which fills a missing date from
    /// the read clock. It always gives a non-null value for a time alone, so it fails that row on every day.
    /// </summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The parsed time, or null when the text does not parse.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: added for hostile validation finding R9-F4 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</remarks>
    private static DateTimeOffset? ClockFillingRead(string stored) =>
        !string.IsNullOrWhiteSpace(stored) && DateTimeOffset.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;

    /// <summary>
    /// FR-MCP-040 ac-1 wrong reader: the round 9 production reader, the unrestricted parse accepted when the parsed year
    /// appears as four digits in the text. A year and month alone parses with day 1 and passes that test, so it fails
    /// the "year and month alone" row on every day.
    /// </summary>
    /// <param name="stored">The stored text.</param>
    /// <returns>The parsed time, or null.</returns>
    /// <remarks>2026-11-03, FR-MCP-040 ac-1 (appended "A readable stored time states its year, its month and its day as date components"): added for hostile validation round 10 finding R10-F1, between rounds under the operator's "don't gate on me between rounds".</remarks>
    private static DateTimeOffset? YearInTextRead(string stored) =>
        ClockFillingRead(stored) is { } value && stored.Contains(value.Year.ToString("D4", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            ? value
            : null;

    /// <summary>FR-MCP-040 ac-1: link values that appear only in mapping rows, with their independently stated meaning.</summary>
    /// <returns>The link rows.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8); the time-alone link is read through the main rows.</remarks>
    private static IEnumerable<(string Name, string Stored, DateTimeOffset? Expected)> LinkRows() =>
    [
        ("link 21st", "2026-10-21T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 21, 8, 0, 0, TimeSpan.Zero)),
        ("link 20th", "2026-10-20T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero)),
        ("link 22nd", "2026-10-22T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
    ];
}
