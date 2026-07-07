using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TodoPromptService"/>.
/// </summary>
public sealed class TodoPromptServiceTests
{
    [Fact]
    public async Task StreamImplementAsync_UsesInfiniteCopilotTimeout()
    {
        var todo = new TodoFlatItem
        {
            Id = "TODO-1",
            Title = "Implement me",
            Section = "mvp-app",
            Priority = "high",
            Done = false,
        };
        var todoService = Substitute.For<ITodoService>();
        todoService.GetByIdAsync("TODO-1", Arg.Any<CancellationToken>()).Returns(todo);

        var accessor = TestWorkspaceAccessorHelper.Create(todoService);
        var promptOptions = Substitute.For<IOptionsMonitor<TodoPromptOptions>>();
        promptOptions.CurrentValue.Returns(new TodoPromptOptions { BaseUrl = "http://localhost:7147" });

        var promptProvider = Substitute.For<ITodoPromptProvider>();
        promptProvider.GetImplementPromptAsync(Arg.Any<CancellationToken>()).Returns("Implement {id}");

        AgentCliClientOptions? capturedOptions = null;
        var copilotClient = Substitute.For<IAgentCliClient>();
        copilotClient.InvokeStreamingAsync(Arg.Any<string>(), Arg.Any<AgentCliClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.ArgAt<AgentCliClientOptions?>(1);
                return StreamLines();
            });

        var sut = new TodoPromptService(
            accessor,
            copilotClient,
            promptOptions,
            promptProvider,
            NullLogger<TodoPromptService>.Instance);

        var lines = new List<string>();
        await foreach (var line in sut.StreamImplementAsync("TODO-1", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true))
            lines.Add(line);

        Assert.NotEmpty(lines);
        Assert.NotNull(capturedOptions);
        Assert.Equal(Timeout.InfiniteTimeSpan, capturedOptions!.Timeout);
    }

    private static async IAsyncEnumerable<string> StreamLines()
    {
        yield return "line 1";
        await Task.Yield();
        yield return "line 2";
    }
}
