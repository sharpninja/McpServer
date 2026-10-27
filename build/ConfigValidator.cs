using System.Text.RegularExpressions;

/// <summary>
/// Validates MCP appsettings instance configuration (YAML format).
/// Ported from scripts/Validate-McpConfig.ps1.
/// </summary>
static partial class ConfigValidator
{
    /// <summary>TR-MCP-NUKE-008: the configuration root, canonical <c>QBrainAi:</c> or legacy <c>Mcp:</c>.</summary>
    [GeneratedRegex(@"^(QBrainAi|Mcp):\s*$")]
    private static partial Regex McpSectionRegex();

    /// <summary>TR-MCP-NUKE-008 AC-003: any other top-level key ends the current root.</summary>
    [GeneratedRegex(@"^[A-Za-z0-9_][A-Za-z0-9_\-]*:")]
    private static partial Regex TopLevelKeyRegex();

    private const string CanonicalRoot = "QBrainAi";
    private const string LegacyRoot = "Mcp";

    [GeneratedRegex(@"^  Instances:\s*$")]
    private static partial Regex InstancesSectionRegex();

    [GeneratedRegex(@"^    ([A-Za-z0-9_][A-Za-z0-9_\-]*):\s*$")]
    private static partial Regex InstanceNameRegex();

    [GeneratedRegex(@"^      RepoRoot:\s*(.*)$")]
    private static partial Regex RepoRootRegex();

    [GeneratedRegex(@"^      Port:\s*(.*)$")]
    private static partial Regex PortRegex();

    [GeneratedRegex(@"^      TodoStorage:\s*$")]
    private static partial Regex TodoStorageSectionRegex();

    [GeneratedRegex(@"^        Provider:\s*(.*)$")]
    private static partial Regex ProviderRegex();

    [GeneratedRegex(@"^        SqliteDataSource:\s*(.*)$")]
    private static partial Regex SqliteDataSourceRegex();

    /// <summary>Represents a parsed MCP instance from YAML.</summary>
    public sealed class InstanceConfig
    {
        /// <summary>TR-MCP-NUKE-008 AC-003: distinguishes absent keys from explicitly empty or invalid values.</summary>
        internal HashSet<string> ParsedFields { get; } = new(StringComparer.Ordinal);

        public string? RepoRoot { get; set; }
        public int? Port { get; set; }
        public string? TodoProvider { get; set; }
        public string? SqliteDataSource { get; set; }
    }

    /// <summary>
    /// Parses MCP instance configurations from YAML content lines.
    /// Returns null if neither the QBrainAi nor the legacy Mcp section is found.
    /// TR-MCP-NUKE-008 AC-003: each root is parsed on its own and the results are merged per key with the
    /// canonical QBrainAi root winning, whatever the document order, as CanonicalSectionProjectionProvider does
    /// within one file layer.
    /// </summary>
    public static Dictionary<string, InstanceConfig>? ParseInstances(string[] lines)
    {
        var roots = new Dictionary<string, Dictionary<string, InstanceConfig>>(StringComparer.Ordinal);
        Dictionary<string, InstanceConfig>? instances = null;
        var inInstances = false;
        string? currentInstance = null;
        var inTodoStorage = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var rootMatch = McpSectionRegex().Match(line);
            if (rootMatch.Success || TopLevelKeyRegex().IsMatch(line))
            {
                instances = rootMatch.Success
                    ? roots.TryGetValue(rootMatch.Groups[1].Value, out var existing)
                        ? existing
                        : roots[rootMatch.Groups[1].Value] = new Dictionary<string, InstanceConfig>(StringComparer.OrdinalIgnoreCase)
                    : null;
                inInstances = false;
                currentInstance = null;
                inTodoStorage = false;
                continue;
            }

            if (instances is null) continue;

            if (InstancesSectionRegex().IsMatch(line))
            {
                inInstances = true;
                currentInstance = null;
                inTodoStorage = false;
                continue;
            }

            if (!inInstances) continue;

            // A sibling key under the root ends its Instances block; the other root may still follow.
            if (Regex.IsMatch(line, @"^  [A-Za-z0-9_][A-Za-z0-9_\-]*:\s*$"))
            {
                inInstances = false;
                currentInstance = null;
                inTodoStorage = false;
                continue;
            }

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
                instances[currentInstance].ParsedFields.Add(nameof(InstanceConfig.RepoRoot));
                instances[currentInstance].RepoRoot = UnquoteScalar(repoRootMatch.Groups[1].Value);
                inTodoStorage = false;
                continue;
            }

