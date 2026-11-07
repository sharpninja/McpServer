using QBrainAi.TestSupport.TemporaryInstance;
using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests the environment a temporary instance starts its server with: every inherited variable in the product's
/// configuration and variable namespaces (Mcp__, QBrainAi__, MCP_, QBRAINAI_, MCPSERVER_ and ConnectionStrings__, matched
/// ignoring case) and the provider fallbacks DATABASE_URL and POSTGRES_CONNECTION_STRING are removed, the instance's own
/// settings are applied, and everything else is kept. Also tests the check made before the server starts: every
/// product-namespace value is one the instance set, and every database the instance selected lies inside its owned
/// root.
/// </summary>
/// <remarks>
/// Requirement coverage: TR-MCP-NUKE-019 AC-6 as replaced on 2026-10-29 (the temporary instance's database is the one
/// inside its owned root; no inherited setting, in configuration or the process environment and in the canonical or
/// legacy form, selects another database: a connection string, a data source, a database provider or an instance name;
/// a selection outside the owned root is refused before the instance starts; operator 2026-10-29: "Strengthen ACs, fix
/// both (Recommended)"; hostile validation round 8b finding R8B-F3). Test data: scripted inherited maps, the instance's
/// own settings and the child environment each must give, as literal strings with both separator styles, run against
/// builders and checks passed in, so the contract is proved on scripted references with negative controls before the
/// shared functions exist; no test reads or writes the process environment, the file system or the network.
/// </remarks>
public sealed class OwnedInstanceEnvironmentTests
{
    private const string OwnedRoot = "/scratch/owned";
    private const string OwnedDatabase = "/scratch/owned/data/qbrainai.db";
    private const string LiveDatabase = "/home/example/live/qbrainai.db";

    /// <summary>The instance's own settings in the scripted rows: its database in both forms, its workspace and port.</summary>
    internal static readonly Dictionary<string, string> Owned = OwnedSettings(OwnedDatabase, "/scratch/owned/workspace");

