using System.Diagnostics;
using System.Security.Cryptography;

namespace NukeBuild.Tests;

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
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-NoLogo");
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(syncScript);
            startInfo.ArgumentList.Add("-PluginRoot");
            startInfo.ArgumentList.Add(generatedPlugin);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start the plugin-core sync process.");
            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(true);
            var output = await standardOutput.ConfigureAwait(true);
            var error = await standardError.ConfigureAwait(true);

            Assert.True(
                process.ExitCode == 0,
                $"Plugin-core sync exited {process.ExitCode}.{Environment.NewLine}{output}{error}");

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
