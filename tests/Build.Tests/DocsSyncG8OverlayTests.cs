using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// Overlay G8: docs, SyncAgentPlugins, ValidateTraceability, ValidateConfig, and wiki-export
/// gates against shipped Nuke targets and validators. Skipped tests are failures.
/// </summary>
public sealed class DocsSyncG8OverlayTests
{
    private static readonly Regex FrHeadingPattern = new(
        @"^##\s+(FR-[A-Z0-9]+(?:[-.–]+[A-Z0-9]+)*)\b",
        RegexOptions.Compiled);

    /// <summary>G8 / C3: Nuke ValidateTraceability invokes TraceabilityValidator on docs/Project.</summary>
    [Fact]
    public void G8_NukeValidateTraceabilityTarget_InvokesShippedValidator()
    {
        var target = typeof(Build).GetProperty("ValidateTraceability", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(target);
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.ValidateTraceability.cs"));
        Assert.Contains("TraceabilityValidator.Validate", source, StringComparison.Ordinal);
        Assert.Contains("docs", source, StringComparison.Ordinal);
        Assert.Contains("Project", source, StringComparison.Ordinal);
    }

    /// <summary>G8 / C3: shipped TraceabilityValidator passes on live docs/Project.</summary>
    [Fact]
    public void G8_ValidateTraceability_ShippedValidator_PassesOnLiveDocsProject()
    {
        var docsPath = Path.Combine(FindRepositoryRoot(), "docs", "Project");
        var result = TraceabilityValidator.Validate(
            File.ReadAllLines(Path.Combine(docsPath, "Functional-Requirements.md")),
            File.ReadAllLines(Path.Combine(docsPath, "Technical-Requirements.md")),
            File.ReadAllLines(Path.Combine(docsPath, "Testing-Requirements.md")),
            File.ReadAllLines(Path.Combine(docsPath, "TR-per-FR-Mapping.md")),
            File.ReadAllLines(Path.Combine(docsPath, "Requirements-Matrix.md")));

        Assert.False(
            result.HasFrErrors,
            "C3 FR gaps: mapping=[" + string.Join(",", result.MissingFrInMapping)
            + "] matrix=[" + string.Join(",", result.MissingFrInMatrix) + "]");

        var functionalLines = File.ReadAllLines(Path.Combine(docsPath, "Functional-Requirements.md"));
        var required = TraceabilityValidator.GetIdsFromHeadings(functionalLines, FrHeadingPattern)
            .Where(id => id.StartsWith("FR-MCP-WIKIEXPORT-", StringComparison.Ordinal)
                || id.StartsWith("FR-MCP-HYGIENE-", StringComparison.Ordinal)
                || id.StartsWith("FR-MCP-HOSTILEREVIEW-", StringComparison.Ordinal)
                || id.StartsWith("FR-MCP-PLUGININT-", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(required);
        foreach (var id in required)
        {
            Assert.DoesNotContain(id, result.MissingFrInMapping);
            Assert.DoesNotContain(id, result.MissingFrInMatrix);
        }
    }

    /// <summary>G8 / C2: Nuke ValidateConfig invokes ConfigValidator on appsettings.</summary>
    [Fact]
    public void G8_NukeValidateConfigTarget_InvokesShippedValidator()
    {
        var target = typeof(Build).GetProperty("ValidateConfig", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(target);
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.ValidateConfig.cs"));
        Assert.Contains("ConfigValidator.ParseInstances", source, StringComparison.Ordinal);
        Assert.Contains("ConfigValidator.Validate", source, StringComparison.Ordinal);
        Assert.Contains("appsettings.yaml", source, StringComparison.Ordinal);
    }

    /// <summary>G8 / C2: shipped ConfigValidator passes on live Support.Mcp appsettings.yaml.</summary>
    [Fact]
    public void G8_ValidateConfig_ShippedValidator_PassesOnLiveAppsettings()
    {
        var configPath = Path.Combine(FindRepositoryRoot(), "src", "McpServer.Support.Mcp", "appsettings.yaml");
        Assert.True(File.Exists(configPath), configPath);
        var instances = ConfigValidator.ParseInstances(File.ReadAllLines(configPath));
        Assert.NotNull(instances);
        var configDir = Path.GetDirectoryName(configPath)!;
        var errors = ConfigValidator.Validate(
            instances!,
            path => Directory.Exists(Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(configDir, path))));
        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    /// <summary>G8 / C5: Nuke SyncAgentPlugins invokes the canonical plugin-core sync script.</summary>
    [Fact]
    public void G8_NukeSyncAgentPluginsTarget_InvokesCoreSyncScript()
    {
        var target = typeof(Build).GetProperty("SyncAgentPlugins", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(target);
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.SyncAgentPlugins.cs"));
        Assert.Contains("sync-plugin-core.ps1", source, StringComparison.Ordinal);
        Assert.Contains("plugins", source, StringComparison.Ordinal);
        Assert.Contains("core", source, StringComparison.Ordinal);
    }

    /// <summary>G8 / C5: official plugin lib-ps copies match canonical plugins/core/lib-ps.</summary>
    [Fact]
    public void G8_SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore()
    {
        var repoRoot = FindRepositoryRoot();
        var canonicalDir = Path.Combine(repoRoot, "plugins", "core", "lib-ps");
        Assert.True(Directory.Exists(canonicalDir), canonicalDir);
        var githubRoot = Directory.GetParent(repoRoot)?.FullName;
        Assert.False(string.IsNullOrWhiteSpace(githubRoot));
        var officialPlugins = new[]
        {
            "mcpserver-codex-plugin",
            "mcpserver-claude-code-plugin",
            "mcpserver-copilot-plugin",
            "mcpserver-cline-plugin",
            "mcpserver-grok-plugin",
        };

        var mismatches = new List<string>();
        foreach (var plugin in officialPlugins)
        {
            var pluginRoot = Path.Combine(githubRoot!, plugin);
            if (!Directory.Exists(pluginRoot))
            {
                mismatches.Add(plugin + ": plugin root missing");
                continue;
            }

            foreach (var canonicalFile in Directory.GetFiles(canonicalDir, "*.ps1", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(canonicalFile);
                var candidates = new[]
                {
                    Path.Combine(pluginRoot, "lib", name),
                    Path.Combine(pluginRoot, "lib-ps", name),
                };
                var copy = candidates.FirstOrDefault(File.Exists);
                if (copy is null)
                {
                    mismatches.Add(plugin + ": missing " + name);
                    continue;
                }

                if (!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(File.ReadAllBytes(canonicalFile)),
                    SHA256.HashData(File.ReadAllBytes(copy))))
                {
                    mismatches.Add(plugin + ": checksum drift " + name);
                }
            }
        }

        Assert.True(mismatches.Count == 0, string.Join("; ", mismatches));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
