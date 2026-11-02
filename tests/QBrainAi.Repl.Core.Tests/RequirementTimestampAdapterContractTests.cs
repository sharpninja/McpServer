using System.Globalization;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// FR-MCP-040 ac-1 (BDPv4 mock gate, PR 87 slice P): the REPL's FR, TR and TEST adapters report the creation and update
/// times stored with the record, and the mapping adapter its stored creation time, as round-trip text that keeps the
/// stored offset; a read never reports the time of the read, and nothing stored gives null. Fixed rows and an
/// independent scripted formatter; wrong adapters (read time, swapped, always null, converted to local time) are
/// rejected by the same assertions inside this passing harness. No clock, store or environment is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: added the adapter timestamp contract (mock gate ug-r9-mockgate-p-1).</description></item>
/// </list>
/// </remarks>
public sealed class RequirementTimestampAdapterContractTests
{
    /// <summary>Stored times and the exact text each adapter must report.</summary>
    internal static readonly (string Name, DateTimeOffset? Created, DateTimeOffset? Updated, string? CreatedText, string? UpdatedText)[] Rows =
    [
        ("both stored in UTC", new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 21, 9, 30, 15, TimeSpan.Zero),
            "2026-10-20T15:00:00.0000000+00:00", "2026-10-21T09:30:15.0000000+00:00"),
        ("stored offset kept", new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.FromHours(-5)), new DateTimeOffset(2026, 10, 20, 10, 0, 1, TimeSpan.FromHours(-5)),
            "2026-10-20T10:00:00.0000000-05:00", "2026-10-20T10:00:01.0000000-05:00"),
        ("nothing stored", null, null, null, null),
        ("only creation stored", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), null, "2026-01-02T03:04:05.0000000+00:00", null),
    ];

    /// <summary>The adapter kinds; a mapping stores only its creation time.</summary>
    internal static readonly string[] Kinds = ["fr", "tr", "test", "mapping"];

    /// <summary>The scripted adapters satisfy every row for every kind.</summary>
    [Fact]
    public void ScriptedAdapters_SatisfyEveryRow() => AssertAdapters(ScriptedAdapter);

    /// <summary>Each wrong adapter fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("read time")]
    [InlineData("swapped")]
    [InlineData("always null")]
    [InlineData("local time")]
    public void WrongAdapters_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertAdapters((kind, created, updated) => fault switch
        {
            "read time" => (DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture), DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
            "swapped" => (Text(updated), kind == "mapping" ? null : Text(created)),
            "always null" => (null, null),
            "local time" => (Text(created?.ToOffset(created.Value.Offset + TimeSpan.FromHours(1))), Text(updated?.ToOffset(updated.Value.Offset + TimeSpan.FromHours(1)))),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        }));

    /// <summary>Asserts every row for every kind: the reported creation text, and for FR, TR and TEST the update text.</summary>
    /// <param name="adapt">Builds the adapter of a kind over the given stored times and returns its reported texts.</param>
    internal static void AssertAdapters(Func<string, DateTimeOffset?, DateTimeOffset?, (string? Created, string? Updated)> adapt)
    {
        foreach (var kind in Kinds)
        {
            foreach (var row in Rows)
            {
                var actual = adapt(kind, row.Created, row.Updated);
                Assert.True(string.Equals(row.CreatedText, actual.Created, StringComparison.Ordinal), $"{kind} {row.Name}: created '{actual.Created}'");
                if (kind != "mapping")
                    Assert.True(string.Equals(row.UpdatedText, actual.Updated, StringComparison.Ordinal), $"{kind} {row.Name}: updated '{actual.Updated}'");
            }
        }
    }

    private static (string? Created, string? Updated) ScriptedAdapter(string kind, DateTimeOffset? created, DateTimeOffset? updated) =>
        (Text(created), kind == "mapping" ? null : Text(updated));

    // Independent of the production formatter: an explicit custom pattern, not the "o" specifier.
    private static string? Text(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture);
}
