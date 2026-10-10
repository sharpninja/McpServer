using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 / PLAN-PLUGINHANDOFF-001 Phase C P1:
/// the PluginSessionLogIntegration Nuke target, solution project, and eight-row
/// catalog must exist before adapter work starts. Skipped tests are failures.
/// </summary>
public sealed class PluginSessionLogIntegrationTargetTests
{
    /// <summary>
    /// P1 red: Nuke target PluginSessionLogIntegration exists and treats skipped tests as failures.
    /// </summary>
    [Fact]
    public void BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail()
    {
        var repoRoot = FindRepositoryRoot();
        var buildType = typeof(Build);
        var target = buildType.GetProperty("PluginSessionLogIntegration");
        Assert.NotNull(target);

        var buildSources = Directory.GetFiles(Path.Combine(repoRoot, "build"), "Build*.cs", SearchOption.TopDirectoryOnly);
        var combined = string.Join(Environment.NewLine, buildSources.Select(File.ReadAllText));
        Assert.Contains("PluginSessionLogIntegration", combined, StringComparison.Ordinal);
        Assert.True(
            combined.Contains("Skipped", StringComparison.Ordinal) &&
            (combined.Contains("SkipIsFailure", StringComparison.Ordinal) ||
             Regex.IsMatch(combined, @"PluginSessionLogIntegration[\s\S]{0,4000}(Skipped|skip).*fail", RegexOptions.IgnoreCase)),
            "PluginSessionLogIntegration must treat skipped tests as failures.");
    }

    /// <summary>
    /// P1 red: the solution includes tests/McpServer.PluginIntegration.Tests.
    /// </summary>
    [Fact]
    public void Solution_ContainsMcpServerPluginIntegrationTestsProject()
    {
        var repoRoot = FindRepositoryRoot();
        var sln = File.ReadAllText(Path.Combine(repoRoot, "McpServer.sln"));
        Assert.Contains("McpServer.PluginIntegration.Tests", sln, StringComparison.Ordinal);
        Assert.True(
            File.Exists(Path.Combine(repoRoot, "tests", "McpServer.PluginIntegration.Tests", "McpServer.PluginIntegration.Tests.csproj")),
            "tests/McpServer.PluginIntegration.Tests/McpServer.PluginIntegration.Tests.csproj is missing.");
    }

    /// <summary>
    /// P1 red: FR-MCP-PLUGININT-001 catalog has exactly eight enabled scenarios.
    /// </summary>
    [Fact]
    public void Catalog_HasExactlyEightEnabledScenarios()
    {
        var repoRoot = FindRepositoryRoot();
        var catalogPath = Path.Combine(
            repoRoot,
            "tests",
            "McpServer.PluginIntegration.Tests",
            "scenarios",
            "plugin-sessionlog-scenarios.json");
        Assert.True(File.Exists(catalogPath), "plugin-sessionlog-scenarios.json is missing.");
        var json = File.ReadAllText(catalogPath);
        Assert.Contains("Codex", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Claude Code", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Claude Cowork", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Copilot", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Grok", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cline", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OpenCode", json, StringComparison.OrdinalIgnoreCase);
        var enabledCount = Regex.Matches(json, "\"enabled\"\\s*:\\s*true", RegexOptions.IgnoreCase).Count;
        Assert.Equal(8, enabledCount);
    }

    /// <summary>
    /// P18 green: PluginSessionLogIntegration SkipIsFailure throws when TRX reports skipped tests and passes when skipped=0.
    /// </summary>
    [Fact]
    public void NukeTarget_SkipIsFailure()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mcp-pluginint-trx-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var green = Path.Combine(directory, "green.trx");
            File.WriteAllText(green, TrxCounters(skipped: 0, notExecuted: 0, executed: 1, total: 1, failed: 0));
            Build.FailIfPluginSessionLogSkipped(green);

            var skipped = Path.Combine(directory, "skipped.trx");
            File.WriteAllText(skipped, TrxCounters(skipped: 1, notExecuted: 0, executed: 0, total: 1, failed: 0));
            var ex = Assert.Throws<InvalidOperationException>(() => Build.FailIfPluginSessionLogSkipped(skipped));
            Assert.Contains("SkipIsFailure", ex.Message, StringComparison.Ordinal);

            var empty = Path.Combine(directory, "empty.trx");
            File.WriteAllText(empty, TrxCounters(skipped: 0, notExecuted: 0, executed: 0, total: 0, failed: 0));
            var emptyEx = Assert.Throws<InvalidOperationException>(() => Build.FailIfPluginSessionLogSkipped(empty));
            Assert.Contains("SkipIsFailure", emptyEx.Message, StringComparison.Ordinal);

            var failed = Path.Combine(directory, "failed.trx");
            File.WriteAllText(failed, TrxCounters(skipped: 0, notExecuted: 0, executed: 1, total: 1, failed: 1));
            var failedEx = Assert.Throws<InvalidOperationException>(() => Build.FailIfPluginSessionLogSkipped(failed));
            Assert.Contains("SkipIsFailure", failedEx.Message, StringComparison.Ordinal);

            var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "build", "Build.PluginSessionLogIntegration.cs"));
            Assert.Contains("PreflightPluginSessionLogAiUnitStrategy", source, StringComparison.Ordinal);
            Assert.Contains("SetFilter(\"PluginInt=Deterministic\")", source, StringComparison.Ordinal);
            Assert.Contains("SetFilter(\"PluginInt=AI\")", source, StringComparison.Ordinal);

            Build.PreflightPluginSessionLogAiUnitStrategy(FindRepositoryRoot());
            var missing = Path.Combine(directory, "missing-root");
            Assert.ThrowsAny<Exception>(() => Build.PreflightPluginSessionLogAiUnitStrategy(missing));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string TrxCounters(int skipped, int notExecuted, int executed, int total, int failed)
    {
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<TestRun xmlns=\"http://microsoft.com/schemas/visualstudio/2010/test\">" +
            "<ResultSummary outcome=\"Completed\">" +
            "<Counters total=\"" + total + "\" executed=\"" + executed + "\" passed=\"" + (executed - failed) +
            "\" failed=\"" + failed + "\" skipped=\"" + skipped + "\" notExecuted=\"" + notExecuted + "\" />" +
            "</ResultSummary></TestRun>";
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
