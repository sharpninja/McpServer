using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-001: Help transcript JSONL persistence tests.
/// </summary>
public sealed class HelpTranscriptWriterTests
{
    [Fact]
    public async Task AppendAsync_WritesSingleJsonLinePerEntry()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var writer = CreateWriter();

        var entry = new AgentHelpTranscriptEntry
        {
            TimestampUtc = "2026-07-08T12:00:00Z",
            SessionId = "help-test-session",
            TurnId = "turn-0001",
            Role = "user",
            Category = "transcript",
            Text = "How do I run the tests?",
        };

        await writer.AppendAsync(dataRoot, entry, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var filePath = Path.Combine(dataRoot, "agent-help", "transcripts", "help-test-session.jsonl");
        Assert.True(File.Exists(filePath));

        var lines = await File.ReadAllLinesAsync(filePath, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(lines);
        Assert.Contains("\"sessionId\":\"help-test-session\"", lines[0], StringComparison.Ordinal);
        Assert.Contains("\"text\":\"How do I run the tests?\"", lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AppendAsync_AppendsMultipleEntriesWithoutOverwriting()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var writer = CreateWriter();
        var sessionId = "help-append-session";

        await writer.AppendAsync(
            dataRoot,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = "2026-07-08T12:00:01Z",
                SessionId = sessionId,
                Role = "user",
                Category = "transcript",
                Text = "First message",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await writer.AppendAsync(
            dataRoot,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = "2026-07-08T12:00:02Z",
                SessionId = sessionId,
                Role = "assistant",
                Category = "transcript",
                Text = "Second message",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var entries = await writer.ReadAllAsync(dataRoot, sessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(2, entries.Count);
        Assert.Equal("user", entries[0].Role);
        Assert.Equal("assistant", entries[1].Role);
    }

    [Fact]
    public async Task ReadAllAsync_ReturnsEmpty_WhenTranscriptFileDoesNotExist()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var writer = CreateWriter();

        var entries = await writer.ReadAllAsync(dataRoot, "missing-session", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Empty(entries);
    }

    private static HelpTranscriptWriter CreateWriter()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AgentHelpOptions());
        var monitor = new TestOptionsMonitor<AgentHelpOptions>(options.Value);
        return new HelpTranscriptWriter(monitor, NullLogger<HelpTranscriptWriter>.Instance);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class
    {
        public TestOptionsMonitor(T value) => CurrentValue = value;

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}