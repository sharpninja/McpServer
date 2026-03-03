namespace McpServer.Director.Tests;

/// <summary>
/// Tests for the <c>agents</c> command and its subcommands:
/// <c>agents definitions</c>, <c>agents workspace</c>, <c>agents events</c>.
/// Requires the MCP server to be running.
/// </summary>
public sealed class AgentsCommandTests
{
    [Fact]
    public async Task AgentsHelp_ExitZero_ListsSubcommands()
    {
        var result = await DirectorRunner.RunAsync("agents --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("definitions", result.StdOut);
        Assert.Contains("workspace", result.StdOut);
        Assert.Contains("events", result.StdOut);
    }

    // ── agents definitions ──────────────────────────────────────────────

    [Fact]
    public async Task AgentsDefinitionsHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("agents definitions --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task AgentsDefinitions_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("agents definitions");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ID", result.AllOutput);
        Assert.Contains("Display Name", result.AllOutput);
    }

    [Fact]
    public async Task AgentsDefsAlias_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("agents defs");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("ID", result.AllOutput);
    }

    // ── agents workspace ────────────────────────────────────────────────

    [Fact]
    public async Task AgentsWorkspaceHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("agents workspace --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task AgentsWorkspace_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("agents workspace");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Agent ID", result.AllOutput);
    }

    [Fact]
    public async Task AgentsWsAlias_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("agents ws");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Agent ID", result.AllOutput);
    }

    // ── agents events ───────────────────────────────────────────────────

    [Fact]
    public async Task AgentsEventsHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("agents events --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-id", result.StdOut);
        Assert.Contains("--limit", result.StdOut);
    }

    [Fact]
    public async Task AgentsEvents_WithAgentId_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("agents events system --limit 5");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Timestamp", result.AllOutput);
        Assert.Contains("Event", result.AllOutput);
    }
}
