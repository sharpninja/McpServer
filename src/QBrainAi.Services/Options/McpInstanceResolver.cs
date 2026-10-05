using QBrainAi.Common.AgentCli;

namespace QBrainAi.Support.Mcp.Options;

/// <summary>
/// Resolves effective MCP configuration values, with optional per-instance overrides.
/// </summary>
public static class McpInstanceResolver
{
    /// <summary>
    /// Gets requested instance name from command-line args or MCP_INSTANCE environment variable.
    /// Supports: --instance name OR --instance=name.
    /// </summary>
    public static string? GetRequestedInstanceName(string[] args)
    {
        var fromArgs = GetArgValue(args, "instance");
        if (!string.IsNullOrWhiteSpace(fromArgs))
            return fromArgs.Trim();

        var fromEnv = ProductEnvironment.GetLegacy("MCP_INSTANCE");
        return string.IsNullOrWhiteSpace(fromEnv) ? null : fromEnv.Trim();
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: True when both the canonical QBrainAi section and the 1.x Mcp section exist.
    /// Callers log one warning. Per-key reads still prefer QBrainAi.
    /// </summary>
    public static bool CanonicalAndLegacySectionsBothPresent(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.GetSection("QBrainAi").Exists() && configuration.GetSection("Mcp").Exists();
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: Copies the effective QBrainAi section onto Mcp as the last source added so far.
    /// Mcp-only keys remain. Sources added after this call still win, so tests can override Mcp after the projection.
    /// </summary>
    /// <param name="builder">Configuration builder that already exposes the sources added so far.</param>
    public static void ProjectCanonicalSectionOverLegacy(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (builder is not IConfiguration current)
            throw new ArgumentException("The configuration builder must expose the sources added so far.", nameof(builder));

        var canonical = current.GetSection("QBrainAi");
        if (!canonical.Exists())
            return;

        var overlay = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        CopySection(canonical, "Mcp", overlay);
        if (overlay.Count == 0)
            return;

        builder.AddInMemoryCollection(overlay);
    }

    /// <summary>
    /// TR-MCP-QBRAIN-005: Builds a configuration whose root is the product section.
    /// Keys from Mcp are copied first. QBrainAi keys overwrite them.
    /// </summary>
    public static IConfiguration GetEffectiveProductConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        CopySection(configuration.GetSection("Mcp"), string.Empty, merged);
        CopySection(configuration.GetSection("QBrainAi"), string.Empty, merged);
        return new ConfigurationBuilder().AddInMemoryCollection(merged).Build();
    }

    /// <summary>
    /// Reads an effective value from either Mcp:Instances:{instance}:{key} or Mcp:{key}.
    /// </summary>
    public static string? GetEffectiveMcpValue(IConfiguration configuration, string? instanceName, string key)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(key);

        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            var canonicalInstance = configuration[$"QBrainAi:Instances:{instanceName}:{key}"];
            if (!string.IsNullOrWhiteSpace(canonicalInstance))
                return canonicalInstance;

            var legacyInstance = configuration[$"Mcp:Instances:{instanceName}:{key}"];
            if (!string.IsNullOrWhiteSpace(legacyInstance))
                return legacyInstance;
        }

        var canonical = configuration[$"QBrainAi:{key}"];
        if (!string.IsNullOrWhiteSpace(canonical))
            return canonical;

