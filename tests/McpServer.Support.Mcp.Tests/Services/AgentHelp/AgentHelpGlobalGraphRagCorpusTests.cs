using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-GRAPHRAG-GLOBAL-001: Validates Agent Help prefers global GraphRAG excerpts before pinned filesystem paths.
/// </summary>
public sealed class AgentHelpGlobalGraphRagCorpusTests
{
    /// <summary>
    /// TEST-MCP-GRAPHRAG-GLOBAL-001: BootstrapAsync merges global GraphRAG excerpts ahead of workspace pinned docs.
    /// </summary>
    [Fact]
    public async Task BootstrapAsync_PrefersGlobalGraphRagExcerpts()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        File.WriteAllText(Path.Combine(workspaceRoot, "AGENTS.md"), "# Agents\nLocal workspace guidance only.");

        var globalSource = Substitute.For<IGlobalGraphRagCorpusSource>();
        globalSource
            .QueryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new GlobalGraphRagCorpusExcerpt("global:docs/context/todo-schema.md", "Use workflow.todo.update with doneSummary."),
            ]);

        var options = new AgentHelpOptions
        {
            CorpusBootstrapEnabled = true,
            PreferGlobalGraphRag = true,
            MaxContextCharacters = 8000,
            ContextSearchChunkLimit = 4,
            PinnedPaths = ["workspace:AGENTS.md"],
        };
        var monitor = new AgentHelpTestOptionsMonitor<AgentHelpOptions>(options);
        var service = new AgentHelpCorpusService(
            monitor,
            new HttpContextAccessor(),
            AgentHelpPinnedPathResolverTestFactory.Create(),
            NullLogger<AgentHelpCorpusService>.Instance,
            globalSource);

        var summary = await service.BootstrapAsync(
            workspaceRoot,
            "MCP TODO workflow",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("workflow.todo.update", summary.ContextPackText, StringComparison.Ordinal);
        Assert.Contains("global:docs/context/todo-schema.md", summary.SourceKeys, StringComparer.OrdinalIgnoreCase);
        await globalSource.Received(1).QueryAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}