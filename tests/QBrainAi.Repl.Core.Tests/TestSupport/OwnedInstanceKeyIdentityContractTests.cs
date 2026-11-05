using Xunit;

namespace QBrainAi.Repl.Core.Tests.TestSupport;

/// <summary>
/// Tests configuration-key identity in the environment a temporary instance starts with (FR-MCP-TESTRESOURCE-001 ac-4,
/// appended 2026-11-05, "A setting is identified by its configuration key, not by its spelling" and "A name is matched
/// exactly, as the process environment holds it"): an inherited colon, double-underscore or mixed-separator spelling of
/// a product key or of the host's endpoint section (Kestrel), alone or host-prefixed, in either enumeration order,
/// changes nothing in the instance's effective configuration; an inherited spelling of a key the instance sets, alone
/// or host-prefixed, is replaced by the instance's setting; every other inherited variable is kept with its exact name and value, two names
/// that differ only in case included, by the builder and by the REPL start; and the start check refuses a product key
/// the instance did not set, under any spelling, and two names of one product key or of one key the instance sets that
/// differ only in separators or case.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-TESTRESOURCE-001 ac-4, the unit-proved rule TR-MCP-NUKE-019 AC-6 relies on. Test data:
/// scripted inherited maps, the owned settings of <see cref="OwnedInstanceEnvironmentTests.Owned"/> (and, for the rows
/// whose instance sets a key outside the product's namespaces, those settings plus one), the settings of a REPL start
/// and literal start environments, as plain strings. The oracle for the builder rows is
/// <see cref="EffectiveConfiguration"/>, an independent statement of how .NET configuration reads a process
/// environment: the environment-variable provider replaces "__" with ":", ignores case, lets a later variable of one key
/// replace an earlier one and maps the five connection-string prefixes into ConnectionStrings, and the host also reads
/// the DOTNET_ and ASPNETCORE_ names with the prefix removed (dotnet/runtime v10.0.0,
/// EnvironmentVariablesConfigurationProvider). Each builder row is judged with the inherited map and the child
/// environment each enumerated in both orders: the effective configuration of the product keys and of the keys the
/// instance sets must equal that of the instance's own settings, and every other name of the child must be one the row
/// keeps, with its exact spelling and value. Maps that hold environment names compare them ordinally, as a POSIX process
/// environment does. No test reads or writes the process environment, the file system or the network.
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): added the key-identity builder, start-check and pipeline rows with an independent oracle, a scripted reference, wrong builders and checks and per-row wrong responses (mock gate), because hostile validation round 12 finding R12-F1 showed that matching raw prefixes let an inherited colon spelling of a connection string reach the instance.</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (same appended sentence): appended the two bindings to the shared builder and start check, unchanged rows and assertions, after the mock gate passed and TemporaryInstanceChecks read configuration keys (hostile validation round 12 finding R12-F1).</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): the maps that hold environment names (the row orders, the scripted builder and the start environments) compare names ordinally instead of ignoring case; builder rows for unrelated names that differ only in case and for another case or separator spelling of a key the instance sets; builder rows are judged on the product keys and the instance's own keys by the oracle and on every other name exactly; start-check rows for two names of an owned key that differ only in case or separators and an accepted unrelated pair; REPL start rows for the environment a REPL child of an instance starts with; wrong builders, checks and REPL starts for each, the earlier ignore-case builder among them; mock gate, the bindings held out until the production change. Because the seventh holistic read before hostile validation round 13 (finding g1) showed that the ignore-case child map merged unrelated inherited names that differ only in case, against ac-4's "unrelated variables are kept unchanged", and the rows could not hold such a pair.</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (same appended sentence): re-appended the two bindings unchanged and appended the binding of the shared REPL start boundary, after mock gate ug-r13z4c-mockgate-1 passed and TemporaryInstanceChecks and OwnedReplStart kept names exactly (seventh holistic read before hostile validation round 13, finding g1).</description></item>
/// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 ("a DOTNET_ or ASPNETCORE_ prefix places the rest of the name in the host's configuration, so every ... spelling of a key, alone or host-prefixed, sets that key", with the append "A name is matched exactly, as the process environment holds it"): the reference rule for a key the instance sets reads the keys a name sets with and without a host prefix; builder rows for an unprefixed spelling of an owned host-prefixed setting, for a host-prefixed spelling of owned settings and for unrelated host-prefixed names kept; start-check rows for the instance's own DOTNET_ and ASPNETCORE_ pair (accepted) and for two cases of an unprefixed spelling of an owned host-prefixed setting (refused); a REPL start row; wrong builders, checks and a REPL start for each, the earlier rule among them; mock gate, the bindings held out until the production change. Because the eighth holistic read before hostile validation round 13 (finding g1) showed that an inherited NOLOGO beside the owned DOTNET_NOLOGO, or DOTNET_PORT beside the owned PORT, reached the instance, and no row held such a name.</description></item>
/// </list>
/// </remarks>
public sealed class OwnedInstanceKeyIdentityContractTests
{
    private const string OwnedRoot = "/scratch/owned";
    private const string OwnedDatabase = "/scratch/owned/data/qbrainai.db";
    private const string LiveDatabase = "/home/example/live/qbrainai.db";
    private const string OwnedAddress = "http://127.0.0.1:49123";
    private const string LiveAddress = "http://127.0.0.1:7147";
    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;

