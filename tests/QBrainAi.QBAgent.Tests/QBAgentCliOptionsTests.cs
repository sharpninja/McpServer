using QBrainAi.QBAgent;
using Microsoft.Extensions.AI;

namespace QBrainAi.QBAgent.Tests;

public sealed class QBAgentCliOptionsTests
{
    [Fact]
    public void ParseDefaultHidesIntent()
    {
        var options = QBAgentCliOptions.Parse([], environment: new Dictionary<string, string?>());
        Assert.False(options.ShowIntent);
        Assert.Null(options.StartDirectory);
    }

    [Fact]
    public void ParseShowIntentFlagOverridesHideEnv()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ShowIntentFlag, @"Q:\__mcp_unit_test__\McpServerManager"],
            environment: new Dictionary<string, string?>
            {
                [QBAgentCliOptions.HideIntentEnv] = "1",
            });
        Assert.True(options.ShowIntent);
        Assert.Equal(@"Q:\__mcp_unit_test__\McpServerManager", options.StartDirectory);
    }

    [Fact]
    public void ParseHideIntentFlagOverridesShowEnv()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.HideIntentFlag],
            environment: new Dictionary<string, string?>
            {
                [QBAgentCliOptions.ShowIntentEnv] = "1",
            });
        Assert.False(options.ShowIntent);
    }

    [Fact]
    public void ParseShowIntentEnvWithoutFlagsShowsIntent()
    {
        var options = QBAgentCliOptions.Parse(
            [],
            environment: new Dictionary<string, string?>
            {
                [QBAgentCliOptions.ShowIntentEnv] = "true",
            });
        Assert.True(options.ShowIntent);
    }

    [Fact]
    public void ParseResumeWithoutValueIsLatest()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag],
            environment: new Dictionary<string, string?>());
        Assert.Equal(QBAgentCliOptions.ResumeLatest, options.ResumeSessionId);
    }

    [Fact]
    public void ParseResumeWithSessionIdAndDirectory()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag, "qbagent-20260911T150405123Z", @"Q:\__mcp_unit_test__\McpServerManager"],
            environment: new Dictionary<string, string?>());
        Assert.Equal("qbagent-20260911T150405123Z", options.ResumeSessionId);
        Assert.Equal(@"Q:\__mcp_unit_test__\McpServerManager", options.StartDirectory);
    }

    [Fact]
    public void ParseResumeThenShowIntentDoesNotTreatFlagAsSessionId()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag, QBAgentCliOptions.ShowIntentFlag],
            environment: new Dictionary<string, string?>());
        Assert.Equal(QBAgentCliOptions.ResumeLatest, options.ResumeSessionId);
        Assert.True(options.ShowIntent);
    }

    [Fact]
    public void IntentDisplayHidesJsonEnvelope()
    {
        var json = """{"intent":"final","content":"visible"}""";
        Assert.Equal("visible", QBAgentIntentDisplay.FormatForDisplay(json, showIntent: false));
        var shown = QBAgentIntentDisplay.FormatForDisplay(json, showIntent: true);
        Assert.StartsWith("intent: final", shown, StringComparison.Ordinal);
        Assert.Contains(json, shown, StringComparison.Ordinal);
    }

    [Fact]
    public void HasPendingToolHandbackIgnoresHistoricalToolResultsAfterUserTurn()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, [new FunctionCallContent("call_0", "read_file")]),
            new(ChatRole.Tool, [new FunctionResultContent("call_0", "file body")]),
            new(ChatRole.Assistant, "done"),
            new(ChatRole.User, "do it"),
        };
        Assert.False(QBAgentProgressChatClient.HasPendingToolHandback(messages));
    }

    [Fact]
    public void HasPendingToolHandbackTrueWhenNewestMessageIsToolResult()
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, [new FunctionCallContent("call_0", "read_file")]),
            new(ChatRole.Tool, [new FunctionResultContent("call_0", "file body")]),
        };
        Assert.True(QBAgentProgressChatClient.HasPendingToolHandback(messages));
    }

    [Fact]
    public void IntentDisplayShowIntentPrefixesMissingWhenNoEnvelope()
    {
        var shown = QBAgentIntentDisplay.FormatForDisplay("plain prose", showIntent: true);
        Assert.StartsWith("intent: missing", shown, StringComparison.Ordinal);
        Assert.Contains("plain prose", shown, StringComparison.Ordinal);
    }
}
