using Microsoft.Data.Sqlite;

namespace QBrainAi.TestSupport.TemporaryInstance;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-4 and ac-12: the checks an integration case uses to prove it runs on a temporary instance it owns, never
/// the live server, its marker or its API key. Pure functions over strings and numbers (no file system, network or
/// environment), linked into the unit project that proves their contract (QBrainAi.Repl.Core.Tests) and the
/// integration projects whose cases call them. Each check returns null when the input passes and a message naming the
/// violation otherwise, so a caller asserts <c>Assert.Null(check)</c> and the failure message says what was wrong.
/// </summary>
internal static class TemporaryInstanceChecks
{
    /// <summary>
    /// Checks the REPL host's start arguments: a <c>--workspace-path</c> and a <c>--marker-file</c> are present and
    /// both lie strictly inside <paramref name="ownedRoot"/>, compared segment by segment ('/' and '\' both separate
    /// segments), and neither path holds a '.' or '..' segment.
    /// </summary>
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
    /// Checks a health answer from the temporary instance: the status is 200 and the body holds the request's nonce
    /// (ordinal), the byte-for-byte echo the server's liveness contract gives.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the answer.</param>
    /// <param name="body">The answer body, or null.</param>
    /// <param name="nonce">The nonce the request sent.</param>
    /// <returns>Null when the answer passes; otherwise the violation.</returns>
    internal static string? CheckHealthAnswer(int statusCode, string? body, string nonce)
    {
        if (string.IsNullOrEmpty(nonce))
            return "The health request carried no nonce to echo.";

        if (statusCode != 200)
            return $"The temporary instance answered health with status {statusCode}, not 200.";

        return body is not null && body.Contains(nonce, StringComparison.Ordinal)
            ? null
            : $"The temporary instance's health answer did not echo the nonce '{nonce}'.";
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
    /// <paramref name="ownedRoot"/>, compared segment by segment with no '.' or '..' segment.
    /// </summary>
    /// <param name="child">The environment the process will start with.</param>
    /// <param name="settings">The instance's own variables.</param>
    /// <param name="ownedRoot">The temporary instance's root folder.</param>
    /// <returns>Null when the environment passes; otherwise the violation.</returns>
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
            if ((key.EndsWith("__Provider", StringComparison.OrdinalIgnoreCase)
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
    /// builder) that lies strictly inside <paramref name="ownedRoot"/>, compared segment by segment with no '.' or '..'
    /// segment. A missing connection string, one with no Data Source (a server connection string), and any other
    /// selection are refused.
    /// </summary>
    /// <param name="connectionString">The connection string the instance's database context holds, or null.</param>
    /// <param name="ownedRoot">The temporary instance's owned data folder.</param>
    /// <returns>Null when the selection passes; otherwise the violation.</returns>
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

    private static bool IsStrictlyInside(string path, string root)
    {
        if (!IsAbsolute(path) || !IsAbsolute(root)) return false;
        var pathSegments = Segments(path);
        if (pathSegments.Any(segment => segment is "." or ".."))
            return false;

        var rootSegments = Segments(root);
        if (pathSegments.Length <= rootSegments.Length)
            return false;

        for (var index = 0; index < rootSegments.Length; index++)
        {
            if (!string.Equals(pathSegments[index], rootSegments[index], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static string[] Segments(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsAbsolute(string path) => path.StartsWith('/') || path.StartsWith(@"\\", StringComparison.Ordinal)
        || (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && path[2] is '/' or '\\');
}
