namespace McpServer.Director.Tests;

/// <summary>
/// Tests for <c>validate</c> and <c>init</c> commands.
/// Requires the MCP server to be running.
/// </summary>
public sealed class ValidateAndInitCommandTests
{
    // ── validate ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("validate --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task Validate_Completes()
    {
        var result = await DirectorRunner.RunAsync("validate");

        // Valid or invalid, should not crash.
        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }

    // ── init ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task InitHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("init --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task Init_Completes()
    {
        var result = await DirectorRunner.RunAsync("init");

        Assert.True(result.ExitCode == 0, $"Unexpected exit code {result.ExitCode}: {result.AllOutput}");
    }
}
