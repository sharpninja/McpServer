using System.Text.RegularExpressions;

/// <summary>
/// Validates MCP appsettings instance configuration (YAML format).
/// Ported from scripts/Validate-McpConfig.ps1.
/// </summary>
static partial class ConfigValidator
{
    [GeneratedRegex(@"^Mcp:\s*$")]
    private static partial Regex McpSectionRegex();

    [GeneratedRegex(@"^  Instances:\s*$")]
    private static partial Regex InstancesSectionRegex();

    [GeneratedRegex(@"^    ([A-Za-z0-9_][A-Za-z0-9_\-]*):\s*$")]
    private static partial Regex InstanceNameRegex();

    [GeneratedRegex(@"^      RepoRoot:\s*(.+)$")]
    private static partial Regex RepoRootRegex();

    [GeneratedRegex(@"^      Port:\s*(.+)$")]
    private static partial Regex PortRegex();

    [GeneratedRegex(@"^      TodoStorage:\s*$")]
    private static partial Regex TodoStorageSectionRegex();

    [GeneratedRegex(@"^        Provider:\s*(.+)$")]
    private static partial Regex ProviderRegex();

    [GeneratedRegex(@"^        SqliteDataSource:\s*(.+)$")]
    private static partial Regex SqliteDataSourceRegex();

    /// <summary>Represents a parsed MCP instance from YAML.</summary>
    public sealed class InstanceConfig
    {
        public string? RepoRoot { get; set; }
        public int? Port { get; set; }
        public string? TodoProvider { get; set; }
        public string? SqliteDataSource { get; set; }
    }

    /// <summary>
    /// Parses MCP instance configurations from YAML content lines.
    /// Returns null if no Mcp section is found.
    /// </summary>
    public static Dictionary<string, InstanceConfig>? ParseInstances(string[] lines)
    {
        var hasMcp = false;
        var instances = new Dictionary<string, InstanceConfig>(StringComparer.OrdinalIgnoreCase);
        var inInstances = false;
        string? currentInstance = null;
        var inTodoStorage = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            if (McpSectionRegex().IsMatch(line))
            {
                hasMcp = true;
                continue;
            }

            if (!hasMcp) continue;

            if (InstancesSectionRegex().IsMatch(line))
            {
                inInstances = true;
                currentInstance = null;
                inTodoStorage = false;
                continue;
            }

            if (!inInstances) continue;

            // A sibling key under Mcp ends the Instances block
            if (Regex.IsMatch(line, @"^  [A-Za-z0-9_][A-Za-z0-9_\-]*:\s*$") && !InstancesSectionRegex().IsMatch(line))
                break;

            var instanceMatch = InstanceNameRegex().Match(line);
            if (instanceMatch.Success)
            {
                currentInstance = instanceMatch.Groups[1].Value;
                instances[currentInstance] = new InstanceConfig();
                inTodoStorage = false;
                continue;
            }

            if (currentInstance is null) continue;

            if (TodoStorageSectionRegex().IsMatch(line))
            {
                inTodoStorage = true;
                continue;
            }

            var repoRootMatch = RepoRootRegex().Match(line);
            if (repoRootMatch.Success)
            {
                instances[currentInstance].RepoRoot = UnquoteScalar(repoRootMatch.Groups[1].Value);
                inTodoStorage = false;
                continue;
            }

            var portMatch = PortRegex().Match(line);
            if (portMatch.Success)
            {
                if (int.TryParse(UnquoteScalar(portMatch.Groups[1].Value), out var port))
                    instances[currentInstance].Port = port;
                inTodoStorage = false;
                continue;
            }

            if (inTodoStorage)
            {
                var providerMatch = ProviderRegex().Match(line);
                if (providerMatch.Success)
                {
                    instances[currentInstance].TodoProvider = UnquoteScalar(providerMatch.Groups[1].Value);
                    continue;
                }

                var sqliteMatch = SqliteDataSourceRegex().Match(line);
                if (sqliteMatch.Success)
                {
                    instances[currentInstance].SqliteDataSource = UnquoteScalar(sqliteMatch.Groups[1].Value);
                }
            }
        }

        return hasMcp ? instances : null;
    }

    /// <summary>
    /// Validates parsed instances for port conflicts, missing required fields, and valid providers.
    /// Returns a list of validation error messages. Empty list means valid.
    /// </summary>
    public static List<string> Validate(Dictionary<string, InstanceConfig> instances, Func<string, bool>? directoryExists = null)
    {
        var errors = new List<string>();
        var ports = new Dictionary<int, string>();
        directoryExists ??= Directory.Exists;

        foreach (var (name, instance) in instances)
        {
            if (string.IsNullOrWhiteSpace(instance.RepoRoot))
            {
                errors.Add($"Instance '{name}' missing RepoRoot.");
                continue;
            }

            if (!directoryExists(instance.RepoRoot))
                errors.Add($"Instance '{name}' RepoRoot does not exist: '{instance.RepoRoot}'.");

            if (instance.Port is null or <= 0)
            {
                errors.Add($"Instance '{name}' has invalid port.");
                continue;
            }

            if (ports.TryGetValue(instance.Port.Value, out var existing))
                errors.Add($"Duplicate port '{instance.Port}' in instances '{existing}' and '{name}'.");
            else
                ports[instance.Port.Value] = name;

            var provider = (instance.TodoProvider ?? "yaml").Trim().ToLowerInvariant();
            if (provider is not "yaml" and not "sqlite")
            {
                errors.Add($"Instance '{name}' has unsupported TodoStorage provider '{provider}'. Allowed: yaml, sqlite.");
                continue;
            }

            if (provider == "sqlite" && string.IsNullOrWhiteSpace(instance.SqliteDataSource))
                errors.Add($"Instance '{name}' provider sqlite requires TodoStorage.SqliteDataSource.");
        }

        return errors;
    }

    private static string UnquoteScalar(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed[0] == '\'' && trimmed[^1] == '\'') ||
             (trimmed[0] == '"' && trimmed[^1] == '"')))
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }
}
