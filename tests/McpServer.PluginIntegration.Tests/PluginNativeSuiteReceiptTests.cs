using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P19: official plugin native suites, SyncAgentPlugins rerun, and branch/SHA receipts.
/// Maps TEST-MCP-PLUGININT-001 AC5 native-suite half. Fail until docs/receipts/pluginint-p19-* exists and parses Failed 0 Skipped 0.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginNativeSuiteReceiptTests
{
    /// <summary>
    /// P19 red: each official catalog plugin has a native-suite receipt with Failed 0 and Skipped 0.
    /// </summary>
    [Fact]
    public void PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero()
    {
        var repoRoot = FindRepositoryRoot();
        var catalog = PluginSessionLogCatalog.LoadAndValidate(repoRoot);
        var receipt = PluginNativeSuiteReceipt.LoadLatest(repoRoot);
        Assert.Equal(8, catalog.Count);
        Assert.Equal(8, receipt.Plugins.Count);
        foreach (var scenario in catalog)
        {
            var row = Assert.Single(receipt.Plugins, item => string.Equals(item.RepositoryName, scenario.RepositoryName, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(0, row.Failed);
            Assert.Equal(0, row.Skipped);
            Assert.False(string.IsNullOrWhiteSpace(row.NativeSuite));
            var logPath = Path.Combine(receipt.DirectoryPath, row.LogFile.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(logPath), scenario.RepositoryName + " native suite log missing: " + row.LogFile);
            Assert.True(ParseFailedSkipped(File.ReadAllText(logPath), out var failed, out var skipped), scenario.RepositoryName + " log did not parse Failed/Skipped.");
            Assert.Equal(0, failed);
            Assert.Equal(0, skipped);
        }
    }

    /// <summary>
    /// P19 red: after SyncAgentPlugins, each official plugin native suite still Failed 0 Skipped 0.
    /// </summary>
    [Fact]
    public void PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero()
    {
        var repoRoot = FindRepositoryRoot();
        var catalog = PluginSessionLogCatalog.LoadAndValidate(repoRoot);
        var receipt = PluginNativeSuiteReceipt.LoadLatest(repoRoot);
        Assert.Equal(8, receipt.AfterSync.Count);
        foreach (var scenario in catalog)
        {
            var row = Assert.Single(receipt.AfterSync, item => string.Equals(item.RepositoryName, scenario.RepositoryName, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(0, row.Failed);
            Assert.Equal(0, row.Skipped);
            var logPath = Path.Combine(receipt.DirectoryPath, row.LogFile.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(logPath), scenario.RepositoryName + " after-sync log missing: " + row.LogFile);
            Assert.True(ParseFailedSkipped(File.ReadAllText(logPath), out var failed, out var skipped), scenario.RepositoryName + " after-sync log did not parse Failed/Skipped.");
            Assert.Equal(0, failed);
            Assert.Equal(0, skipped);
        }
    }

    /// <summary>
    /// P19 red: receipt records git branch and SHA and unrelated commit count is zero.
    /// </summary>
    [Fact]
    public void PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit()
    {
        var receipt = PluginNativeSuiteReceipt.LoadLatest(FindRepositoryRoot());
        Assert.False(string.IsNullOrWhiteSpace(receipt.Branch));
        Assert.Matches("^[0-9a-f]{40}$", receipt.Sha);
        Assert.Equal(0, receipt.UnrelatedCommitCount);
        var head = ReadGitHead(FindRepositoryRoot());
        Assert.Equal(head, receipt.Sha);
        var gitPath = Path.Combine(receipt.DirectoryPath, "git.json");
        Assert.True(File.Exists(gitPath), "P19 git.json is missing.");
        var gitText = File.ReadAllText(gitPath);
        Assert.Contains(receipt.Branch, gitText, StringComparison.Ordinal);
        Assert.Contains(receipt.Sha, gitText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unrelatedCommitCount", gitText, StringComparison.Ordinal);
    }

    private static bool ParseFailedSkipped(string log, out int failed, out int skipped)
    {
        failed = -1;
        skipped = -1;
        if (string.IsNullOrWhiteSpace(log))
        {
            return false;
        }

        var pester = System.Text.RegularExpressions.Regex.Match(
            log,
            @"Tests Passed:\s*\d+,\s*Failed:\s*(\d+),\s*Skipped:\s*(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (pester.Success)
        {
            failed = int.Parse(pester.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            skipped = int.Parse(pester.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        var jest = System.Text.RegularExpressions.Regex.Match(
            log,
            @"^Tests:\s+(?:(\d+)\s+failed,\s*)?(?:(\d+)\s+skipped,\s*)?(?:(\d+)\s+passed)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
        if (jest.Success)
        {
            failed = jest.Groups[1].Success ? int.Parse(jest.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
            skipped = jest.Groups[2].Success ? int.Parse(jest.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
            return true;
        }

        var failedLine = System.Text.RegularExpressions.Regex.Matches(
            log,
            @"(?im)^Failed(?:!)?:\s*(\d+)\s*$");
        var skippedLine = System.Text.RegularExpressions.Regex.Matches(
            log,
            @"(?im)^Skipped:\s*(\d+)\s*$");
        if (failedLine.Count == 0 || skippedLine.Count == 0)
        {
            return false;
        }

        failed = int.Parse(failedLine[failedLine.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        skipped = int.Parse(skippedLine[skippedLine.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    private static string ReadGitHead(string repositoryRoot)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repositoryRoot,
            ArgumentList = { "rev-parse", "HEAD" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git rev-parse.");
        var output = process.StandardOutput.ReadToEnd().Trim().ToLowerInvariant();
        process.WaitForExit();
        if (process.ExitCode != 0 || output.Length != 40)
        {
            throw new InvalidOperationException("git rev-parse HEAD failed: " + process.StandardError.ReadToEnd());
        }

        return output;
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