    private static readonly string[] HostPrefixes = ["DOTNET_", "ASPNETCORE_"];
    private static readonly string[] ConnectionStringPrefixes = ["SQLCONNSTR_", "SQLAZURECONNSTR_", "MYSQLCONNSTR_", "POSTGRESQLCONNSTR_", "CUSTOMCONNSTR_"];
    private static readonly string[] ReferenceSections = ["Mcp", "QBrainAi", "ConnectionStrings", "Kestrel"];
    private static readonly string[] ReferenceFlatPrefixes = ["MCP_", "QBRAINAI_", "MCPSERVER_"];
    private static readonly string[] ReferenceNames =
        ["DATABASE_URL", "POSTGRES_CONNECTION_STRING", "CONTENTROOT", "ENVIRONMENT", "URLS", "APPLICATIONNAME", "WEBROOT",
            "HTTP_PORTS", "HTTPS_PORTS", "HOSTINGSTARTUPASSEMBLIES", "HOSTINGSTARTUPEXCLUDEASSEMBLIES", "PREVENTHOSTINGSTARTUP"];
    private static readonly string[] RawPrefixes = ["Mcp__", "QBrainAi__", "MCP_", "QBRAINAI_", "MCPSERVER_", "ConnectionStrings__"];

    /// <summary>The settings of the REPL start rows: the owned workspace, server address and trust cache, and one host control.</summary>
    private static readonly Dictionary<string, string> ReplSettings = new(StringComparer.Ordinal)
    {
        ["MCP_WORKSPACE_PATH"] = "/scratch/owned/workspace",
        ["MCP_SERVER_URL"] = OwnedAddress,
        ["QBRAINAI_REPL_CACHE_DIR"] = "/scratch/owned/repl-cache",
        ["DOTNET_NOLOGO"] = "1",
    };

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
        ("unrelated names that differ only in case are kept",
            new() { ["http_proxy"] = "http://proxy.example:3128", ["HTTP_PROXY"] = "http://proxy.example:8080", ["PATH"] = "/usr/bin" },
            new() { ["http_proxy"] = "http://proxy.example:3128", ["HTTP_PROXY"] = "http://proxy.example:8080", ["PATH"] = "/usr/bin" }),
        ("unrelated host-prefixed names are kept",
            new() { ["DOTNET_ROOT"] = "/usr/share/dotnet", ["DOTNET_gcServer"] = "1", ["PATH"] = "/usr/bin" },
            new() { ["DOTNET_ROOT"] = "/usr/share/dotnet", ["DOTNET_gcServer"] = "1", ["PATH"] = "/usr/bin" }),
    ];

    /// <summary>
    /// The builder rows whose instance sets a key outside the product's namespaces (PORT, or a logging level): a row
    /// name, the instance's settings, the inherited environment, and the inherited variables the child must keep.
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string> Settings, Dictionary<string, string?> Inherited, Dictionary<string, string> Kept)[] OwnedSettingRows =
    [
        ("another case of an owned setting is replaced",
            Owned,
            new() { ["port"] = "7147", ["Port"] = "7148", ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
        ("another separator spelling of an owned setting is replaced",
            OwnedWith(null, ("Logging__LogLevel__Default", "Error")),
            new() { ["Logging:LogLevel:Default"] = "Debug", ["logging__loglevel__default"] = "Trace", ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
        ("an unprefixed spelling of an owned host-prefixed setting is replaced",
            OwnedWith(null, ("DOTNET_NOLOGO", "1")),
            new() { ["NOLOGO"] = "0", ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
        ("a host-prefixed spelling of an owned setting is replaced",
            OwnedWith(null, ("Logging__LogLevel__Default", "Error")),
            new() { ["DOTNET_Logging__LogLevel__Default"] = "Trace", ["DOTNET_port"] = "7147", ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
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
        ("unrelated names that differ only in case",
            Child(Owned, ("http_proxy", "http://proxy.example:3128"), ("HTTP_PROXY", "http://proxy.example:8080")), Owned, true),
        ("two names of an owned product key that differ only in case",
            Child(Owned, ("mcp_workspace_path", "/scratch/owned/workspace")), Owned, false),
        ("two names of an owned setting that differ only in case", Child(Owned, ("port", "7147")), Owned, false),
        ("two separator spellings of an owned setting",
            Child(OwnedWith(null, ("Logging__LogLevel__Default", "Error")), ("Logging:LogLevel:Default", "Debug")),
            OwnedWith(null, ("Logging__LogLevel__Default", "Error")), false),
        ("owned settings under both host prefixes",
            Child(OwnedWith(null, ("DOTNET_ENVIRONMENT", "Production"), ("ASPNETCORE_ENVIRONMENT", "Production"))),
            OwnedWith(null, ("DOTNET_ENVIRONMENT", "Production"), ("ASPNETCORE_ENVIRONMENT", "Production")), true),
        ("two cases of an unprefixed spelling of an owned host-prefixed setting",
            Child(OwnedWith(null, ("DOTNET_NOLOGO", "1")), ("NOLOGO", "0"), ("nologo", "0")),
            OwnedWith(null, ("DOTNET_NOLOGO", "1")), false),
    ];

    /// <summary>
    /// The REPL start rows: a row name, the environment a REPL child of a temporary instance would inherit, and the
    /// inherited variables it must keep exactly; every other inherited name must be gone and the server address must be
    /// the instance's own (<c>http://127.0.0.1:49123</c>).
    /// </summary>
    internal static readonly (string Name, Dictionary<string, string?> Inherited, Dictionary<string, string> Kept)[] ReplStartRows =
    [
        ("unrelated names that differ only in case reach the REPL child",
            new() { ["http_proxy"] = "http://proxy.example:3128", ["HTTP_PROXY"] = "http://proxy.example:8080", ["PATH"] = "/usr/bin", ["mcp_server_url"] = LiveAddress },
            new() { ["http_proxy"] = "http://proxy.example:3128", ["HTTP_PROXY"] = "http://proxy.example:8080", ["PATH"] = "/usr/bin" }),
        ("an inherited server address in another case is replaced",
            new() { ["Mcp_Server_Url"] = LiveAddress, ["QBRAINAI_SERVER_URL"] = LiveAddress, ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
        ("an unprefixed spelling of the REPL's host setting is replaced",
            new() { ["NOLOGO"] = "0", ["PATH"] = "/usr/bin" },
            new() { ["PATH"] = "/usr/bin" }),
    ];

    /// <summary>Every builder row with the settings it is judged under: the shared owned settings for <see cref="BuildRows"/>.</summary>
    private static IEnumerable<(string Name, Dictionary<string, string> Settings, Dictionary<string, string?> Inherited, Dictionary<string, string> Kept)> AllBuildRows =>
        BuildRows.Select(row => (Name: row.Name, Settings: Owned, Inherited: row.Inherited, Kept: row.Kept)).Concat(OwnedSettingRows);

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
    /// Returns the names of the builder rows on which <paramref name="build"/> gives a child environment, for either
    /// enumeration order of the inherited map and of the child environment, whose effective configuration of the product
    /// keys and of the keys the instance sets differs from that of the instance's own settings, or which holds any other
    /// name than the inherited ones the row keeps, with their exact spelling and value.
    /// </summary>
    /// <param name="build">The builder under test: the inherited environment and the instance's settings to the child environment.</param>
    /// <returns>The failing row names.</returns>
    internal static List<string> BuildContractFailures(Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, Dictionary<string, string?>> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var failures = new List<string>();
        foreach (var row in AllBuildRows)
        {
            var expected = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var (key, value) in row.Kept)
                expected[key] = value;
            foreach (var (key, value) in row.Settings)
                expected[key] = value;
            var expectedView = Governed(EffectiveConfiguration(expected), row.Settings);

            var same = true;
            foreach (var inherited in BothOrders(row.Inherited))
            {
                var child = build(inherited, row.Settings);
                same &= SameView(Governed(EffectiveConfiguration(child), row.Settings), expectedView)
                    && SameView(Governed(EffectiveConfiguration(child.Reverse()), row.Settings), expectedView)
                    && KeepsExactly(child, row.Kept, row.Settings);
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
        return AllBuildRows
            .Where(row => BothOrders(row.Inherited).Any(inherited => check(build(inherited, row.Settings), row.Settings, OwnedRoot) is not null))
            .Select(row => row.Name)
            .ToList();
    }

    /// <summary>
    /// Returns the names of the REPL start rows on which <paramref name="apply"/>, given the inherited environment as an
    /// ordinal map in either enumeration order, throws, or leaves an environment that misses a kept name or value, keeps
    /// another inherited name, or holds another server address than the instance's own.
    /// </summary>
    /// <param name="apply">The REPL start under test: replaces the given environment with the REPL child's, or throws.</param>
    /// <returns>The failing row names.</returns>
    internal static List<string> ReplStartFailures(Action<IDictionary<string, string?>> apply)
    {
        ArgumentNullException.ThrowIfNull(apply);
        var failures = new List<string>();
        foreach (var row in ReplStartRows)
        {
            var same = true;
            foreach (var inherited in BothOrders(row.Inherited))
            {
                IDictionary<string, string?> environment = new Dictionary<string, string?>(inherited, StringComparer.Ordinal);
                try
                {
                    apply(environment);
                }
                catch (Exception error) when (error is InvalidOperationException or ArgumentException)
                {
                    same = false;
                    continue;
                }

                same &= environment.TryGetValue("MCP_SERVER_URL", out var address) && string.Equals(address, OwnedAddress, StringComparison.Ordinal)
                    && row.Kept.All(pair => environment.TryGetValue(pair.Key, out var value) && string.Equals(value, pair.Value, StringComparison.Ordinal))
                    && row.Inherited.Keys.Where(key => !row.Kept.ContainsKey(key)).All(key => !environment.ContainsKey(key));
            }

            if (!same)
                failures.Add(row.Name);
        }

        return failures;
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

    /// <summary>Mock gate positive control: the scripted reference builder, check and REPL start satisfy every builder, check, pipeline and REPL start row.</summary>
    [Fact]
    public void ScriptedKeyIdentity_SatisfiesTheContract()
    {
        Assert.Empty(BuildContractFailures(ScriptedBuild));
        Assert.Empty(CheckContractFailures(ScriptedCheck));
        Assert.Empty(PipelineFailures(ScriptedBuild, ScriptedCheck));
        Assert.Empty(ReplStartFailures(ScriptedReplStart));
    }

    /// <summary>
    /// Mock gate negative controls on the builder, each rejected inside a passing harness on the row named: the round 12
    /// raw-prefix builder, one that knows both separators but not the host prefixes, one that keeps the endpoint
    /// section, one that removes every key holding a separator, one that drops the instance's own settings, one that
    /// merges names that differ only in case (the builder before the seventh holistic read), one that keeps other
    /// spellings of the instance's own keys, one that matches those keys by name instead of by key, one that matches
    /// them without the host prefixes (the builder before the eighth holistic read), and one that removes every
    /// host-prefixed name.
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
    [InlineData("collapses case variants", "unrelated names that differ only in case are kept")]
    [InlineData("keeps other spellings of owned settings", "another case of an owned setting is replaced")]
    [InlineData("keeps other spellings of owned settings", "another separator spelling of an owned setting is replaced")]
    [InlineData("owned settings matched by name, not key", "another separator spelling of an owned setting is replaced")]
    [InlineData("owned settings matched without host prefixes", "an unprefixed spelling of an owned host-prefixed setting is replaced")]
    [InlineData("owned settings matched without host prefixes", "a host-prefixed spelling of an owned setting is replaced")]
    [InlineData("removes every host-prefixed name", "unrelated host-prefixed names are kept")]
    public void WrongKeyIdentityBuilders_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, BuildContractFailures(WrongBuild(wrong)));

    /// <summary>
    /// Mock gate negative controls on the start check, each rejected on the row named: the round 12 raw-prefix check, one
    /// without the rule for two names of one key, one whose provider rule reads only double-underscore names, one that
    /// refuses every name holding a colon, one whose rule for two names matches case, one whose rule for two names
    /// covers product keys only, one whose rule for two names covers every name, one whose rule for two names misses
    /// the unprefixed spellings of the instance's host-prefixed keys (the check before the eighth holistic read), and one
    /// whose rule for two names also groups names that differ only in their host prefix.
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
    [InlineData("two-name rule matches case", "two names of an owned product key that differ only in case")]
    [InlineData("two-name rule on product keys only", "two names of an owned setting that differ only in case")]
    [InlineData("two-name rule on product keys only", "two separator spellings of an owned setting")]
    [InlineData("two-name rule on every name", "unrelated names that differ only in case")]
    [InlineData("two-name rule on owned keys without host prefixes", "two cases of an unprefixed spelling of an owned host-prefixed setting")]
    [InlineData("two-name rule strips host prefixes", "owned settings under both host prefixes")]
    public void WrongKeyIdentityChecks_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, CheckContractFailures(WrongCheck(wrong)));

    /// <summary>
    /// Mock gate negative controls on the REPL start, each rejected on the row named: one that reads the replaced
    /// environment into a map that ignores case (the REPL start before the seventh holistic read), one that builds the
    /// REPL child in such a map, and one that matches the REPL's own keys without the host prefixes (the REPL start before
    /// the eighth holistic read).
    /// </summary>
    /// <param name="wrong">The name of the wrong REPL start.</param>
    /// <param name="failingRow">The REPL start row that must reject it.</param>
    [Theory]
    [InlineData("reads the start environment ignoring case", "unrelated names that differ only in case reach the REPL child")]
    [InlineData("builds the REPL child ignoring case", "unrelated names that differ only in case reach the REPL child")]
    [InlineData("matches the REPL's own keys without host prefixes", "an unprefixed spelling of the REPL's host setting is replaced")]
    public void WrongReplStarts_AreRejectedInsidePassingHarness(string wrong, string failingRow) =>
        Assert.Contains(failingRow, ReplStartFailures(WrongReplStart(wrong)));

    /// <summary>
    /// Mock gate: the start check catches what a wrong builder leaves; the round 12 raw-prefix builder followed by the
    /// scripted check refuses the colon spelling it kept.
    /// </summary>
    [Fact]
    public void RawPrefixBuilder_LeavesAnAliasTheCheckRefuses() =>
        Assert.Contains("a colon spelling of the canonical connection string", PipelineFailures(WrongBuild("raw prefixes only"), ScriptedCheck));

    /// <summary>
    /// Mock gate: the start check catches another spelling of a key the instance sets that a wrong builder leaves, in
    /// another case and in other separators.
    /// </summary>
    [Fact]
    public void OwnedSpellingBuilder_LeavesNamesTheCheckRefuses()
    {
        var failures = PipelineFailures(WrongBuild("keeps other spellings of owned settings"), ScriptedCheck);
        Assert.Contains("another case of an owned setting is replaced", failures);
        Assert.Contains("another separator spelling of an owned setting is replaced", failures);
    }

    /// <summary>
    /// Mock gate: every builder, start-check and REPL start row rejects a response that is wrong on that row alone (a
    /// child environment missing one owned setting or one kept name, or the opposite verdict), and only that row.
    /// </summary>
    [Fact]
    public void EveryKeyIdentityRow_RejectsAWrongResponse()
    {
        foreach (var target in AllBuildRows)
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

        foreach (var target in ReplStartRows)
        {
            Assert.Equal([target.Name], ReplStartFailures(environment =>
            {
                var isTarget = environment.Keys.Order(StringComparer.Ordinal)
                    .SequenceEqual(target.Inherited.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
                ScriptedReplStart(environment);
                if (isTarget)
                    environment.Remove("PATH");
            }));
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

    /// <summary>
    /// Binding: the shared REPL start boundary,
    /// <see cref="QBrainAi.TestSupport.TemporaryInstance.OwnedReplStart.ApplyOwnedStartEnvironment"/>, with the plan
    /// <see cref="QBrainAi.TestSupport.TemporaryInstance.OwnedReplStart.CreatePlan"/> makes for the owned workspace, marker
    /// and address, satisfies every REPL start row the scripted reference passed and the wrong REPL starts failed.
    /// </summary>
    [Fact]
    public void ReplStart_KeepsUnrelatedNamesExactly() =>
        Assert.Empty(ReplStartFailures(environment => QBrainAi.TestSupport.TemporaryInstance.OwnedReplStart.ApplyOwnedStartEnvironment(
            environment,
            QBrainAi.TestSupport.TemporaryInstance.OwnedReplStart.CreatePlan(
                "host.dll", "/scratch/owned/workspace", "/scratch/owned/workspace/AGENTS-README-FIRST.yaml", OwnedRoot, OwnedAddress),
            OwnedRoot,
            OwnedAddress)));

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

    /// <summary>
    /// Reference rule for a key the instance sets: one of the keys the name sets (<see cref="KeysSetBy"/>) equals, ignoring
    /// case, one of the keys a setting's name sets, so an unprefixed or host-prefixed spelling of an owned key counts.
    /// </summary>
    /// <param name="key">The name.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>True when the name sets a key the instance sets.</returns>
    private static bool SetsOwnedKey(string key, IReadOnlyDictionary<string, string> settings)
    {
        var keys = KeysSetBy(key).ToList();
        return settings.Keys.Any(own => KeysSetBy(own).Any(ownKey => keys.Contains(ownKey, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// The configuration keys an environment name sets, as the oracle reads it: its identity, and, when it starts with a
    /// host prefix (DOTNET_ or ASPNETCORE_), the identity of the rest, which the host reads.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The keys.</returns>
    private static IEnumerable<string> KeysSetBy(string name) =>
        HostPrefixes.Where(prefix => name.StartsWith(prefix, IgnoreCase)).Select(prefix => Identity(name[prefix.Length..])).Prepend(Identity(name));

    /// <summary>
    /// The part of an effective configuration the oracle judges: the product keys and the keys the instance's settings
    /// set; every other name is judged exactly by <see cref="KeepsExactly"/>.
    /// </summary>
    /// <param name="view">The effective configuration.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>The judged part, keys compared ignoring case.</returns>
    private static Dictionary<string, string?> Governed(Dictionary<string, string?> view, IReadOnlyDictionary<string, string> settings)
    {
        var owned = EffectiveConfiguration(settings.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)));
        return view.Where(pair => IsProductKey(pair.Key) || owned.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True when every name of the child that is not one of the instance's settings is a kept name, spelled and valued exactly, and every kept name is there.</summary>
    /// <param name="child">The child environment.</param>
    /// <param name="kept">The inherited names the child must keep.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>True when the child keeps exactly the kept names.</returns>
    private static bool KeepsExactly(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> kept, IReadOnlyDictionary<string, string> settings)
    {
        var others = child.Where(pair => !settings.Keys.Contains(pair.Key, StringComparer.Ordinal)).ToList();
        return others.Count == kept.Count
            && kept.All(pair => others.Any(other => string.Equals(other.Key, pair.Key, StringComparison.Ordinal) && string.Equals(other.Value, pair.Value, StringComparison.Ordinal)));
    }

    /// <summary>
    /// Reference builder: every inherited product key and every inherited spelling of a key the instance sets removed,
    /// the instance's settings applied, everything else kept under its exact name.
    /// </summary>
    /// <param name="inherited">The inherited environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>The child environment, names compared ordinally.</returns>
    private static Dictionary<string, string?> ScriptedBuild(IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings) =>
        BuildWith(inherited, settings, key => IsProductKey(key) || SetsOwnedKey(key, settings), applySettings: true, StringComparer.Ordinal);

    /// <summary>Builds a child environment with the given removal rule and name comparer.</summary>
    /// <param name="inherited">The inherited environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="remove">Whether an inherited name is removed.</param>
    /// <param name="applySettings">Whether the instance's settings are applied.</param>
    /// <param name="comparer">How the child environment compares names.</param>
    /// <returns>The child environment.</returns>
    private static Dictionary<string, string?> BuildWith(
        IReadOnlyDictionary<string, string?> inherited, IReadOnlyDictionary<string, string> settings, Func<string, bool> remove, bool applySettings,
        StringComparer comparer)
    {
        var child = new Dictionary<string, string?>(comparer);
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
        "raw prefixes only" => (inherited, settings) => BuildWith(inherited, settings, IsRawProductKey, applySettings: true, StringComparer.Ordinal),
        "both separators, no host prefixes" => (inherited, settings) => BuildWith(inherited, settings, key =>
            ReferenceSections.Any(section => key.StartsWith(section + ":", IgnoreCase) || key.StartsWith(section + "__", IgnoreCase))
            || ReferenceFlatPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase))
            || ReferenceNames.Any(name => key.Equals(name, IgnoreCase)), applySettings: true, StringComparer.Ordinal),
        "keeps the endpoint section" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) && !Identity(key).StartsWith("Kestrel:", IgnoreCase), applySettings: true, StringComparer.Ordinal),
        "removes every key with a separator" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || key.Contains(':', StringComparison.Ordinal) || key.Contains("__", StringComparison.Ordinal), applySettings: true, StringComparer.Ordinal),
        "drops the owned settings" => (inherited, settings) => BuildWith(inherited, settings, IsProductKey, applySettings: false, StringComparer.Ordinal),
        "collapses case variants" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || SetsOwnedKey(key, settings), applySettings: true, StringComparer.OrdinalIgnoreCase),
        "keeps other spellings of owned settings" => (inherited, settings) => BuildWith(inherited, settings, IsProductKey, applySettings: true, StringComparer.Ordinal),
        "owned settings matched by name, not key" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || settings.Keys.Any(own => string.Equals(own, key, IgnoreCase)), applySettings: true, StringComparer.Ordinal),
        "owned settings matched without host prefixes" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || settings.Keys.Any(own => string.Equals(Identity(own), Identity(key), IgnoreCase)), applySettings: true, StringComparer.Ordinal),
        "removes every host-prefixed name" => (inherited, settings) => BuildWith(inherited, settings,
            key => IsProductKey(key) || SetsOwnedKey(key, settings) || HostPrefixes.Any(prefix => key.StartsWith(prefix, IgnoreCase)), applySettings: true, StringComparer.Ordinal),
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
    /// product key or of one key the instance sets differ only in separators or case, every database-engine setting names
    /// SQLite, and every database the settings select lies strictly inside the owned root.
    /// </summary>
    /// <param name="child">The child environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="root">The owned root.</param>
    /// <returns>Null when accepted; otherwise the refusal.</returns>
    private static string? ScriptedCheck(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings, string root) =>
        CheckWith(child, settings, root, IsProductKey, TwoNames, ProviderSetting);

    /// <summary>Reference rule for two names: two names of one product key or of one key the instance sets, by identity ignoring case.</summary>
    /// <param name="child">The child environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <returns>True when the child holds two such names.</returns>
    private static bool TwoNames(IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings) =>
        TwoNamesAmong(child.Keys.Where(key => IsProductKey(key) || SetsOwnedKey(key, settings)), StringComparer.OrdinalIgnoreCase);

    /// <summary>True when two of the names have one identity under the comparer.</summary>
    /// <param name="names">The names.</param>
    /// <param name="comparer">How identities compare.</param>
    /// <returns>True when two names share an identity.</returns>
    private static bool TwoNamesAmong(IEnumerable<string> names, StringComparer comparer) =>
        names.GroupBy(Identity, comparer).Any(group => group.Count() > 1);

    /// <summary>Checks a start environment with the given rules.</summary>
    /// <param name="child">The child environment.</param>
    /// <param name="settings">The instance's settings.</param>
    /// <param name="root">The owned root.</param>
    /// <param name="isProduct">The product-key rule.</param>
    /// <param name="twoNames">The rule for two names of one key.</param>
    /// <param name="isProvider">The database-engine setting rule.</param>
    /// <returns>Null when accepted; otherwise the refusal.</returns>
    private static string? CheckWith(
        IReadOnlyDictionary<string, string?> child, IReadOnlyDictionary<string, string> settings, string root,
        Func<string, bool> isProduct, Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>, bool> twoNames, Func<string, bool> isProvider)
    {
        foreach (var (key, value) in child)
        {
            if (isProduct(key) && (!settings.TryGetValue(key, out var own) || !string.Equals(own, value, StringComparison.Ordinal)))
                return "unowned";
        }

        if (twoNames(child, settings))
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
        "raw prefixes only" => (child, settings, root) => CheckWith(child, settings, root, IsRawProductKey, (_, _) => false,
            key => key.EndsWith("__Database__Provider", IgnoreCase) || key.EndsWith("__DatabaseProvider", IgnoreCase) || key.EndsWith("DATABASE_PROVIDER", IgnoreCase)),
        "no rule for two names" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey, (_, _) => false, ProviderSetting),
        "provider rule on double underscores only" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey, TwoNames,
            key => key.EndsWith("__Database__Provider", IgnoreCase) || key.EndsWith("__DatabaseProvider", IgnoreCase) || key.EndsWith("DATABASE_PROVIDER", IgnoreCase)),
        "refuses every colon key" => (child, settings, root) =>
            child.Keys.Any(key => key.Contains(':', StringComparison.Ordinal)) ? "colon" : ScriptedCheck(child, settings, root),
        "two-name rule matches case" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey,
            (names, own) => TwoNamesAmong(names.Keys.Where(key => IsProductKey(key) || SetsOwnedKey(key, own)), StringComparer.Ordinal), ProviderSetting),
        "two-name rule on product keys only" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey,
            (names, _) => TwoNamesAmong(names.Keys.Where(IsProductKey), StringComparer.OrdinalIgnoreCase), ProviderSetting),
        "two-name rule on every name" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey,
            (names, _) => TwoNamesAmong(names.Keys, StringComparer.OrdinalIgnoreCase), ProviderSetting),
        "two-name rule on owned keys without host prefixes" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey,
            (names, own) => TwoNamesAmong(names.Keys.Where(key => IsProductKey(key) || own.Keys.Any(name => string.Equals(Identity(name), Identity(key), IgnoreCase))), StringComparer.OrdinalIgnoreCase),
            ProviderSetting),
        "two-name rule strips host prefixes" => (child, settings, root) => CheckWith(child, settings, root, IsProductKey,
            (names, own) => names.Keys.Where(key => IsProductKey(key) || SetsOwnedKey(key, own))
                .GroupBy(key => KeysSetBy(key).Last(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1),
            ProviderSetting),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong check."),
    };

    /// <summary>
    /// Reference REPL start: the REPL child's environment built from the given one by the reference builder with the
    /// REPL settings, refused by the reference check or put in place of the given one.
    /// </summary>
    /// <param name="environment">The environment, replaced in place.</param>
    private static void ScriptedReplStart(IDictionary<string, string?> environment)
    {
        var child = ScriptedBuild(new Dictionary<string, string?>(environment, StringComparer.Ordinal), ReplSettings);
        if (ScriptedCheck(child, ReplSettings, OwnedRoot) is not null)
            throw new InvalidOperationException("refused");
        ReplaceWith(environment, child);
    }

    /// <summary>The wrong REPL starts of the negative controls.</summary>
    /// <param name="wrong">The wrong REPL start's name.</param>
    /// <returns>The wrong REPL start.</returns>
    private static Action<IDictionary<string, string?>> WrongReplStart(string wrong) => wrong switch
    {
        "reads the start environment ignoring case" => environment =>
        {
            ScriptedReplStart(environment);
            _ = environment.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        },
        "builds the REPL child ignoring case" => environment => ReplaceWith(environment,
            BuildWith(new Dictionary<string, string?>(environment, StringComparer.Ordinal), ReplSettings,
                key => IsProductKey(key) || SetsOwnedKey(key, ReplSettings), applySettings: true, StringComparer.OrdinalIgnoreCase)),
        "matches the REPL's own keys without host prefixes" => environment => ReplaceWith(environment,
            BuildWith(new Dictionary<string, string?>(environment, StringComparer.Ordinal), ReplSettings,
                key => IsProductKey(key) || ReplSettings.Keys.Any(own => string.Equals(Identity(own), Identity(key), IgnoreCase)), applySettings: true, StringComparer.Ordinal)),
        _ => throw new ArgumentOutOfRangeException(nameof(wrong), wrong, "Unknown wrong REPL start."),
    };

    /// <summary>Puts a child environment in place of an environment.</summary>
    /// <param name="environment">The environment, replaced in place.</param>
    /// <param name="child">The child environment.</param>
    private static void ReplaceWith(IDictionary<string, string?> environment, Dictionary<string, string?> child)
    {
        environment.Clear();
        foreach (var (key, value) in child)
            environment[key] = value;
    }

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
    /// <returns>The two orders, as new maps that compare names ordinally, so names that differ only in case stay two names.</returns>
    private static IEnumerable<Dictionary<string, string?>> BothOrders(IReadOnlyDictionary<string, string?> map)
    {
        var forward = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in map)
            forward[key] = value;
        yield return forward;

        var reverse = new Dictionary<string, string?>(StringComparer.Ordinal);
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

    /// <summary>True when two maps hold the same names (ordinal) with ordinally equal values.</summary>
    /// <param name="left">One map.</param>
    /// <param name="right">The other map.</param>
    /// <returns>True when equal.</returns>
    private static bool SameMap(IReadOnlyDictionary<string, string?> left, IReadOnlyDictionary<string, string?> right) =>
        left.Count == right.Count
        && right.All(pair => left.Any(other => string.Equals(other.Key, pair.Key, StringComparison.Ordinal) && string.Equals(other.Value, pair.Value, StringComparison.Ordinal)));

    /// <summary>A child environment holding the settings and the extra variables.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="extra">The extra variables.</param>
    /// <returns>The child environment, names compared ordinally.</returns>
    private static Dictionary<string, string?> Child(Dictionary<string, string> settings, params (string Key, string Value)[] extra)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
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
