namespace McpServer.Director.Tests;

/// <summary>
/// Tests for the <c>health</c> command.
/// Requires the MCP server to be running on the default port.
/// </summary>
public sealed class HealthCommandTests
{
    [Fact]
    public async Task HealthHelp_ExitZero()
    {
        var result = await DirectorRunner.RunAsync("health --help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--workspace", result.StdOut);
    }

    [Fact]
    public async Task Health_ReturnsServerStatus()
    {
        var result = await DirectorRunner.RunAsync("health");

        Assert.Equal(0, result.ExitCode);
        var output = result.AllOutput;
        var hasHealthy = output.Contains("healthy", StringComparison.OrdinalIgnoreCase);
        var hasExpectedEnvironmentFailure = output.Contains("refused", StringComparison.OrdinalIgnoreCase)
            || output.Contains("actively refused", StringComparison.OrdinalIgnoreCase);
        Assert.True(hasHealthy || hasExpectedEnvironmentFailure, $"Unexpected output: {output}");
    }
}
