using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="MarkerFileService.ResolvePrompt"/> template resolution.</summary>
public sealed class MarkerFileServiceTests
{
    private const string BaseUrl = "http://localhost:7148";

    [Fact]
    public void ResolvePrompt_NullGlobal_NullWorkspace_ReturnsDefault()
    {
        var result = MarkerFileService.ResolvePrompt(BaseUrl, null, null);

        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
        Assert.Contains($"GET {BaseUrl}/health", result);
        Assert.Contains($"{BaseUrl}/mcp-transport", result);
    }

    [Fact]
    public void ResolvePrompt_EmptyGlobal_ReturnsDefault()
    {
        var result = MarkerFileService.ResolvePrompt(BaseUrl, "  ", null);

        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
    }

    [Fact]
    public void ResolvePrompt_CustomGlobal_ReplacesDefault()
    {
        var template = "Custom prompt for {baseUrl} with special instructions.";

        var result = MarkerFileService.ResolvePrompt(BaseUrl, template, null);

        Assert.Equal($"Custom prompt for {BaseUrl} with special instructions.", result);
        Assert.DoesNotContain("MCP Context Server", result);
    }

    [Fact]
    public void ResolvePrompt_WorkspaceAppends()
    {
        var workspaceTemplate = "This workspace uses Python. Prefer pytest for testing.";

        var result = MarkerFileService.ResolvePrompt(BaseUrl, null, workspaceTemplate);

        // Should contain both the default prompt and the workspace prompt
        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
        Assert.Contains("This workspace uses Python", result);
    }

    [Fact]
    public void ResolvePrompt_WorkspaceBaseUrlSubstitution()
    {
        var workspaceTemplate = "Dev server at {baseUrl}/api. Use {baseUrl}/docs for API docs.";

        var result = MarkerFileService.ResolvePrompt(BaseUrl, null, workspaceTemplate);

        Assert.Contains($"Dev server at {BaseUrl}/api", result);
        Assert.Contains($"Use {BaseUrl}/docs for API docs", result);
    }

    [Fact]
    public void ResolvePrompt_BothCustom_CombinesWithNewlines()
    {
        var global = "Global: server at {baseUrl}";
        var workspace = "Workspace: extra config for {baseUrl}";

        var result = MarkerFileService.ResolvePrompt(BaseUrl, global, workspace);

        Assert.Equal($"Global: server at {BaseUrl}\n\nWorkspace: extra config for {BaseUrl}", result);
    }

    [Fact]
    public void ResolvePrompt_EmptyWorkspace_NotAppended()
    {
        var result = MarkerFileService.ResolvePrompt(BaseUrl, null, "   ");

        // Should be the default prompt only, no trailing separator
        Assert.DoesNotContain("\n\n   ", result);
        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsBaseUrlPlaceholder()
    {
        Assert.Contains("{baseUrl}", MarkerFileService.DefaultPromptTemplate);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsAllCapabilitySections()
    {
        var template = MarkerFileService.DefaultPromptTemplate;
        Assert.Contains("## Server Health", template);
        Assert.Contains("## API Discovery", template);
        Assert.Contains("## Session Logging", template);
        Assert.Contains("## Available Capabilities", template);
        Assert.Contains("Context Search", template);
        Assert.Contains("Todo Management", template);
        Assert.Contains("MCP Protocol", template);
    }
}
