namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>session-log list</c> command (and <c>sl</c> alias).
/// Requires the MCP server to be running.
/// </summary>
public sealed class SessionLogCommandTests
{
    [Fact]
    public async Task SessionLogHelp_ExitZero_ListsSubcommands()
    {
        var result = await DirectorRunner.RunAsync("session-log --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("list", result.StdOut);
    }

    [Fact]
    public async Task SlAlias_Help_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("sl --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("list", result.StdOut);
    }

    [Fact]
    public async Task SessionLogListHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("session-log list --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
        Assert.Contains("--limit", result.StdOut);
    }

    [Fact]
    public async Task SessionLogList_Completes()
    {
        var result = await DirectorRunner.RunAsync("session-log list --limit 5");

        // The command should exit cleanly. The server may return data or a validation error
        // (e.g. SourceType required) depending on server version — either way it should not crash.
        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.AllOutput));
    }

    [Fact]
    public async Task SlListAlias_Completes()
    {
        var result = await DirectorRunner.RunAsync("sl list --limit 3");

        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.AllOutput));
    }
}
