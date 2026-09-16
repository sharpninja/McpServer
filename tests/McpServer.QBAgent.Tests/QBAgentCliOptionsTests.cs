using McpServer.QBAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tests;

public sealed class QBAgentCliOptionsTests
{
    [Fact]
    public void Parse_Default_HidesIntent()
    {
        var options = QBAgentCliOptions.Parse([], environment: new Dictionary<string, string?>());
        Assert.False(options.ShowIntent);
        Assert.Null(options.StartDirectory);
    }

    [Fact]
    public void Parse_ShowIntentFlag_OverridesHideEnv()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ShowIntentFlag, @"F:\GitHub\McpServerManager"],
            environment: new Dictionary<string, string?>
            {
                [QBAgentCliOptions.HideIntentEnv] = "1",
            });
        Assert.True(options.ShowIntent);
        Assert.Equal(@"F:\GitHub\McpServerManager", options.StartDirectory);
    }

    [Fact]
    public void Parse_HideIntentFlag_OverridesShowEnv()
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
    public void Parse_ShowIntentEnv_WithoutFlags_ShowsIntent()
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
    public void Parse_ResumeWithoutValue_IsLatest()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag],
            environment: new Dictionary<string, string?>());
        Assert.Equal(QBAgentCliOptions.ResumeLatest, options.ResumeSessionId);
    }

    [Fact]
    public void Parse_ResumeWithSessionId_AndDirectory()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag, "qbagent-20260911T150405123Z", @"F:\GitHub\McpServerManager"],
            environment: new Dictionary<string, string?>());
        Assert.Equal("qbagent-20260911T150405123Z", options.ResumeSessionId);
        Assert.Equal(@"F:\GitHub\McpServerManager", options.StartDirectory);
    }

    [Fact]
    public void Parse_ResumeThenShowIntent_DoesNotTreatFlagAsSessionId()
    {
        var options = QBAgentCliOptions.Parse(
            [QBAgentCliOptions.ResumeFlag, QBAgentCliOptions.ShowIntentFlag],
            environment: new Dictionary<string, string?>());
        Assert.Equal(QBAgentCliOptions.ResumeLatest, options.ResumeSessionId);
        Assert.True(options.ShowIntent);
    }

    [Fact]
    public void IntentDisplay_HidesJsonEnvelope()
    {
        var json = """{"intent":"final","content":"visible"}""";
        Assert.Equal("visible", QBAgentIntentDisplay.FormatForDisplay(json, showIntent: false));
        var shown = QBAgentIntentDisplay.FormatForDisplay(json, showIntent: true);
        Assert.StartsWith("intent: final", shown, StringComparison.Ordinal);
        Assert.Contains(json, shown, StringComparison.Ordinal);
    }

    [Fact]
    public void HasPendingToolHandback_IgnoresHistoricalToolResultsAfterUserTurn()
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
    public void HasPendingToolHandback_TrueWhenNewestMessageIsToolResult()
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
    public void IntentDisplay_ShowIntent_PrefixesMissingWhenNoEnvelope()
    {
        var shown = QBAgentIntentDisplay.FormatForDisplay("plain prose", showIntent: true);
        Assert.StartsWith("intent: missing", shown, StringComparison.Ordinal);
        Assert.Contains("plain prose", shown, StringComparison.Ordinal);
    }
}
