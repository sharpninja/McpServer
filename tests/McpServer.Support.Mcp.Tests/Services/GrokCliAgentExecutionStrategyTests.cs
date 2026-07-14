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

    /// <summary>TEST-MCP-BUGTRIAGE-042: Grok one-shot forwards the configured model argument.</summary>
    [Fact]
    public void BuildGrokArgumentList_ConfiguredModel_IncludesModelFlag()
    {
        var args = GrokCliAgentExecutionStrategy.BuildGrokArgumentList(
            workingDirectory: @"F:\GitHub\McpServer",
            promptFilePath: @"C:\temp\grok-prompt.txt",
            model: "grok-4.3");

        Assert.Contains("--model", args);
        Assert.Contains("grok-4.3", args);
        var orderedArgs = args.ToArray();
        Assert.True(Array.IndexOf(orderedArgs, "--model") < Array.IndexOf(orderedArgs, "--output-format"));
    }

    /// <summary>
    /// TEST-MCP-BUGTRIAGE-043: the sentinel model value <c>auto</c> (any casing) means "let the
    /// Grok CLI choose its default" and must not be forwarded, because the Grok CLI rejects
    /// <c>--model auto</c> with "unknown model id" and the triage runner substitutes <c>auto</c>
    /// for unset tier models. Fixture: BuildGrokArgumentList with model "auto"/"AUTO"/whitespace.
    /// </summary>
    [Theory]
    [InlineData("auto")]
    [InlineData("AUTO")]
    [InlineData("  auto  ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void BuildGrokArgumentList_AutoOrEmptyModel_OmitsModelFlag(string? model)
    {
        var args = GrokCliAgentExecutionStrategy.BuildGrokArgumentList(
            workingDirectory: @"F:\GitHub\McpServer",
            promptFilePath: @"C:\temp\grok-prompt.txt",
            model: model);

        Assert.DoesNotContain("--model", args);
        Assert.DoesNotContain("auto", args, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The one-shot command line carries the prompt file, working directory, and plan/effort flags in order.</summary>
    [Fact]
    public void BuildGrokArgumentList_ContainsExpectedFlagsInOrder()
    {
        var args = GrokCliAgentExecutionStrategy.BuildGrokArgumentList(
            workingDirectory: @"F:\GitHub\McpServer",
            promptFilePath: @"C:\temp\grok-prompt.txt");

        // "high" is the strongest effort level every deployed Grok CLI accepts; "max" is
        // rejected at startup by current CLIs ("unknown effort level 'max'").
        Assert.Equal(
            new[]
            {
                "--prompt-file", @"C:\temp\grok-prompt.txt",
                "--cwd", @"F:\GitHub\McpServer",
                "--permission-mode", "plan",
                "--output-format", "plain",
                "--effort", "high",
                "--reasoning-effort", "high",
            },
            args);
    }
}
