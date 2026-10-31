using QBrainAi.TestSupport.TemporaryInstance;
using Xunit.Sdk;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4/ac-12 and FR-MCP-REPL-008 ac-4: slice I contracts over explicit maps,
/// paths and answers. Scripted positive and negative controls precede the shared implementation binding.
/// No case reads the process environment, file system, network or host platform.
/// </summary>
public sealed class OwnedInstanceIsolationContractTests
{
    internal const string Root = "/scratch/owned";
    internal const string Address = "http://127.0.0.1:49123";

    /// <summary>Inherited configuration keys that must never select the host or database of an owned instance.</summary>
    internal static readonly string[] RemovedKeys =
    [
        "DOTNET_Mcp__IdentityServer__Enabled", "ASPNETCORE_QBrainAi__IdentityServer__Enabled",
        "DOTNET_ConnectionStrings__Mcp", "ASPNETCORE_Mcp__Database__Provider",
        "dotnet_qbrainai__database__sqlite__connectionstring", "aspnetcore_mcp__dataroot",
        "SQLCONNSTR_Mcp", "SQLAZURECONNSTR_Mcp", "MYSQLCONNSTR_Mcp", "POSTGRESQLCONNSTR_Mcp", "CUSTOMCONNSTR_Mcp",
        "CONTENTROOT", "DOTNET_CONTENTROOT", "ASPNETCORE_CONTENTROOT",
        "ENVIRONMENT", "DOTNET_ENVIRONMENT", "ASPNETCORE_ENVIRONMENT",
        "URLS", "DOTNET_URLS", "ASPNETCORE_URLS", "APPLICATIONNAME", "DOTNET_APPLICATIONNAME", "ASPNETCORE_APPLICATIONNAME",
        "HOSTINGSTARTUPASSEMBLIES", "DOTNET_HOSTINGSTARTUPASSEMBLIES", "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES",
    ];

