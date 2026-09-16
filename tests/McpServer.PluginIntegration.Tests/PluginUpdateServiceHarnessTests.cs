using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P20: UpdateService harness with sanitized fixtures and staging/prod approval fail-closed.
/// Maps TEST-MCP-PLUGININT-001 AC5 deploy half. Fail until P20 receipts and the promotion gate exist.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginUpdateServiceHarnessTests
{
    /// <summary>
    /// P20 red: Session Log harness against development UpdateService with sanitized fixtures is Failed 0 Skipped 0.
    /// </summary>
    [Fact]
    public void PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero()
    {
        var repoRoot = FindRepositoryRoot();
        var receipt = PluginUpdateServiceHarnessReceipt.LoadLatest(repoRoot);
        Assert.Equal("Development", receipt.Environment, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("UpdateService", receipt.DeployTarget, StringComparer.OrdinalIgnoreCase);
        Assert.True(receipt.SanitizedFixtures);
        Assert.False(string.IsNullOrWhiteSpace(receipt.ServiceName));
        Assert.Equal(0, receipt.Failed);
        Assert.Equal(0, receipt.Skipped);
        Assert.False(string.IsNullOrWhiteSpace(receipt.Branch));
        Assert.Matches("^[0-9a-f]{40}$", receipt.Sha);
        PluginGitHistory.AssertShaIsAncestorOfHead(repoRoot, receipt.Sha);
        var logPath = Path.Combine(receipt.DirectoryPath, receipt.LogFile.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(logPath), "P20 harness log is missing: " + receipt.LogFile);
        var log = File.ReadAllText(logPath);
        Assert.Contains("UpdateService", log, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sanitized", log, StringComparison.OrdinalIgnoreCase);
        Assert.True(ParseFailedSkipped(log, out var failed, out var skipped), "P20 harness log did not parse Failed/Skipped.");
        Assert.Equal(0, failed);
        Assert.Equal(0, skipped);
    }

    /// <summary>
    /// P20 red: Staging and Production plugin promotion is blocked without an operator approval artifact.
    /// </summary>
    [Fact]
    public void PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag()
    {
        var repoRoot = FindRepositoryRoot();
        var gate = PluginPromotionGate.Load(repoRoot);
        Assert.True(gate.RequiresOperatorApproval("Staging"));
        Assert.True(gate.RequiresOperatorApproval("Production"));
        Assert.False(gate.RequiresOperatorApproval("Development"));
        Assert.False(gate.Allow("Staging", operatorApprovalArtifactPath: null));
        Assert.False(gate.Allow("Production", operatorApprovalArtifactPath: null));
        var missing = Path.Combine(repoRoot, "docs", "receipts", "plugin-promotion-approval-missing.json");
        Assert.False(File.Exists(missing), "C-red-P20 must not plant a staging/prod approval artifact.");
        Assert.False(gate.Allow("Staging", missing));
        Assert.False(gate.Allow("Production", missing));
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

        var failedLine = System.Text.RegularExpressions.Regex.Matches(log, @"(?im)^Failed(?:!)?:\s*(\d+)\s*$");
        var skippedLine = System.Text.RegularExpressions.Regex.Matches(log, @"(?im)^Skipped:\s*(\d+)\s*$");
        if (failedLine.Count == 0 || skippedLine.Count == 0)
        {
            return false;
        }

        failed = int.Parse(failedLine[failedLine.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        skipped = int.Parse(skippedLine[skippedLine.Count - 1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        return true;
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
