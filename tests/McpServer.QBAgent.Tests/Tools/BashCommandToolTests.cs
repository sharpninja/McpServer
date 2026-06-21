using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tests.Tools;

/// <summary>
/// TEST-MCP-QBTOOLS-003: Verifies the optional run_bash tool reports unavailability cleanly when Git Bash is not
/// installed and returns output when it is (FR-MCP-QBTOOLS-003).
/// </summary>
public sealed class BashCommandToolTests
{
    private const string Workspace = "F:/work/repo";

    /// <summary>When bash.exe is missing, the runner returns -1/"not found" and the tool reports available=false.</summary>
    [Fact]
    public async Task Run_WhenBashMissing_ReturnsUnavailable()
    {
        var runner = new FakeProcessRunner(new ProcessRunResult(-1, null, "bash not found."));
        var tool = new BashCommandTool(runner, Workspace);

        var result = await tool.RunAsync("echo hi").ConfigureAwait(true);

        Assert.False(result.Available);
        Assert.False(result.Success);
        Assert.Contains("not available", result.Error!, StringComparison.Ordinal);
    }

    /// <summary>When bash runs, the tool reports available=true and surfaces stdout and exit code.</summary>
    [Fact]
    public async Task Run_WhenBashPresent_ReturnsOutput()
    {
        var runner = new FakeProcessRunner(new ProcessRunResult(0, "hi", null));
        var tool = new BashCommandTool(runner, Workspace);

        var result = await tool.RunAsync("echo hi").ConfigureAwait(true);

        Assert.True(result.Available);
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hi", result.Output);
        Assert.Equal("bash", runner.LastRequest!.FileName);
        Assert.StartsWith("-lc ", runner.LastRequest!.Arguments, StringComparison.Ordinal);
    }

    /// <summary>A non-zero bash exit is available but not successful.</summary>
    [Fact]
    public async Task Run_WhenBashFails_AvailableButNotSuccess()
    {
        var runner = new FakeProcessRunner(new ProcessRunResult(2, null, "boom"));
        var tool = new BashCommandTool(runner, Workspace);

        var result = await tool.RunAsync("false").ConfigureAwait(true);

        Assert.True(result.Available);
        Assert.False(result.Success);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("boom", result.Error);
    }
}