    /// <summary>Unrelated CLI/runtime controls and the owned host values must survive the builder.</summary>
    internal static readonly Dictionary<string, string> OwnSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DOTNET_ENVIRONMENT"] = "Production", ["ASPNETCORE_ENVIRONMENT"] = "Production",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1", ["DOTNET_NOLOGO"] = "1",
        ["Mcp__Database__Provider"] = "sqlite", ["Mcp__Database__Sqlite__ConnectionString"] = "Data Source=/scratch/owned/data/main.db",
    };

    /// <summary>Database rows exercise SQLite synonyms, quoting, rootedness, provider and malformed inputs.</summary>
    internal static readonly (string Name, string? Connection, string Root, string Provider, bool Accepted)[] DatabaseRows =
    [
        ("DataSource synonym", "DataSource=/scratch/owned/data/main.db", Root, "sqlite", true),
        ("Filename synonym", "Filename=/scratch/owned/data/main.db", Root, "sqlite", true),
        ("quoted source", "Data Source=\"/scratch/owned/data/main.db\"", Root, "sqlite", true),
        ("semicolon in quoted source", "Data Source=\"/scratch/owned/data/a;b.db\"", Root, "sqlite", true),
        ("relative path", "Data Source=scratch/owned/data/main.db", Root, "sqlite", false),
        ("relative root", "Data Source=scratch/owned/data/main.db", "scratch/owned", "sqlite", false),
        ("drive-relative path", "Data Source=C:scratch/owned/data/main.db", "C:/scratch/owned", "sqlite", false),
        ("Windows absolute path", "Filename=C:/scratch/owned/data/main.db", "C:/scratch/owned", "sqlite", true),
        ("UNC absolute path", "DataSource=//server/share/owned/main.db", "//server/share/owned", "sqlite", true),
        ("foreign provider", "Data Source=/scratch/owned/data/main.db", Root, "sqlserver", false),
        ("relational SQLite provider", "Data Source=/scratch/owned/data/main.db", Root, "Microsoft.EntityFrameworkCore.Sqlite", true),
        ("last synonym wins outside", "Filename=/scratch/owned/data/main.db;Data Source=/scratch/other/main.db", Root, "sqlite", false),
        ("last synonym wins inside", "Data Source=/scratch/other/main.db;Filename=/scratch/owned/data/main.db", Root, "sqlite", true),
        ("broken quoting", "Data Source=\"/scratch/owned/data/main.db", Root, "sqlite", false),
        ("memory database", "Data Source=:memory:", Root, "sqlite", false),
    ];

    /// <summary>Start contexts must supply the owned server and an owned trust-cache folder.</summary>
    internal static readonly (string Name, Dictionary<string, string?> Environment, bool Accepted)[] StartRows =
    [
        ("owned context", StartEnvironment(Address, "/scratch/owned/cache"), true),
        ("missing address", StartEnvironment(null, "/scratch/owned/cache"), false),
        ("foreign address", StartEnvironment("http://127.0.0.1:49124", "/scratch/owned/cache"), false),
        ("canonical address conflict", StartEnvironment(Address, "/scratch/owned/cache", canonicalAddress: "http://127.0.0.1:49124"), false),
        ("missing cache", StartEnvironment(Address, null), false),
        ("foreign cache", StartEnvironment(Address, "/scratch/other/cache"), false),
        ("cache prefix sibling", StartEnvironment(Address, "/scratch/owned2/cache"), false),
        ("cache parent escape", StartEnvironment(Address, "/scratch/owned/../other/cache"), false),
        ("canonical cache conflict", StartEnvironment(Address, "/scratch/owned/cache", canonicalCache: "/scratch/other/cache"), false),
    ];

    /// <summary>Verifies the complete environment contract against an injected builder, preserving exact values.</summary>
    internal static void AssertEnvironment(Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> builder)
    {
        foreach (var key in RemovedKeys)
        {
            var inherited = new Dictionary<string, string?> { [key] = "foreign", ["PATH"] = "/tools", ["DOTNET_ROOT"] = "/runtime" };
            var expected = new Dictionary<string, string?>(OwnSettings.ToDictionary(p => p.Key, p => (string?)p.Value), StringComparer.OrdinalIgnoreCase)
            {
                ["PATH"] = "/tools", ["DOTNET_ROOT"] = "/runtime",
            };
            var actual = builder(inherited, OwnSettings);
            Assert.True(expected.Count == actual.Count && expected.All(p => actual.TryGetValue(p.Key, out var value) && value == p.Value), key);
        }
    }

    /// <summary>Verifies provider-aware database selection using exactly the same immutable cases for every binding.</summary>
    internal static void AssertDatabase(Func<string?, string, string, string?> check)
    {
        foreach (var row in DatabaseRows)
            Assert.True((check(row.Connection, row.Root, row.Provider) is null) == row.Accepted, row.Name);
    }

    /// <summary>Verifies the server-address and trust-cache part of the start context.</summary>
    internal static void AssertStart(Func<IReadOnlyDictionary<string, string?>, string?> check)
    {
        foreach (var row in StartRows)
            Assert.True((check(row.Environment) is null) == row.Accepted, row.Name);
    }

    /// <summary>Mock gate: the independent scripted responses satisfy the three contracts.</summary>
    [Fact]
    public void ScriptedIsolation_SatisfiesEveryContract()
    {
        AssertEnvironment(ScriptedBuilder);
        AssertDatabase(ScriptedDatabase);
        AssertStart(ScriptedStart);
    }

    /// <summary>Mock gate: retaining any host setting or dropping the owned host/CLI controls is rejected.</summary>
    [Fact]
    public void WrongEnvironmentBuilders_AreRejected()
    {
        foreach (var key in RemovedKeys)
        {
            Assert.ThrowsAny<XunitException>(() => AssertEnvironment((inherited, settings) =>
            {
                var result = ScriptedBuilder(inherited, settings);
                if (inherited.TryGetValue(key, out var value)) result[key] = value;
                return result;
            }));
        }
        Assert.ThrowsAny<XunitException>(() => AssertEnvironment((inherited, settings) =>
            ScriptedBuilder(inherited, settings).Where(p => !p.Key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>Mock gate: every database verdict has a negative control, including each syntax and provider boundary.</summary>
    [Fact]
    public void WrongDatabaseChecks_AreRejectedForEveryRow()
    {
        foreach (var target in DatabaseRows)
        {
            Assert.ThrowsAny<XunitException>(() => AssertDatabase((connection, root, provider) =>
                connection == target.Connection && root == target.Root && provider == target.Provider
                    ? target.Accepted ? "refused" : null
                    : ScriptedDatabase(connection, root, provider)));
        }
    }

    /// <summary>Mock gate: missing/foreign server and cache settings cannot silently pass the start contract.</summary>
    [Fact]
    public void WrongStartChecks_AreRejectedForEveryRow()
    {
        foreach (var target in StartRows)
        {
            Assert.ThrowsAny<XunitException>(() => AssertStart(environment => ReferenceEquals(environment, target.Environment)
                ? target.Accepted ? "refused" : null
                : ScriptedStart(environment)));
        }
    }

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-12: an omitted or empty request nonce is always refused.</summary>
    [Fact]
    public void ScriptedHealth_RejectsMissingNonceControl()
    {
        AssertHealth((status, body, nonce) => status == 200 && nonce.Length > 0 && body is not null && body.Contains(nonce, StringComparison.Ordinal) ? null : "refused");
        Assert.ThrowsAny<XunitException>(() => AssertHealth((status, body, nonce) =>
            status == 200 && body is not null && body.Contains(nonce, StringComparison.Ordinal) ? null : "refused"));
    }

    /// <summary>FR-MCP-REPL-008 ac-4: explicit and unset cache directories use an independent path oracle.</summary>
    [Fact]
    public void ScriptedCacheDirectory_SatisfiesContractAndRejectsWrongSelectors()
    {
        AssertCacheDirectory((configured, profile) => string.IsNullOrWhiteSpace(configured) ? Path.Combine(profile, ".mcpserver") : configured);
        Assert.ThrowsAny<XunitException>(() => AssertCacheDirectory((_, profile) => Path.Combine(profile, ".mcpserver")));
        Assert.ThrowsAny<XunitException>(() => AssertCacheDirectory((configured, _) => configured ?? string.Empty));
    }

    /// <summary>Mock gate: a real consumer must invoke its guard before producing a usable REPL start.</summary>
    [Fact]
    public void ScriptedReplConsumer_EnforcesItsGuard()
    {
        AssertConsumer(ScriptedConsumer);
        Assert.ThrowsAny<XunitException>(() => AssertConsumer(_ => ExpectedStart()));
    }

    /// <summary>Checks guard invocation, refusal propagation and the exact owned arguments and settings.</summary>
    internal static void AssertConsumer(Func<Func<IReadOnlyList<string>, IReadOnlyDictionary<string, string?>, string?>,
        (IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string?> Environment)> create)
    {
        var expected = ExpectedStart();
        var calls = 0;
        var actual = create((arguments, environment) =>
        {
            calls++;
            Assert.Equal(expected.Arguments, arguments);
            foreach (var pair in expected.Environment) Assert.Equal(pair.Value, environment[pair.Key]);
            return null;
        });
        Assert.Equal(1, calls);
        Assert.Equal(expected.Arguments, actual.Arguments);
        foreach (var pair in expected.Environment) Assert.Equal(pair.Value, actual.Environment[pair.Key]);
        calls = 0;
        var refusal = Assert.Throws<InvalidOperationException>(() => create((_, _) => { calls++; return "scripted start refusal"; }));
        Assert.Equal(1, calls);
        Assert.Equal("scripted start refusal", refusal.Message);
    }

    /// <summary>Mock gate: the environment adapter validates before touching the inherited dictionary.</summary>
    [Fact]
    public void ScriptedEnvironmentAdapter_IsAtomicAndReplacesInheritedValues()
    {
        AssertAdapter(ScriptedAdapter);
        Assert.ThrowsAny<XunitException>(() => AssertAdapter((target, settings, accepted) =>
        {
            target.Clear();
            if (!accepted) throw new InvalidOperationException("refused");
            foreach (var pair in settings) target[pair.Key] = pair.Value;
        }));
    }

    /// <summary>Uses explicit dictionaries to verify atomic refusal and accepted replacement, with no process state.</summary>
    internal static void AssertAdapter(Action<IDictionary<string, string?>, IReadOnlyDictionary<string, string>, bool> apply)
    {
        var inherited = new Dictionary<string, string?> { ["MCP_INSTANCE"] = "foreign", ["PATH"] = "/tools" };
        var own = new Dictionary<string, string> { ["MCP_INSTANCE"] = "owned" };
        Assert.Throws<InvalidOperationException>(() => apply(inherited, own, false));
        Assert.True(inherited.TryGetValue("MCP_INSTANCE", out var originalInstance));
        Assert.Equal("foreign", originalInstance);
        Assert.True(inherited.TryGetValue("PATH", out var originalPath));
        Assert.Equal("/tools", originalPath);
        apply(inherited, own, true);
        Assert.True(inherited.TryGetValue("MCP_INSTANCE", out var selectedInstance));
        Assert.Equal("owned", selectedInstance);
        Assert.True(inherited.TryGetValue("PATH", out var selectedPath));
        Assert.Equal("/tools", selectedPath);
        Assert.Equal(2, inherited.Count);
    }

    /// <summary>Asserts the cache path selector against explicit inputs, without reading any real user profile.</summary>
    internal static void AssertCacheDirectory(Func<string?, string, string> resolve)
    {
        Assert.Equal("/scratch/owned/cache", resolve("/scratch/owned/cache", "/profiles/example"));
        Assert.Equal(Path.Combine("/profiles/example", ".mcpserver"), resolve(null, "/profiles/example"));
        Assert.Equal(Path.Combine("/profiles/example", ".mcpserver"), resolve("  ", "/profiles/example"));
    }

    /// <summary>Asserts both nonce-presence boundaries using the same answer and explicit request values.</summary>
    internal static void AssertHealth(Func<int, string?, string, string?> check)
    {
        Assert.Null(check(200, "Healthy nonce-owned", "nonce-owned"));
        Assert.NotNull(check(200, "Healthy nonce-owned", string.Empty));
    }

    /// <summary>RINT-6: each pre-existing database row rejects a wrong verdict through its actual contract assertion.</summary>
    [Fact]
    public void PriorDatabaseRows_RejectEveryWrongVerdict()
    {
        foreach (var target in OwnedDatabaseSelectionTests.Rows)
        {
            var failures = OwnedDatabaseSelectionTests.ContractFailures((connection, root) =>
            {
                var row = OwnedDatabaseSelectionTests.Rows.Single(r => r.ConnectionString == connection && r.Root == root);
                return (row.Name == target.Name ? !row.Accepted : row.Accepted) ? null : "refused";
            });
            Assert.Equal([target.Name], failures);
        }
    }

    /// <summary>RINT-6: every pre-existing workspace/marker, health and restart row rejects a wrong verdict.</summary>
    [Fact]
    public void PriorStartHealthAndRestartRows_RejectEveryWrongVerdict()
    {
        foreach (var target in TemporaryInstanceChecksTests.StartRows)
        {
            Assert.Equal([target.Name], TemporaryInstanceChecksTests.StartContractFailures((arguments, _) =>
            {
                var row = TemporaryInstanceChecksTests.StartRows.Single(r => ReferenceEquals(r.Arguments, arguments));
                return (row.Name == target.Name ? !row.Accepted : row.Accepted) ? null : "refused";
            }));
        }
        foreach (var target in TemporaryInstanceChecksTests.HealthRows)
        {
            Assert.Equal([target.Name], TemporaryInstanceChecksTests.HealthContractFailures((status, body, _) =>
            {
                var row = TemporaryInstanceChecksTests.HealthRows.Single(r => r.Status == status && r.Body == body);
                return (row.Name == target.Name ? !row.Accepted : row.Accepted) ? null : "refused";
            }));
        }
        foreach (var target in TemporaryInstanceChecksTests.RestartRows)
        {
            Assert.Equal([target.Name], TemporaryInstanceChecksTests.RestartContractFailures((starts, exited, output) =>
            {
                var row = TemporaryInstanceChecksTests.RestartRows.Single(r => r.Starts == starts && r.Exited == exited && r.Output == output);
                return row.Name == target.Name ? !row.Restart : row.Restart;
            }));
        }
    }

    /// <summary>RINT-6: every pre-existing environment build and start-check row rejects a wrong response.</summary>
    [Fact]
    public void PriorEnvironmentRows_RejectEveryWrongResponse()
    {
        foreach (var target in OwnedInstanceEnvironmentTests.BuildRows)
        {
            Assert.Equal([target.Name], OwnedInstanceEnvironmentTests.BuildContractFailures((inherited, own) =>
            {
                var row = OwnedInstanceEnvironmentTests.BuildRows.Single(r => ReferenceEquals(r.Inherited, inherited));
                var result = row.Kept.ToDictionary(p => p.Key, p => (string?)p.Value, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in own) result[pair.Key] = pair.Value;
                if (row.Name == target.Name) result["MCP_INSTANCE"] = "wrong";
                return result;
            }));
        }
        foreach (var target in OwnedInstanceEnvironmentTests.CheckRows)
        {
            Assert.Equal([target.Name], OwnedInstanceEnvironmentTests.CheckContractFailures((child, _, _) =>
            {
                var row = OwnedInstanceEnvironmentTests.CheckRows.Single(r => ReferenceEquals(r.Child, child));
                return (row.Name == target.Name ? !row.Accepted : row.Accepted) ? null : "refused";
            }));
        }
    }

    private static Dictionary<string, string?> ScriptedBuilder(IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings)
    {
        var result = inherited.Where(p => p.Key is "PATH" or "DOTNET_ROOT").ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in settings) result[pair.Key] = pair.Value;
        return result;
    }

    private static string? ScriptedDatabase(string? connection, string root, string provider) =>
        DatabaseRows.Single(row => row.Connection == connection && row.Root == root && row.Provider == provider).Accepted ? null : "refused";

    private static string? ScriptedStart(IReadOnlyDictionary<string, string?> environment) =>
        StartRows.Single(row => ReferenceEquals(row.Environment, environment)).Accepted ? null : "refused";

    private static Dictionary<string, string?> StartEnvironment(string? address, string? cache, string? canonicalAddress = null, string? canonicalCache = null)
    {
        var result = new Dictionary<string, string?>();
        if (address is not null) result["MCP_SERVER_URL"] = address;
        if (cache is not null) result["MCP_REPL_CACHE_DIR"] = cache;
        if (canonicalAddress is not null) result["QBRAINAI_SERVER_URL"] = canonicalAddress;
        if (canonicalCache is not null) result["QBRAINAI_REPL_CACHE_DIR"] = canonicalCache;
        return result;
    }

    private static (IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string?> Environment) ExpectedStart() =>
        (["host.dll", "--agent-stdio", "--agent", "test-agent", "--workspace-path", "/scratch/owned/workspace", "--marker-file", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml"],
            new Dictionary<string, string?>
            {
                ["MCP_SERVER_URL"] = Address, ["QBRAINAI_REPL_CACHE_DIR"] = Path.Combine(Root, "repl-cache"),
                ["MCP_WORKSPACE_PATH"] = "/scratch/owned/workspace", ["PLUGIN_AGENT_NAME"] = "test-agent",
            });

    private static (IReadOnlyList<string> Arguments, IReadOnlyDictionary<string, string?> Environment) ScriptedConsumer(
        Func<IReadOnlyList<string>, IReadOnlyDictionary<string, string?>, string?> guard)
    {
        var result = ExpectedStart();
        var error = guard(result.Arguments, result.Environment);
        if (error is not null) throw new InvalidOperationException(error);
        return result;
    }

    private static void ScriptedAdapter(IDictionary<string, string?> target, IReadOnlyDictionary<string, string> settings, bool accepted)
    {
        if (!accepted) throw new InvalidOperationException("refused");
        var result = target.Where(pair => pair.Key == "PATH").ToDictionary(pair => pair.Key, pair => pair.Value);
        foreach (var pair in settings) result[pair.Key] = pair.Value;
        target.Clear();
        foreach (var pair in result) target[pair.Key] = pair.Value;
    }
}
