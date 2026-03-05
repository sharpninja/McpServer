namespace McpServer.Support.Mcp.Options;

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

        var fromEnv = Environment.GetEnvironmentVariable("MCP_INSTANCE");
        return string.IsNullOrWhiteSpace(fromEnv) ? null : fromEnv.Trim();
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
            var instanceValue = configuration[$"Mcp:Instances:{instanceName}:{key}"];
            if (!string.IsNullOrWhiteSpace(instanceValue))
                return instanceValue;
        }

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

        var instances = configuration.GetSection("Mcp:Instances").GetChildren().ToList();
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
    public static void ValidateTodoStorage(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = (GetEffectiveMcpValue(configuration, instanceName, "TodoStorage:Provider") ?? "yaml")
            .Trim()
            .ToUpperInvariant();
        if (provider is not ("YAML" or "SQLITE"))
            throw new InvalidOperationException($"Unsupported TODO storage provider '{provider}'. Allowed values: yaml, sqlite.");

        if (provider == "SQLITE")
        {
            var sqliteDataSource = GetEffectiveMcpValue(configuration, instanceName, "TodoStorage:SqliteDataSource");
            if (string.IsNullOrWhiteSpace(sqliteDataSource))
                throw new InvalidOperationException("Mcp:TodoStorage:SqliteDataSource is required when TodoStorage:Provider is sqlite.");
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
