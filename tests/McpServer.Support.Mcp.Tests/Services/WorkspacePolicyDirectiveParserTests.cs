using McpServer.Common.Copilot;
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
        var copilot = Substitute.For<ICopilotClient>();
        copilot.InvokeAsync(Arg.Any<string>(), Arg.Any<CopilotClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new CopilotResult { State = CopilotResultState.Error, Stderr = "spawn failed" });

        var parser = CreateParser(copilot);
        var result = await parser.ParseAsync("Ban chinese sources from all workspaces", workspacePathHint: null).ConfigureAwait(true);

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
        var copilot = Substitute.For<ICopilotClient>();
        copilot.InvokeAsync(Arg.Any<string>(), Arg.Any<CopilotClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new CopilotResult { State = CopilotResultState.Error, Stderr = "spawn failed" });

        var parser = CreateParser(copilot);
        var result = await parser.ParseAsync("please make this better", workspacePathHint: null).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    private static WorkspacePolicyDirectiveParser CreateParser(ICopilotClient copilotClient)
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
