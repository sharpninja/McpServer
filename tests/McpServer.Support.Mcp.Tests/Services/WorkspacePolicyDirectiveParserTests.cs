using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WorkspacePolicyDirectiveParser"/>.
/// </summary>
public sealed class WorkspacePolicyDirectiveParserTests
{
    [Fact]
    public async Task ParseAsync_WhenCopilotFails_FallsBackToDeterministicParser()
    {
        var copilot = Substitute.For<IAgentCliClient>();
        copilot.InvokeAsync(Arg.Any<string>(), Arg.Any<AgentCliClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentCliResult { State = AgentCliResultState.Error, Stderr = "spawn failed" });

        var parser = CreateParser(copilot);
        var result = await parser.ParseAsync(
            "Ban chinese sources from all workspaces",
            workspacePathHint: null,
            ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Directive);
        Assert.Equal("add", result.Directive!.Action);
        Assert.Equal("country_of_origin", result.Directive.Category);
        Assert.Equal("all", result.Directive.Scope);
        Assert.Contains("CN", result.Directive.Values);
        Assert.Equal("fallback", result.Directive.Parser);
    }

    [Fact]
    public async Task ParseAsync_InvalidDirective_ReturnsFailure()
    {
        var copilot = Substitute.For<IAgentCliClient>();
        copilot.InvokeAsync(Arg.Any<string>(), Arg.Any<AgentCliClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new AgentCliResult { State = AgentCliResultState.Error, Stderr = "spawn failed" });

        var parser = CreateParser(copilot);
        var result = await parser.ParseAsync(
            "please make this better",
            workspacePathHint: null,
            ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ParseAsync_UsesInfiniteCopilotTimeout()
    {
        AgentCliClientOptions? capturedOptions = null;
        var copilot = Substitute.For<IAgentCliClient>();
        copilot.InvokeAsync(Arg.Any<string>(), Arg.Any<AgentCliClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.ArgAt<AgentCliClientOptions?>(1);
                return new AgentCliResult
                {
                    State = AgentCliResultState.Success,
                    Body = """{"action":"add","category":"country_of_origin","values":["CN"],"scope":"all"}""",
                };
            });

        var parser = CreateParser(copilot);
        var result = await parser.ParseAsync(
            "Ban chinese sources from all workspaces",
            workspacePathHint: null,
            ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(capturedOptions);
        Assert.Equal(Timeout.InfiniteTimeSpan, capturedOptions!.Timeout);
    }

    private static WorkspacePolicyDirectiveParser CreateParser(IAgentCliClient copilotClient)
    {
        var todoService = Substitute.For<ITodoService>();
        var accessor = TestWorkspaceAccessorHelper.Create(todoService, repoRoot: ".");

        var promptOptions = Substitute.For<IOptionsMonitor<TodoPromptOptions>>();
        promptOptions.CurrentValue.Returns(new TodoPromptOptions());

        return new WorkspacePolicyDirectiveParser(
            copilotClient,
            accessor,
            promptOptions,
            NullLogger<WorkspacePolicyDirectiveParser>.Instance);
    }
}
