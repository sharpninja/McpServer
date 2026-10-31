using QBrainAi.Support.Mcp.Services;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Tests the environment-delta rule a desktop process launch uses to pass on only the variables it changes: given the
/// requested environment and an explicit baseline, a value equal to the baseline (ordinal) is omitted, a changed value
/// or a variable the baseline lacks is kept exactly, a null requested value is skipped, and no deltas gives null.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 as appended on 2026-10-29 (code that
/// compares a requested environment with the process environment takes that baseline as an explicit input, and its
/// unit contract supplies both maps; operator 2026-10-29: "Strengthen ACs, fix both (Recommended)"; hostile validation
/// round 8b finding R8B-F1) and TEST-MCP-TRIAGE-003 (desktop process launches pass only explicit environment deltas).
/// Test data: scripted requested maps and baseline maps with the delta each must give, run against builders passed in,
/// so the contract is proved on a scripted reference with negative controls before the production builder takes an
/// explicit baseline; no test reads or writes the process environment.
/// </remarks>
public sealed class EnvironmentDeltaContractTests
{
    /// <summary>
    /// The contract rows: a row name, the requested environment, the baseline, and the delta the builder must give
    /// (null when nothing differs).
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Requested, Dictionary<string, string?> Baseline, Dictionary<string, string>? Expected)[] Rows =
    [
        ("a changed token is kept",
            new() { ["GH_TOKEN"] = "child-token" },
            new() { ["GH_TOKEN"] = "parent-token" },
            new() { ["GH_TOKEN"] = "child-token" }),
        ("an unchanged token is omitted",
            new() { ["GH_TOKEN"] = "same-token" },
            new() { ["GH_TOKEN"] = "same-token" },
            null),
        ("unchanged defaults are omitted and a changed path is kept",
            new() { ["PATH"] = "/opt/added:/usr/bin", ["TEMP"] = "/var/tmp", ["HOME"] = "/home/example" },
            new() { ["PATH"] = "/usr/bin", ["TEMP"] = "/var/tmp", ["HOME"] = "/home/example" },
            new() { ["PATH"] = "/opt/added:/usr/bin" }),
        ("a variable the baseline lacks is kept",
            new() { ["NEW_SETTING"] = "value" },
            new(),
            new() { ["NEW_SETTING"] = "value" }),
        ("a null requested value is skipped",
            new() { ["CLEARED"] = null, ["KEPT"] = "value" },
            new(),
            new() { ["KEPT"] = "value" }),
        ("a value that differs only in case is kept",
            new() { ["MODE"] = "Strict" },
            new() { ["MODE"] = "strict" },
            new() { ["MODE"] = "Strict" }),
        ("no deltas gives null",
            new(),
            new() { ["PATH"] = "/usr/bin" },
            null),
    ];

    /// <summary>
    /// Returns the names of the rows on which <paramref name="builder"/> gives the wrong delta: a map where null is
    /// required, null where a map is required, or a map whose keys or values differ (ordinal).
    /// </summary>
    /// <param name="builder">The delta builder under test: the requested map and a baseline lookup to the delta.</param>
    internal static List<string> ContractFailures(Func<IEnumerable<KeyValuePair<string, string?>>, Func<string, string?>, Dictionary<string, string>?> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var failures = new List<string>();
        foreach (var row in Rows)
        {
            var actual = builder(row.Requested, key => row.Baseline.TryGetValue(key, out var value) ? value : null);
            if (!SameDelta(row.Expected, actual))
                failures.Add(row.Name);
        }

        return failures;
    }

    /// <summary>Mock gate positive control: the scripted reference builder satisfies every row.</summary>
    [Fact]
    public void ScriptedDeltaBuilder_SatisfiesTheContract() =>
        Assert.Empty(ContractFailures(ScriptedDelta));

    /// <summary>
    /// Mock gate negative controls, each rejected inside a passing harness on the row named: a builder that keeps every
    /// requested value, one that omits a variable whenever the baseline has it, one that compares values ignoring case,
    /// and one that returns an empty map instead of null.
    /// </summary>
    /// <param name="wrong">The name of the wrong builder.</param>
    /// <param name="failingRow">The contract row that must reject it.</param>
    [Theory]
    [InlineData("keeps every value", "an unchanged token is omitted")]
    [InlineData("omits by key presence", "a changed token is kept")]
    [InlineData("ignores case in values", "a value that differs only in case is kept")]
    [InlineData("empty map instead of null", "no deltas gives null")]
    public void WrongDeltaBuilders_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, ContractFailures(WrongDelta(wrong)));

    /// <summary>
    /// Binding: the production builder, <c>DesktopProcessSpawner.ExtractEnvironment(requested, baseline)</c> of
    /// <see cref="DesktopProcessSpawner"/>, satisfies the same rows the scripted reference passed and the wrong builders
    /// failed.
    /// </summary>
    [Fact]
    public void ProductionDeltaBuilder_SatisfiesTheContract() =>
        Assert.Empty(ContractFailures(DesktopProcessSpawner.ExtractEnvironment));

    /// <summary>Binding, the reviewer's named case: with unequal token values the production delta holds the requested token.</summary>
    [Fact]
    public void ExtractEnvironment_ChangedTokenIsIncluded()
    {
        var delta = Production("a changed token is kept");
        Assert.NotNull(delta);
        Assert.Equal("child-token", delta["GH_TOKEN"]);
    }

    /// <summary>Binding, the reviewer's named case: with equal token values the production delta omits the token.</summary>
    [Fact]
    public void ExtractEnvironment_UnchangedTokenIsOmitted() =>
        Assert.Null(Production("an unchanged token is omitted"));

    /// <summary>
    /// Binding, the reviewer's named case: unchanged default variables are absent while the changed path is present.
    /// </summary>
    [Fact]
    public void ExtractEnvironment_DefaultVariablesAreOmitted()
    {
        var delta = Production("unchanged defaults are omitted and a changed path is kept");
        Assert.NotNull(delta);
        var only = Assert.Single(delta);
        Assert.Equal("PATH", only.Key);
        Assert.Equal("/opt/added:/usr/bin", only.Value);
    }

    private static Dictionary<string, string>? Production(string rowName)
    {
        var row = Rows.Single(candidate => candidate.Name == rowName);
        return DesktopProcessSpawner.ExtractEnvironment(row.Requested, key => row.Baseline.TryGetValue(key, out var value) ? value : null);
    }

    private static Dictionary<string, string>? ScriptedDelta(IEnumerable<KeyValuePair<string, string?>> requested, Func<string, string?> baseline)
    {
        var delta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in requested)
        {
            if (value is not null && !string.Equals(baseline(key), value, StringComparison.Ordinal))
                delta[key] = value;
        }

        return delta.Count > 0 ? delta : null;
    }

    private static Func<IEnumerable<KeyValuePair<string, string?>>, Func<string, string?>, Dictionary<string, string>?> WrongDelta(string wrong) => wrong switch
    {
        "keeps every value" => (requested, _) => Collect(requested, (_, _) => true),
        "omits by key presence" => (requested, baseline) => Collect(requested, (key, _) => baseline(key) is null),
        "ignores case in values" => (requested, baseline) => Collect(requested, (key, value) => !string.Equals(baseline(key), value, StringComparison.OrdinalIgnoreCase)),
        "empty map instead of null" => (requested, baseline) => ScriptedDelta(requested, baseline) ?? new Dictionary<string, string>(),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong delta builder."),
    };

    private static Dictionary<string, string>? Collect(IEnumerable<KeyValuePair<string, string?>> requested, Func<string, string, bool> keep)
    {
        var delta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in requested)
        {
            if (value is not null && keep(key, value))
                delta[key] = value;
        }

        return delta.Count > 0 ? delta : null;
    }

    private static bool SameDelta(Dictionary<string, string>? expected, Dictionary<string, string>? actual)
    {
        if (expected is null || actual is null)
            return expected is null && actual is null;

        return expected.Count == actual.Count
            && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && string.Equals(value, pair.Value, StringComparison.Ordinal));
    }
}
