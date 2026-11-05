using Microsoft.Data.Sqlite;

namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the checks an integration case uses to prove it runs on a temporary instance it owns, never
/// the live server, its marker or its API key. Pure functions over strings and numbers (no file system, network or
/// environment), linked into the unit project that proves their contract (QBrainAi.Repl.Core.Tests) and the
/// integration projects whose cases call them. Each check returns null when the input passes and a message naming the
/// violation otherwise, so a caller asserts <c>Assert.Null(check)</c> and the failure message says what was wrong.
/// Since 2026-11-02 it also holds the FR-MCP-TESTRESOURCE-001 check that a test's generated-artifact folder lies outside
/// the checkout (<see cref="CheckTestOutputFolder"/>).
/// </summary>
internal static class TemporaryInstanceChecks
{
    /// <summary>
    /// Checks the REPL host's start arguments: a <c>--workspace-path</c> and a <c>--marker-file</c> are present and
    /// both lie strictly inside <paramref name="ownedRoot"/> (<see cref="IsStrictlyInside"/>: the same root kind and
    /// root identity, then segment by segment with '/' and '\' both separating segments, UNC and drive-letter paths
    /// ignoring case and POSIX paths ordinally), and neither path holds a '.' or '..' segment.
    /// </summary>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-4 (strengthened: segments compared under the path's own syntax): the containment helper compares Windows-form paths ignoring case, the sibling of hostile validation finding R9-F3 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)"). 2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): documentation only, the summary now states the root rule slice U added to the containment helper (hostile validation round 10 finding R10-F2; stale summary found before round 11).</remarks>
    /// <param name="arguments">The argument list the REPL host was started with.</param>
    /// <param name="ownedRoot">The temporary instance's root folder.</param>
    /// <returns>Null when the arguments pass; otherwise the violation.</returns>
    internal static string? CheckReplStartArguments(IReadOnlyList<string> arguments, string ownedRoot)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedRoot);

        var workspace = ValueAfter(arguments, "--workspace-path");
        if (workspace is null)
            return "The REPL host was started without --workspace-path.";

        var marker = ValueAfter(arguments, "--marker-file");
        if (marker is null)
            return "The REPL host was started without --marker-file.";

        if (!IsStrictlyInside(workspace, ownedRoot))
            return $"The REPL host's workspace '{workspace}' is not inside the temporary instance root '{ownedRoot}'.";

        return IsStrictlyInside(marker, ownedRoot)
            ? null
            : $"The REPL host's marker '{marker}' is not inside the temporary instance root '{ownedRoot}'.";
    }

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4: checks the final REPL arguments, owned server and isolated trust cache.</summary>
    /// <param name="arguments">The actual child arguments.</param>
    /// <param name="environment">The actual child environment.</param>
    /// <param name="ownedRoot">The instance's absolute root.</param>
    /// <param name="serverAddress">The instance's own server address.</param>
    /// <returns>Null only when every consumer setting belongs to the instance.</returns>
    internal static string? CheckReplStartContext(IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string?> environment, string ownedRoot, string serverAddress)
    {
        var violation = CheckReplStartArguments(arguments, ownedRoot);
        if (violation is not null) return violation;
        var address = AliasedValue(environment, "SERVER_URL");
        if (!string.Equals(address, serverAddress, StringComparison.Ordinal))
            return $"The REPL host's server address '{address}' is not its instance's address '{serverAddress}'.";
        var cache = AliasedValue(environment, "REPL_CACHE_DIR");
        return cache is not null && IsStrictlyInside(cache, ownedRoot)
            ? null
            : $"The REPL host's trust cache '{cache}' is not inside its owned root '{ownedRoot}'.";
    }

    private static string? AliasedValue(IReadOnlyDictionary<string, string?> environment, string suffix) =>
        environment.TryGetValue("QBRAINAI_" + suffix, out var canonical) && !string.IsNullOrWhiteSpace(canonical)
            ? canonical
            : environment.GetValueOrDefault("MCP_" + suffix);

    /// <summary>
    /// Checks a health answer from the temporary instance: the status is 200 and the JSON body's text field "nonce" (the
    /// server's health shape) equals the whole nonce the request sent, compared ordinally. A longer or shorter nonce, the
    /// nonce text in another field, and a missing, malformed or non-text nonce field are refused.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the answer.</param>
    /// <param name="body">The answer body, or null.</param>
    /// <param name="nonce">The nonce the request sent.</param>
    /// <returns>Null when the answer passes; otherwise the violation.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 ac-12 (strengthened: the answer's nonce field equals the whole nonce): compares the nonce field exactly instead of finding the nonce anywhere in the body, because hostile validation finding R9-F2 showed a longer nonce or the nonce in another field passed (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
    /// </list>
    /// </remarks>
    internal static string? CheckHealthAnswer(int statusCode, string? body, string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
            return "The health request carried no nonce to echo.";

        if (statusCode != 200)
            return $"The temporary instance answered health with status {statusCode}, not 200.";

        return string.Equals(NonceField(body), nonce, StringComparison.Ordinal)
            ? null
            : $"The temporary instance's health answer did not echo the nonce '{nonce}'.";
    }

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-12: the text value of a JSON health body's "nonce" field.</summary>
    /// <param name="body">The answer body, or null.</param>
    /// <returns>The nonce text, or null when the body is missing, is not a JSON object, or has no text nonce field.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-12: added for hostile validation finding R9-F2.</remarks>
    private static string? NonceField(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(body);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object
                && document.RootElement.TryGetProperty("nonce", out var field)
                && field.ValueKind == System.Text.Json.JsonValueKind.String
                    ? field.GetString()
                    : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>TR-MCP-NUKE-019: the most starts a case makes of a temporary instance that keeps losing its port.</summary>
    internal const int MaxStarts = 3;

    /// <summary>
    /// TR-MCP-NUKE-019: decides whether a temporary instance that failed to start is started again on a newly
    /// picked port. Only an instance whose process exited and whose output says its address was already in use
    /// (ignoring case) is restarted, and only while fewer than <see cref="MaxStarts"/> starts were made; any other start
    /// failure, an instance still running, or a third lost port is not, so the case fails with the instance's output.
    /// </summary>
    /// <param name="starts">The starts made so far, the failed one included.</param>
    /// <param name="exited">Whether the instance process has exited.</param>
    /// <param name="output">The instance process output.</param>
    /// <returns>True when the case starts a new instance on a new port.</returns>
    internal static bool ShouldRestartOnNewPort(int starts, bool exited, string output) =>
        starts < MaxStarts
        && exited
        && (output ?? string.Empty).Contains("address already in use", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the configuration sections no inherited setting may reach, the product's
    /// canonical and legacy sections, the connection strings and the host's endpoint section, matched on the setting's
    /// configuration key (<see cref="ConfigurationKeyIdentity"/>) ignoring case, so every separator spelling of a key in
    /// them counts; an inherited variable in one of them never reaches a temporary instance or its REPL child.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): replaces the raw prefixes Mcp__, QBrainAi__ and ConnectionStrings__ with sections matched on the configuration key, and adds the endpoint section Kestrel, because hostile validation round 12 finding R12-F1 showed that an inherited colon or mixed-separator spelling passed the raw prefixes and that inherited endpoint settings could replace the instance's address.</description></item>
    /// </list>
    /// </remarks>
    internal static readonly IReadOnlyList<string> ConfigurationSections = ["Mcp", "QBrainAi", "ConnectionStrings", "Kestrel"];

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the flat variable prefixes of the product, matched on the raw name ignoring case
    /// (the legacy MCP_, the canonical QBRAINAI_ and the plugin's MCPSERVER_ names).
    /// </summary>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): split from the earlier single prefix list, which also held the section prefixes (hostile validation round 12 finding R12-F1).</remarks>
    internal static readonly IReadOnlyList<string> ProductVariablePrefixes = ["MCP_", "QBRAINAI_", "MCPSERVER_"];

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: the configuration key a process-environment name sets, as .NET configuration reads it:
    /// every double underscore stands for the ':' delimiter (keys are compared ignoring case by the callers).
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>The configuration key.</returns>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): added so the scrub and the start check judge a setting by its key, not its spelling (hostile validation round 12 finding R12-F1).</remarks>
    internal static string ConfigurationKeyIdentity(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Replace("__", ":", StringComparison.Ordinal);
    }

    /// <summary>TR-MCP-NUKE-019 AC-6: the provider fallback variables the database resolver also reads.</summary>
    internal static readonly IReadOnlyList<string> ProviderFallbacks = ["DATABASE_URL", "POSTGRES_CONNECTION_STRING"];

    /// <summary>
    /// TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): builds the environment a temporary instance, or the
    /// REPL child of one, starts with. Every inherited variable whose configuration key lies in
    /// <see cref="ConfigurationSections"/>, alone or host-prefixed, every flat product variable
    /// (<see cref="ProductVariablePrefixes"/>), every connection-string variable and every provider fallback and host
    /// setting (<see cref="ProviderFallbacks"/>; all ignoring case) is removed, so no inherited connection string, data
    /// source, provider, instance name or endpoint, under any separator spelling, can select another database or address;
    /// so is every inherited spelling of a key one of the instance's own <paramref name="settings"/> sets, alone or
    /// host-prefixed (<see cref="SetsOwnedKey"/>); the settings are then applied; every other inherited variable is kept under its
    /// exact name, so two unrelated names that differ only in case stay two variables.
    /// </summary>
    /// <param name="inherited">The environment the process would inherit.</param>
    /// <param name="settings">The instance's own variables.</param>
    /// <returns>The child environment, names compared ordinally, as a POSIX process environment compares them.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): the removal rule now reads each name's configuration key, so colon and mixed-separator spellings and the endpoint section are removed too (hostile validation round 12 finding R12-F1; mock gate ug-r13z4-mockgate-a-1).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): the child compares names ordinally instead of ignoring case, and an inherited spelling of a key the instance sets is removed before the settings are applied, because the seventh holistic read before hostile validation round 13 (finding g1) showed that the ignore-case map merged unrelated inherited names that differ only in case, and the ordinal map alone would have kept an inherited "port" beside the owned "PORT" (mock gate ug-r13z4c-mockgate-1).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 ("so every ... spelling of a key, alone or host-prefixed, sets that key"): an inherited host-prefix spelling of a key the instance sets, or an unprefixed spelling of a key the instance sets under a host prefix, is removed too, through <see cref="SetsOwnedKey"/>, because the eighth holistic read before hostile validation round 13 (finding g1) showed that an inherited NOLOGO beside the owned DOTNET_NOLOGO, or DOTNET_PORT beside the owned PORT, reached the instance (mock gate ug-r13z6-mockgate-1).</description></item>
    /// </list>
    /// </remarks>
    internal static Dictionary<string, string?> BuildOwnedInstanceEnvironment(
        IReadOnlyDictionary<string, string?> inherited,
        IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(inherited);
        ArgumentNullException.ThrowIfNull(settings);

        var child = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in inherited)
        {
            if (!IsProductVariable(key) && !SetsOwnedKey(key, settings))
                child[key] = value;
        }

        foreach (var (key, value) in settings)
            child[key] = value;

        return child;
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): checks a child environment before the process starts:
    /// every product variable, under any separator spelling, is one the instance set, with the value it set; no two names
    /// of one product key, or of one key the instance sets (<see cref="SetsOwnedKey"/>), differ only in separators or
    /// case (<see cref="ConfigurationKeyIdentity"/>, ignoring case), so the value .NET reads cannot depend on enumeration
    /// order; every database-engine setting, under any separator
    /// spelling, names SQLite; and every database the instance's settings select (a connection string's Data Source, or a
    /// data source value) lies strictly inside <paramref name="ownedRoot"/> (<see cref="IsStrictlyInside"/>: the same root
    /// kind and root identity, then segment by segment with no '.' or '..' segment).
    /// </summary>
    /// <param name="child">The environment the process will start with.</param>
    /// <param name="settings">The instance's own variables.</param>
    /// <param name="ownedRoot">The temporary instance's root folder.</param>
    /// <returns>Null when the environment passes; otherwise the violation.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 ac-4; TR-MCP-NUKE-019 AC-6: the SQLite-only rule now applies to database-engine settings only (Database__Provider, DatabaseProvider, DATABASE_PROVIDER, any prefix), because it refused TodoStorage's valid 'database' backend and broke the stdio host case.</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): documentation only, the summary now states the root rule slice U added to the containment helper (hostile validation round 10 finding R10-F2; stale summary found before round 11).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): product variables are recognized by configuration key, two names of one product key are refused, and the database-engine rule reads the configuration key, because hostile validation round 12 finding R12-F1 showed that an unowned colon spelling passed the check and a colon-spelled provider escaped the SQLite rule (mock gate ug-r13z4-mockgate-a-1).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): the rule for two names also covers the keys the instance sets outside the product's namespaces (PORT, a logging level), because the seventh holistic read before hostile validation round 13 (finding g1) showed that once the child keeps names exactly, two spellings of such a key could reach the instance (mock gate ug-r13z4c-mockgate-1).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): the names the rule for two names covers now include the unprefixed spellings of a key the instance sets under a host prefix (<see cref="SetsOwnedKey"/>), while names are still grouped by <see cref="ConfigurationKeyIdentity"/>, so two names that differ only in their host prefix, such as the instance's own DOTNET_ENVIRONMENT and ASPNETCORE_ENVIRONMENT, are not refused; because the eighth holistic read before hostile validation round 13 (finding g1) showed the rule missed such spellings (mock gate ug-r13z6-mockgate-1).</description></item>
    /// </list>
    /// </remarks>
    internal static string? CheckOwnedInstanceEnvironment(
        IReadOnlyDictionary<string, string?> child,
        IReadOnlyDictionary<string, string> settings,
        string ownedRoot)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedRoot);

        foreach (var (key, value) in child)
        {
            if (IsProductVariable(key) && (!settings.TryGetValue(key, out var own) || !string.Equals(own, value, StringComparison.Ordinal)))
                return $"The temporary instance's environment holds '{key}', which the instance did not set.";
        }

        // FR-MCP-TESTRESOURCE-001 ac-4: two names of one product key or of one key the instance sets (differing only in
        // separators or case) would let the enumeration order decide which value .NET reads, so the start is refused.
        var twoNames = child.Keys
            .Where(key => IsProductVariable(key) || SetsOwnedKey(key, settings))
            .GroupBy(ConfigurationKeyIdentity, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (twoNames is not null)
            return $"The temporary instance's environment sets '{twoNames.Key}' under more than one name.";

        foreach (var (key, value) in settings)
        {
            // FR-MCP-TESTRESOURCE-001 ac-4: only database-engine settings must name SQLite: hierarchical Database:Provider,
            // flat DatabaseProvider (each under any separator spelling) and the DATABASE_PROVIDER environment aliases, with
            // any host or instance prefix. Other provider settings, such as TodoStorage's 'database' backend, keep the exact
            // ownership check above.
            var identity = ConfigurationKeyIdentity(key);
            if ((identity.EndsWith(":Database:Provider", StringComparison.OrdinalIgnoreCase)
                    || identity.EndsWith(":DatabaseProvider", StringComparison.OrdinalIgnoreCase)
                    || key.EndsWith("DATABASE_PROVIDER", StringComparison.OrdinalIgnoreCase)) && !IsSqliteProvider(value))
                return $"The temporary instance's database provider '{value}' is not SQLite.";
            var database = key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
                ? LastDataSource(value) ?? value
                : key.Contains("DATA_SOURCE", StringComparison.OrdinalIgnoreCase) || key.Contains("DataSource", StringComparison.OrdinalIgnoreCase)
                    ? value
                    : null;
            if (database is not null && !IsStrictlyInside(database, ownedRoot))
                return $"The temporary instance's database '{database}' is not inside its owned root '{ownedRoot}'.";
        }

        return null;
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-6 (hostile validation findings R8B-F2 and R8B-F3): checks the real database selection of an
    /// in-process temporary instance before it starts: <paramref name="connectionString"/>, the one its database
    /// context holds, names a Data Source (keyword matched ignoring case; the last one wins, as in a connection string
    /// builder) that lies strictly inside <paramref name="ownedRoot"/> (<see cref="IsStrictlyInside"/>: the same root kind
    /// and root identity, then segment by segment with no '.' or '..' segment). A missing connection string, one with no
    /// Data Source (a server connection string), and any other selection are refused.
    /// </summary>
    /// <param name="connectionString">The connection string the instance's database context holds, or null.</param>
    /// <param name="ownedRoot">The temporary instance's owned data folder.</param>
    /// <returns>Null when the selection passes; otherwise the violation.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): documentation only, the summary now states the root rule slice U added to the containment helper (hostile validation round 10 finding R10-F2; stale summary found before round 11).</remarks>
    internal static string? CheckDatabaseSelection(string? connectionString, string ownedRoot) =>
        CheckDatabaseSelection(connectionString, ownedRoot, "sqlite");

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4: refuses non-SQLite providers and parses SQLite's own connection-string syntax.</summary>
    /// <param name="connectionString">The database context's final connection string.</param>
    /// <param name="ownedRoot">The owned absolute data root.</param>
    /// <param name="provider">The context's actual provider name.</param>
    /// <returns>Null for a rooted SQLite file strictly inside the owned root; otherwise the refusal.</returns>
    internal static string? CheckDatabaseSelection(string? connectionString, string ownedRoot, string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownedRoot);

        if (!IsSqliteProvider(provider))
            return $"The temporary instance's database provider '{provider}' is not SQLite.";
        if (string.IsNullOrWhiteSpace(connectionString))
            return "The temporary instance's database context holds no connection string.";

        var database = LastDataSource(connectionString);
        return database is not null && IsStrictlyInside(database, ownedRoot)
            ? null
            : $"The temporary instance's database '{database ?? connectionString}' is not inside its owned root '{ownedRoot}'.";
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (second criterion: "Generated test artifacts are written only to test-owned disposable
    /// output locations, not to repository fixture or documentation paths"): accepts a folder for a test's generated
    /// artifacts only when it is an absolute path with no '.' or '..' segment that lies outside
    /// <paramref name="checkoutRoot"/>, compared through <see cref="PathIdentity"/>: the root (the POSIX root, a drive
    /// letter, or a UNC server and share written with either separator) is part of the path's identity, then segments
    /// split on either separator are compared, UNC and drive-letter paths ignoring case and POSIX paths ordinally. The
    /// checkout root itself, any folder inside it, a case or separator alias of either on Windows and a relative path
    /// are refused. Within one path syntax a path under another root, server, share or drive is another location; a
    /// folder in the other syntax than the checkout (a POSIX path against a Windows drive or UNC path), a Windows device
    /// path ("\\?\", "\\.\" or "\??\", either separator) and a Windows segment that ends in a dot or a space or holds a
    /// colon, a UNC server and share name included, are refused, because each can name a location inside the checkout.
    /// </summary>
    /// <param name="folder">The candidate artifact folder.</param>
    /// <param name="checkoutRoot">The repository checkout root.</param>
    /// <returns>Null when the folder is a test-owned location outside the checkout; otherwise the refusal, naming the folder.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion): added, because two memory-bench stub cases wrote into the checkout's docs/benchmarks/results and rewrote two tracked files on every lane run (operator 2026-11-02: "Fix in scope"; mock gate ug-r9-mockgate-s-1).</description></item>
    /// <item><term>2026-11-02</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "compared ... under the path's own syntax"): containment and equality compare Windows-form paths ignoring case, because hostile validation finding R9-F3 showed a case alias of the checkout was accepted (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "A path's root is part of its identity"): the folder and the checkout are compared through <see cref="PathIdentity"/>, so a forward-slash, backslash or mixed-separator UNC alias of the checkout or of a folder inside it is refused, and a path under another root kind, server, share or drive is another location, because hostile validation round 10 finding R10-F2 showed a forward-slash UNC alias was accepted (operator 2026-11-03: "don't gate on me between rounds").</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion): documentation only, the summary now states the root rule of the entry above instead of the 2026-11-02 rule (stale summary found before round 11).</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "That holds only within one path syntax"): refuses a folder in the other path syntax than the checkout, a Windows device path and a Windows segment that ends in a dot or a space or holds a colon, because the hostile validation round 11 that stopped without a verdict found a device path alias of a folder inside the checkout accepted, and the sibling sweep found the cross-syntax and segment aliases (operator 2026-11-03: "don't gate on me between rounds").</description></item>
    /// <item><term>2026-11-03</term><description>Correction of the entry above (holistic read before hostile validation round 12, finding B3): "don't gate on me between rounds" covers fixing findings after a DISAGREE verdict, and round 11 stopped with none; the change was kept by the operator's choice "Mark ready, then HV (Recommended)" (2026-11-03 13:11). That entry stays as written.</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 (second criterion, appended "The UNC server and share names are held to the same segment rule"): the segment alias rule also covers a UNC server and share name (<see cref="PathIdentity.AliasCheckedNames"/>), so a trailing-dot or trailing-space server or share alias and a share name holding a colon are refused, because the holistic read before hostile validation round 12 (finding D3) found a trailing-dot server alias of the checkout accepted (operator 2026-11-03: "Strengthen + fix (Recommended)").</description></item>
    /// </list>
    /// </remarks>
    internal static string? CheckTestOutputFolder(string folder, string checkoutRoot)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkoutRoot);

        var identity = PathIdentity.Parse(folder);
        var checkout = PathIdentity.Parse(checkoutRoot);
        var acceptable = identity.Kind != PathRootKind.None
            && !identity.Segments.Any(segment => segment is "." or "..")
            && !IsDevicePath(folder)
            && identity.IsWindowsSyntax == checkout.IsWindowsSyntax
            && !(identity.IsWindowsSyntax && identity.AliasCheckedNames.Any(IsWindowsSegmentAlias))
            && !IsStrictlyInside(folder, checkoutRoot)
            && !identity.SameLocation(checkout);
        return acceptable
            ? null
            : $"The test artifact folder '{folder}' is not a test-owned location outside the checkout '{checkoutRoot}'.";
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether a path is a Windows device path: two separators of
    /// either kind, then '?' or '.', then a separator ("\\?\", "\\.\", "//?/"), or a separator, "??" and a separator
    /// ("\??\"). Windows resolves such a path to a drive, a share or a device, which a lexical check cannot place.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>True for a device path.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "That holds only within one path syntax"): added, because the hostile validation round 11 that stopped without a verdict found "\\?\C:\" read as a UNC share named "?/C:", so a device alias of a folder inside the checkout was accepted.</remarks>
    private static bool IsDevicePath(string path) =>
        path.Length >= 4
        && ((IsPathSeparator(path[0]) && IsPathSeparator(path[1]) && path[2] is '?' or '.' && IsPathSeparator(path[3]))
            || (IsPathSeparator(path[0]) && path[1] == '?' && path[2] == '?' && IsPathSeparator(path[3])));

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether a Windows path segment is one Windows resolves to
    /// another name (it ends in a dot or a space) or reads as a stream (it holds a colon).
    /// </summary>
    /// <param name="segment">The segment.</param>
    /// <returns>True for such a segment.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "That holds only within one path syntax"): added by the sibling sweep of the findings of the hostile validation round 11 that stopped without a verdict ("repo." and "repo " name "repo" on Windows).</remarks>
    private static bool IsWindowsSegmentAlias(string segment) =>
        segment.EndsWith('.') || segment.EndsWith(' ') || segment.Contains(':', StringComparison.Ordinal);

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: whether a character is a path separator of
    /// either kind; the one separator test of this class, used by <see cref="IsDevicePath"/> and
    /// <see cref="PathIdentity.Parse"/>.
    /// </summary>
    /// <param name="value">The character.</param>
    /// <returns>True for '/' or '\'.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for <see cref="IsDevicePath"/>.</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: now also the separator test of <see cref="PathIdentity.Parse"/>, replacing the duplicate PathIdentity.IsSeparator, because the holistic read before hostile validation round 12 found the same test written twice in this file (DRY; operator 2026-11-03: "Strengthen + fix (Recommended)").</description></item>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: reads the separator set from <see cref="PathSeparators"/>, which PathIdentity.Split also uses, because the second holistic read before hostile validation round 12 (finding C5) found Split still stating the set itself. Behavior unchanged.</description></item>
    /// </list>
    /// </remarks>
    private static bool IsPathSeparator(char value) => Array.IndexOf(PathSeparators, value) >= 0;

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: the two path separators, '/' and '\'; the one
    /// statement of the separator set of this class, used by <see cref="IsPathSeparator"/> and PathIdentity.Split.
    /// </summary>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: added, because the second holistic read before hostile validation round 12 (finding C5) found the separator set stated twice in this file (DRY).</remarks>
    private static readonly char[] PathSeparators = ['/', '\\'];

    /// <summary>FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: the kind of a path's root.</summary>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: added, because hostile validation round 10 finding R10-F2 showed the checks flattened the root into the segments.</remarks>
    private enum PathRootKind
    {
        /// <summary>Not an absolute path.</summary>
        None,

        /// <summary>The POSIX root, one leading '/'.</summary>
        Posix,

        /// <summary>A drive letter, a colon and a separator.</summary>
        Drive,

        /// <summary>Two leading separators of either kind, then a server and a share.</summary>
        Unc,
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: a path's root (kind and identity: "/", the drive
    /// letter, or "server/share") and its segments after the root, split on either separator. UNC and drive-letter paths
    /// are Windows syntax and compare ignoring case; POSIX paths compare ordinally. Two paths share a location only under
    /// the same root kind and root identity.
    /// </summary>
    /// <param name="Kind">The root kind.</param>
    /// <param name="Root">The root identity.</param>
    /// <param name="Segments">The segments after the root.</param>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-03</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root") and the generated-artifact criterion (appended "A path's root is part of its identity"): added, replacing the segment split that dropped the root and the Windows-form test that missed forward-slash UNC paths, because hostile validation round 10 finding R10-F2 showed a single-rooted path was accepted under a UNC owned root and a forward-slash UNC alias of the checkout was compared ordinally (operator 2026-11-03: "don't gate on me between rounds"). Follows the repository's own reading of "//" as UNC (WorkspaceIdentityPath).</description></item>
    /// </list>
    /// </remarks>
    private readonly record struct PathIdentity(PathRootKind Kind, string Root, string[] Segments)
    {
        /// <summary>How this path's root and segments compare: ignoring case for Windows syntax, ordinally for POSIX.</summary>
        internal StringComparison Comparison => Kind is PathRootKind.Drive or PathRootKind.Unc ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        /// <summary>
        /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether the path is in Windows syntax (a drive letter
        /// or a UNC share) rather than POSIX syntax.
        /// </summary>
        /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "That holds only within one path syntax"): added, because a folder is compared with the checkout only within one path syntax.</remarks>
        internal bool IsWindowsSyntax => Kind is PathRootKind.Drive or PathRootKind.Unc;

        /// <summary>
        /// FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): the names the Windows segment alias rule covers: for a
        /// UNC path its server and share names, then its segments; otherwise its segments.
        /// </summary>
        /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion (appended "The UNC server and share names are held to the same segment rule"): added, because the holistic read before hostile validation round 12 (finding D3) found a trailing-dot server alias of the checkout accepted (operator 2026-11-03: "Strengthen + fix (Recommended)").</remarks>
        internal IEnumerable<string> AliasCheckedNames => Kind == PathRootKind.Unc ? Root.Split('/').Concat(Segments) : Segments;

        /// <summary>Parses a path into its root and segments.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The identity; kind None when the path is not absolute.</returns>
        /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: uses the class's one separator test <see cref="IsPathSeparator"/> instead of its own copy (DRY; holistic read before hostile validation round 12).</remarks>
        internal static PathIdentity Parse(string path)
        {
            if (path.Length > 2 && IsPathSeparator(path[0]) && IsPathSeparator(path[1]) && !IsPathSeparator(path[2]))
            {
                var parts = Split(path[2..]);
                return parts.Length >= 2 ? new PathIdentity(PathRootKind.Unc, parts[0] + "/" + parts[1], parts[2..]) : new PathIdentity(PathRootKind.None, string.Empty, []);
            }

            if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsPathSeparator(path[2]))
                return new PathIdentity(PathRootKind.Drive, path[..1], Split(path[3..]));
            return path.StartsWith('/')
                ? new PathIdentity(PathRootKind.Posix, "/", Split(path[1..]))
                : new PathIdentity(PathRootKind.None, string.Empty, []);
        }

        /// <summary>Whether another identity has the same root kind and root identity.</summary>
        /// <param name="other">The other identity.</param>
        /// <returns>True for the same root.</returns>
        internal bool SameRoot(PathIdentity other) =>
            Kind != PathRootKind.None && Kind == other.Kind && string.Equals(Root, other.Root, Comparison);

        /// <summary>Whether another identity names the same location: the same root and the same segments.</summary>
        /// <param name="other">The other identity.</param>
        /// <returns>True for the same location.</returns>
        internal bool SameLocation(PathIdentity other)
        {
            var comparison = Comparison;
            return SameRoot(other) && Segments.Length == other.Segments.Length
                && Segments.Zip(other.Segments).All(pair => string.Equals(pair.First, pair.Second, comparison));
        }

        /// <summary>The non-empty parts of a text split on either separator.</summary>
        /// <param name="text">The text after the root.</param>
        /// <returns>The parts.</returns>
        /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: splits on the class's one separator set <see cref="PathSeparators"/> instead of its own copy, because the second holistic read before hostile validation round 12 (finding C5) found the set stated twice (DRY). Behavior unchanged.</remarks>
        private static string[] Split(string text) => text.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    private static readonly string[] ConnectionStringPrefixes =
        ["SQLCONNSTR_", "SQLAZURECONNSTR_", "MYSQLCONNSTR_", "POSTGRESQLCONNSTR_", "CUSTOMCONNSTR_"];

    private static readonly string[] HostSettings =
        ["CONTENTROOT", "ENVIRONMENT", "URLS", "APPLICATIONNAME", "WEBROOT", "HTTP_PORTS", "HTTPS_PORTS",
            "HOSTINGSTARTUPASSEMBLIES", "HOSTINGSTARTUPEXCLUDEASSEMBLIES", "PREVENTHOSTINGSTARTUP"];

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: whether an environment name is a product variable: any ASPNETCORE_ name, a DOTNET_
    /// name whose rest is one, a name whose configuration key lies in <see cref="ConfigurationSections"/> (the section
    /// itself or below it), a flat product variable, a connection-string variable, or a provider fallback or host setting
    /// read by its configuration key; all ignoring case.
    /// </summary>
    /// <param name="key">The environment name.</param>
    /// <returns>True for a product variable.</returns>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 (appended "A setting is identified by its configuration key, not by its spelling"): sections are matched on the configuration key instead of raw double-underscore prefixes (hostile validation round 12 finding R12-F1; mock gate ug-r13z4-mockgate-a-1).</remarks>
    private static bool IsProductVariable(string key)
    {
        if (key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
            return IsProductVariable(key["DOTNET_".Length..]);
        var identity = ConfigurationKeyIdentity(key);
        return ConfigurationSections.Any(section => string.Equals(identity, section, StringComparison.OrdinalIgnoreCase)
                || identity.StartsWith(section + ":", StringComparison.OrdinalIgnoreCase))
            || ProductVariablePrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || ConnectionStringPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || ProviderFallbacks.Concat(HostSettings).Any(name => string.Equals(identity, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: whether an environment name sets the same configuration key as one of the instance's
    /// own settings: one of the keys it sets (<see cref="ConfigurationKeysSetBy"/>) equals, ignoring case, one of the keys a
    /// setting's name sets, so another case, separator or host-prefix spelling of an owned key counts and the owned
    /// setting governs it.
    /// </summary>
    /// <param name="key">The environment name.</param>
    /// <param name="settings">The instance's own variables.</param>
    /// <returns>True when the name sets a key the instance sets.</returns>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 (appended "A name is matched exactly, as the process environment holds it"): added for the builder and the rule for two names, because the seventh holistic read before hostile validation round 13 (finding g1) showed that the keys the instance sets outside the product's namespaces had no spelling rule (mock gate ug-r13z4c-mockgate-1).</description></item>
    /// <item><term>2026-11-05</term><description>FR-MCP-TESTRESOURCE-001 ac-4 ("a DOTNET_ or ASPNETCORE_ prefix places the rest of the name in the host's configuration, so every ... spelling of a key, alone or host-prefixed, sets that key"): compares the keys a name sets with and without a host prefix instead of its double-underscore spelling alone, because the eighth holistic read before hostile validation round 13 (finding g1) showed that an inherited NOLOGO beside the owned DOTNET_NOLOGO, or DOTNET_PORT beside the owned PORT, was kept (mock gate ug-r13z6-mockgate-1).</description></item>
    /// </list>
    /// </remarks>
    private static bool SetsOwnedKey(string key, IReadOnlyDictionary<string, string> settings)
    {
        var keys = ConfigurationKeysSetBy(key);
        return settings.Keys.Any(own => ConfigurationKeysSetBy(own).Any(ownKey => keys.Contains(ownKey, StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4: the configuration keys an environment name sets: its
    /// <see cref="ConfigurationKeyIdentity"/>, and, when the name starts with a host prefix (DOTNET_ or ASPNETCORE_, ignoring
    /// case), the identity of the rest of the name, which the host's configuration reads.
    /// </summary>
    /// <param name="name">The environment name.</param>
    /// <returns>The keys, the unprefixed reading first.</returns>
    /// <remarks>2026-11-05, FR-MCP-TESTRESOURCE-001 ac-4 ("a DOTNET_ or ASPNETCORE_ prefix places the rest of the name in the host's configuration"): added so <see cref="SetsOwnedKey"/> reads a host-prefix spelling of an owned key, because the eighth holistic read before hostile validation round 13 (finding g1) showed it read only the double-underscore spelling (mock gate ug-r13z6-mockgate-1).</remarks>
    internal static IReadOnlyList<string> ConfigurationKeysSetBy(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var keys = new List<string> { ConfigurationKeyIdentity(name) };
        foreach (var prefix in HostPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                keys.Add(ConfigurationKeyIdentity(name[prefix.Length..]));
        }

        return keys;
    }

    private static readonly string[] HostPrefixes = ["DOTNET_", "ASPNETCORE_"];

    private static bool IsSqliteProvider(string provider) =>
        string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal);

    private static string? LastDataSource(string connectionString)
    {
        try
        {
            return new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? ValueAfter(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.Ordinal))
                return arguments[index + 1];
        }

        return null;
    }

    /// <summary>
    /// FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: whether an absolute path lies strictly inside an
    /// absolute root: both parsed by <see cref="PathIdentity"/>, the same root kind and root identity, no '.' or '..'
    /// segment in the path, more segments than the root, and the root's segments as its first segments, compared under
    /// the path's own syntax (UNC and drive-letter paths ignoring case, POSIX paths ordinally).
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="root">The root.</param>
    /// <returns>True when the path is strictly inside the root.</returns>
    /// <remarks>2026-11-02, FR-MCP-TESTRESOURCE-001 ac-4 and the generated-artifact criterion: compares under the paths' own syntax instead of ordinally everywhere (hostile validation finding R9-F3 and its ac-4 sibling; operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)"). 2026-11-03, FR-MCP-TESTRESOURCE-001 ac-4 (appended "Strict containment in the owned root also requires the same root"): requires the same root kind and root identity before comparing segments (<see cref="PathIdentity"/>), because hostile validation round 10 finding R10-F2 showed a single-rooted path was accepted under a UNC root. 2026-11-03, documentation only: the summary no longer cites the removed member SegmentComparison and states the root rule (stale summary found before round 11; test projects build without documentation output, so no compiler check caught the dangling reference).</remarks>
    private static bool IsStrictlyInside(string path, string root)
    {
        var pathIdentity = PathIdentity.Parse(path);
        var rootIdentity = PathIdentity.Parse(root);
        if (!pathIdentity.SameRoot(rootIdentity) || pathIdentity.Segments.Any(segment => segment is "." or ".."))
            return false;
        if (pathIdentity.Segments.Length <= rootIdentity.Segments.Length)
            return false;

        var comparison = pathIdentity.Comparison;
        for (var index = 0; index < rootIdentity.Segments.Length; index++)
        {
            if (!string.Equals(pathIdentity.Segments[index], rootIdentity.Segments[index], comparison))
                return false;
        }

        return true;
    }
}
