namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Resolves PostgreSQL connection strings for Railway and similar environments:
/// converts postgresql:// URLs to Npgsql key=value format and supports env fallback for unexpanded templates.
/// </summary>
/// <remarks>
/// <b>Deprecated.</b> Prefer <see cref="RailwayConnectionStringBuilder"/> for new code.
/// This class remains for backward compatibility with <c>DatabaseMigrationService</c>.
/// </remarks>
[Obsolete("Use RailwayConnectionStringBuilder instead. This class will be removed in a future version.")]
public static class PostgresConnectionStringResolver
{
    /// <summary>
    /// Converts a postgresql:// or postgres:// configured value to Npgsql key=value connection string (e.g. for Railway DATABASE_URL).
    /// </summary>
    /// <param name="configuredValue">The configured value to convert.</param>
    /// <returns>Npgsql connection string, or the original string if not a postgres URL.</returns>
    public static string ConvertPostgresConnectionString(string configuredValue)
    {
        var url = configuredValue;
        if (string.IsNullOrWhiteSpace(url))
            return url;
        url = url.Trim();
        if (!url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return url;
        try
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo?.Split(':', 2) ?? [];
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = (uri.AbsolutePath?.TrimStart('/') ?? "railway").Replace("/", "", StringComparison.Ordinal);
            var parts = new List<string>
            {
                "Host=" + host,
                "Port=" + port,
                "Database=" + database,
                "Username=" + username
            };
            if (!string.IsNullOrEmpty(password))
                parts.Add("Password=" + password);

            // Honor sslmode from query string; default to Require for Railway compatibility.
            var sslMode = "Require";
            var query = uri.Query;
            if (!string.IsNullOrEmpty(query))
            {
                foreach (var param in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = param.Split('=', 2);
                    if (kv.Length == 2 && string.Equals(kv[0], "sslmode", StringComparison.OrdinalIgnoreCase))
                    {
                        sslMode = char.ToUpperInvariant(kv[1][0]) + kv[1][1..];
                        break;
                    }
                }
            }

            parts.Add("SSL Mode=" + sslMode);
            if (!string.Equals(sslMode, "Disable", StringComparison.OrdinalIgnoreCase))
                parts.Add("Trust Server Certificate=true");
            return string.Join(";", parts);
        }
        catch (UriFormatException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
            return url;
        }
    }

    /// <summary>
    /// Resolves a connection string: converts postgres URLs to Npgsql format and falls back to environment variables when the configured value looks like an unexpanded template (${{...}} or ${...}).
    /// </summary>
    /// <param name="configured">The configured connection string (e.g. from appsettings or ConnectionStrings:key).</param>
    /// <param name="envVarNames">Environment variable names to try in order when configured value is an invalid template.</param>
    /// <returns>Resolved Npgsql connection string, or null if nothing could be resolved.</returns>
    public static string? ResolveConnectionString(string? configured, params string[] envVarNames)
    {
        ArgumentNullException.ThrowIfNull(envVarNames);
        var isInvalidTemplate = !string.IsNullOrWhiteSpace(configured)
            && (configured!.Contains("${{", StringComparison.Ordinal) || configured.Contains("${", StringComparison.Ordinal));
        var isPostgresUrl = !string.IsNullOrWhiteSpace(configured)
            && (configured!.TrimStart().StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                || configured.TrimStart().StartsWith("postgres://", StringComparison.OrdinalIgnoreCase));

        if (isPostgresUrl)
            return ConvertPostgresConnectionString(configured!);

        if (isInvalidTemplate && envVarNames.Length > 0)
        {
            foreach (var name in envVarNames)
            {
                var fromEnv = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(fromEnv))
                    continue;
                if (fromEnv.TrimStart().StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                    || fromEnv.TrimStart().StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
                    return ConvertPostgresConnectionString(fromEnv);
                return fromEnv;
            }
        }

        return string.IsNullOrWhiteSpace(configured) ? null : configured;
    }
}
