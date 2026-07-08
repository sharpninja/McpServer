using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-009: Validates Agent Help corpus bootstrap loads pinned workspace documents.
/// </summary>
public sealed class AgentHelpCorpusServiceTests
{
    /// <summary>
    /// TEST-MCP-HELP-009: BootstrapAsync loads pinned docs and returns non-stub context for a real workspace fixture.
    /// </summary>
    [Fact]
    public async Task BootstrapAsync_LoadsPinnedDocsAndBuildsContextPack()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "docs", "context"));
        File.WriteAllText(
            Path.Combine(workspaceRoot, "AGENTS.md"),
            "# Agents\nUse workflow.todo.update to mark TODOs done.");
        File.WriteAllText(
            Path.Combine(workspaceRoot, "docs", "context", "todo-schema.md"),
            "# TODO schema\nSet done: true and include doneSummary.");

        var options = new AgentHelpOptions
        {
            CorpusBootstrapEnabled = true,
            MaxContextCharacters = 8000,
            ContextSearchChunkLimit = 4,
            PinnedPaths =
            [
                "workspace:AGENTS.md",
                "workspace:docs/context/todo-schema.md",
            ],
        };
        var monitor = new AgentHelpTestOptionsMonitor<AgentHelpOptions>(options);
        var service = new AgentHelpCorpusService(
            monitor,
            new HttpContextAccessor(),
            AgentHelpPinnedPathResolverTestFactory.Create(),
            NullLogger<AgentHelpCorpusService>.Instance);

        var summary = await service.BootstrapAsync(workspaceRoot, "MCP TODO workflow", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(summary.ChunkCount >= 1);
        Assert.Contains("workflow.todo.update", summary.ContextPackText, StringComparison.Ordinal);
        Assert.Contains("workspace:AGENTS.md", summary.SourceKeys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Stub context pack", summary.Summary, StringComparison.Ordinal);
    }
}