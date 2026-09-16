using System.Security.Cryptography;

namespace NukeBuild.Tests;

/// <summary>
/// FR-MCP-PLUGINCORE-004 AC3: canonical plugin-core lib-ps files must match official plugin copies.
/// </summary>
public sealed class SyncAgentPluginsChecksumTests
{
    /// <summary>
    /// Official plugin lib-ps copies of canonical core files share SHA256 with plugins/core/lib-ps.
    /// </summary>
    [Fact]
    public void SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore()
    {
        var repoRoot = FindRepositoryRoot();
        var canonicalDir = Path.Combine(repoRoot, "plugins", "core", "lib-ps");
        Assert.True(Directory.Exists(canonicalDir), "canonical plugins/core/lib-ps is missing");

        var githubRoot = Directory.GetParent(repoRoot)?.FullName;
        Assert.False(string.IsNullOrWhiteSpace(githubRoot));
        var officialPlugins = new[]
        {
            "mcpserver-codex-plugin",
            "mcpserver-claude-code-plugin",
            "mcpserver-copilot-plugin",
            "mcpserver-cline-plugin",
            "mcpserver-grok-plugin"
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
                    Path.Combine(pluginRoot, "lib-ps", name)
                };
                var copy = candidates.FirstOrDefault(File.Exists);
                if (copy is null)
                {
                    mismatches.Add(plugin + ": missing " + name);
                    continue;
                }

                if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(File.ReadAllBytes(canonicalFile)), SHA256.HashData(File.ReadAllBytes(copy))))
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
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
