using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: Verifies the Codex CLI execution strategy used by
/// background triage invokes <c>codex exec</c> non-interactively. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because every test that starts a session writes the process environment:
/// the strategy's temp-folder lookup reads the CLI temp variables, so those tests set and restore them through
/// <see cref="CliTempVariables"/> and are integration tests (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008;
/// hostile validation finding R6-F1).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/CodexCliAgentExecutionStrategyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 4 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class CodexCliAgentExecutionStrategyTestsBase in Services/CodexCliAgentExecutionStrategyTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CodexCliAgentExecutionStrategyTests : CodexCliAgentExecutionStrategyTestsBase
{
    /// <summary>
    /// TEST-MCP-TRIAGE-005: A failed Codex CLI run preserves stderr as the
    /// inspectable research-run failure reason. The strategy's temp-folder lookup reads the CLI temp variables, so the
    /// test sets and restores them through <see cref="CliTempVariables"/> and is an integration test
    /// (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WhenCodexFails_ReturnsErrorResult()
    {
        await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async _ =>
        {
            var spawner = new CapturingProcessSpawner(exitCode: 2, stderr: "not authenticated");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Error, result.State);
            Assert.Contains("not authenticated", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: Timed-out Codex CLI triage runs preserve partial
    /// stdout and stderr instead of returning only the timeout message. The strategy's temp-folder lookup reads the
    /// CLI temp variables, so the test sets and restores them through <see cref="CliTempVariables"/> and is an
    /// integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WhenCodexTimesOut_PreservesPartialStreams()
    {
        await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async _ =>
        {
            var spawner = new CapturingProcessSpawner(
                exitCode: 0,
                stdout: "analysis started",
                stderr: "loading context",
                waitForCancellation: true);
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

            var result = await session.ReadInitialResponseAsync(timeout.Token).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Error, result.State);
            Assert.Contains("analysis started", result.Stdout, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("loading context", result.Stderr, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cancelled or timed out", result.Stderr, StringComparison.OrdinalIgnoreCase);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: Codex CLI triage runs stream stdout and stderr
    /// chunks to the configured output callback while the process is running. The strategy's temp-folder lookup reads
    /// the CLI temp variables, so the test sets and restores them through <see cref="CliTempVariables"/> and is an
    /// integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithOutputCallback_StreamsAgentOutput()
    {
        await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async _ =>
        {
            var streamed = new List<string>();
            var spawner = new CapturingProcessSpawner(
                exitCode: 0,
                outputBody: """{"title":"triage result"}""",
                stdout: "analysis started" + Environment.NewLine + "analysis done",
                stderr: "loading context");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);
            var request = CreateRequest(outputReceivedAsync: (streamName, text) =>
            {
                streamed.Add($"{streamName}:{text}");
                return Task.CompletedTask;
            });

            await using var session = await strategy.CreateSessionAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Contains("stdout:analysis started", streamed);
            Assert.Contains("stdout:analysis done", streamed);
            Assert.Contains("stderr:loading context", streamed);
        }).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: The Codex CLI strategy writes one-shot output
    /// files under the configured shared temp root so a service-spawned
    /// interactive-user codex process can write the final response file. The test points
    /// <c>MCPSERVER_CODEX_ONESHOT_TEMP</c>, which the strategy reads first, at a test-owned folder and every other CLI
    /// temp variable, its fallback <c>MCPSERVER_CODEX_TRIAGE_TEMP</c> included, at a separate fallback folder, and
    /// restores them through <see cref="CliTempVariables"/>, so the output path proves the primary name won; it writes
    /// the process environment and is an integration test (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <returns>A task that completes when the assertions have run.</returns>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReadInitialResponseAsync_WithConfiguredSharedTempRoot_UsesSharedOutputPath()
    {
        await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async tempRoot =>
        {
            var spawner = new CapturingProcessSpawner(exitCode: 0, outputBody: """{"title":"triage result"}""");
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.NotNull(spawner.OutputLastMessagePath);
            Assert.StartsWith(tempRoot, spawner.OutputLastMessagePath, StringComparison.OrdinalIgnoreCase);
            Assert.Null(spawner.OutputSchemaPath);
            Assert.Null(spawner.OutputSchemaJson);
        }).ConfigureAwait(true);
    }
}
