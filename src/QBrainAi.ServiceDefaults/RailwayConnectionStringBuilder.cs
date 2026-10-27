using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Fluent builder that resolves and converts PostgreSQL connection strings for Railway
/// (and similar PaaS environments). Replaces scattered boilerplate across service Program.cs files.
/// </summary>
/// <remarks>
/// <para>Usage:</para>
/// <code>
/// new RailwayConnectionStringBuilder(builder.Configuration)
///     .WithConfigKey("marketing")
///     .WithEnvironmentFallback("DATABASE_URL", "DATABASE_PRIVATE_URL", "ConnectionStrings__marketing", "POSTGRES_URL")
///     .WithInvalidTemplatePlaceholder()
///     .Apply(builder.Configuration);
/// </code>
/// </remarks>
public sealed class RailwayConnectionStringBuilder
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private string _configKey = string.Empty;
    private string[] _envVarNames = [];
    private bool _useInvalidTemplatePlaceholder;
    private bool _preferEnvInDevelopment;
    private string? _environment;
    private SslMode _defaultSslMode = SslMode.Require;

    /// <summary>
    /// Initializes a new builder with access to the application configuration.
    /// </summary>
    /// <param name="configuration">The application configuration containing connection strings.</param>
    public RailwayConnectionStringBuilder(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>
    /// Sets the connection string key (e.g. "marketing", "funwashad", "shortener").
    /// This corresponds to <c>ConnectionStrings:{key}</c> in configuration.
    /// </summary>
    public RailwayConnectionStringBuilder WithConfigKey(string key)
    {
        _configKey = key ?? throw new ArgumentNullException(nameof(key));
        return this;
    }

    /// <summary>
    /// Sets the environment variable names to try as fallback when the configured value is
    /// missing or contains unexpanded Railway templates (<c>${{...}}</c> or <c>${...}</c>).
    /// </summary>
    public RailwayConnectionStringBuilder WithEnvironmentFallback(params string[] envVarNames)
    {
        _envVarNames = envVarNames ?? throw new ArgumentNullException(nameof(envVarNames));
        return this;
    }

    /// <summary>
    /// When enabled, if the configured value is an unexpanded Railway template and no
    /// environment variable resolves, a safe placeholder connection string is injected
    /// so Npgsql never receives the raw template.
    /// </summary>
    public RailwayConnectionStringBuilder WithInvalidTemplatePlaceholder()
    {
        _useInvalidTemplatePlaceholder = true;
        return this;
    }

    /// <summary>
    /// When enabled, environment variables are checked first in Development so local
    /// debugging can use a Railway staging database.
    /// </summary>
    public RailwayConnectionStringBuilder PreferEnvironmentInDevelopment()
    {
        _preferEnvInDevelopment = true;
        return this;
    }

    /// <summary>
    /// Sets the hosting environment name (e.g. "Development"). Required when
    /// <see cref="PreferEnvironmentInDevelopment"/> is used.
    /// </summary>
    public RailwayConnectionStringBuilder WithEnvironmentName(string environmentName)
    {
        _environment = environmentName;
        return this;
    }

    /// <summary>
    /// Sets the default SSL mode used when the URI does not contain a <c>?sslmode=</c> parameter.
    /// Defaults to <see cref="SslMode.Require"/> for Railway compatibility.
    /// </summary>
    public RailwayConnectionStringBuilder WithDefaultSslMode(SslMode sslMode)
    {
        _defaultSslMode = sslMode;
        return this;
    }

    /// <summary>
    /// Resolves the connection string and injects it into <paramref name="configuration"/>
    /// under <c>ConnectionStrings:{configKey}</c>.
    /// </summary>
    /// <param name="configuration">The mutable configuration manager to write back to.</param>
    /// <returns>The resolved Npgsql connection string, or <c>null</c> if nothing could be resolved.</returns>
    public string? Apply(ConfigurationManager configuration)
    {
        var resolved = Build();
        if (resolved != null)
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{_configKey}"] = resolved
            });
        }

        return resolved;
    }

    /// <summary>
    /// Resolves and returns the Npgsql connection string without injecting it.
    /// </summary>
    public string? Build()
    {
        if (string.IsNullOrEmpty(_configKey))
            throw new InvalidOperationException("WithConfigKey must be called before Build/Apply.");

        var configured = _configuration[$"ConnectionStrings:{_configKey}"];

        // In Development, optionally prefer environment variables over config.
        if (_preferEnvInDevelopment && IsDevelopment())
        {
            var fromEnv = TryResolveFromEnvironment();
            if (fromEnv != null)
                return fromEnv;
        }

        // If configured value is an unexpanded template, try env fallback first.
        if (IsInvalidTemplate(configured))
        {
            var fromEnv = TryResolveFromEnvironment();
            if (fromEnv != null)
                return fromEnv;

            // No env var found — use placeholder if enabled.
            if (_useInvalidTemplatePlaceholder)
                return $"Host=0.0.0.0.invalid;Port=5432;Database={_configKey};Username=postgres;Password=;SSL Mode=Disable";

            return null;
        }

        // If configured value is a valid postgres URL, convert it.
        if (IsPostgresUrl(configured))
            return ConvertPostgresConnectionString(configured!);

        // Already a key=value string or null — return as-is.
        return string.IsNullOrWhiteSpace(configured) ? null : configured;
    }

    private string? TryResolveFromEnvironment()
    {
        foreach (var name in _envVarNames)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                continue;
            return IsPostgresUrl(value) ? ConvertPostgresConnectionString(value) : value;
        }

        return null;
    }

    private bool IsDevelopment() =>
        string.Equals(_environment, "Development", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a <c>postgresql://</c> or <c>postgres://</c> configured value to an Npgsql key=value connection string,
    /// honoring query string parameters (e.g. <c>?sslmode=disable</c>).
    /// </summary>
    /// <param name="configuredValue">The PostgreSQL configured value to convert.</param>
    /// <returns>Npgsql connection string.</returns>
    public string ConvertPostgresConnectionString(string configuredValue)
    {
        var url = configuredValue;
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

        // Parse query parameters for known Npgsql settings.
        var queryParams = ParseQueryString(uri.Query);
        var sslMode = queryParams.TryGetValue("sslmode", out var sm) ? NormalizeSslMode(sm) : _defaultSslMode.ToNpgsqlString();
        parts.Add("SSL Mode=" + sslMode);
        if (!string.Equals(sslMode, "Disable", StringComparison.OrdinalIgnoreCase))
            parts.Add("Trust Server Certificate=true");

        // Forward other common Npgsql parameters from query string.
        AddIfPresent(parts, queryParams, "application_name", "Application Name");
        AddIfPresent(parts, queryParams, "connect_timeout", "Timeout");
        AddIfPresent(parts, queryParams, "command_timeout", "Command Timeout");
        AddIfPresent(parts, queryParams, "options", "Options");
        AddIfPresent(parts, queryParams, "search_path", "Search Path");

        return string.Join(";", parts);
    }

    private static Dictionary<string, string> ParseQueryString(string? query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return result;
        foreach (var param in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = param.Split('=', 2);
            if (kv.Length == 2)
                result[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1]);
        }

        return result;
    }

    private static string NormalizeSslMode(string value)
    {
        if (string.Equals(value, "disable", StringComparison.OrdinalIgnoreCase)) return "Disable";
        if (string.Equals(value, "allow", StringComparison.OrdinalIgnoreCase)) return "Allow";
        if (string.Equals(value, "prefer", StringComparison.OrdinalIgnoreCase)) return "Prefer";
        if (string.Equals(value, "require", StringComparison.OrdinalIgnoreCase)) return "Require";
        if (string.Equals(value, "verify-ca", StringComparison.OrdinalIgnoreCase)) return "VerifyCA";
        if (string.Equals(value, "verify-full", StringComparison.OrdinalIgnoreCase)) return "VerifyFull";
        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static void AddIfPresent(List<string> parts, Dictionary<string, string> queryParams, string queryKey, string npgsqlKey)
    {
        if (queryParams.TryGetValue(queryKey, out var value) && !string.IsNullOrEmpty(value))
            parts.Add(npgsqlKey + "=" + value);
    }

    private static bool IsPostgresUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value!.TrimStart().StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || value.TrimStart().StartsWith("postgres://", StringComparison.OrdinalIgnoreCase));

    private static bool IsInvalidTemplate(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value!.Contains("${{", StringComparison.Ordinal) || value.Contains("${", StringComparison.Ordinal));
}

/// <summary>
/// PostgreSQL SSL modes supported by Npgsql.
/// </summary>
public enum SslMode
{
    /// <summary>SSL is not used.</summary>
    Disable,
    /// <summary>SSL is used if the server supports it.</summary>
    Allow,
    /// <summary>SSL is preferred but not required.</summary>
    Prefer,
    /// <summary>SSL is required.</summary>
    Require,
    /// <summary>SSL is required and the server certificate must be verified.</summary>
    VerifyCA,
    /// <summary>SSL is required and the server certificate hostname must match.</summary>
    VerifyFull
}

/// <summary>
/// Extension methods for <see cref="SslMode"/>.
/// </summary>
public static class SslModeExtensions
{
    /// <summary>
    /// Returns the Npgsql connection string representation of the SSL mode.
    /// </summary>
    public static string ToNpgsqlString(this SslMode mode) => mode switch
    {
        SslMode.Disable => "Disable",
        SslMode.Allow => "Allow",
        SslMode.Prefer => "Prefer",
        SslMode.Require => "Require",
        SslMode.VerifyCA => "VerifyCA",
        SslMode.VerifyFull => "VerifyFull",
        _ => "Require"
    };
}
