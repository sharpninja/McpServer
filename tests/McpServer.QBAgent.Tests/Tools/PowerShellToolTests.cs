using McpServer.McpAgent.PowerShellSessions;
using McpServer.QBAgent.Tools;

namespace McpServer.QBAgent.Tests.Tools;

/// <summary>
/// TEST-MCP-QBTOOLS-007: Verifies the run_powershell tool creates a hosted session lazily and executes the command
/// against it (FR-MCP-QBTOOLS-002).
/// </summary>
public sealed class PowerShellToolTests
{
    /// <summary>The tool creates one session and runs the command, returning the captured output.</summary>
    [Fact]
    public async Task Run_CreatesSession_AndExecutesCommand()
    {
        var manager = new FakePowerShellSessionManager(new PowerShellSessionCommandResult { Success = true, Output = "PONG" });
        var tool = new PowerShellTool(manager, "F:/work/repo");

        var result = await tool.RunAsync("Write-Output PONG").ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("PONG", result.Output);
        Assert.Equal("Write-Output PONG", manager.LastCommand);
        Assert.Equal(1, manager.CreatedSessions);
    }

    /// <summary>A second call reuses the same session (no new session created).</summary>
    [Fact]
    public async Task Run_Twice_ReusesSession()
    {
        var manager = new FakePowerShellSessionManager(new PowerShellSessionCommandResult { Success = true, Output = "x" });
        var tool = new PowerShellTool(manager, "F:/work/repo");

        await tool.RunAsync("one").ConfigureAwait(true);
        await tool.RunAsync("two").ConfigureAwait(true);

        Assert.Equal(1, manager.CreatedSessions);
        Assert.Equal("two", manager.LastCommand);
    }

    /// <summary>An empty command is rejected before any session work.</summary>
    [Fact]
    public async Task Run_EmptyCommand_Rejected()
    {
        var manager = new FakePowerShellSessionManager(new PowerShellSessionCommandResult());
        var tool = new PowerShellTool(manager, "F:/work/repo");

        var result = await tool.RunAsync("   ").ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(0, manager.CreatedSessions);
    }

    /// <summary>When session creation fails, the tool surfaces the error and does not execute a command.</summary>
    [Fact]
    public async Task Run_WhenCreateFails_ReturnsError()
    {
        var manager = new FakePowerShellSessionManager(new PowerShellSessionCommandResult(), createSucceeds: false);
        var tool = new PowerShellTool(manager, "F:/work/repo");

        var result = await tool.RunAsync("Write-Output x").ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.True(result.HadErrors);
        Assert.Null(manager.LastCommand);
    }

    /// <summary>Disposing the tool closes the reused session.</summary>
    [Fact]
    public async Task Dispose_ClosesSession()
    {
        var manager = new FakePowerShellSessionManager(new PowerShellSessionCommandResult { Success = true, Output = "x" });
        var tool = new PowerShellTool(manager, "F:/work/repo");

        await tool.RunAsync("one").ConfigureAwait(true);
        tool.Dispose();

        Assert.Equal(1, manager.ClosedSessions);
    }
}