        return configuration[$"Mcp:{key}"];
    }

    /// <summary>
    /// Reads an effective int value from either Mcp:Instances:{instance}:{key} or Mcp:{key}.
    /// </summary>
    public static int GetEffectiveMcpInt(IConfiguration configuration, string? instanceName, string key, int fallback)
    {
        var raw = GetEffectiveMcpValue(configuration, instanceName, key);
        return int.TryParse(raw, out var value) ? value : fallback;
    }

    /// <summary>
    /// Resolves a configured sqlite datasource against the effective data folder when the datasource is relative.
    /// Instance-scoped overrides are honored.
    /// </summary>
    public static string ResolveSqliteDataSource(IConfiguration configuration, string? instanceName)
    {
        var dataSource = GetEffectiveMcpValue(configuration, instanceName, "DataSource") ?? "mcp.db";
        return ResolveDataPath(configuration, instanceName, dataSource);
    }

    /// <summary>
    /// Resolves the effective data folder from root-level <c>DataFolder</c>,
    /// falling back to legacy <c>Mcp:DataDirectory</c> for backward compatibility.
    /// </summary>
    public static string GetEffectiveDataFolder(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var rootDataFolder = configuration["DataFolder"];
        if (!string.IsNullOrWhiteSpace(rootDataFolder))
            return ResolveFullPath(rootDataFolder);

        var legacyDataDirectory = GetEffectiveMcpValue(configuration, instanceName, "DataDirectory");
        if (!string.IsNullOrWhiteSpace(legacyDataDirectory))
            return ResolveFullPath(legacyDataDirectory);

        return ResolveFullPath(".");
    }

    /// <summary>
    /// Resolves a configured path against the effective data folder when the path is relative.
    /// </summary>
    public static string ResolveDataPath(IConfiguration configuration, string? instanceName, string configuredPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuredPath);

        if (Path.IsPathRooted(configuredPath))
            return ResolveFullPath(configuredPath);

        var dataFolder = GetEffectiveDataFolder(configuration, instanceName);
        return ResolveFullPath(Path.Combine(dataFolder, configuredPath));
    }

    /// <summary>
    /// Validates configured instances for duplicate ports and basic required values.
    /// </summary>
    public static void ValidateInstances(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var instances = configuration.GetSection("QBrainAi:Instances").GetChildren().ToList();
        if (instances.Count == 0)
            instances = configuration.GetSection("Mcp:Instances").GetChildren().ToList();
        if (instances.Count == 0)
            return;

        var usedPorts = new Dictionary<int, string>();
        foreach (var instance in instances)
        {
            var name = instance.Key;
            var repoRoot = instance["RepoRoot"];
            if (string.IsNullOrWhiteSpace(repoRoot))
                throw new InvalidOperationException($"Mcp:Instances:{name}:RepoRoot is required.");
            var resolvedRoot = ResolveFullPath(repoRoot);
            if (Path.IsPathRooted(repoRoot) && !Directory.Exists(resolvedRoot))
                throw new InvalidOperationException($"Mcp:Instances:{name}:RepoRoot '{repoRoot}' does not exist. Create the folder or update configuration.");

            var rawPort = instance["Port"];
            if (!int.TryParse(rawPort, out var port))
                throw new InvalidOperationException($"Mcp:Instances:{name}:Port must be a valid integer.");

            if (usedPorts.TryGetValue(port, out var existing))
                throw new InvalidOperationException($"Duplicate MCP instance port {port} found in instances '{existing}' and '{name}'.");

            usedPorts[port] = name;
        }
    }

    /// <summary>
    /// Validates TODO storage provider and provider-specific settings for the selected effective instance.
    /// </summary>
    /// <remarks>
    /// TR-MCP-TODO-005 (provider-agnostic): accepts only <c>database</c>. The legacy
    /// <c>sqlite</c> value is accepted and mapped to <c>database</c> with a one-time warning
    /// logged via <see cref="System.Diagnostics.Trace"/>. The removed <c>yaml</c> provider is
    /// rejected with a clear error. When provider is <c>database</c> the
    /// <c>Mcp:Database:Provider</c> setting (TR-MCP-CFG-007) must be non-empty.
    /// </remarks>
    public static void ValidateTodoStorage(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var rawProvider = (GetEffectiveMcpValue(configuration, instanceName, "TodoStorage:Provider") ?? "database")
            .Trim();
        var provider = rawProvider.ToUpperInvariant();

        if (provider == "SQLITE")
        {
            System.Diagnostics.Trace.TraceWarning(
                "Mcp:TodoStorage:Provider='sqlite' is deprecated; treating as 'database' (TODO storage now follows Mcp:Database:Provider).");
            provider = "DATABASE";
        }

        if (provider == "YAML")
            throw new InvalidOperationException(
                "Mcp:TodoStorage:Provider='yaml' has been removed; the database is the sole source of truth and " +
                "TODO.yaml is a read-only projection. Set Mcp:TodoStorage:Provider='database' and configure " +
                "Mcp:Database:Provider (TR-MCP-CFG-007).");

        if (provider != "DATABASE")
            throw new InvalidOperationException(
                $"Unsupported TODO storage provider '{rawProvider}'. Allowed value: database (legacy alias: sqlite).");

        if (provider == "DATABASE")
        {
            var dbProvider = GetEffectiveMcpValue(configuration, instanceName, "Database:Provider");
            if (string.IsNullOrWhiteSpace(dbProvider))
                throw new InvalidOperationException(
                    "Mcp:Database:Provider is required when Mcp:TodoStorage:Provider is 'database'. " +
                    "Set it to one of: sqlite, sqlserver, postgresql (TR-MCP-CFG-007).");
        }
    }

    private static void CopySection(IConfigurationSection section, string prefix, IDictionary<string, string?> target)
    {
        foreach (var child in section.GetChildren())
        {
            var path = string.IsNullOrEmpty(prefix) ? child.Key : prefix + ":" + child.Key;
            if (child.Value is not null)
                target[path] = child.Value;
            CopySection(child, path, target);
        }
    }

    private static string ResolveFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            throw new InvalidOperationException($"Invalid path '{path}'.", ex);
        }
    }

    private static string? GetArgValue(string[] args, string key)
    {
        if (args is null || args.Length == 0)
            return null;

        var prefix = $"--{key}=";
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg.Substring(prefix.Length);

            if (string.Equals(arg, $"--{key}", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }

        return null;
    }
}
