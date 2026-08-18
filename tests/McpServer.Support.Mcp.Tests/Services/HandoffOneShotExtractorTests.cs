using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-003: consumer tests for versioned HandoffTodoDraft one-shot extraction.
/// </summary>
public sealed class HandoffOneShotExtractorTests
{
    /// <summary>TEST-HANDOFF-003: extractor invokes AgentPoolOneShotContext.HandoffTodoDraft.</summary>
    [Fact]
    public async Task ExtractAsync_UsesHandoffTodoDraftContextAndVersionedPrompt()
    {
        AgentPoolOneShotRequest? captured = null;
        var pool = Substitute.For<IAgentPoolService>();
        pool.EnqueueOneShotAsync(Arg.Any<AgentPoolOneShotRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<AgentPoolOneShotRequest>();
                return new AgentPoolEnqueueResult { Success = true, JobId = "job-handoff-1", AgentName = "plan-agent" };
            });
        pool.SubscribeJobStreamAsync("job-handoff-1", Arg.Any<CancellationToken>())
            .Returns(CompletedStream("job-handoff-1", """{"id":"MCP-HANDOFFDEMO-019"}"""));
        var sut = new HandoffOneShotExtractor(pool);

        var result = await sut.ExtractAsync(
            @"F:\GitHub\McpServer",
            "handoff text",
            agentName: "plan-agent",
            promptTemplateId: HandoffPromptDefaults.TemplateId,
            TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.Equal(AgentPoolOneShotContext.HandoffTodoDraft, captured.Context);
        Assert.Equal(HandoffPromptDefaults.TemplateId, captured.PromptTemplateId);
        Assert.Equal(HandoffPromptDefaults.PromptVersion, result.PromptVersion);
        Assert.Contains("handoffText", captured.Values!.Keys);
    }

    private static async IAsyncEnumerable<AgentPoolJobStreamEventDto> CompletedStream(string jobId, string text)
    {
        yield return new AgentPoolJobStreamEventDto
        {
            JobId = jobId,
            EventType = "completed",
            Status = "completed",
            Text = text,
        };
        await Task.CompletedTask;
    }
}
