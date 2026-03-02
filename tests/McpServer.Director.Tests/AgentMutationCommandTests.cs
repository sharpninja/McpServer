namespace McpServer.Director.Tests;

/// <summary>
/// Tests for agent mutation commands: <c>add</c>, <c>ban</c>, <c>unban</c>, <c>delete</c>.
/// These commands require the MCP server to be running.
/// </summary>
public sealed class AgentMutationCommandTests
{
    // ── add ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("add --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-id", result.StdOut);
        Assert.Contains("--isolation", result.StdOut);
        Assert.Contains("--enabled", result.StdOut);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task Add_WithTestAgent_Completes()
    {
        var result = await DirectorRunner.RunAsync("add test-cli-agent --isolation worktree");

        // May succeed or fail depending on server state; should not crash.
        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }

    // ── ban ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task BanHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("ban --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-id", result.StdOut);
        Assert.Contains("--reason", result.StdOut);
        Assert.Contains("--global", result.StdOut);
        Assert.Contains("--until-pr", result.StdOut);
    }

    [Fact]
    public async Task Ban_WithTestAgent_Completes()
    {
        var result = await DirectorRunner.RunAsync("ban test-cli-agent --reason \"CLI test\"");

        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }

    // ── unban ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UnbanHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("unban --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-id", result.StdOut);
        Assert.Contains("--global", result.StdOut);
    }

    [Fact]
    public async Task Unban_WithTestAgent_Completes()
    {
        var result = await DirectorRunner.RunAsync("unban test-cli-agent");

        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }

    // ── delete ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("delete --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-id", result.StdOut);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task Delete_WithTestAgent_Completes()
    {
        var result = await DirectorRunner.RunAsync("delete test-cli-agent");

        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }
}
