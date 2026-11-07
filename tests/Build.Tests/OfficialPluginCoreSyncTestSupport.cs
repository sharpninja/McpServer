using System.Diagnostics;
using System.Security.Cryptography;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001: provides AssertCanonicalCorePropagatesAsync, which checks that
/// build/Build.SyncAgentPlugins.cs names the five official plugin repositories and syncs each plugin root, then runs
/// plugins/core/sync/sync-plugin-core.ps1 into a temporary plugin root through BoundedProcess (the only child-process
/// route TR-MCP-NUKE-009 AC-006 allows, bounded at five minutes) and asserts exit code 0 and a matching SHA256 for
/// every top-level *.ps1 file of plugins/core/lib-ps; its only callers are the two Category=Integration checksum cases
/// of NukeBuild.IntegrationTests (SyncAgentPluginsChecksumTests and DocsSyncG8OverlayTests), which compile this file by
/// link. Data: the repository checkout passed as repoRoot and a temporary plugin root under the system temp folder,
/// removed in a finally block.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-NUKE-009 AC-006; FR-MCP-BUILD-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the code is unchanged.</remarks>
internal static class OfficialPluginCoreSyncTestSupport
{
    private static readonly string[] s_officialPlugins =
    [
        "mcpserver-codex-plugin",
        "mcpserver-claude-code-plugin",
        "mcpserver-copilot-plugin",
        "mcpserver-cline-plugin",
        "mcpserver-grok-plugin",
    ];

    internal static async Task AssertCanonicalCorePropagatesAsync(
        string repoRoot,
        CancellationToken cancellationToken)
    {
        var canonicalDir = Path.Combine(repoRoot, "plugins", "core", "lib-ps");
        Assert.True(Directory.Exists(canonicalDir), $"Canonical plugin core is missing: {canonicalDir}");

        var buildSource = await File.ReadAllTextAsync(
            Path.Combine(repoRoot, "build", "Build.SyncAgentPlugins.cs"),
            cancellationToken).ConfigureAwait(true);
        foreach (var plugin in s_officialPlugins)
            Assert.Contains($"\"{plugin}\"", buildSource, StringComparison.Ordinal);
        Assert.Contains(
            "foreach (var pluginRoot in pluginRoots)",
            buildSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "SyncPluginCorePackage(RootDirectory, pluginRoot, syncScript, wrapperScript",
            buildSource,
            StringComparison.Ordinal);

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "mcpserver-plugin-core-sync-tests",
            Guid.NewGuid().ToString("N"));
        var generatedPlugin = Path.Combine(temporaryRoot, s_officialPlugins[0]);
        Directory.CreateDirectory(generatedPlugin);

        try
        {
            var syncScript = Path.Combine(
                repoRoot,
                "plugins",
                "core",
                "sync",
                "sync-plugin-core.ps1");
            var startInfo = new ProcessStartInfo("pwsh")
            {
                WorkingDirectory = repoRoot,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(syncScript);
            startInfo.ArgumentList.Add("-PluginRoot");
            startInfo.ArgumentList.Add(generatedPlugin);

            // TR-MCP-NUKE-009 AC-006: read child output and wait for the child only through BoundedProcess.
            var result = await BoundedProcess.RunAsync(startInfo, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(true);

            Assert.True(
                result.ExitCode == 0,
                $"Plugin-core sync exited {result.ExitCode}.{Environment.NewLine}{result.Stdout}{result.Stderr}");

            var generatedLib = Path.Combine(generatedPlugin, "lib");
            var mismatches = new List<string>();
            foreach (var canonicalFile in Directory.GetFiles(
                         canonicalDir,
                         "*.ps1",
                         SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(canonicalFile);
                var generatedFile = Path.Combine(generatedLib, name);
                if (!File.Exists(generatedFile))
                {
                    mismatches.Add("missing " + name);
                    continue;
                }

                var canonicalBytes = await File.ReadAllBytesAsync(
                    canonicalFile,
                    cancellationToken).ConfigureAwait(true);
                var generatedBytes = await File.ReadAllBytesAsync(
                    generatedFile,
                    cancellationToken).ConfigureAwait(true);
                if (!CryptographicOperations.FixedTimeEquals(
                        SHA256.HashData(canonicalBytes),
                        SHA256.HashData(generatedBytes)))
                {
                    mismatches.Add("checksum drift " + name);
                }
            }

            Assert.True(mismatches.Count == 0, string.Join("; ", mismatches));
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
                Directory.Delete(temporaryRoot, recursive: true);
        }
    }
}
