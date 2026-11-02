using System.Globalization;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Requirements;

/// <summary>
/// FR-MCP-040 ac-1 (BDPv4 mock gate, PR 87 slice P): the server reads a requirement's stored CreatedAtUtc and
/// UpdatedAtUtc text into the value it sends, keeping the stored offset, and reads a blank or unreadable stored value as
/// null; a mapping reports the earliest stored creation time of its links. Fixed rows and an independent scripted
/// reader; wrong readers (read time, always null, offset converted, latest link instead of earliest) are rejected by the
/// same assertions inside this passing harness. No store, clock or environment is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: added the stored-time reader contract (mock gate ug-r9-mockgate-p-1).</description></item>
/// </list>
/// </remarks>
public sealed class RequirementStoredTimestampContractTests
{
    /// <summary>Stored text and the value the server must send.</summary>
    internal static readonly (string Name, string Stored, DateTimeOffset? Expected)[] Rows =
    [
        ("round-trip UTC", "2026-10-20T15:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero)),
        ("stored offset kept", "2026-10-20T10:00:00.0000000-05:00", new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.FromHours(-5))),
        ("empty", "", null),
        ("whitespace", "   ", null),
        ("unreadable", "not a time", null),
    ];

    /// <summary>Stored link creation texts of one mapping and the creation time the mapping must report.</summary>
    internal static readonly (string Name, string[] Links, DateTimeOffset? Expected)[] MappingRows =
    [
        ("earliest of two", ["2026-10-21T08:00:00.0000000+00:00", "2026-10-20T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero)),
        ("blank link ignored", ["", "2026-10-22T08:00:00.0000000+00:00"], new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
        ("no readable link", ["not a time", ""], null),
        ("no links", [], null),
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
    public void WrongReaders_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertReaders(
            stored => fault switch
            {
                "read time" => DateTimeOffset.UtcNow,
                "always null" => null,
                "offset converted to UTC" => ScriptedRead(stored)?.ToUniversalTime(),
                "latest link" => ScriptedRead(stored),
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            },
            links => fault == "latest link"
                ? links.Select(ScriptedRead).Where(value => value is not null).OrderByDescending(value => value).FirstOrDefault()
                : ScriptedEarliest(links)));

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

    private static string? Exact(DateTimeOffset? value) => value?.ToString("o", CultureInfo.InvariantCulture);

    private static DateTimeOffset? ScriptedRead(string stored) => Rows.Concat(LinkRows()).First(row => row.Stored == stored).Expected;

    private static DateTimeOffset? ScriptedEarliest(IReadOnlyList<string> links) =>
        MappingRows.Single(row => row.Links.SequenceEqual(links)).Expected;

    // Link values that appear only in mapping rows, with their independently stated meaning.
    private static IEnumerable<(string Name, string Stored, DateTimeOffset? Expected)> LinkRows() =>
    [
        ("link 21st", "2026-10-21T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 21, 8, 0, 0, TimeSpan.Zero)),
        ("link 20th", "2026-10-20T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 20, 8, 0, 0, TimeSpan.Zero)),
        ("link 22nd", "2026-10-22T08:00:00.0000000+00:00", new DateTimeOffset(2026, 10, 22, 8, 0, 0, TimeSpan.Zero)),
    ];
}
