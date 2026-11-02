using System.Globalization;
using System.Text.Json;
using Xunit.Sdk;

namespace QBrainAi.Client.Tests;

/// <summary>
/// FR-MCP-040 ac-1 (BDPv4 mock gate, PR 87 slice P): the typed client reads the createdAtUtc and updatedAtUtc the server
/// sends for an FR, TR or TEST, and createdAtUtc for a mapping, keeping the sent offset; an absent or null value reads
/// as null. Fixed JSON rows and an independent scripted reader over JsonDocument; wrong readers (fields ignored,
/// swapped, read time) are rejected by the same assertions inside this passing harness. No network or clock is used.
/// </summary>
public sealed class RequirementTimestampJsonContractTests
{
    /// <summary>The timestamp members of a payload and the values the client must read.</summary>
    internal static readonly (string Name, string Members, DateTimeOffset? Created, DateTimeOffset? Updated)[] Rows =
    [
        ("both sent in UTC", "\"createdAtUtc\":\"2026-10-20T15:00:00+00:00\",\"updatedAtUtc\":\"2026-10-21T09:30:15+00:00\",",
            new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 21, 9, 30, 15, TimeSpan.Zero)),
        ("sent offset kept", "\"createdAtUtc\":\"2026-10-20T10:00:00-05:00\",\"updatedAtUtc\":\"2026-10-20T10:00:01-05:00\",",
            new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.FromHours(-5)), new DateTimeOffset(2026, 10, 20, 10, 0, 1, TimeSpan.FromHours(-5))),
        ("not sent", "", null, null),
        ("sent as null", "\"createdAtUtc\":null,\"updatedAtUtc\":null,", null, null),
    ];

    /// <summary>The payload kinds; a mapping carries only createdAtUtc.</summary>
    internal static readonly string[] Kinds = ["fr", "tr", "test", "mapping"];

    /// <summary>The scripted reader satisfies every row for every kind.</summary>
    [Fact]
    public void ScriptedReader_SatisfiesEveryRow() => AssertReader(ScriptedReader);

    /// <summary>Each wrong reader fails the same assertions.</summary>
    /// <param name="fault">The deliberate fault.</param>
    [Theory]
    [InlineData("fields ignored")]
    [InlineData("swapped")]
    [InlineData("read time")]
    public void WrongReaders_AreRejected(string fault) =>
        Assert.ThrowsAny<XunitException>(() => AssertReader((kind, json) =>
        {
            var read = ScriptedReader(kind, json);
            return fault switch
            {
                "fields ignored" => (null, null),
                "swapped" => (read.Updated, kind == "mapping" ? null : read.Created),
                "read time" => (DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                _ => throw new ArgumentOutOfRangeException(nameof(fault)),
            };
        }));

    /// <summary>Builds the payload of a kind around a row's timestamp members.</summary>
    /// <param name="kind">fr, tr, test or mapping.</param>
    /// <param name="members">The row's timestamp members, each followed by a comma.</param>
    /// <returns>The complete JSON object.</returns>
    internal static string Payload(string kind, string members) => kind switch
    {
        "fr" => "{" + members + "\"id\":\"FR-X-001\",\"title\":\"t\",\"body\":\"b\"}",
        "tr" => "{" + members + "\"id\":\"TR-X-001\",\"title\":\"t\",\"body\":\"b\"}",
        "test" => "{" + members + "\"id\":\"TEST-X-001\",\"condition\":\"c\"}",
        "mapping" => "{" + members.Replace(",\"updatedAtUtc\":null", string.Empty, StringComparison.Ordinal) + "\"frId\":\"FR-X-001\",\"trIds\":[\"TR-X-001\"],\"testIds\":[]}",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Asserts every row for every kind: the read creation time, and for FR, TR and TEST the update time,
    /// compared as instant and offset.</summary>
    /// <param name="read">Reads a kind's payload and returns its timestamps.</param>
    internal static void AssertReader(Func<string, string, (DateTimeOffset? Created, DateTimeOffset? Updated)> read)
    {
        foreach (var kind in Kinds)
        {
            foreach (var row in Rows)
            {
                var actual = read(kind, Payload(kind, row.Members));
                Assert.Equal(Exact(row.Created), Exact(actual.Created));
                if (kind != "mapping")
                    Assert.Equal(Exact(row.Updated), Exact(actual.Updated));
            }
        }
    }

    private static string? Exact(DateTimeOffset? value) => value?.ToString("o", CultureInfo.InvariantCulture);

    private static (DateTimeOffset? Created, DateTimeOffset? Updated) ScriptedReader(string kind, string json)
    {
        using var document = JsonDocument.Parse(json);
        return (Member(document.RootElement, "createdAtUtc"), kind == "mapping" ? null : Member(document.RootElement, "updatedAtUtc"));
    }

    private static DateTimeOffset? Member(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.None)
            : null;
}
