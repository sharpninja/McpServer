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
    /// FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the configuration and variable namespaces of the product, matched ignoring case; an
    /// inherited variable in one of them never reaches a temporary instance or its REPL child.
    /// </summary>
    internal static readonly IReadOnlyList<string> ProductNamespaces = ["Mcp__", "QBrainAi__", "MCP_", "QBRAINAI_", "MCPSERVER_", "ConnectionStrings__"];

    /// <summary>TR-MCP-NUKE-019 AC-6: the provider fallback variables the database resolver also reads.</summary>
    internal static readonly IReadOnlyList<string> ProviderFallbacks = ["DATABASE_URL", "POSTGRES_CONNECTION_STRING"];

    /// <summary>
    /// TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): builds the environment a temporary instance, or the
    /// REPL child of one, starts with. Every inherited variable in <see cref="ProductNamespaces"/> (ignoring case) and
    /// every provider fallback in <see cref="ProviderFallbacks"/> is removed, so no inherited connection string, data
    /// source, provider or instance name can select another database; the instance's own <paramref name="settings"/> are
    /// then applied; every other inherited variable is kept.
    /// </summary>
    /// <param name="inherited">The environment the process would inherit.</param>
    /// <param name="settings">The instance's own variables.</param>
    /// <returns>The child environment, keys compared ignoring case.</returns>
    internal static Dictionary<string, string?> BuildOwnedInstanceEnvironment(
        IReadOnlyDictionary<string, string?> inherited,
        IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(inherited);
        ArgumentNullException.ThrowIfNull(settings);

        var child = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in inherited)
        {
            if (!IsProductVariable(key))
                child[key] = value;
        }

        foreach (var (key, value) in settings)
            child[key] = value;

        return child;
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-6 (hostile validation finding R8B-F3): checks a child environment before the process starts:
    /// every variable in the product's namespaces is one the instance set, with the value it set, and every database
    /// the instance's settings select (a connection string's Data Source, or a data source value) lies strictly inside
    /// <paramref name="ownedRoot"/> (<see cref="IsStrictlyInside"/>: the same root kind and root identity, then segment by
    /// segment with no '.' or '..' segment).
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

        foreach (var (key, value) in settings)
        {
            // FR-MCP-TESTRESOURCE-001 ac-4: only database-engine settings must name SQLite: hierarchical Database__Provider,
            // flat DatabaseProvider and the DATABASE_PROVIDER environment aliases, with any host or instance prefix. Other
            // provider settings, such as TodoStorage's 'database' backend, keep the exact ownership check above.
            if ((key.EndsWith("__Database__Provider", StringComparison.OrdinalIgnoreCase)
                    || key.EndsWith("__DatabaseProvider", StringComparison.OrdinalIgnoreCase)
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
    /// colon are refused, because each can name a location inside the checkout.
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
            && !(identity.IsWindowsSyntax && identity.Segments.Any(IsWindowsSegmentAlias))
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

    /// <summary>FR-MCP-TESTRESOURCE-001 (generated-artifact criterion): whether a character is a path separator of either kind.</summary>
    /// <param name="value">The character.</param>
    /// <returns>True for '/' or '\'.</returns>
    /// <remarks>2026-11-03, FR-MCP-TESTRESOURCE-001 generated-artifact criterion: added for <see cref="IsDevicePath"/>.</remarks>
    private static bool IsPathSeparator(char value) => value is '/' or '\\';

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

        /// <summary>Parses a path into its root and segments.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The identity; kind None when the path is not absolute.</returns>
        internal static PathIdentity Parse(string path)
        {
            if (path.Length > 2 && IsSeparator(path[0]) && IsSeparator(path[1]) && !IsSeparator(path[2]))
            {
                var parts = Split(path[2..]);
                return parts.Length >= 2 ? new PathIdentity(PathRootKind.Unc, parts[0] + "/" + parts[1], parts[2..]) : new PathIdentity(PathRootKind.None, string.Empty, []);
            }

            if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && IsSeparator(path[2]))
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

        /// <summary>Whether a character is a path separator of either kind.</summary>
        /// <param name="value">The character.</param>
        /// <returns>True for '/' or '\'.</returns>
        private static bool IsSeparator(char value) => value is '/' or '\\';

        /// <summary>The non-empty parts of a text split on either separator.</summary>
        /// <param name="text">The text after the root.</param>
        /// <returns>The parts.</returns>
        private static string[] Split(string text) => text.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static readonly string[] ConnectionStringPrefixes =
        ["SQLCONNSTR_", "SQLAZURECONNSTR_", "MYSQLCONNSTR_", "POSTGRESQLCONNSTR_", "CUSTOMCONNSTR_"];

    private static readonly string[] HostSettings =
        ["CONTENTROOT", "ENVIRONMENT", "URLS", "APPLICATIONNAME", "WEBROOT", "HTTP_PORTS", "HTTPS_PORTS",
            "HOSTINGSTARTUPASSEMBLIES", "HOSTINGSTARTUPEXCLUDEASSEMBLIES", "PREVENTHOSTINGSTARTUP"];

    private static bool IsProductVariable(string key)
    {
        if (key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
            return IsProductVariable(key["DOTNET_".Length..]);
        return ProductNamespaces.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || ConnectionStringPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || ProviderFallbacks.Concat(HostSettings).Any(name => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
    }

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
