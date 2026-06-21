using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBEXEC-001: Verifies MCP-internal vs external tool classification and that the interceptor executes
/// internal tools server-side (stripping them) while leaving external and unhandled-internal calls for the agent.
/// </summary>
public sealed class QuadBrainToolInterceptionTests
{
    /// <summary><c>mcp_</c>-prefixed tools are internal; others are external.</summary>
    [Theory]
    [InlineData("mcp_todo_update", true)]
    [InlineData("mcp_repo_write", true)]
    [InlineData("mcp_requirements_create_tr", true)]
    [InlineData("do_local_thing", false)]
    [InlineData("", false)]
    public void Classifier_IdentifiesInternalTools(string toolName, bool expectedInternal)
        => Assert.Equal(expectedInternal, new QuadBrainToolClassifier().IsInternal(toolName));

    /// <summary>Only external tools remain as commands; internal successes are stripped and internal non-successes are reported as failures, never emitted to the agent.</summary>
    [Fact]
    public async Task Interceptor_OnlyExternalRemains_InternalSuccessStripped_InternalNonSuccessFailed()
    {
        var interceptor = new QuadBrainToolInterceptor(
            new QuadBrainToolClassifier(),
            new FakeExecutor(handled: "mcp_todo_update"));
        var calls = new[]
        {
            Call("mcp_todo_update"),     // internal, handled -> stripped (executed)
            Call("mcp_repo_write"),      // internal, unhandled -> failure note (NOT a command)
            Call("do_local_thing"),      // external -> remains as a command
        };

        var result = await interceptor.InterceptAsync(calls, turnId: null).ConfigureAwait(true);

        Assert.Equal("mcp_todo_update", Assert.Single(result.Executed).ToolCall.Function.Name);
        Assert.Equal("do_local_thing", Assert.Single(result.RemainingToolCalls).Function.Name);
        var failed = Assert.Single(result.Failed);
        Assert.Equal("mcp_repo_write", failed.ToolCall.Function.Name);
        Assert.False(string.IsNullOrWhiteSpace(failed.Outcome.Error));
    }

    /// <summary>A handled-but-failed internal tool is reported as a failure, not emitted to the agent.</summary>
    [Fact]
    public async Task Interceptor_FailedInternal_IsReportedNotEmitted()
    {
        var interceptor = new QuadBrainToolInterceptor(
            new QuadBrainToolClassifier(),
            new FakeExecutor(failed: "mcp_todo_update"));

        var result = await interceptor.InterceptAsync([Call("mcp_todo_update")], turnId: null).ConfigureAwait(true);

        Assert.Empty(result.Executed);
        Assert.Empty(result.RemainingToolCalls);
        Assert.Equal("mcp_todo_update", Assert.Single(result.Failed).ToolCall.Function.Name);
    }

    private static OpenAiToolCall Call(string name)
        => new() { Id = $"call_{name}", Function = new OpenAiFunctionCall { Name = name, Arguments = "{}" } };

    private sealed class FakeExecutor(string? handled = null, string? failed = null) : IQuadBrainInternalToolExecutor
    {
        public Task<InternalToolExecutionOutcome> TryExecuteAsync(
            OpenAiToolCall toolCall,
            string? turnId,
            CancellationToken cancellationToken = default)
        {
            if (failed is not null && toolCall.Function.Name == failed)
                return Task.FromResult(InternalToolExecutionOutcome.Fail("boom"));
            if (handled is not null && toolCall.Function.Name == handled)
                return Task.FromResult(InternalToolExecutionOutcome.Ok("{\"ok\":true}"));
            return Task.FromResult(InternalToolExecutionOutcome.Unhandled);
        }
    }
}
