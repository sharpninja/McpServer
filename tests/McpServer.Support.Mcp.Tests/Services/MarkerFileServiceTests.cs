using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for <see cref="MarkerFileService.ResolvePrompt"/> template resolution.</summary>
public sealed class MarkerFileServiceTests
{
    private const string BaseUrl = "http://localhost:7147";

    private static Dictionary<string, object?> MakeContext(string baseUrl = BaseUrl, string? apiKey = null) =>
        MarkerFileService.BuildTemplateContext(baseUrl, apiKey, workspace: null, workspacePath: @"C:\test", workspaceName: "test");

    [Fact]
    public void ResolvePrompt_NullGlobal_NullWorkspace_ReturnsDefault()
    {
        var result = MarkerFileService.ResolvePrompt(MakeContext(), null, null);

        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
        Assert.Contains($"GET {BaseUrl}/health", result);
        Assert.Contains($"{BaseUrl}/mcp-transport", result);
    }

    [Fact]
    public void ResolvePrompt_EmptyGlobal_ReturnsDefault()
    {
        var result = MarkerFileService.ResolvePrompt(MakeContext(), "  ", null);

        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
    }

    [Fact]
    public void ResolvePrompt_CustomGlobal_ReplacesDefault()
    {
        var template = "Custom prompt for {{baseUrl}} with special instructions.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), template, null);

        Assert.Equal($"Custom prompt for {BaseUrl} with special instructions.", result);
        Assert.DoesNotContain("MCP Context Server", result);
    }

    [Fact]
    public void ResolvePrompt_WorkspaceAppends()
    {
        var workspaceTemplate = "This workspace uses Python. Prefer pytest for testing.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), null, workspaceTemplate);

        // Should contain both the default prompt and the workspace prompt
        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
        Assert.Contains("This workspace uses Python", result);
    }

