using McpServer.Cqrs.Mvvm;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-003 / TEST-HANDOFF-006: handoff DI remains constructible without an agent pool
/// and Director exec delegates to the shared service.
/// </summary>
public sealed class HandoffServiceRegistrationTests
{
    /// <summary>
    /// TEST-HANDOFF-003: AddHandoffServices does not require IAgentPoolService to resolve the extractor.
    /// </summary>
    [Fact]
    public async Task AddHandoffServices_WithoutAgentPool_ResolvesUnavailableExtractor()
    {
        var services = new ServiceCollection();
        services.AddHandoffServices();
        await using var provider = services.BuildServiceProvider();

        var extractor = provider.GetRequiredService<IHandoffOneShotExtractor>();
        var result = await extractor.ExtractAsync(
            @"F:\GitHub\McpServer",
            "handoff text",
            agentName: null,
            promptTemplateId: null,
            TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Agent pool is not registered", result.Error, StringComparison.Ordinal);
        Assert.DoesNotContain("handoff text", result.Error, StringComparison.Ordinal);
    }

    /// <summary>TEST-HANDOFF-006: Director ingest command executes through IHandoffDirectorExecutor.</summary>
    [Fact]
    public async Task HandoffIngestDirectorCommand_PrimaryCommand_DelegatesToExecutor()
    {
        var executor = Substitute.For<IHandoffDirectorExecutor>();
        executor.IngestAsync("Content", null, "handoff", null, "DraftOnly", false, null, null, Arg.Any<CancellationToken>())
            .Returns(new HandoffIngestionResult { Success = true });
        var command = new HandoffIngestDirectorCommand(executor)
        {
            SourceKind = "Content",
            Content = "handoff",
            Mode = "DraftOnly",
        };

        await command.PrimaryCommand.ExecuteAsync(null);

        var result = Assert.IsType<HandoffIngestionResult>(command.Result);
        Assert.True(result.Success);
        await executor.Received(1).IngestAsync(
            "Content",
            null,
            "handoff",
            null,
            "DraftOnly",
            false,
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
