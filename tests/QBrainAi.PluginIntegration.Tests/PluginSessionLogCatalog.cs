using System.Text.Json;
using System.Text.Json.Serialization;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// FR-MCP-PLUGININT-001: catalog load and validation for the eight plugin Session Log scenarios.
/// </summary>
public static class PluginSessionLogCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
    };

    /// <summary>Primary clone root when <paramref name="repositoryRoot"/> is a worktree.</summary>
    /// <param name="repositoryRoot">QBrainAi checkout, including a worktree.</param>
    /// <returns>The main repository directory.</returns>
    public static string PrimaryRepositoryRoot(string repositoryRoot)
    {
        var parent = SiblingPluginParent(repositoryRoot);
        var primary = Path.Combine(parent, "QBrainAi");
        return Directory.Exists(primary) && File.Exists(Path.Combine(primary, "QBrainAi.sln"))
            ? primary
            : repositoryRoot;
    }

    /// <summary>
    /// Parent directory that holds sibling plugin repositories, even when
    /// <paramref name="repositoryRoot"/> is a git worktree.
    /// </summary>
    /// <param name="repositoryRoot">QBrainAi checkout, including a worktree.</param>
    /// <returns>The directory that contains mcpserver-*-plugin repositories.</returns>
    public static string SiblingPluginParent(string repositoryRoot)
    {
        var primary = repositoryRoot;
        var gitFile = Path.Combine(repositoryRoot, ".git");
        if (File.Exists(gitFile))
        {
            var gitDirLine = File.ReadLines(gitFile).FirstOrDefault(line =>
                line.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase));
            if (gitDirLine is not null)
            {
                var gitDir = gitDirLine["gitdir:".Length..].Trim();
                if (!Path.IsPathRooted(gitDir))
                    gitDir = Path.GetFullPath(Path.Combine(repositoryRoot, gitDir));
                var main = Directory.GetParent(gitDir)?.Parent?.Parent;
                if (main is not null && File.Exists(Path.Combine(main.FullName, "QBrainAi.sln")))
                    primary = main.FullName;
            }
        }

        return Directory.GetParent(primary)?.FullName
            ?? throw new InvalidOperationException("Cannot resolve sibling plugin parent directory.");
    }

    /// <summary>
    /// Loads plugin-sessionlog-scenarios.json and validates TR-MCP-PLUGININT-001 AC1 fields.
    /// </summary>
    /// <param name="repositoryRoot">QBrainAi repository root.</param>
    /// <returns>Enabled catalog rows.</returns>
    public static IReadOnlyList<PluginSessionLogScenario> LoadAndValidate(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var catalogPath = Path.Combine(repositoryRoot, "tests", "QBrainAi.PluginIntegration.Tests", "scenarios", "plugin-sessionlog-scenarios.json");
        if (!File.Exists(catalogPath))
        {
            throw new FileNotFoundException("plugin-sessionlog-scenarios.json is missing.", catalogPath);
        }

        var document = JsonSerializer.Deserialize<CatalogFile>(File.ReadAllText(catalogPath), JsonOptions)
            ?? throw new InvalidOperationException("Catalog JSON deserialized to null.");
        if (document.Scenarios is null || document.Scenarios.Count == 0)
        {
            throw new InvalidOperationException("Catalog scenarios array is missing or empty.");
        }

        var parent = SiblingPluginParent(repositoryRoot);

        var enabled = new List<PluginSessionLogScenario>();
        foreach (var row in document.Scenarios)
        {
            if (row is null || !row.Enabled)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Name) ||
                string.IsNullOrWhiteSpace(row.AgentSourceType) ||
                string.IsNullOrWhiteSpace(row.RepositoryName) ||
                string.IsNullOrWhiteSpace(row.CacheFolder) ||
                string.IsNullOrWhiteSpace(row.Entrypoint) ||
                row.RequiredEnvironmentVariables is null ||
                row.RequiredEnvironmentVariables.Count == 0 ||
                !Enum.TryParse<PluginHostKind>(row.HostKind, ignoreCase: true, out var hostKind))
            {
                throw new InvalidOperationException("Catalog row is missing TR-MCP-PLUGININT-001 AC1 fields: " + row?.Name);
            }

            if (row.Entrypoint.Contains("..", StringComparison.Ordinal) ||
                Path.IsPathRooted(row.Entrypoint))
            {
                throw new InvalidOperationException("Catalog entrypoint must be a repository-relative path: " + row.Name);
            }

            var pluginRoot = Path.Combine(parent, row.RepositoryName);
            if (!Directory.Exists(pluginRoot))
            {
                throw new InvalidOperationException("Plugin repository root is missing: " + row.RepositoryName);
            }

            var entrypointPath = Path.Combine(pluginRoot, row.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(entrypointPath))
            {
                throw new InvalidOperationException("Plugin entrypoint is missing: " + row.Name + " " + row.Entrypoint);
            }

            var versionPresent = File.Exists(Path.Combine(pluginRoot, ".version")) ||
                File.Exists(Path.Combine(pluginRoot, "package.json"));
            if (!versionPresent)
            {
                throw new InvalidOperationException("Plugin version metadata is missing: " + row.Name);
            }

            enabled.Add(new PluginSessionLogScenario
            {
                Name = row.Name,
                HostKind = hostKind,
                AgentSourceType = row.AgentSourceType,
                RepositoryName = row.RepositoryName,
                CacheFolder = row.CacheFolder,
                Entrypoint = row.Entrypoint,
                RequiredEnvironmentVariables = row.RequiredEnvironmentVariables,
                Enabled = true,
            });
        }

        if (enabled.Count != 8)
        {
            throw new InvalidOperationException("Catalog must have exactly eight enabled scenarios, found " + enabled.Count + ".");
        }

        var uniqueKeys = enabled.Select(row => row.AgentSourceType + ":" + row.CacheFolder).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (uniqueKeys != enabled.Count)
        {
            throw new InvalidOperationException("Catalog agent-source plus cache-folder identities are not unique.");
        }

        return enabled;
    }

    private sealed class CatalogFile
    {
        [JsonPropertyName("scenarios")]
        public List<CatalogRow>? Scenarios { get; set; }
    }

    private sealed class CatalogRow
    {
        public string? Name { get; set; }

        public string? HostKind { get; set; }

        public string? AgentSourceType { get; set; }

        public string? RepositoryName { get; set; }

        public string? CacheFolder { get; set; }

        public string? Entrypoint { get; set; }

        public List<string>? RequiredEnvironmentVariables { get; set; }

        public bool Enabled { get; set; }
    }
}
