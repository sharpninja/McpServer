// SPDX-License-Identifier: Apache-2.0
namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-006 AC-001 (BUG-BUILD-003): verifies that <c>Build.ResolveOfficialPluginSiblingParent</c> returns the directory that
/// holds the mcpserver-*-plugin repositories from the main checkout's worktrees, including the Claude Code <c>.claude/worktrees</c>
/// layout. Build-infrastructure test, accountable through TR only. Uses a temporary directory tree with an empty
/// <c>mcpserver-codex-plugin</c> folder as the sibling marker; no git repositories or mocks.
/// </summary>
public sealed class PluginParentResolutionTests : IDisposable
{
    private readonly string _parent = Path.Combine(Path.GetTempPath(), "nuke-plugin-parent-" + Guid.NewGuid().ToString("N"));

    /// <summary>Creates the sibling marker folder under the temporary plugin parent.</summary>
    public PluginParentResolutionTests()
    {
        Directory.CreateDirectory(Path.Combine(_parent, "mcpserver-codex-plugin"));
    }

    /// <summary>
    /// From each supported worktree layout, the resolver returns the plugin parent (TR-MCP-NUKE-006 AC-001):
    /// <c>McpServer/.claude/worktrees/&lt;name&gt;</c>, <c>McpServer/.mcpServer/worktrees/&lt;name&gt;</c>, and
    /// <c>&lt;repo&gt;/.worktrees/&lt;name&gt;</c>.
    /// </summary>
    /// <param name="relativeWorktree">The worktree path relative to the plugin parent.</param>
    [Theory]
    [InlineData("McpServer/.claude/worktrees/session-1")]
    [InlineData("McpServer/.mcpServer/worktrees/session-1")]
    [InlineData("cline/.worktrees/mcpserver-qbrain")]
    public void ResolveOfficialPluginSiblingParent_FromWorktree_ReturnsPluginParent(string relativeWorktree)
    {
        var worktree = Path.Combine(_parent, relativeWorktree.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(worktree);

        var resolved = Build.ResolveOfficialPluginSiblingParent(worktree);

        Assert.Equal(Path.GetFullPath(_parent), resolved);
    }

    /// <summary>Removes the temporary directory tree.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_parent))
            Directory.Delete(_parent, recursive: true);
    }
}
