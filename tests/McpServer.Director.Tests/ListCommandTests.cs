namespace McpServer.Director.Tests;

/// <summary>
/// Tests for the <c>list</c> command (workspace listing).
/// Requires the MCP server to be running.
/// </summary>
public sealed class ListCommandTests
{
    [Fact]
    public async Task ListHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("list --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task List_ReturnsWorkspaceTable()
    {
        var result = await DirectorRunner.RunAsync("list");

        Assert.Equal(0, result.ExitCode);
        // The table should contain column headers.
        Assert.Contains("Name", result.AllOutput);
        Assert.Contains("Path", result.AllOutput);
        Assert.Contains("Enabled", result.AllOutput);
    }
}
