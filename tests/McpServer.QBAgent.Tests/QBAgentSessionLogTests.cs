using System.Text.Json;
using McpServer.QBAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tests;

public sealed class QBAgentSessionLogTests
{
    [Fact]
    public void CreateNew_WritesJsonlUnderRoot_WithSessionId()
    {
        var root = NewRoot();
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 11, 15, 4, 5, 123, TimeSpan.Zero));
        var log = QBAgentSessionLog.CreateNew(root, clock, workspace: @"F:\GitHub\McpServerManager");

        Assert.Equal("qbagent-20260911T150405123Z", log.SessionId);
        Assert.Equal(Path.Combine(root, "qbagent-20260911T150405123Z.jsonl"), log.FilePath);
        Assert.True(File.Exists(log.FilePath));
        var first = File.ReadLines(log.FilePath).First();
        using var doc = JsonDocument.Parse(first);
        Assert.Equal("meta", doc.RootElement.GetProperty("role").GetString());
        Assert.Equal(log.SessionId, doc.RootElement.GetProperty("sessionId").GetString());
    }

    [Fact]
    public void Append_WritesOneJsonObjectPerLine()
    {
        var root = NewRoot();
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 11, 15, 4, 5, TimeSpan.Zero));
        var log = QBAgentSessionLog.CreateNew(root, clock);
        log.Append("user", "hello");
        log.Append("assistant", "world");

        var lines = File.ReadAllLines(log.FilePath);
        Assert.Equal(3, lines.Length);
        Assert.Contains("\"role\":\"user\"", lines[1], StringComparison.Ordinal);
        Assert.Contains("hello", lines[1], StringComparison.Ordinal);
        Assert.Contains("\"role\":\"assistant\"", lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void Open_ReplaysUserAndAssistantAsChatMessages()
    {
        var root = NewRoot();
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 11, 15, 4, 5, TimeSpan.Zero));
        var created = QBAgentSessionLog.CreateNew(root, clock);
        created.Append("user", "first prompt");
        created.Append("assistant", "first answer");

        var opened = QBAgentSessionLog.Open(created.SessionId, root);
        var messages = opened.ToChatMessages();
        Assert.Equal(2, messages.Count);
        Assert.Equal(ChatRole.User, messages[0].Role);
        Assert.Equal("first prompt", messages[0].Text);
        Assert.Equal(ChatRole.Assistant, messages[1].Role);
        Assert.Equal("first answer", messages[1].Text);
    }

    [Fact]
    public void OpenLatest_ReturnsMostRecentSession()
    {
        var root = NewRoot();
        var older = QBAgentSessionLog.CreateNew(
            root, new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 11, 15, 0, 0, TimeSpan.Zero)));
        var newer = QBAgentSessionLog.CreateNew(
            root, new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 11, 16, 0, 0, TimeSpan.Zero)));

        var latest = QBAgentSessionLog.OpenLatest(root);
        Assert.Equal(newer.SessionId, latest.SessionId);
        Assert.NotEqual(older.SessionId, latest.SessionId);
    }

    [Fact]
    public void Open_MissingSession_ThrowsFileNotFound()
    {
        var root = NewRoot();
        var ex = Assert.Throws<FileNotFoundException>(() => QBAgentSessionLog.Open("qbagent-missing", root));
        Assert.Contains("qbagent-missing", ex.Message, StringComparison.Ordinal);
    }

    private static string NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "qbagent-sessions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
