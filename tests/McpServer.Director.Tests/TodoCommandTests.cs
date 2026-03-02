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
        Assert.Contains("ID", result.AllOutput);
        Assert.Contains("Title", result.AllOutput);
    }

    [Fact]
    public async Task TodoList_WithSectionFilter_Completes()
    {
        var result = await DirectorRunner.RunAsync("todo list --section nonexistent");

        Assert.Equal(0, result.ExitCode);
    }
}
