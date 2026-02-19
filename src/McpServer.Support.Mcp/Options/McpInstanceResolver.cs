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
    /// Resolves a configured sqlite datasource against Mcp:DataDirectory when the datasource is relative.
    /// Instance-scoped overrides are honored.
    /// </summary>
    public static string ResolveSqliteDataSource(IConfiguration configuration, string? instanceName)
    {
        var dataSource = GetEffectiveMcpValue(configuration, instanceName, "DataSource") ?? "mcp.db";
        if (Path.IsPathRooted(dataSource))
            return dataSource;

        var dataDirectory = GetEffectiveMcpValue(configuration, instanceName, "DataDirectory") ?? ".";
        return Path.GetFullPath(Path.Combine(dataDirectory, dataSource));
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

            var rawPort = instance["Port"];
            if (!int.TryParse(rawPort, out var port))
                throw new InvalidOperationException($"Mcp:Instances:{name}:Port must be a valid integer.");

            if (usedPorts.TryGetValue(port, out var existing))
                throw new InvalidOperationException($"Duplicate MCP instance port {port} found in instances '{existing}' and '{name}'.");

            usedPorts[port] = name;
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
