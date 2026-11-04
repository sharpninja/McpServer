using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-SERVICEUPDATE-001 for TR-MCP-SERVICEUPDATE-001 criterion repeated-properties: the rows a reader of
/// <c>systemctl show</c> output must meet, proven against an independent reference reader and against wrong readers
/// before the production reader changes (BDPv4 mock gate). The same rows bind to the production reader in
/// <see cref="ServiceUpdateTests"/>.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term>2026-11-04</term><description>TR-MCP-SERVICEUPDATE-001 criteria repeated-properties, preserve and
/// lifecycle (slice Z3): systemd 262 prints one <c>EnvironmentFiles=</c> line per environment file, and the redeploy of
/// 2026-11-04 stopped at the second line before any service change because the reader keyed lines by property name.
/// These rows state the reading the criterion needs: repeated lines of one property are one value, their non-empty
/// values in order separated by one space, so every environment file is validated and preserved.</description></item>
/// </list>
/// </remarks>
public sealed class ServiceUnitPropertiesContractTests
{
    /// <summary>
    /// The contract rows: systemctl show output and the properties a reader must return for it. Expected values come
    /// from the criterion and from systemd's output format, never from a reader under test.
    /// </summary>
    internal static readonly (string Name, string Output, (string Key, string Value)[] Expected)[] Rows =
    [
        ("single properties", "LoadState=loaded\nType=simple\n",
            [("LoadState", "loaded"), ("Type", "simple")]),
        ("two EnvironmentFiles lines", "EnvironmentFiles=/etc/a.env (ignore_errors=no)\nEnvironmentFiles=/etc/b.env (ignore_errors=yes)\n",
            [("EnvironmentFiles", "/etc/a.env (ignore_errors=no) /etc/b.env (ignore_errors=yes)")]),
        ("repeated lines keep their order", "DropInPaths=/etc/z.conf\nDropInPaths=/etc/a.conf\nDropInPaths=/etc/m.conf\n",
            [("DropInPaths", "/etc/z.conf /etc/a.conf /etc/m.conf")]),
        ("carriage returns are removed", "Type=simple\r\nEnvironmentFiles=/etc/a.env (ignore_errors=no)\r\nEnvironmentFiles=/etc/b.env (ignore_errors=no)\r\n",
            [("Type", "simple"), ("EnvironmentFiles", "/etc/a.env (ignore_errors=no) /etc/b.env (ignore_errors=no)")]),
        ("an empty value stays empty", "Environment=\nPrivateTmp=no\n",
            [("Environment", ""), ("PrivateTmp", "no")]),
        ("an empty repeated line adds nothing", "EnvironmentFiles=\nEnvironmentFiles=/etc/a.env (ignore_errors=no)\nEnvironmentFiles=\n",
            [("EnvironmentFiles", "/etc/a.env (ignore_errors=no)")]),
        ("a value keeps its own equals signs", "Environment=A=1 B=2\n",
            [("Environment", "A=1 B=2")]),
        ("a line without a property is ignored", "garbage\nType=exec\n\n",
            [("Type", "exec")]),
    ];

    /// <summary>A reader of systemctl show output, as the contract sees it.</summary>
    internal delegate IReadOnlyDictionary<string, string> PropertyReader(string output);

    /// <summary>Independent reference reader: a regular expression per line, values gathered per key in order.</summary>
    private static IReadOnlyDictionary<string, string> ReferenceReader(string output)
    {
        var gathered = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(output, @"^(?<key>[^=\r\n]*)=(?<value>[^\r\n]*)\r?$", RegexOptions.Multiline | RegexOptions.CultureInvariant))
        {
            var key = match.Groups["key"].Value;
            if (!gathered.TryGetValue(key, out var values)) gathered[key] = values = [];
            values.Add(match.Groups["value"].Value);
        }
        return gathered.ToDictionary(pair => pair.Key, pair => string.Join(' ', pair.Value.Where(value => value.Length > 0)), StringComparer.Ordinal);
    }

    /// <summary>Wrong readers each contract row set must reject; the first is the defect the redeploy met.</summary>
    internal static readonly (string Name, PropertyReader Reader)[] WrongReaders =
    [
        ("throws on a repeated key", output => output.Split('\n').Where(line => line.Contains('=')).Select(line => line.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => pair[1].TrimEnd('\r'), StringComparer.Ordinal)),
        ("keeps the first line", output => Gather(output, (old, _) => old)),
        ("keeps the last line", output => Gather(output, (_, value) => value)),
        ("keeps carriage returns", output => output.Split('\n').Where(line => line.Contains('=')).Select(line => line.Split('=', 2))
            .GroupBy(pair => pair[0]).ToDictionary(group => group.Key, group => string.Join(' ', group.Select(pair => pair[1]).Where(v => v.Length > 0)), StringComparer.Ordinal)),
        ("sorts repeated values", output => ReferenceReader(output).ToDictionary(pair => pair.Key,
            pair => string.Join(' ', pair.Value.Split(" /", StringSplitOptions.None).Select((part, i) => i == 0 ? part : "/" + part).Order(StringComparer.Ordinal)), StringComparer.Ordinal)),
        ("joins empty values too", output => output.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Contains('=')).Select(line => line.Split('=', 2))
            .GroupBy(pair => pair[0]).ToDictionary(group => group.Key, group => string.Join(' ', group.Select(pair => pair[1])), StringComparer.Ordinal)),
    ];

    /// <summary>Folds repeated keys with a chosen rule; used only by the wrong readers.</summary>
    private static IReadOnlyDictionary<string, string> Gather(string output, Func<string, string, string> fold)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in output.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Contains('=')))
        {
            var pair = line.Split('=', 2);
            result[pair[0]] = result.TryGetValue(pair[0], out var old) ? fold(old, pair[1]) : pair[1];
        }
        return result;
    }

    /// <summary>True when a reader returns exactly the expected properties of a row (no more, no fewer).</summary>
    internal static bool Meets(PropertyReader reader, (string Name, string Output, (string Key, string Value)[] Expected) row)
    {
        IReadOnlyDictionary<string, string> actual;
        try { actual = reader(row.Output); }
        catch (ArgumentException) { return false; }
        return actual.Count == row.Expected.Length
            && row.Expected.All(pair => actual.TryGetValue(pair.Key, out var value) && string.Equals(value, pair.Value, StringComparison.Ordinal));
    }

    /// <summary>The reference reader meets every contract row (rows: <see cref="Rows"/>).</summary>
    [Fact]
    public void ReferenceReader_MeetsEveryRow()
    {
        foreach (var row in Rows)
            Assert.True(Meets(ReferenceReader, row), row.Name);
    }

    /// <summary>Each wrong reader fails at least one contract row, so the rows discriminate (negative controls).</summary>
    /// <param name="index">The wrong reader's position in <see cref="WrongReaders"/>.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void WrongReader_FailsAtLeastOneRow(int index)
    {
        var (name, reader) = WrongReaders[index];
        Assert.Contains(Rows, row => !Meets(reader, row));
        Assert.False(string.IsNullOrEmpty(name));
    }

    /// <summary>The defect reader the redeploy met fails exactly the rows that repeat a property line.</summary>
    [Fact]
    public void DefectReader_FailsOnlyTheRepeatedRows()
    {
        var failed = Rows.Where(row => !Meets(WrongReaders[0].Reader, row)).Select(row => row.Name).ToArray();
        Assert.Equal(
            new[] { "two EnvironmentFiles lines", "repeated lines keep their order", "carriage returns are removed", "an empty repeated line adds nothing" },
            failed);
    }
}