    /// <summary>
    /// The builder contract rows: a row name, the inherited environment, and the extra variables (beyond the instance's
    /// own settings) the child environment must keep.
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Inherited, Dictionary<string, string> Kept)[] BuildRows =
    [
        ("an inherited external connection string is removed",
            new() { ["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase, ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
        ("the canonical connection string is removed",
            new() { ["QBrainAi__Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("the legacy data source variable is removed",
            new() { ["MCP_SQLITE_DATA_SOURCE"] = LiveDatabase },
            new()),
        ("canonical and legacy forms together are removed",
            new() { ["QBRAINAI_SQLITE_DATA_SOURCE"] = LiveDatabase, ["Mcp__Database__Sqlite__DataSource"] = LiveDatabase },
            new()),
        ("an inherited provider is removed",
            new() { ["Mcp__Database__Provider"] = "postgresql", ["MCP_DATABASE_PROVIDER"] = "postgresql" },
            new()),
        ("an inherited instance name and its settings are removed",
            new() { ["QBRAINAI_INSTANCE"] = "live", ["MCP_INSTANCE"] = "live", ["Mcp__Instances__live__Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("the provider fallbacks are removed",
            new() { ["DATABASE_URL"] = "postgres://live/qbrainai", ["POSTGRES_CONNECTION_STRING"] = "Host=live", ["ConnectionStrings__Mcp"] = "Host=live" },
            new()),
        ("a namespace written in another case is removed",
            new() { ["qbrainai__database__sqlite__connectionstring"] = "Data Source=" + LiveDatabase, ["Mcpserver_Workspace_Path"] = "/home/example/github" },
            new()),
        ("the owned database stays selected exactly",
            new(),
            new()),
        ("unrelated variables are kept",
            new() { ["PATH"] = "/usr/bin", ["HOME"] = "/home/example", ["DOTNET_ROOT"] = "/usr/share/dotnet" },
            new() { ["PATH"] = "/usr/bin", ["HOME"] = "/home/example", ["DOTNET_ROOT"] = "/usr/share/dotnet" }),
    ];

    /// <summary>
    /// The start-check contract rows: a row name, the child environment, the instance's own settings, the owned root,
    /// and whether the check must accept the environment.
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Child, Dictionary<string, string> Settings, string Root, bool Accepted)[] CheckRows =
    [
        ("the owned layout", With(Owned, ("PATH", "/usr/bin")), Owned, OwnedRoot, true),
        ("the owned layout with Windows separators",
            With(OwnedSettings(@"C:\scratch\owned\data\qbrainai.db", @"C:\scratch\owned\workspace")),
            OwnedSettings(@"C:\scratch\owned\data\qbrainai.db", @"C:\scratch\owned\workspace"), @"C:\scratch\owned", true),
        ("an inherited product key left in place", With(Owned, ("QBRAINAI_INSTANCE", "live")), Owned, OwnedRoot, false),
        ("an owned value changed after the build", With(Owned, ("Mcp__Database__Sqlite__ConnectionString", "Data Source=" + LiveDatabase)), Owned, OwnedRoot, false),
        ("a database outside the owned root",
            With(OwnedSettings(LiveDatabase, "/scratch/owned/workspace")), OwnedSettings(LiveDatabase, "/scratch/owned/workspace"), OwnedRoot, false),
        ("a sibling root sharing the prefix",
            With(OwnedSettings("/scratch/owned2/data/qbrainai.db", "/scratch/owned/workspace")), OwnedSettings("/scratch/owned2/data/qbrainai.db", "/scratch/owned/workspace"), OwnedRoot, false),
        ("a parent-segment escape",
            With(OwnedSettings("/scratch/owned/../live/qbrainai.db", "/scratch/owned/workspace")), OwnedSettings("/scratch/owned/../live/qbrainai.db", "/scratch/owned/workspace"), OwnedRoot, false),
    ];

    /// <summary>
    /// Returns the names of the builder rows on which <paramref name="build"/> gives the wrong child environment: the
    /// instance's own settings plus exactly the kept variables (ordinal values; keys compared ignoring case).
    /// </summary>
    /// <param name="build">The builder under test: the inherited environment and the instance's settings to the child environment.</param>
    internal static List<string> BuildContractFailures(Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var failures = new List<string>();
        foreach (var row in BuildRows)
        {
            var expected = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in row.Kept)
                expected[key] = value;
            foreach (var (key, value) in Owned)
                expected[key] = value;

            var actual = build(row.Inherited, Owned);
            var same = actual.Count == expected.Count
                && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && string.Equals(value, pair.Value, StringComparison.Ordinal));
            if (!same)
                failures.Add(row.Name);
        }

        return failures;
    }

    /// <summary>Returns the names of the start-check rows on which <paramref name="check"/> gives the wrong verdict.</summary>
    /// <param name="check">The check under test: the child environment, the instance's settings and the owned root to a violation or null.</param>
    internal static List<string> CheckContractFailures(Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return CheckRows
            .Where(row => (check(row.Child, row.Settings, row.Root) is null) != row.Accepted)
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>Mock gate positive control: the scripted reference builder and check satisfy every row.</summary>
    [Fact]
    public void ScriptedOwnedEnvironment_SatisfiesTheContract()
    {
        Assert.Empty(BuildContractFailures(ScriptedBuild));
        Assert.Empty(CheckContractFailures(ScriptedCheck));
    }

    /// <summary>
    /// Mock gate negative controls on the builder, each rejected inside a passing harness on the row named: a builder
    /// that keeps inherited values, one that removes only the legacy forms, one that drops the instance's own settings
    /// (refusing everything), one that matches the namespaces case-sensitively, and one that keeps the provider
    /// fallbacks.
    /// </summary>
    /// <param name="wrong">The name of the wrong builder.</param>
    /// <param name="failingRow">The builder row that must reject it.</param>
    [Theory]
    [InlineData("keeps inherited values", "canonical and legacy forms together are removed")]
    [InlineData("removes only the legacy forms", "canonical and legacy forms together are removed")]
    [InlineData("drops the owned settings", "the owned database stays selected exactly")]
    [InlineData("case-sensitive namespaces", "a namespace written in another case is removed")]
    [InlineData("keeps the provider fallbacks", "the provider fallbacks are removed")]
    public void WrongBuilders_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, BuildContractFailures(WrongBuild(wrong)));

    /// <summary>
    /// Mock gate negative controls on the start check, each rejected on the row named: a check that accepts anything,
    /// one that checks only the owned database paths, and one that compares a string prefix without a segment
    /// boundary.
    /// </summary>
    /// <param name="wrong">The name of the wrong check.</param>
    /// <param name="failingRow">The check row that must reject it.</param>
    [Theory]
    [InlineData("accepts anything", "a database outside the owned root")]
    [InlineData("database paths only", "an inherited product key left in place")]
    [InlineData("string prefix", "a sibling root sharing the prefix")]
    public void WrongChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, CheckContractFailures(WrongCheck(wrong)));

    /// <summary>
    /// Binding: the shared builder and check, <see cref="TemporaryInstanceChecks.BuildOwnedInstanceEnvironment"/> and
    /// <see cref="TemporaryInstanceChecks.CheckOwnedInstanceEnvironment"/> (TR-MCP-NUKE-019 AC-6), satisfy the same rows
    /// the scripted references passed and the wrong builders and checks failed.
    /// </summary>
    [Fact]
    public void SharedOwnedEnvironment_SatisfiesTheContract()
    {
        Assert.Empty(BuildContractFailures(TemporaryInstanceChecks.BuildOwnedInstanceEnvironment));
        Assert.Empty(CheckContractFailures(TemporaryInstanceChecks.CheckOwnedInstanceEnvironment));
    }

    /// <summary>
    /// Binding, the reviewer's named case: an inherited connection string that selects a database outside the owned
    /// root does not reach the child environment, and the owned connection string is the one it holds.
    /// </summary>
    [Fact]
    public void OwnedInstanceConfiguration_RejectsExternalConnectionString()
    {
        var child = TemporaryInstanceChecks.BuildOwnedInstanceEnvironment(
            new Dictionary<string, string?> { ["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase },
            Owned);
        Assert.Equal("Data Source=" + OwnedDatabase, child["Mcp__Database__Sqlite__ConnectionString"]);
        Assert.Null(TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(child, Owned, OwnedRoot));
        Assert.NotNull(TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(
            With(Owned, ("Mcp__Database__Sqlite__ConnectionString", "Data Source=" + LiveDatabase)), Owned, OwnedRoot));
    }

    /// <summary>
    /// Binding, the reviewer's named case: canonical and legacy forms of the database settings, separately and
    /// together, cannot change the owned database.
    /// </summary>
    [Fact]
    public void OwnedInstanceConfiguration_CanonicalAndLegacyOverridesCannotEscape()
    {
        foreach (var inherited in new[]
                 {
                     new Dictionary<string, string?> { ["QBrainAi__Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase },
                     new Dictionary<string, string?> { ["MCP_SQLITE_DATA_SOURCE"] = LiveDatabase },
                     new Dictionary<string, string?> { ["QBRAINAI_SQLITE_DATA_SOURCE"] = LiveDatabase, ["Mcp__Database__Sqlite__DataSource"] = LiveDatabase },
                 })
        {
            var child = TemporaryInstanceChecks.BuildOwnedInstanceEnvironment(inherited, Owned);
            Assert.DoesNotContain(child, pair => pair.Value is not null && pair.Value.Contains(LiveDatabase, StringComparison.Ordinal));
            Assert.Null(TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(child, Owned, OwnedRoot));
        }
    }

    /// <summary>
    /// Binding, the reviewer's named case: a valid owned configuration keeps exactly the expected database selection,
    /// so an implementation that refuses every input cannot satisfy it.
    /// </summary>
    [Fact]
    public void OwnedInstanceConfiguration_OwnedDatabaseRemainsSelected()
    {
        var child = TemporaryInstanceChecks.BuildOwnedInstanceEnvironment(new Dictionary<string, string?>(), Owned);
        Assert.Equal(OwnedDatabase, child["MCP_SQLITE_DATA_SOURCE"]);
        Assert.Equal("Data Source=" + OwnedDatabase, child["QBrainAi__Database__Sqlite__ConnectionString"]);
        Assert.Equal("sqlite", child["Mcp__Database__Provider"]);
        Assert.Null(TemporaryInstanceChecks.CheckOwnedInstanceEnvironment(child, Owned, OwnedRoot));
    }

    private static readonly string[] ProductPrefixes = ["Mcp__", "QBrainAi__", "MCP_", "QBRAINAI_", "MCPSERVER_", "ConnectionStrings__"];
    private static readonly string[] ProviderFallbacks = ["DATABASE_URL", "POSTGRES_CONNECTION_STRING"];

    private static Dictionary<string, string> OwnedSettings(string database, string workspace) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["MCP_SQLITE_DATA_SOURCE"] = database,
        ["Mcp__Database__Provider"] = "sqlite",
        ["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=" + database,
        ["QBrainAi__Database__Provider"] = "sqlite",
        ["QBrainAi__Database__Sqlite__ConnectionString"] = "Data Source=" + database,
        ["MCP_WORKSPACE_PATH"] = workspace,
        ["PORT"] = "49123",
    };

    private static Dictionary<string, string?> With(Dictionary<string, string> settings, params (string Key, string Value)[] extra)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in settings)
            map[key] = value;
        foreach (var (key, value) in extra)
            map[key] = value;
        return map;
    }

    private static bool IsProductKey(string key, StringComparison comparison) =>
        ProductPrefixes.Any(prefix => key.StartsWith(prefix, comparison))
        || ProviderFallbacks.Any(name => string.Equals(key, name, comparison));

    private static Dictionary<string, string?> ScriptedBuild(IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings) =>
        BuildWith(inherited, settings, key => IsProductKey(key, StringComparison.OrdinalIgnoreCase), applySettings: true);

    private static Dictionary<string, string?> BuildWith(IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings, Func<string, bool> remove, bool applySettings)
    {
        var child = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in inherited)
        {
            if (!remove(key))
                child[key] = value;
        }

        if (applySettings)
        {
            foreach (var (key, value) in settings)
                child[key] = value;
        }

        return child;
    }

    private static Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> WrongBuild(string wrong) => wrong switch
    {
        "keeps inherited values" => (inherited, settings) => BuildWith(inherited, settings, _ => false, applySettings: true),
        "removes only the legacy forms" => (inherited, settings) => BuildWith(inherited, settings,
            key => key.StartsWith("Mcp__", StringComparison.OrdinalIgnoreCase) || key.StartsWith("MCP_", StringComparison.OrdinalIgnoreCase), applySettings: true),
        "drops the owned settings" => (inherited, settings) => BuildWith(inherited, settings, key => IsProductKey(key, StringComparison.OrdinalIgnoreCase), applySettings: false),
        "case-sensitive namespaces" => (inherited, settings) => BuildWith(inherited, settings, key => IsProductKey(key, StringComparison.Ordinal), applySettings: true),
        "keeps the provider fallbacks" => (inherited, settings) => BuildWith(inherited, settings,
            key => ProductPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)), applySettings: true),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong builder."),
    };

    private static string? ScriptedCheck(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings, string root)
    {
        foreach (var (key, value) in child)
        {
            if (IsProductKey(key, StringComparison.OrdinalIgnoreCase)
                && (!settings.TryGetValue(key, out var own) || !string.Equals(own, value, StringComparison.Ordinal)))
                return $"The temporary instance's environment holds '{key}', which the instance did not set.";
        }

        foreach (var database in DatabasePaths(settings))
        {
            if (!SegmentInside(database, root, refuseParentSegments: true))
                return $"The temporary instance's database '{database}' is not inside its owned root '{root}'.";
        }

        return null;
    }

    private static Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, string, string?> WrongCheck(string wrong) => wrong switch
    {
        "accepts anything" => (_, _, _) => null,
        "database paths only" => (_, settings, root) =>
            DatabasePaths(settings).All(database => SegmentInside(database, root, refuseParentSegments: true)) ? null : "outside",
        "string prefix" => (child, settings, root) =>
            DatabasePaths(settings).All(database => database.StartsWith(root, StringComparison.Ordinal) && !database.Contains("..", StringComparison.Ordinal))
                ? ScriptedLeftoverCheck(child, settings)
                : "outside",
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong check."),
    };

    private static string? ScriptedLeftoverCheck(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings) =>
        child.Any(pair => IsProductKey(pair.Key, StringComparison.OrdinalIgnoreCase)
            && (!settings.TryGetValue(pair.Key, out var own) || !string.Equals(own, pair.Value, StringComparison.Ordinal)))
            ? "leftover"
            : null;

    private static IEnumerable<string> DatabasePaths(IReadOnlyDictionary<string, string> settings)
    {
        foreach (var (key, value) in settings)
        {
            if (key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase))
                yield return DataSourceOf(value);
            else if (key.Contains("DATA_SOURCE", StringComparison.OrdinalIgnoreCase) || key.Contains("DataSource", StringComparison.OrdinalIgnoreCase))
                yield return value;
        }
    }

    private static string DataSourceOf(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(part[..separator].Trim(), "Data Source", StringComparison.OrdinalIgnoreCase))
                return part[(separator + 1)..].Trim();
        }

        return connectionString;
    }

    private static bool SegmentInside(string path, string root, bool refuseParentSegments)
    {
        var pathSegments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        var rootSegments = root.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (refuseParentSegments && pathSegments.Any(segment => segment is "." or ".."))
            return false;
        return pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }
}
