using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-HELP-008, TR-MCP-HELP-010: Verifies the Grok CLI execution strategy identity,
/// executable resolution defaults, and one-shot command-line construction.
/// </summary>
public sealed class GrokCliAgentExecutionStrategyTests
{
    /// <summary>The strategy reports the canonical <c>grok-cli</c> name.</summary>
    [Fact]
    public void Name_IsGrokCli()
    {
        var strategy = new GrokCliAgentExecutionStrategy(
            processEnvironment: null!,
            processSpawner: null!,
            logger: NullLogger<GrokCliAgentExecutionStrategy>.Instance);

        Assert.Equal("grok-cli", strategy.Name);
    }

    /// <summary>An empty AgentPath resolves to the bare <c>grok</c> executable.</summary>
    [Fact]
    public void ResolveGrokExecutable_EmptyAgentPath_ReturnsGrok()
    {
        Assert.Equal("grok", GrokCliAgentExecutionStrategy.ResolveGrokExecutable(""));
        Assert.Equal("grok", GrokCliAgentExecutionStrategy.ResolveGrokExecutable(null));
    }

    /// <summary>A non-grok AgentPath (e.g. the default <c>cline</c>) is overridden to <c>grok</c>.</summary>
    [Fact]
    public void ResolveGrokExecutable_NonGrokAgentPath_ReturnsGrok()
    {
        Assert.Equal("grok", GrokCliAgentExecutionStrategy.ResolveGrokExecutable("cline"));
    }

    /// <summary>An explicit grok binary path (bare name or full path) is preserved.</summary>
    [Fact]
    public void ResolveGrokExecutable_ExplicitGrokPath_IsPreserved()
    {
        Assert.Equal("grok", GrokCliAgentExecutionStrategy.ResolveGrokExecutable("grok"));
        Assert.Equal(
            @"C:\Users\kingd\.grok\bin\grok.exe",
            GrokCliAgentExecutionStrategy.ResolveGrokExecutable(@"C:\Users\kingd\.grok\bin\grok.exe"));
    }

    /// <summary>The one-shot command line carries the prompt file, working directory, and plan/effort flags in order.</summary>
    [Fact]
    public void BuildGrokArgumentList_ContainsExpectedFlagsInOrder()
    {
        var args = GrokCliAgentExecutionStrategy.BuildGrokArgumentList(
            workingDirectory: @"F:\GitHub\McpServer",
            promptFilePath: @"C:\temp\grok-prompt.txt");

        Assert.Equal(
            new[]
            {
                "--prompt-file", @"C:\temp\grok-prompt.txt",
                "--cwd", @"F:\GitHub\McpServer",
                "--permission-mode", "plan",
                "--output-format", "plain",
                "--effort", "max",
                "--reasoning-effort", "max",
            },
            args);
    }
}
