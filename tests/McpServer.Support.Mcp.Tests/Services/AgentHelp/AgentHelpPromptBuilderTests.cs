using McpServer.Support.Mcp.Services.AgentHelp;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-008: Validates Agent Help system prompt construction and caller linkage formatting.
/// </summary>
public sealed class AgentHelpPromptBuilderTests
{
    /// <summary>
    /// TEST-MCP-HELP-008: BuildSystemPrompt includes caller linkage, topic, issue summary, and context excerpts.
    /// </summary>
    [Fact]
    public void BuildSystemPrompt_IncludesCallerTopicAndContext()
    {
        var context = new AgentHelpPromptContext
        {
            WorkspacePath = @"F:\GitHub\McpServer",
            Topic = "MCP TODO workflow",
            CallerAgent = "GrokCode",
            CallerSessionId = "GrokCode-20260708T165206Z-mcpserver-session",
            CallerRequestId = "req-20260708T190000Z-001",
            IssueSummary = "Need steps to mark PLAN-AGENTHELP-001 done.",
            TodoId = "PLAN-AGENTHELP-001",
            ContextPackText = "### docs/context/todo-schema.md\nUse workflow.todo.update with done: true.",
            SourceKeys = ["docs/context/todo-schema.md"],
        };

        var prompt = AgentHelpPromptBuilder.BuildSystemPrompt(context);

        Assert.Contains("GrokCode", prompt, StringComparison.Ordinal);
        Assert.Contains("PLAN-AGENTHELP-001", prompt, StringComparison.Ordinal);
        Assert.Contains("workflow.todo.update", prompt, StringComparison.Ordinal);
        Assert.Contains("MCP TODO workflow", prompt, StringComparison.Ordinal);
        Assert.Contains("docs/context/todo-schema.md", prompt, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-HELP-008: BuildTurnPrompt composes system prompt and user message without duplication.
    /// </summary>
    [Fact]
    public void BuildTurnPrompt_CombinesSystemAndUserMessage()
    {
        var context = new AgentHelpPromptContext
        {
            WorkspacePath = @"F:\GitHub\McpServer",
            Topic = "general",
            ContextPackText = "Marker guidance.",
        };

        var turnPrompt = AgentHelpPromptBuilder.BuildTurnPrompt(
            context,
            "How do I mark a TODO done?");

        Assert.StartsWith("SYSTEM:", turnPrompt, StringComparison.Ordinal);
        Assert.Contains("USER:", turnPrompt, StringComparison.Ordinal);
        Assert.Contains("How do I mark a TODO done?", turnPrompt, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-HELP-008: SynthesizeEchoResponse returns actionable guidance from loaded context.
    /// </summary>
    [Fact]
    public void SynthesizeEchoResponse_UsesContextPackForGuidance()
    {
        var context = new AgentHelpPromptContext
        {
            WorkspacePath = @"F:\GitHub\McpServer",
            Topic = "MCP TODO workflow",
            ContextPackText = "### docs/context/todo-schema.md\nMark complete with workflow.todo.update and doneSummary.",
            SourceKeys = ["docs/context/todo-schema.md"],
        };

        var response = AgentHelpPromptBuilder.SynthesizeEchoResponse(
            context,
            "How do I mark PLAN-AGENTHELP-001 done with the Grok plugin?");

        Assert.Contains("workflow.todo.update", response, StringComparison.Ordinal);
        Assert.Contains("docs/context/todo-schema.md", response, StringComparison.Ordinal);
        Assert.DoesNotContain("You said:", response, StringComparison.Ordinal);
    }
}