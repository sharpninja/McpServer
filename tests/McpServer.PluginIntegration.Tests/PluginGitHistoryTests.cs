using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC5: PluginGitHistory ancestor check rejects unrelated SHAs and accepts HEAD.
/// </summary>
[Trait("PluginInt", "Deterministic")]
public sealed class PluginGitHistoryTests
{
    /// <summary>
    /// Current HEAD is an ancestor of itself, so the helper must pass.
    /// </summary>
    [Fact]
    public void AssertShaIsAncestorOfHead_CurrentHead_Passes()
    {
        var repoRoot = FindRepositoryRoot();
        var head = PluginGitHistory.ReadHead(repoRoot);
        PluginGitHistory.AssertShaIsAncestorOfHead(repoRoot, head);
    }

    /// <summary>
    /// A 40-character hex string that is not a commit object must fail the helper.
    /// </summary>
    [Fact]
    public void AssertShaIsAncestorOfHead_NonCommitSha_Fails()
    {
        var exception = Record.Exception(() =>
            PluginGitHistory.AssertShaIsAncestorOfHead(
                FindRepositoryRoot(),
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        Assert.NotNull(exception);
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
