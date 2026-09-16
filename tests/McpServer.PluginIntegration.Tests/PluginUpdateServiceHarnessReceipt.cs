using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P20: parsed harness receipt under docs/receipts/pluginint-p20-*.
/// Maps TEST-MCP-PLUGININT-001 AC5 deploy half. Fail until a Development UpdateService receipt exists.
/// </summary>
public sealed class PluginUpdateServiceHarnessReceipt
{
    private static readonly Regex StampPattern = new(@"^pluginint-p20-\d{8}T\d{6}Z$", RegexOptions.CultureInvariant);

    /// <summary>UTC stamp from the receipt directory name.</summary>
    public required string Stamp { get; init; }

    /// <summary>Absolute receipt directory.</summary>
    public required string DirectoryPath { get; init; }

    /// <summary>Deploy environment. P20 green is Development only.</summary>
    public required string Environment { get; init; }

    /// <summary>Deploy target name. Must be UpdateService.</summary>
    public required string DeployTarget { get; init; }

    /// <summary>Windows service name recorded by the harness.</summary>
    public required string ServiceName { get; init; }

    /// <summary>True when fixtures omit live secrets and operator identities.</summary>
    public required bool SanitizedFixtures { get; init; }

    /// <summary>Failed test count from the harness log.</summary>
    public required int Failed { get; init; }

    /// <summary>Skipped test count from the harness log.</summary>
    public required int Skipped { get; init; }

    /// <summary>Receipt-relative harness log path.</summary>
    public required string LogFile { get; init; }

    /// <summary>Git branch recorded when the harness ran.</summary>
    public required string Branch { get; init; }

    /// <summary>40-character git SHA of the tree bound to this receipt.</summary>
    public required string Sha { get; init; }

    /// <summary>
    /// Loads the newest docs/receipts/pluginint-p20-* summary and validates P20 harness fields.
    /// </summary>
    /// <param name="repositoryRoot">McpServer repository root.</param>
    /// <returns>Validated receipt.</returns>
    public static PluginUpdateServiceHarnessReceipt LoadLatest(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var receiptsRoot = Path.Combine(repositoryRoot, "docs", "receipts");
        if (!Directory.Exists(receiptsRoot))
        {
            throw new DirectoryNotFoundException("docs/receipts is missing.");
        }

        var directories = Directory.GetDirectories(receiptsRoot, "pluginint-p20-*")
            .Select(path => new DirectoryInfo(path))
            .Where(info => StampPattern.IsMatch(info.Name))
            .OrderByDescending(info => info.Name, StringComparer.Ordinal)
            .ToList();
        if (directories.Count == 0)
        {
            throw new FileNotFoundException("No docs/receipts/pluginint-p20-<utc> receipt directory exists.");
        }

        var directory = directories[0];
        var summaryPath = Path.Combine(directory.FullName, "summary.json");
        if (!File.Exists(summaryPath))
        {
            throw new FileNotFoundException("P20 summary.json is missing.", summaryPath);
        }

        var document = JsonSerializer.Deserialize<SummaryFile>(File.ReadAllText(summaryPath), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("P20 summary.json deserialized to null.");

        if (!string.Equals(document.Environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("P20 harness environment must be Development.");
        }

        if (!string.Equals(document.DeployTarget, "UpdateService", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("P20 harness deployTarget must be UpdateService.");
        }

        if (!document.SanitizedFixtures)
        {
            throw new InvalidOperationException("P20 harness must record sanitizedFixtures true.");
        }

        if (string.IsNullOrWhiteSpace(document.ServiceName) || string.IsNullOrWhiteSpace(document.LogFile))
        {
            throw new InvalidOperationException("P20 harness receipt is missing serviceName or logFile.");
        }

        if (string.IsNullOrWhiteSpace(document.Branch)
            || string.IsNullOrWhiteSpace(document.Sha)
            || document.Sha.Length != 40)
        {
            throw new InvalidOperationException("P20 harness receipt is missing branch or 40-character sha.");
        }

        return new PluginUpdateServiceHarnessReceipt
        {
            Stamp = directory.Name["pluginint-p20-".Length..],
            DirectoryPath = directory.FullName,
            Environment = "Development",
            DeployTarget = "UpdateService",
            ServiceName = document.ServiceName,
            SanitizedFixtures = document.SanitizedFixtures,
            Failed = document.Failed,
            Skipped = document.Skipped,
            LogFile = document.LogFile,
            Branch = document.Branch,
            Sha = document.Sha.ToLowerInvariant(),
        };
    }

    private sealed class SummaryFile
    {
        public string? Environment { get; set; }

        public string? DeployTarget { get; set; }

        public string? ServiceName { get; set; }

        public bool SanitizedFixtures { get; set; }

        public int Failed { get; set; } = -1;

        public int Skipped { get; set; } = -1;

        public string? LogFile { get; set; }

        public string? Branch { get; set; }

        public string? Sha { get; set; }
    }
}
