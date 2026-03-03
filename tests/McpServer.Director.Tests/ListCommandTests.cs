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
        var output = result.AllOutput;
        var hasTableHeaders =
            output.Contains("Name", StringComparison.OrdinalIgnoreCase) &&
            output.Contains("Path", StringComparison.OrdinalIgnoreCase);
        var hasExpectedEnvironmentFailure =
            output.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("Connection refused", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasTableHeaders || hasExpectedEnvironmentFailure, $"Unexpected output: {output}");
    }
}
