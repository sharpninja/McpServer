using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: verifies the production triage research prompt template
/// renders the group JSON contract passed to the configured direct triage agent.
/// </summary>
public sealed class TriagePromptTemplateTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: the triage research template exists in the repository
    /// template file and renders with the required group JSON variable.
    /// </summary>
    [Fact]
    public async Task TriageResearchBugReportTemplate_RendersGroupJsonContract()
    {
        using var sut = new PromptTemplateService(
            Microsoft.Extensions.Options.Options.Create(new TemplateStorageOptions
            {
                FilePath = Path.Combine(FindRepositoryRoot(), "templates", "prompt-templates.yaml"),
            }),
            new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance),
            NullLogger<PromptTemplateService>.Instance);

        var result = await sut.TestAsync(
            "triage-research-bug-report",
            new PromptTemplateTestRequest
            {
                Variables = new Dictionary<string, object?>
                {
                    ["groupJson"] = """{"groupId":"triage-group-001","reports":[{"title":"bug"}]}""",
                    ["groupId"] = "triage-group-001",
                    ["workspacePath"] = @"F:\GitHub\McpServer",
                    ["reportCount"] = 1,
                },
            }).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        Assert.Contains("schema-valid JSON", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("triage-group-001", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains(@"""reports""", result.RenderedContent, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing McpServer.sln.");
    }
}
