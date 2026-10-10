using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC5: parsed P19 native-suite receipt under docs/receipts/pluginint-p19-*.
/// </summary>
public sealed class PluginNativeSuiteReceipt
{
    private static readonly Regex StampPattern = new(@"^pluginint-p19-\d{8}T\d{6}Z$", RegexOptions.CultureInvariant);
    private static readonly Regex ShaPattern = new("^[0-9a-f]{40}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> AllowedSuites = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pester",
        "Bats",
        "Jest",
        "xUnit",
    };

    /// <summary>UTC stamp from the receipt directory name.</summary>
    public required string Stamp { get; init; }

    /// <summary>Absolute receipt directory.</summary>
    public required string DirectoryPath { get; init; }

    /// <summary>Native suite rows before SyncAgentPlugins.</summary>
    public required IReadOnlyList<PluginNativeSuiteRow> Plugins { get; init; }

    /// <summary>Native suite rows after SyncAgentPlugins.</summary>
    public required IReadOnlyList<PluginNativeSuiteRow> AfterSync { get; init; }

    /// <summary>Recorded git branch.</summary>
    public required string Branch { get; init; }

    /// <summary>Recorded 40-character git SHA.</summary>
    public required string Sha { get; init; }

    /// <summary>Count of unrelated commits recorded in the receipt.</summary>
    public required int UnrelatedCommitCount { get; init; }

    /// <summary>
    /// Loads the newest docs/receipts/pluginint-p19-* summary and validates P19 AC fields.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    /// <returns>Validated receipt.</returns>
    public static PluginNativeSuiteReceipt LoadLatest(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var receiptsRoot = Path.Combine(repositoryRoot, "docs", "receipts");
        if (!Directory.Exists(receiptsRoot))
        {
            throw new DirectoryNotFoundException("docs/receipts is missing.");
        }

        var directories = Directory.GetDirectories(receiptsRoot, "pluginint-p19-*")
            .Select(path => new DirectoryInfo(path))
            .Where(info => StampPattern.IsMatch(info.Name))
            .OrderByDescending(info => info.Name, StringComparer.Ordinal)
            .ToList();
        if (directories.Count == 0)
        {
            throw new FileNotFoundException("No docs/receipts/pluginint-p19-<utc> receipt directory exists.");
        }

        var directory = directories[0];
        var summaryPath = Path.Combine(directory.FullName, "summary.json");
        if (!File.Exists(summaryPath))
        {
            throw new FileNotFoundException("P19 summary.json is missing.", summaryPath);
        }

        var document = JsonSerializer.Deserialize<SummaryFile>(File.ReadAllText(summaryPath), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("P19 summary.json deserialized to null.");

        var plugins = ValidateRows(document.Plugins, "plugins");
        var afterSync = ValidateRows(document.AfterSync, "afterSync");
        if (string.IsNullOrWhiteSpace(document.Branch))
        {
            throw new InvalidOperationException("P19 receipt is missing git.branch.");
        }

        if (string.IsNullOrWhiteSpace(document.Sha) || !ShaPattern.IsMatch(document.Sha))
        {
            throw new InvalidOperationException("P19 receipt git.sha must be a 40-character lowercase hex SHA.");
        }

        return new PluginNativeSuiteReceipt
        {
            Stamp = directory.Name["pluginint-p19-".Length..],
            DirectoryPath = directory.FullName,
            Plugins = plugins,
            AfterSync = afterSync,
            Branch = document.Branch,
            Sha = document.Sha.ToLowerInvariant(),
            UnrelatedCommitCount = document.UnrelatedCommitCount,
        };
    }

    private static IReadOnlyList<PluginNativeSuiteRow> ValidateRows(List<PluginNativeSuiteRow>? rows, string label)
    {
        if (rows is null || rows.Count == 0)
        {
            throw new InvalidOperationException("P19 receipt " + label + " is missing.");
        }

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.RepositoryName)
                || string.IsNullOrWhiteSpace(row.NativeSuite)
                || !AllowedSuites.Contains(row.NativeSuite)
                || string.IsNullOrWhiteSpace(row.LogFile))
            {
                throw new InvalidOperationException("P19 receipt " + label + " row is incomplete: " + row.RepositoryName);
            }
        }

        return rows;
    }

    private sealed class SummaryFile
    {
        public List<PluginNativeSuiteRow>? Plugins { get; set; }

        public List<PluginNativeSuiteRow>? AfterSync { get; set; }

        public string? Branch { get; set; }

        public string? Sha { get; set; }

        [JsonPropertyName("unrelatedCommitCount")]
        public int UnrelatedCommitCount { get; set; }
    }
}

/// <summary>
/// TEST-MCP-PLUGININT-001 AC5: one plugin native-suite row inside a P19 receipt.
/// </summary>
public sealed class PluginNativeSuiteRow
{
    /// <summary>Sibling plugin repository folder name.</summary>
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>Native suite kind: Pester, Bats, Jest, or xUnit.</summary>
    public string NativeSuite { get; set; } = string.Empty;

    /// <summary>Failed test count parsed from the native suite log.</summary>
    public int Failed { get; set; } = -1;

    /// <summary>Skipped test count parsed from the native suite log.</summary>
    public int Skipped { get; set; } = -1;

    /// <summary>Receipt-relative log path.</summary>
    public string LogFile { get; set; } = string.Empty;
}