    [Fact]
    public void ResolvePrompt_WorkspaceBaseUrlSubstitution()
    {
        var workspaceTemplate = "Dev server at {{baseUrl}}/api. Use {{baseUrl}}/docs for API docs.";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), null, workspaceTemplate);

        Assert.Contains($"Dev server at {BaseUrl}/api", result);
        Assert.Contains($"Use {BaseUrl}/docs for API docs", result);
    }

    [Fact]
    public void ResolvePrompt_BothCustom_CombinesWithNewlines()
    {
        var global = "Global: server at {{baseUrl}}";
        var workspace = "Workspace: extra config for {{baseUrl}}";

        var result = MarkerFileService.ResolvePrompt(MakeContext(), global, workspace);

        Assert.Equal($"Global: server at {BaseUrl}\n\nWorkspace: extra config for {BaseUrl}", result);
    }

    [Fact]
    public void ResolvePrompt_EmptyWorkspace_NotAppended()
    {
        var result = MarkerFileService.ResolvePrompt(MakeContext(), null, "   ");

        // Should be the default prompt only, no trailing separator
        Assert.DoesNotContain("\n\n   ", result);
        Assert.Contains($"MCP Context Server at {BaseUrl}", result);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsBaseUrlPlaceholder()
    {
        Assert.Contains("{{baseUrl}}", MarkerFileService.DefaultPromptTemplate);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsAllCapabilitySections()
    {
        var template = MarkerFileService.DefaultPromptTemplate;
        Assert.Contains("## Rules", template);
        Assert.Contains("## Where Things Live", template);
        Assert.Contains("## Context Loading by Task Type", template);
        Assert.Contains("## Protocols", template);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsContextLoadingReferences()
    {
        var template = MarkerFileService.DefaultPromptTemplate;
        Assert.Contains("docs/context/", template);
        Assert.Contains("session-log-schema.md", template);
        Assert.Contains("todo-schema.md", template);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsWorkspace()
    {
        var template = MarkerFileService.DefaultPromptTemplate;
        Assert.Contains("## Workspace", template);
        Assert.Contains("{{workspace.Name}}", template);
    }

    [Fact]
    public void DefaultPromptTemplate_ContainsAuthSection()
    {
        var template = MarkerFileService.DefaultPromptTemplate;
        Assert.Contains("## Authentication", template);
        Assert.Contains("{{apiKey}}", template);
        Assert.Contains("X-Api-Key", template);
    }

    [Fact]
    public void BuildTemplateContext_WithWorkspaceDto_IncludesAllProperties()
    {
        var ws = new WorkspaceDto
        {
            Name = "MyProject",
            WorkspacePath = @"C:\projects\my",
            TodoPath = @"C:\projects\my\todo.yaml",
            DataDirectory = @"C:\data\my",
            TunnelProvider = "cloudflare",
            IsPrimary = true,
            IsEnabled = true,
            DateTimeCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateTimeModified = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RunAs = "admin",
            PromptTemplate = "custom template",
            StatusPrompt = TodoPromptDefaults.StatusPrompt,
            ImplementPrompt = TodoPromptDefaults.ImplementPrompt,
            PlanPrompt = TodoPromptDefaults.PlanPrompt,
        };

        var ctx = MarkerFileService.BuildTemplateContext("http://localhost:7200", "tok123", ws, ws.WorkspacePath, ws.Name);

        Assert.Equal("http://localhost:7200", ctx["baseUrl"]);
        Assert.Equal("tok123", ctx["apiKey"]);
        var wsDict = Assert.IsType<Dictionary<string, object?>>(ctx["workspace"]);
        Assert.Equal("MyProject", wsDict["Name"]);
        Assert.Equal(true, wsDict["IsPrimary"]);
        Assert.Equal("cloudflare", wsDict["TunnelProvider"]);
    }

    [Fact]
    public void BuildTemplateContext_NullWorkspace_UsesFallbacks()
    {
        var ctx = MarkerFileService.BuildTemplateContext("http://localhost:7147", null, null, @"C:\ws", "fallback");

        Assert.Equal(string.Empty, ctx["apiKey"]);
        var wsDict = Assert.IsType<Dictionary<string, object?>>(ctx["workspace"]);
        Assert.Equal("fallback", wsDict["Name"]);
        Assert.Equal(@"C:\ws", wsDict["WorkspacePath"]);
        Assert.Equal(false, wsDict["IsPrimary"]);
    }

    [Fact]
    public void ResolvePrompt_HandlebarsRendersWorkspaceProperties()
    {
        var ws = new WorkspaceDto
        {
            Name = "TestProj",
            WorkspacePath = @"C:\test",
            TodoPath = @"C:\test\todo.yaml",
            DataDirectory = @"C:\test",
            TunnelProvider = null,
            IsPrimary = false,
            IsEnabled = true,
            DateTimeCreated = DateTime.UtcNow,
            DateTimeModified = DateTime.UtcNow,
            RunAs = null,
            PromptTemplate = null,
            StatusPrompt = TodoPromptDefaults.StatusPrompt,
            ImplementPrompt = TodoPromptDefaults.ImplementPrompt,
            PlanPrompt = TodoPromptDefaults.PlanPrompt,
        };

        var ctx = MarkerFileService.BuildTemplateContext(BaseUrl, "mytoken", ws, ws.WorkspacePath, ws.Name);
        var result = MarkerFileService.ResolvePrompt(ctx, null, null);

        Assert.Contains("TestProj", result);
        Assert.Contains("mytoken", result);
    }

    [Fact]
    public async Task WriteMarkerAsync_EmitsUtcTimestampsAndDiagnosticsEndpoints()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mcp-marker-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var serverStartedAtUtc = new DateTimeOffset(2026, 2, 26, 8, 30, 0, TimeSpan.Zero);

            await MarkerFileService.WriteMarkerAsync(
                workspacePath: tempDir,
                port: 7147,
                workspaceName: "test",
                serverStartedAtUtc: serverStartedAtUtc);

            var markerPath = Path.Combine(tempDir, MarkerFileService.MarkerFileName);
            var yaml = await File.ReadAllTextAsync(markerPath);

            Assert.Contains("markerWrittenAtUtc:", yaml);
            Assert.Contains($"serverStartedAtUtc: {serverStartedAtUtc:o}", yaml);
            Assert.Contains("serverStartupUtc: /server-startup-utc", yaml);
            Assert.Contains("markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}", yaml);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup for temp test directory.
            }
        }
    }
}
