using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests configuration-key identity in the environment a temporary instance starts with (FR-MCP-TESTRESOURCE-001 ac-4,
/// appended 2026-11-05, "A setting is identified by its configuration key, not by its spelling"): an inherited colon,
/// double-underscore or mixed-separator spelling of a product key or of the host's endpoint section (Kestrel), alone or
/// host-prefixed, in either enumeration order, changes nothing in the instance's effective configuration, unrelated
/// variables are kept, and the start check refuses a product key the instance did not set, under any spelling, and two
/// names of one product key that differ only in separators or case.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-4, the unit-proved rule TR-MCP-NUKE-019 AC-6 relies on. Test data:
/// scripted inherited maps, the owned settings of <see cref="OwnedInstanceEnvironmentTests.Owned"/> and literal start
/// environments, as plain strings. The oracle for the builder rows is <see cref="EffectiveConfiguration"/>, an
/// independent statement of how .NET configuration reads a process environment: the environment-variable provider
/// replaces "__" with ":", ignores case, lets a later variable of one key replace an earlier one and maps the five
/// connection-string prefixes into ConnectionStrings, and the host also reads the DOTNET_ and ASPNETCORE_ names with
/// the prefix removed (dotnet/runtime v10.0.0, EnvironmentVariablesConfigurationProvider). Each builder row is judged
/// with the inherited map and the child environment each enumerated in both orders. No test reads or writes the process
/// environment, the file system or the network.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): added the key-identity builder, start-check and pipeline rows with an independent oracle, a scripted reference, wrong builders and checks and per-row wrong responses (mock gate), because hostile validation round 12 finding R12-F1 showed that matching raw prefixes let an inherited colon spelling of a connection string reach the instance.</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (same appended sentence): appended the two bindings to the shared builder and start check, unchanged rows and assertions, after the mock gate passed and TemporaryInstanceChecks read configuration keys (hostile validation round 12 finding R12-F1).</description></item>
/// </list>
/// </remarks>
public sealed class OwnedInstanceKeyIdentityContractTests
{
    private const string OwnedRoot = "/scratch/owned";
    private const string OwnedDatabase = "/scratch/owned/data/qbrainai.db";
    private const string LiveDatabase = "/home/example/live/qbrainai.db";
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

    private static readonly string[] HostPrefixes = ["DOTNET_", "ASPNETCORE_"];
    private static readonly string[] ConnectionStringPrefixes = ["SQLCONNSTR_", "SQLAZURECONNSTR_", "MYSQLCONNSTR_", "POSTGRESQLCONNSTR_", "CUSTOMCONNSTR_"];
    private static readonly string[] ReferenceSections = ["Mcp", "QBrainAi", "ConnectionStrings", "Kestrel"];
    private static readonly string[] ReferenceFlatPrefixes = ["MCP_", "QBRAINAI_", "MCPSERVER_"];
    private static readonly string[] ReferenceNames =
        ["DATABASE_URL", "POSTGRES_CONNECTION_STRING", "CONTENTROOT", "ENVIRONMENT", "URLS", "APPLICATIONNAME", "WEBROOT",
            "HTTP_PORTS", "HTTPS_PORTS", "HOSTINGSTARTUPASSEMBLIES", "HOSTINGSTARTUPEXCLUDEASSEMBLIES", "PREVENTHOSTINGSTARTUP"];
    private static readonly string[] RawPrefixes = ["Mcp__", "QBrainAi__", "MCP_", "QBRAINAI_", "MCPSERVER_", "ConnectionStrings__"];

    /// <summary>The instance's own settings in every row (shared with <see cref="OwnedInstanceEnvironmentTests"/>).</summary>
    private static Dictionary<string, string> Owned => OwnedInstanceEnvironmentTests.Owned;

    /// <summary>
    /// The builder rows: a row name, the inherited environment, and the inherited variables the child environment must
    /// keep beside the instance's own settings.
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Inherited, Dictionary<string, string> Kept)[] BuildRows =
    [
        ("a colon spelling of the canonical connection string",
            new() { ["QBrainAi:Database:Sqlite:ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("a colon spelling of the legacy connection string",
            new() { ["Mcp:Database:Sqlite:ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("a mixed spelling with the colon first",
            new() { ["QBrainAi:Database__Sqlite__ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("a mixed spelling with the double underscore first",
            new() { ["Mcp__Database:Sqlite:ConnectionString"] = "Data Source=" + LiveDatabase },
            new()),
        ("colon spellings of a data source and a provider",
            new() { ["Mcp:Database:Sqlite:DataSource"] = LiveDatabase, ["QBrainAi:Database:Provider"] = "postgresql" },
            new()),
        ("host-prefixed colon and mixed spellings",
            new() { ["DOTNET_QBrainAi:Database:Sqlite:ConnectionString"] = "Data Source=" + LiveDatabase, ["ASPNETCORE_Mcp:Database__Provider"] = "sqlserver" },
            new()),
        ("colon spellings of the connection strings section",
            new() { ["ConnectionStrings:Mcp"] = "Host=live", ["connectionstrings:mcpsqlserver"] = "Server=live" },
            new()),
        ("spellings of the workspace, port and endpoint settings",
            new()
            {
                ["Mcp:Workspaces:0:WorkspacePath"] = "/home/example/github", ["QBrainAi:Port"] = "7147",
                ["Kestrel:Endpoints:Http:Url"] = "http://127.0.0.1:7147", ["Kestrel__Endpoints__Https__Url"] = "https://127.0.0.1:7148",
            },
            new()),
        ("spellings of an owned key in another case and separators",
            new() { ["mcp:database:sqlite:connectionstring"] = "Data Source=" + LiveDatabase, ["QBRAINAI__DATABASE__SQLITE__CONNECTIONSTRING"] = "Data Source=" + LiveDatabase },
            new()),
        ("a triple underscore and a bare section key",
            new() { ["Mcp___Database"] = LiveDatabase, ["QBrainAi"] = "live" },
            new()),
        ("unrelated colon and mixed keys are kept",
            new() { ["Logging:LogLevel:Default"] = "Debug", ["Serilog__MinimumLevel:Default"] = "Information", ["PATH"] = "/usr/bin" },
            new() { ["Logging:LogLevel:Default"] = "Debug", ["Serilog__MinimumLevel:Default"] = "Information", ["PATH"] = "/usr/bin" }),
    ];

    /// <summary>
    /// The start-check rows: a row name, the child environment, the instance's own settings, and whether the check must
    /// accept the environment (the owned root is <c>/scratch/owned</c>).
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Child, Dictionary<string, string> Settings, bool Accepted)[] CheckRows =
    [
        ("the owned layout", Child(Owned, ("PATH", "/usr/bin")), Owned, true),
        ("an unrelated colon key", Child(Owned, ("Logging:LogLevel:Default", "Debug")), Owned, true),
        ("an unowned colon spelling of the database",
            Child(Owned, ("QBrainAi:Database:Sqlite:ConnectionString", "Data Source=" + LiveDatabase)), Owned, false),
        ("an unowned mixed spelling of the data source", Child(Owned, ("Mcp:Database__Sqlite__DataSource", LiveDatabase)), Owned, false),
        ("an unowned host-prefixed colon spelling",
            Child(Owned, ("DOTNET_Mcp:Database:Sqlite:ConnectionString", "Data Source=" + LiveDatabase)), Owned, false),
        ("an unowned endpoint setting", Child(Owned, ("Kestrel:Endpoints:Http:Url", "http://127.0.0.1:7147")), Owned, false),
        ("two owned names for one key",
            Child(OwnedWith(null, ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + OwnedDatabase))),
            OwnedWith(null, ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + OwnedDatabase)), false),
        ("an owned colon provider naming SQLite",
            Child(OwnedWith("QBrainAi__Database__Provider", ("QBrainAi:Database:Provider", "sqlite"))),
            OwnedWith("QBrainAi__Database__Provider", ("QBrainAi:Database:Provider", "sqlite")), true),
        ("an owned colon provider naming another engine",
            Child(OwnedWith("QBrainAi__Database__Provider", ("QBrainAi:Database:Provider", "postgresql"))),
            OwnedWith("QBrainAi__Database__Provider", ("QBrainAi:Database:Provider", "postgresql")), false),
        ("an owned colon database outside the root",
            Child(OwnedWith("Mcp__Database__Sqlite__ConnectionString", ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + LiveDatabase))),
            OwnedWith("Mcp__Database__Sqlite__ConnectionString", ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + LiveDatabase)), false),
        ("an owned colon database inside the root",
            Child(OwnedWith("Mcp__Database__Sqlite__ConnectionString", ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + OwnedDatabase))),
            OwnedWith("Mcp__Database__Sqlite__ConnectionString", ("Mcp:Database:Sqlite:ConnectionString", "Data Source=" + OwnedDatabase)), true),
    ];

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 oracle: the configuration .NET builds from <paramref name="environment"/>, read in the
    /// given order. The host layers read the DOTNET_ and ASPNETCORE_ names with the prefix removed; the environment layer
    /// reads every name; each layer replaces "__" with ":", maps the connection-string prefixes into ConnectionStrings
    /// (the provider-name keys .NET also adds for some prefixes are left out; no row depends on them), compares keys
    /// ignoring case, and a later variable of one key replaces an earlier one; a later layer replaces an earlier one.
    /// </summary>
    /// <param name="environment">The environment, in the order it is read.</param>
    /// <returns>The effective configuration, keys compared ignoring case.</returns>
    internal static Dictionary<string, string?> EffectiveConfiguration(IEnumerable<KeyValuePair<string, string?>> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var pairs = environment.ToList();
        var view = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prefix in HostPrefixes)
        {
            foreach (var (key, value) in pairs)
            {
                if (key.StartsWith(prefix, IgnoreCase))
                    view[ConfigurationName(key[prefix.Length..])] = value;
            }
        }

        foreach (var (key, value) in pairs)
            view[ConfigurationName(key)] = value;

        return view;
    }

    /// <summary>
    /// Returns the names of the builder rows on which <paramref name="build"/> gives a child environment whose effective
    /// configuration differs from that of the instance's own settings plus the kept variables, for either enumeration
    /// order of the inherited map and of the child environment.
    /// </summary>
    /// <param name="build">The builder under test: the inherited environment and the instance's settings to the child environment.</param>
    /// <returns>The failing row names.</returns>
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
            var expectedView = EffectiveConfiguration(expected);

            var same = true;
            foreach (var inherited in BothOrders(row.Inherited))
            {
                var child = build(inherited, Owned);
                same &= SameView(EffectiveConfiguration(child), expectedView)
                    && SameView(EffectiveConfiguration(child.Reverse()), expectedView);
            }

            if (!same)
                failures.Add(row.Name);
        }

        return failures;
    }

    /// <summary>Returns the names of the start-check rows on which <paramref name="check"/> gives the wrong verdict for either enumeration order.</summary>
    /// <param name="check">The check under test: the child environment, the instance's settings and the owned root to a violation or null.</param>
    /// <returns>The failing row names.</returns>
    internal static List<string> CheckContractFailures(Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return CheckRows
            .Where(row => BothOrders(row.Child).Any(child => (check(child, row.Settings, OwnedRoot) is null) != row.Accepted))
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>
    /// Returns the names of the builder rows whose built child environment <paramref name="check"/> refuses: the builder
    /// and the start check are used together before a temporary instance starts, so every built environment must pass.
    /// </summary>
    /// <param name="build">The builder.</param>
    /// <param name="check">The start check.</param>
    /// <returns>The failing row names.</returns>
    internal static List<string> PipelineFailures(
        Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> build,
        Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, string, string?> check)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(check);
        return BuildRows
            .Where(row => BothOrders(row.Inherited).Any(inherited => check(build(inherited, Owned), Owned, OwnedRoot) is not null))
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>
    /// Mock gate, the oracle pinned to the documented .NET reading: two spellings of one key give the later value in each
    /// order, a host-prefixed name also sets the key without the prefix, and a connection-string prefix maps into
    /// ConnectionStrings.
    /// </summary>
    [Fact]
    public void Oracle_ReadsSpellingsAsOneKeyInEnumerationOrder()
    {
        var spellings = new List<KeyValuePair<string, string?>> { new("Mcp__Database__Provider", "first"), new("mcp:database:provider", "second") };
        Assert.Equal("second", EffectiveConfiguration(spellings)["Mcp:Database:Provider"]);
        spellings.Reverse();
        Assert.Equal("first", EffectiveConfiguration(spellings)["Mcp:Database:Provider"]);
        Assert.Single(EffectiveConfiguration(spellings));

        var hosted = EffectiveConfiguration([new("DOTNET_QBrainAi__Port", "7147")]);
        Assert.Equal("7147", hosted["QBrainAi:Port"]);
        Assert.Equal("7147", hosted["DOTNET_QBrainAi:Port"]);
        Assert.Equal("Host=live", EffectiveConfiguration([new("SQLCONNSTR_Mcp", "Host=live")])["ConnectionStrings:Mcp"]);
    }

    /// <summary>Mock gate positive control: the scripted reference builder and check satisfy every builder, check and pipeline row.</summary>
    [Fact]
    public void ScriptedKeyIdentity_SatisfiesTheContract()
    {
        Assert.Empty(BuildContractFailures(ScriptedBuild));
        Assert.Empty(CheckContractFailures(ScriptedCheck));
        Assert.Empty(PipelineFailures(ScriptedBuild, ScriptedCheck));
    }

    /// <summary>
    /// Mock gate negative controls on the builder, each rejected inside a passing harness on the row named: the round 12
    /// raw-prefix builder, one that knows both separators but not the host prefixes, one that keeps the endpoint
    /// section, one that removes every key holding a separator, and one that drops the instance's own settings.
    /// </summary>
    /// <param name="wrong">The name of the wrong builder.</param>
    /// <param name="failingRow">The builder row that must reject it.</param>
    [Theory]
    [InlineData("raw prefixes only", "a colon spelling of the canonical connection string")]
    [InlineData("raw prefixes only", "a mixed spelling with the colon first")]
    [InlineData("raw prefixes only", "spellings of an owned key in another case and separators")]
    [InlineData("both separators, no host prefixes", "host-prefixed colon and mixed spellings")]
    [InlineData("keeps the endpoint section", "spellings of the workspace, port and endpoint settings")]
    [InlineData("removes every key with a separator", "unrelated colon and mixed keys are kept")]
    [InlineData("drops the owned settings", "unrelated colon and mixed keys are kept")]
    public void WrongKeyIdentityBuilders_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, BuildContractFailures(WrongBuild(wrong)));

    /// <summary>
    /// Mock gate negative controls on the start check, each rejected on the row named: the round 12 raw-prefix check, one
    /// without the rule for two names of one key, one whose provider rule reads only double-underscore names, and one
    /// that refuses every name holding a colon.
    /// </summary>
    /// <param name="wrong">The name of the wrong check.</param>
    /// <param name="failingRow">The check row that must reject it.</param>
    [Theory]
    [InlineData("raw prefixes only", "an unowned colon spelling of the database")]
    [InlineData("raw prefixes only", "an unowned host-prefixed colon spelling")]
    [InlineData("raw prefixes only", "an owned colon provider naming another engine")]
    [InlineData("no rule for two names", "two owned names for one key")]
    [InlineData("provider rule on double underscores only", "an owned colon provider naming another engine")]
    [InlineData("refuses every colon key", "an owned colon provider naming SQLite")]
    [InlineData("refuses every colon key", "an unrelated colon key")]
    public void WrongKeyIdentityChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, CheckContractFailures(WrongCheck(wrong)));

    /// <summary>
    /// Mock gate: the start check catches what a wrong builder leaves; the round 12 raw-prefix builder followed by the
    /// scripted check refuses the colon spelling it kept.
    /// </summary>
    [Fact]
    public void RawPrefixBuilder_LeavesAnAliasTheCheckRefuses() =>
        Assert.Contains("a colon spelling of the canonical connection string", PipelineFailures(WrongBuild("raw prefixes only"), ScriptedCheck));

    /// <summary>
    /// Mock gate: every builder row and every start-check row rejects a response that is wrong on that row alone (a
    /// child environment missing one owned setting, or the opposite verdict), and only that row.
    /// </summary>
    [Fact]
    public void EveryKeyIdentityRow_RejectsAWrongResponse()
    {
        foreach (var target in BuildRows)
        {
            Assert.Equal([target.Name], BuildContractFailures((inherited, settings) =>
            {
                var result = ScriptedBuild(inherited, settings);
                if (SameKeys(inherited, target.Inherited))
                    result.Remove("MCP_WORKSPACE_PATH");
                return result;
            }));
        }

        foreach (var target in CheckRows)
        {
            Assert.Equal([target.Name], CheckContractFailures((child, settings, root) =>
                SameMap(child, target.Child)
                    ? target.Accepted ? "wrong refusal" : null
                    : ScriptedCheck(child, settings, root)));
        }
    }

    /// <summary>
    /// Binding, the reviewer's named case: the shared builder,
    /// <see cref="QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.BuildOwnedInstanceEnvironment"/>, satisfies
    /// every builder row the scripted reference passed and the wrong builders failed, and every child environment it
    /// builds passes the shared start check.
    /// </summary>
    [Fact]
    public void OwnedEnvironment_NormalizedAliasesCannotOverrideDatabase()
    {
        Assert.Empty(BuildContractFailures(QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.BuildOwnedInstanceEnvironment));
        Assert.Empty(PipelineFailures(
            QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.BuildOwnedInstanceEnvironment,
            QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.CheckOwnedInstanceEnvironment));
    }

    /// <summary>
    /// Binding, the reviewer's named case: the shared start check,
    /// <see cref="QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.CheckOwnedInstanceEnvironment"/>, gives
    /// every start-check row's verdict in both enumeration orders, refusing each unowned spelling of a product key and two
    /// names of one key before the instance starts.
    /// </summary>
    [Fact]
    public void StartCheck_RejectsUnownedNormalizedAliases() =>
        Assert.Empty(CheckContractFailures(QBrainAi.TestSupport.TemporaryInstance.TemporaryInstanceChecks.CheckOwnedInstanceEnvironment));

    /// <summary>The configuration name .NET reads for an environment name: connection-string prefixes mapped, "__" read as ":".</summary>
    /// <param name="name">The environment name, any host prefix already removed.</param>
    /// <returns>The configuration name.</returns>
    private static string ConfigurationName(string name)
    {
        foreach (var prefix in ConnectionStringPrefixes)
        {
            if (name.StartsWith(prefix, IgnoreCase))
                return "ConnectionStrings:" + name[prefix.Length..].Replace("__", ":", StringComparison.Ordinal);
        }

        return name.Replace("__", ":", StringComparison.Ordinal);
    }

    /// <summary>Reference identity of a name: "__" read as ":" (case is ignored by the comparers that use it).</summary>
    /// <param name="key">The name.</param>
    /// <returns>The identity.</returns>
    private static string Identity(string key) => key.Replace("__", ":", StringComparison.Ordinal);

    /// <summary>
    /// Reference rule for a product key: any ASPNETCORE_ name, a DOTNET_ name whose rest is a product key, a name in the
    /// Mcp, QBrainAi, ConnectionStrings or Kestrel section under any separator spelling, a flat MCP_, QBRAINAI_ or
    /// MCPSERVER_ name, a connection-string prefix, or one of the provider fallbacks and host settings.
    /// </summary>
    /// <param name="key">The name.</param>
    /// <returns>True for a product key.</returns>
    private static bool IsProductKey(string key)
    {
        if (key.StartsWith("ASPNETCORE_", IgnoreCase)) return true;
        if (key.StartsWith("DOTNET_", IgnoreCase)) return IsProductKey(key["DOTNET_".Length..]);
        var identity = Identity(key);
        return ReferenceSections.Any(section => identity.Equals(section, IgnoreCase) || identity.StartsWith(section + ":", IgnoreCase))
            || ReferenceFlatPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ConnectionStringPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ReferenceNames.Any(name => identity.Equals(name, IgnoreCase));
    }

    /// <summary>Reference builder: every inherited product key removed, the instance's settings applied, everything else kept.</summary>
    /// <param name="inherited">The inherited environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>The child environment.</returns>
    private static Dictionary<string, string?> ScriptedBuild(IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings) =>
        BuildWith(inherited, settings, IsProductKey, applySettings: true);

    /// <summary>Builds a child environment with the given removal rule.</summary>
    /// <param name="inherited">The inherited environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="remove">Whether an inherited name is removed.</param>
    /// <param name="applySettings">Whether the instance's settings are applied.</param>
    /// <returns>The child environment.</returns>
    private static Dictionary<string, string?> BuildWith(
        IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings, Func<string, bool> remove, bool applySettings)
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

    /// <summary>The wrong builders of the negative controls.</summary>
    /// <param name="wrong">The wrong builder's name.</param>
    /// <returns>The wrong builder.</returns>
    private static Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> WrongBuild(string wrong) => wrong switch
    {
        "raw prefixes only" => (inherited, settings) => BuildWith(inherited, settings, IsRawProductKey, applySettings: true),
        "both separators, no host prefixes" => (inherited, settings) => BuildWith(inherited, settings, key =>
            ReferenceSections.Any(section => key.StartsWith(section + ":", IgnoreCase) || key.StartsWith(section + "__", IgnoreCase))
            || ReferenceFlatPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ReferenceNames.Any(name => key.Equals(name, IgnoreCase)), applySettings: true),
        "keeps the endpoint section" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) && !Identity(key).StartsWith("Kestrel:", IgnoreCase), applySettings: true),
        "removes every key with a separator" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || key.Contains(':', StringComparison.Ordinal) || key.Contains("__", StringComparison.Ordinal), applySettings: true),
        "drops the owned settings" => (inherited, settings) => BuildWith(inherited, settings, IsProductKey, applySettings: false),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong builder."),
    };

    /// <summary>The round 12 rule: a product key is one whose raw name starts with a double-underscore or flat prefix.</summary>
    /// <param name="key">The name.</param>
    /// <returns>True when the raw rule removes it.</returns>
    private static bool IsRawProductKey(string key)
    {
        if (key.StartsWith("ASPNETCORE_", IgnoreCase)) return true;
        if (key.StartsWith("DOTNET_", IgnoreCase)) return IsRawProductKey(key["DOTNET_".Length..]);
        return RawPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ConnectionStringPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ReferenceNames.Any(name => key.Equals(name, IgnoreCase));
    }

    /// <summary>
    /// Reference start check: every product key of the child is one the instance set with that value, no two names of one
    /// product key differ only in separators or case, every database-engine setting names SQLite, and every database the
    /// settings select lies strictly inside the owned root.
    /// </summary>
    /// <param name="child">The child environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="root">The owned root.</param>
    /// <returns>Null when accepted; otherwise the refusal.</returns>
    private static string? ScriptedCheck(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings, string root) =>
        CheckWith(child, settings, root, IsProductKey, twoNamesRule: true, ProviderSetting);

    /// <summary>Checks a start environment with the given rules.</summary>
    /// <param name="child">The child environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="root">The owned root.</param>
    /// <param name="isProduct">The product-key rule.</param>
    /// <param name="twoNamesRule">Whether two names of one product key are refused.</param>
    /// <param name="isProvider">The database-engine setting rule.</param>
    /// <returns>Null when accepted; otherwise the refusal.</returns>
    private static string? CheckWith(
        IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings, string root,
        Func<string, bool> isProduct, bool twoNamesRule, Func<string, bool> isProvider)
    {
        foreach (var (key, value) in child)
        {
            if (isProduct(key) && (!settings.TryGetValue(key, out var own) || !string.Equals(own, value, StringComparison.Ordinal)))
                return "unowned";
        }

        if (twoNamesRule && child.Keys.Where(isProduct).GroupBy(Identity, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return "two names";

        foreach (var (key, value) in settings)
        {
            if (isProvider(key) && !string.Equals(value, "sqlite", IgnoreCase))
                return "provider";
            var database = key.Contains("ConnectionString", IgnoreCase)
                ? DataSourceOf(value)
                : key.Contains("DATA_SOURCE", IgnoreCase) || key.Contains("DataSource", IgnoreCase) ? value : null;
            if (database is not null && !Inside(database, root))
                return "outside";
        }

        return null;
    }

    /// <summary>Reference database-engine setting: Database:Provider or DatabaseProvider under any separator spelling and prefix, or a DATABASE_PROVIDER name.</summary>
    /// <param name="key">The name.</param>
    /// <returns>True for a database-engine setting.</returns>
    private static bool ProviderSetting(string key) =>
        Identity(key).EndsWith(":Database:Provider", IgnoreCase)
        || Identity(key).EndsWith(":DatabaseProvider", IgnoreCase)
        || key.EndsWith("DATABASE_PROVIDER", IgnoreCase);

    /// <summary>The wrong checks of the negative controls.</summary>
    /// <param name="wrong">The wrong check's name.</param>
    /// <returns>The wrong check.</returns>
    private static Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, string, string?> WrongCheck(string wrong) => wrong switch
    {
        "raw prefixes only" => (child, settings, root) => CheckWith(child, settings, root, IsRawProductKey, twoNamesRule: false,
            key => key.EndsWith("__Database__Provider", IgnoreCase) || key.EndsWith("__DatabaseProvider", IgnoreCase) || key.EndsWith("DATABASE_PROVIDER", IgnoreCase)),
        "no rule for two names" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey, twoNamesRule: false, ProviderSetting),
        "provider rule on double underscores only" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey, twoNamesRule: true,
            key => key.EndsWith("__Database__Provider", IgnoreCase) || key.EndsWith("__DatabaseProvider", IgnoreCase) || key.EndsWith("DATABASE_PROVIDER", IgnoreCase)),
        "refuses every colon key" => (child, settings, root) =>
            child.Keys.Any(key => key.Contains(':', StringComparison.Ordinal)) ? "colon" : ScriptedCheck(child, settings, root),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong check."),
    };

    /// <summary>The Data Source of a connection string, or the text itself when it names none.</summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The data source.</returns>
    private static string DataSourceOf(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && string.Equals(part[..separator].Trim(), "Data Source", IgnoreCase))
                return part[(separator + 1)..].Trim();
        }

        return connectionString;
    }

    /// <summary>True when a POSIX path lies strictly inside a POSIX root, segment by segment, with no '.' or '..' segment.</summary>
    /// <param name="path">The path.</param>
    /// <param name="root">The root.</param>
    /// <returns>True when inside.</returns>
    private static bool Inside(string path, string root)
    {
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var rootSegments = root.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return path.StartsWith('/') && pathSegments.All(segment => segment is not "." and not "..")
            && pathSegments.Length > rootSegments.Length
            && rootSegments.SequenceEqual(pathSegments.Take(rootSegments.Length), StringComparer.Ordinal);
    }

    /// <summary>A map in its insertion order and in the reverse order.</summary>
    /// <param name="map">The map.</param>
    /// <returns>The two orders, as new maps with the same comparer behavior (keys compared ignoring case).</returns>
    private static IEnumerable<Dictionary<string, string?>> BothOrders(IReadOnlyDictionary<string, string?> map)
    {
        var forward = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map)
            forward[key] = value;
        yield return forward;

        var reverse = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in map.Reverse())
            reverse[key] = value;
        yield return reverse;
    }

    /// <summary>True when two effective configurations hold the same keys (ignoring case) with ordinally equal values.</summary>
    /// <param name="actual">The actual configuration.</param>
    /// <param name="expected">The expected configuration.</param>
    /// <returns>True when equal.</returns>
    private static bool SameView(Dictionary<string, string?> actual, Dictionary<string, string?> expected) =>
        actual.Count == expected.Count
        && expected.All(pair => actual.TryGetValue(pair.Key, out var value) && string.Equals(value, pair.Value, StringComparison.Ordinal));

    /// <summary>True when two maps hold the same names (ordinal).</summary>
    /// <param name="left">One map.</param>
    /// <param name="right">The other map.</param>
    /// <returns>True when the name sets are equal.</returns>
    private static bool SameKeys(IReadOnlyDictionary<string, string?> left, IReadOnlyDictionary<string, string?> right) =>
        left.Keys.Order(StringComparer.Ordinal).SequenceEqual(right.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);

    /// <summary>True when two maps hold the same names (ignoring case) with ordinally equal values.</summary>
    /// <param name="left">One map.</param>
    /// <param name="right">The other map.</param>
    /// <returns>True when equal.</returns>
    private static bool SameMap(IReadOnlyDictionary<string, string?> left, IReadOnlyDictionary<string, string?> right) =>
        left.Count == right.Count
        && right.All(pair => left.Any(other => string.Equals(other.Key, pair.Key, IgnoreCase) && string.Equals(other.Value, pair.Value, StringComparison.Ordinal)));

    /// <summary>A child environment holding the settings and the extra variables.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="extra">The extra variables.</param>
    /// <returns>The child environment, keys compared ignoring case.</returns>
    private static Dictionary<string, string?> Child(Dictionary<string, string> settings, params (string Key, string Value)[] extra)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in settings)
            map[key] = value;
        foreach (var (key, value) in extra)
            map[key] = value;
        return map;
    }

    /// <summary>The instance's own settings with one name removed and names added.</summary>
    /// <param name="remove">The name to remove, or null.</param>
    /// <param name="add">The names to add.</param>
    /// <returns>The settings, keys compared ignoring case.</returns>
    private static Dictionary<string, string> OwnedWith(string? remove, params (string Key, string Value)[] add)
    {
        var settings = new Dictionary<string, string>(OwnedInstanceEnvironmentTests.Owned, StringComparer.OrdinalIgnoreCase);
        if (remove is not null)
            settings.Remove(remove);
        foreach (var (key, value) in add)
            settings[key] = value;
        return settings;
    }
}
