namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-009: Validates scoped pinned path resolution for workspace and data roots.
/// </summary>
public sealed class AgentHelpPinnedPathResolverTests
{
    /// <summary>
    /// TEST-MCP-HELP-009: workspace: and data: tokens resolve against the expected roots.
    /// </summary>
    [Fact]
    public void TryResolve_ScopedTokens_UseExpectedRoots()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, "data-root");
        Directory.CreateDirectory(Path.Combine(dataRoot, "templates"));
        File.WriteAllText(Path.Combine(workspaceRoot, "AGENTS.md"), "workspace marker");
        File.WriteAllText(Path.Combine(dataRoot, "templates", "prompt-templates.yaml"), "data template");

        var resolver = AgentHelpPinnedPathResolverTestFactory.Create(
            new Dictionary<string, string?> { ["DataFolder"] = dataRoot });

        var workspaceResolved = resolver.TryResolve("workspace:AGENTS.md", workspaceRoot);
        var dataResolved = resolver.TryResolve("data:templates/prompt-templates.yaml", workspaceRoot);
        var missing = resolver.TryResolve("workspace:missing.md", workspaceRoot);

        Assert.NotNull(workspaceResolved);
        Assert.NotNull(dataResolved);
        Assert.Null(missing);
        Assert.Equal(Path.Combine(workspaceRoot, "AGENTS.md"), workspaceResolved.Value.FullPath);
        Assert.Equal(Path.Combine(dataRoot, "templates", "prompt-templates.yaml"), dataResolved.Value.FullPath);
        Assert.Equal("workspace:AGENTS.md", workspaceResolved.Value.SourceKey);
        Assert.Equal("data:templates/prompt-templates.yaml", dataResolved.Value.SourceKey);
    }
}