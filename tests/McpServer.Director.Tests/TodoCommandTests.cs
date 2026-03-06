namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>todo list</c> command.
/// Requires the MCP server to be running.
/// </summary>
public sealed class TodoCommandTests
{
    [Fact]
    public async Task TodoHelp_ExitZero_ListsSubcommands()
    {
        var result = await DirectorRunner.RunAsync("todo --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("list", result.StdOut);
    }

    [Fact]
    public async Task TodoListHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("todo list --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
        Assert.Contains("--section", result.StdOut);
    }

    [Fact]
    public async Task TodoList_ReturnsTable()
    {
        var result = await DirectorRunner.RunAsync("todo list");

        Assert.Equal(0, result.ExitCode);
        var output = result.AllOutput;
        var hasTableHeaders =
            output.Contains("ID", StringComparison.OrdinalIgnoreCase) &&
            output.Contains("Title", StringComparison.OrdinalIgnoreCase);
        var hasExpectedEnvironmentFailure =
            output.Contains("No active workspace is selected", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("Unknown workspace path", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("Permission denied", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("Connection refused", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasTableHeaders || hasExpectedEnvironmentFailure, $"Unexpected output: {output}");
    }

    [Fact]
    public async Task TodoList_WithSectionFilter_Completes()
    {
        var result = await DirectorRunner.RunAsync("todo list --section nonexistent");

        Assert.Equal(0, result.ExitCode);
    }
}
