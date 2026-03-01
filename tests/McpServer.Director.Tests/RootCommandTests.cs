namespace McpServer.Director.Tests;

/// <summary>
/// Tests that the Director root command and <c>--help</c> work correctly.
/// </summary>
public sealed class RootCommandTests
{
    [Fact]
    public async Task RootHelp_ExitZero_ListsAllTopLevelCommands()
    {
        var result = await DirectorRunner.RunAsync("--help");

        Assert.Equal(0, result.ExitCode);

        // Every top-level command should appear in help output.
        Assert.Contains("health", result.StdOut);
        Assert.Contains("list", result.StdOut);
        Assert.Contains("agents", result.StdOut);
        Assert.Contains("add", result.StdOut);
        Assert.Contains("ban", result.StdOut);
        Assert.Contains("unban", result.StdOut);
        Assert.Contains("delete", result.StdOut);
        Assert.Contains("validate", result.StdOut);
        Assert.Contains("init", result.StdOut);
        Assert.Contains("todo", result.StdOut);
        Assert.Contains("session-log", result.StdOut);
        Assert.Contains("login", result.StdOut);
        Assert.Contains("logout", result.StdOut);
        Assert.Contains("whoami", result.StdOut);
        Assert.Contains("config", result.StdOut);
        Assert.Contains("interactive", result.StdOut);
        Assert.Contains("exec", result.StdOut);
        Assert.Contains("list-viewmodels", result.StdOut);
    }

    [Fact]
    public async Task VersionFlag_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("--version");

        Assert.Equal(0, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.StdOut));
    }
}