            var portMatch = PortRegex().Match(line);
            if (portMatch.Success)
            {
                instances[currentInstance].ParsedFields.Add(nameof(InstanceConfig.Port));
                instances[currentInstance].Port = int.TryParse(UnquoteScalar(portMatch.Groups[1].Value), out var port)
                    ? port : null;
                inTodoStorage = false;
                continue;
            }

            if (inTodoStorage)
            {
                var providerMatch = ProviderRegex().Match(line);
                if (providerMatch.Success)
                {
                    instances[currentInstance].ParsedFields.Add(nameof(InstanceConfig.TodoProvider));
                    instances[currentInstance].TodoProvider = UnquoteScalar(providerMatch.Groups[1].Value);
                    continue;
                }

                var sqliteMatch = SqliteDataSourceRegex().Match(line);
                if (sqliteMatch.Success)
                {
                    instances[currentInstance].ParsedFields.Add(nameof(InstanceConfig.SqliteDataSource));
                    instances[currentInstance].SqliteDataSource = UnquoteScalar(sqliteMatch.Groups[1].Value);
                }
            }
        }

        if (roots.Count == 0)
            return null;

        roots.TryGetValue(LegacyRoot, out var legacy);
        roots.TryGetValue(CanonicalRoot, out var canonical);
        return MergeCanonicalOverLegacy(legacy, canonical);
    }

    /// <summary>
    /// TR-MCP-NUKE-008 AC-003: merges the legacy and canonical instance maps per key; a value under the canonical
    /// root wins, and instances or keys only under the legacy root are kept.
    /// </summary>
    private static Dictionary<string, InstanceConfig> MergeCanonicalOverLegacy(
        Dictionary<string, InstanceConfig>? legacy,
        Dictionary<string, InstanceConfig>? canonical)
    {
        var merged = new Dictionary<string, InstanceConfig>(StringComparer.OrdinalIgnoreCase);
        if (legacy is not null)
        {
            foreach (var (name, instance) in legacy)
            {
                merged[name] = new InstanceConfig
                {
                    RepoRoot = instance.RepoRoot,
                    Port = instance.Port,
                    TodoProvider = instance.TodoProvider,
                    SqliteDataSource = instance.SqliteDataSource,
                };
            }
        }

        if (canonical is not null)
        {
            foreach (var (name, instance) in canonical)
            {
                if (!merged.TryGetValue(name, out var target))
                    merged[name] = target = new InstanceConfig();

                if (instance.ParsedFields.Contains(nameof(InstanceConfig.RepoRoot)))
                    target.RepoRoot = instance.RepoRoot;
                if (instance.ParsedFields.Contains(nameof(InstanceConfig.Port)))
                    target.Port = instance.Port;
                if (instance.ParsedFields.Contains(nameof(InstanceConfig.TodoProvider)))
                    target.TodoProvider = instance.TodoProvider;
                if (instance.ParsedFields.Contains(nameof(InstanceConfig.SqliteDataSource)))
                    target.SqliteDataSource = instance.SqliteDataSource;
            }
        }

        return merged;
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

            var provider = (instance.TodoProvider ?? "database").Trim().ToLowerInvariant();
            if (provider == "yaml")
            {
                errors.Add($"Instance '{name}' uses the removed TodoStorage provider 'yaml'. The database is the sole source of truth; use 'database'.");
                continue;
            }

            if (provider is not "database" and not "sqlite")
            {
                errors.Add($"Instance '{name}' has unsupported TodoStorage provider '{provider}'. Allowed: database (legacy alias: sqlite).");
                continue;
            }
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
