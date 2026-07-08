using McpServer.Support.Mcp.Services.AgentHelp;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-002: Deterministic inbound guard tests with fixture corpora.
/// </summary>
public sealed class AgentHelpInboundGuardTests
{
    private readonly AgentHelpInboundGuard _guard = new();

    [Theory]
    [InlineData("injection/ignore-previous-instructions.txt", "injection.ignore-instructions")]
    [InlineData("injection/api-key-exfiltration.txt", "injection.api-key-exfiltration")]
    [InlineData("injection/write-todo-yaml.txt", "injection.write-todo-yaml")]
    [InlineData("injection/disable-guardrails.txt", "injection.disable-guardrails")]
    public void Evaluate_BlocksInjectionFixtures(string fixturePath, string expectedRuleId)
    {
        var message = ReadFixture(fixturePath);
        var result = _guard.Evaluate(message);

        Assert.False(result.Allowed);
        Assert.Equal(expectedRuleId, result.RuleId);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Theory]
    [InlineData("bypass/mcp-tool-failure-description.txt")]
    [InlineData("bypass/benign-todo-yaml-question.txt")]
    [InlineData("bypass/normal-help-request.txt")]
    public void Evaluate_AllowsBenignBypassFixtures(string fixturePath)
    {
        var message = ReadFixture(fixturePath);
        var result = _guard.Evaluate(message);

        Assert.True(result.Allowed);
        Assert.Null(result.RuleId);
    }

    [Fact]
    public void Evaluate_BlocksEmptyMessage()
    {
        var result = _guard.Evaluate("   ");

        Assert.False(result.Allowed);
        Assert.Equal("validation.empty-message", result.RuleId);
    }

    [Fact]
    public void Evaluate_AllowsMcpFailureDescription_EvenWhenWriteTodoMentionedInContext()
    {
        var message = "The MCP tool failed while calling todo_list. Describe why the MCP tool failed and suggest next steps.";
        var result = _guard.Evaluate(message);

        Assert.True(result.Allowed);
    }

    private static string ReadFixture(string relativePath)
        => File.ReadAllText(AgentHelpTestPaths.ResolveFixturePath(relativePath)).Trim();
}