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
    /// TEST-MCP-MARKER-TRIAGE-001: the production marker prompt tells agents to
    /// write failsafe YAML for MCP Server and plugin failures, submit triage,
    /// and stop when triage submission fails.
    /// </summary>
    [Fact]
    public async Task DefaultMarkerPromptTemplate_RendersMcpAndPluginFailureTriageGuidance()
    {
        using var sut = new PromptTemplateService(
            Microsoft.Extensions.Options.Options.Create(new TemplateStorageOptions
            {
                FilePath = Path.Combine(FindRepositoryRoot(), "templates", "prompt-templates.yaml"),
            }),
            new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance),
            NullLogger<PromptTemplateService>.Instance);

        var result = await sut.TestAsync(
            "default-marker-prompt",
            new PromptTemplateTestRequest
            {
                Variables = MarkerFileService.BuildTemplateContext(
                    "http://localhost:7147",
                    "test-token",
                    workspace: null,
                    workspacePath: @"F:\GitHub\McpServer",
                    workspaceName: "McpServer"),
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        Assert.Contains("## MCP and Plugin Failure Reporting", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains(
            "MCP Server failures and required-plugin failures discovered while working must always be written as a normal failsafe YAML report.",
            result.RenderedContent,
            StringComparison.Ordinal);
        Assert.Contains("After the failsafe YAML is written and triage submission succeeds, continue the user's active task", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not wait for triage research, TODO creation, or resolution.", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("separate repair workflow", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("needed changes, acceptance criteria, and validation evidence", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not create TODOs, requirements, GitHub issues, manual repair plans, or alternate reports", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("plugin/REPL failsafe or pending YAML queue", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("If triage submission fails, stop work and notify the user.", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("workflow.triage.report", result.RenderedContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-PLUGIN-PSONLY-001: the production marker prompt tells agents that
    /// deprecated workflow metadata and empty history are not reasons to bypass the plugin.
    /// </summary>
    [Fact]
    public async Task DefaultMarkerPromptTemplate_RendersSessionLogWrapperRecoveryGuidance()
    {
        using var sut = new PromptTemplateService(
            Microsoft.Extensions.Options.Options.Create(new TemplateStorageOptions
            {
                FilePath = Path.Combine(FindRepositoryRoot(), "templates", "prompt-templates.yaml"),
            }),
            new PromptTemplateRenderer(NullLogger<PromptTemplateRenderer>.Instance),
            NullLogger<PromptTemplateService>.Instance);

        var result = await sut.TestAsync(
            "default-marker-prompt",
            new PromptTemplateTestRequest
            {
                Variables = MarkerFileService.BuildTemplateContext(
                    "http://localhost:7147",
                    "test-token",
                    workspace: null,
                    workspacePath: @"F:\GitHub\McpServer",
                    workspaceName: "McpServer"),
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        Assert.Contains("deprecated: true", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("metadata, not wrapper failure", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("empty `workflow.sessionlog.queryHistory` result as a valid no-match result", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("workspace current directory", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("do not use raw REST merely because history is empty or marked deprecated", result.RenderedContent, StringComparison.Ordinal);
    }

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
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success, result.Error);
        Assert.Contains("schema-valid JSON", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("triage-group-001", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains(@"""reports""", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Background triage is not a normal workspace session", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not read AGENTS-README-FIRST.yaml", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not start or update MCP session logs", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not create or update TODOs, requirements, GitHub issues, branches, commits, or MCP state", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Use the supplied Group JSON as the primary source", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Do not run broad recursive repository searches", result.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("Return only the JSON object as soon as you can make a defensible triage determination", result.RenderedContent, StringComparison.Ordinal);
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
