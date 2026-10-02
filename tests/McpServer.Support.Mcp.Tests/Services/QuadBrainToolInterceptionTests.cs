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

    /// <summary>FR-MCP-QBEXEC-001: unhandled internals are notes, never agent tool commands. External calls remain.</summary>
    [Fact]
    public async Task Interceptor_UnhandledInternal_IsNoteNotAgentCommand()
    {
        var interceptor = new QuadBrainToolInterceptor(
            new QuadBrainToolClassifier(),
            new FakeExecutor(handled: "mcp_todo_update"));
        var calls = new[]
        {
            Call("mcp_todo_update"),
            Call("mcp_unknown_tool"),
            Call("do_local_thing"),
        };

        var result = await interceptor.InterceptAsync(calls, turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("mcp_todo_update", Assert.Single(result.Executed).ToolCall.Function.Name);
        Assert.Equal("do_local_thing", Assert.Single(result.RemainingToolCalls).Function.Name);
        Assert.Equal("mcp_unknown_tool", Assert.Single(result.Failed).ToolCall.Function.Name);
    }

    /// <summary>TEST-MCP-QBEXEC-001 AC-4: no catalog mcp_* name remains in RemainingToolCalls (real executor).</summary>
    [Theory]
    [MemberData(nameof(CatalogToolNames))]
    public async Task Interceptor_CatalogName_IsNotEmittedToAgent(string name)
    {
        var fixture = new QuadBrainExecutorTestFixture();
        var executor = fixture.CreateExecutor();
        var interceptor = new QuadBrainToolInterceptor(new QuadBrainToolClassifier(), executor);
        var arguments = await QuadBrainExecutorTestFixture.CatalogArgumentsAsync(executor, name).ConfigureAwait(true);

        var result = await interceptor.InterceptAsync(
            [QuadBrainExecutorTestFixture.Call(name, arguments), Call("do_local_thing")],
            turnId: null,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(result.RemainingToolCalls, call => string.Equals(call.Function.Name, name, StringComparison.Ordinal));
        Assert.Equal("do_local_thing", Assert.Single(result.RemainingToolCalls).Function.Name);
        Assert.Empty(result.Failed);
        Assert.Equal(name, Assert.Single(result.Executed).ToolCall.Function.Name);
    }

    public static TheoryData<string> CatalogToolNames()
    {
        var data = new TheoryData<string>();
        foreach (var catalogName in QuadBrainMcpToolCatalog.All)
            data.Add(catalogName);
        return data;
    }

    /// <summary>A handled-but-failed internal tool is reported as a failure, not emitted to the agent.</summary>
    [Fact]
    public async Task Interceptor_FailedInternal_IsReportedNotEmitted()
    {
        var interceptor = new QuadBrainToolInterceptor(
            new QuadBrainToolClassifier(),
            new FakeExecutor(failed: "mcp_todo_update"));

        var result = await interceptor.InterceptAsync([Call("mcp_todo_update")], turnId: null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
