namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>config show</c>, <c>config set-default-url</c>,
/// and <c>config clear-default-url</c> commands.
/// These commands do NOT require the MCP server.
/// </summary>
public sealed class ConfigCommandTests
{
    [Fact]
    public async Task ConfigHelp_ExitZero_ListsSubcommands()
    {
        var result = await DirectorRunner.RunAsync("config --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("show", result.StdOut);
        Assert.Contains("set-default-url", result.StdOut);
        Assert.Contains("clear-default-url", result.StdOut);
    }

    // ── config show ─────────────────────────────────────────────────────

    [Fact]
    public async Task ConfigShow_ExitZero_ShowsConfigPath()
    {
        var result = await DirectorRunner.RunAsync("config show");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Config Path", result.AllOutput);
        Assert.Contains("Default Base URL", result.AllOutput);
    }

    // ── config set-default-url ──────────────────────────────────────────

    [Fact]
    public async Task ConfigSetDefaultUrlHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("config set-default-url --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("url", result.StdOut);
    }

    [Fact]
    public async Task ConfigSetDefaultUrl_InvalidUrl_PrintsError()
    {
        var result = await DirectorRunner.RunAsync("config set-default-url not-a-url");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("URL must be", result.AllOutput);
    }

    [Fact]
    public async Task ConfigSetDefaultUrl_SetAndClear_RoundTrips()
    {
        // Set a test URL.
        var setResult = await DirectorRunner.RunAsync("config set-default-url http://localhost:9999");
        Assert.Equal(0, setResult.ExitCode);
        Assert.Contains("9999", setResult.AllOutput);

        // Verify it appears in show.
        var showResult = await DirectorRunner.RunAsync("config show");
        Assert.Contains("9999", showResult.AllOutput);

        // Clear it.
        var clearResult = await DirectorRunner.RunAsync("config clear-default-url");
        Assert.Equal(0, clearResult.ExitCode);
        Assert.Contains("cleared", clearResult.AllOutput, StringComparison.OrdinalIgnoreCase);

        // Verify it's gone.
        var showAfter = await DirectorRunner.RunAsync("config show");
        Assert.Contains("not set", showAfter.AllOutput);
    }

    // ── config set-url alias ────────────────────────────────────────────

    [Fact]
    public async Task ConfigSetUrlAlias_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("config set-url http://localhost:7777");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("7777", result.AllOutput);

        // Clean up.
        await DirectorRunner.RunAsync("config clear-default-url");
    }

    // ── config clear-default-url ────────────────────────────────────────

    [Fact]
    public async Task ConfigClearDefaultUrl_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("config clear-default-url");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("cleared", result.AllOutput, StringComparison.OrdinalIgnoreCase);
    }
}
