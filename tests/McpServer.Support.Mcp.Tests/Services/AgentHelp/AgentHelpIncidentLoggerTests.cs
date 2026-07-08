using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-003: Guard incident JSON persistence tests.
/// </summary>
public sealed class AgentHelpIncidentLoggerTests
{
    [Fact]
    public async Task WriteAsync_PersistsIncidentJsonFile()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var logger = CreateLogger();
        var incident = new AgentHelpIncidentRecord
        {
            IncidentId = "incident-001",
            SessionId = "help-session-001",
            TurnId = "turn-0001",
            RuleId = "injection.ignore-instructions",
            Reason = "Inbound message attempts to override prior instructions.",
            MatchedSnippet = "ignore all previous instructions",
            TimestampUtc = "2026-07-08T12:34:56Z",
            WorkspacePath = workspaceRoot,
        };

        await logger.WriteAsync(dataRoot, incident, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var incidentDir = Path.Combine(dataRoot, "agent-help", "incidents");
        var files = Directory.GetFiles(incidentDir, "*.json");
        Assert.Single(files);

        var json = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("\"incidentId\": \"incident-001\"", json, StringComparison.Ordinal);
        Assert.Contains("\"ruleId\": \"injection.ignore-instructions\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadBySessionAsync_ReturnsIncidentsForSession()
    {
        var workspaceRoot = AgentHelpTestPaths.CreateTempWorkspaceRoot();
        var dataRoot = Path.Combine(workspaceRoot, ".mcpServer");
        var logger = CreateLogger();
        var sessionId = "help-session-filter";

        await logger.WriteAsync(
            dataRoot,
            new AgentHelpIncidentRecord
            {
                IncidentId = "incident-a",
                SessionId = sessionId,
                RuleId = "injection.api-key-exfiltration",
                Reason = "Blocked",
                TimestampUtc = "2026-07-08T12:00:00Z",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        await logger.WriteAsync(
            dataRoot,
            new AgentHelpIncidentRecord
            {
                IncidentId = "incident-b",
                SessionId = "other-session",
                RuleId = "injection.disable-guardrails",
                Reason = "Blocked",
                TimestampUtc = "2026-07-08T12:00:01Z",
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var incidents = await logger.ReadBySessionAsync(dataRoot, sessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Single(incidents);
        Assert.Equal("incident-a", incidents[0].IncidentId);
    }

    private static AgentHelpIncidentLogger CreateLogger()
    {
        var monitor = new AgentHelpTestOptionsMonitor<AgentHelpOptions>(new AgentHelpOptions());
        return new AgentHelpIncidentLogger(monitor, NullLogger<AgentHelpIncidentLogger>.Instance);
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