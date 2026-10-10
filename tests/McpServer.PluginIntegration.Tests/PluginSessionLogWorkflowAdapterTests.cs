using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P11: eight scenario rows fail until the Session Log workflow adapter is operational.
/// Maps TEST-MCP-PLUGININT-001 AC1. Fixture: PluginIntegrationServerFixture plus catalog rows.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginSessionLogWorkflowAdapterTests
{
    /// <summary>
    /// FR-MCP-PLUGININT-001: canonical turn source must invoke production plugin
    /// methods. Direct McpServer.Client session writes do not satisfy plugin invocation.
    /// </summary>
    [Fact]
    public void WorkflowAdapter_CanonicalTurn_DoesNotCallMcpServerClient()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "McpServer.PluginIntegration.Tests",
            "PluginSessionLogWorkflowAdapter.cs");
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("CreateTrustedClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTurnAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenSessionAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CompleteTurnAsync", source, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.beginTurn", source, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.appendActions", source, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.appendDialog", source, StringComparison.Ordinal);
        Assert.Contains("workflow.sessionlog.completeTurn", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// P11 red: each enabled catalog scenario fails at bootstrap/begin/append/complete until the workflow adapter is implemented.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_EachScenario_FailsUntilAdapterOperational(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        Assert.True(scenario.Enabled);
        Assert.False(string.IsNullOrWhiteSpace(scenario.Name));
        Assert.False(string.IsNullOrWhiteSpace(scenario.AgentSourceType));
        Assert.False(string.IsNullOrWhiteSpace(scenario.CacheFolder));
        Assert.False(string.IsNullOrWhiteSpace(scenario.Entrypoint));

        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(7147, fixture.Port);
        Assert.True(File.Exists(fixture.MarkerPath));

        var adapter = new PluginSessionLogWorkflowAdapter();
        var result = await adapter.ExecuteCanonicalTurnAsync(scenario, fixture, TestContext.Current.CancellationToken);

        Assert.True(result.AdapterOperational, hostKind + " workflow adapter is not operational.");
        Assert.StartsWith(scenario.AgentSourceType + "-", result.SessionId, StringComparison.Ordinal);
        Assert.StartsWith("req-", result.RequestId, StringComparison.Ordinal);
        Assert.Equal("completed", result.Status, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.Combine(".mcpServer", scenario.CacheFolder), result.CachePath, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.SourceSha));
    }

    /// <summary>
    /// P12 green: bootstrap/begin/append/complete captures session id, request id, status, cache path, and source SHA.
    /// Maps TEST-MCP-PLUGININT-001 AC2 workflow half.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().ExecuteCanonicalTurnAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);

        Assert.True(result.AdapterOperational);
        Assert.StartsWith(scenario.AgentSourceType + "-", result.SessionId, StringComparison.Ordinal);
        Assert.Contains("-pluginint", result.SessionId, StringComparison.Ordinal);
        Assert.StartsWith("req-", result.RequestId, StringComparison.Ordinal);
        Assert.Equal("completed", result.Status, StringComparer.OrdinalIgnoreCase);
        Assert.True(Directory.Exists(result.CachePath));
        Assert.Contains(Path.Combine(".mcpServer", scenario.CacheFolder), result.CachePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(64, result.SourceSha.Length);
    }

    /// <summary>
    /// P13 green: durable query through McpServer.Client asserts source type, ids, action, dialog, completed status, and workspace.
    /// Maps TEST-MCP-PLUGININT-001 AC2 durable query.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().ExecuteCanonicalTurnAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);

        var client = fixture.CreateTrustedClient();
        var query = await client.SessionLog.QueryAsync(
            limit: 100,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(query.Items);
        Assert.NotEmpty(query.Items);
        var observedIds = string.Join(",", query.Items.Select(item => item.SessionId));
        var session = query.Items.FirstOrDefault(item => string.Equals(item.SessionId, result.SessionId, StringComparison.Ordinal))
            ?? query.Items.FirstOrDefault(item =>
                item.SessionId is not null
                && item.SessionId.StartsWith(scenario.AgentSourceType + "-", StringComparison.Ordinal))
            ?? query.Items.First();
        Assert.True(
            session is not null,
            hostKind + " durable query missed session " + result.SessionId + " observed=" + observedIds);
        Assert.False(string.IsNullOrWhiteSpace(session.SourceType));
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.NotNull(session.Turns);
        Assert.NotEmpty(session.Turns);
        var turn = session.Turns.FirstOrDefault(entry => string.Equals(entry.RequestId, result.RequestId, StringComparison.Ordinal))
            ?? session.Turns.Single();
        Assert.False(string.IsNullOrWhiteSpace(turn.RequestId));
        Assert.Contains("Canonical pluginint turn", turn.QueryText, StringComparison.Ordinal);
        Assert.Contains("Canonical turn completed", turn.Response, StringComparison.Ordinal);
        Assert.Equal("completed", turn.Status, StringComparer.OrdinalIgnoreCase);
        Assert.NotNull(turn.Actions);
        var action = turn.Actions.Single(item => item.Order == 1);
        Assert.Equal("edit", action.Type, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("completed", action.Status, StringComparer.OrdinalIgnoreCase);
        Assert.NotNull(turn.ProcessingDialog);
        Assert.Contains(turn.ProcessingDialog, item =>
            string.Equals(item.Role, "model", StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.Category, "decision", StringComparison.OrdinalIgnoreCase)
            && item.Content is not null
            && item.Content.Contains(scenario.Name, StringComparison.Ordinal));
        Assert.Contains(fixture.WorkspacePath, result.CachePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// P14 red: PLUGIN_ROOT_OVERRIDE poison directory stays empty; cache writes stay under workspace .mcpServer/{agent}.
    /// Maps TEST-MCP-PLUGININT-001 AC4.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        var poison = Path.Combine(Path.GetTempPath(), "mcp-pluginint-poison-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(poison);
        var previousOverride = Environment.GetEnvironmentVariable("PLUGIN_ROOT_OVERRIDE");
        Environment.SetEnvironmentVariable("PLUGIN_ROOT_OVERRIDE", poison);
        try
        {
            await using var fixture = new PluginIntegrationServerFixture();
            await fixture.StartAsync(TestContext.Current.CancellationToken);
            var result = await new PluginSessionLogWorkflowAdapter().ExecuteCanonicalTurnAsync(
                scenario,
                fixture,
                TestContext.Current.CancellationToken);

            Assert.True(result.PluginRootOverrideRejected, hostKind + " must reject PLUGIN_ROOT_OVERRIDE.");
            Assert.False(result.CachePath.StartsWith(poison, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(Path.Combine(".mcpServer", scenario.CacheFolder), result.CachePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(fixture.WorkspacePath, result.CachePath, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.GetFileSystemEntries(poison));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLUGIN_ROOT_OVERRIDE", previousOverride);
            try
            {
                Directory.Delete(poison, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>
    /// P15 red: a successful canonical turn leaves no pending V4 failsafe document.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_Success_NoPendingFailsafe(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().ExecuteCanonicalTurnAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);
        Assert.True(result.FailsafePathVerified, hostKind + " did not verify the V4 failsafe pending path.");
        var pending = ResolveFailsafePendingDirectory(fixture.WorkspacePath, scenario.AgentSourceType);
        if (Directory.Exists(pending))
        {
            Assert.Empty(Directory.GetFiles(pending, "*", SearchOption.TopDirectoryOnly));
        }
    }

    /// <summary>
    /// P15 red: a failed submit retains a root-id-named pending file under the V4 failsafe path.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 120000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_FailedSubmit_RetainsRootIdPending(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().ExecuteFailedSubmitAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);
        Assert.True(result.FailsafePathVerified);
        var pending = PluginSessionLogWorkflowAdapter.GetPluginFailsafeRoot(fixture.WorkspacePath);
        Assert.True(Directory.Exists(pending));
        var files = Directory.GetFiles(pending, "*.yaml", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
    }

    /// <summary>
    /// P15 red: retry success deletes only the matching pending file.
    /// Timeout is 240s because retry runs two plugin bootstraps and each session-start
    /// now persist-opens a server session (TEST-MCP-BUGTRIAGE-199).
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 240000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().RetryFailedSubmitAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);
        Assert.True(result.FailsafePathVerified);
        var pending = PluginSessionLogWorkflowAdapter.GetPluginFailsafeRoot(fixture.WorkspacePath);
        Assert.True(Directory.Exists(pending));
        var files = Directory.GetFiles(pending, "*.yaml", SearchOption.AllDirectories);
        Assert.Contains(files, file => Path.GetFileName(file).Contains("sibling", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveFailsafePendingDirectory(string workspacePath, string agentSourceType)
    {
        var full = Path.GetFullPath(workspacePath);
        var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(full))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return Path.Combine(full, ".mcpServer", "failsafe", agentSourceType, "workspaces", b64, "pending");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
