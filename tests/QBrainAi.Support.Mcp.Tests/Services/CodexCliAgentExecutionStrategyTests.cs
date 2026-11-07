using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-003: Verifies the Codex CLI execution strategy used by
/// background triage invokes <c>codex exec</c> non-interactively. The class runs in
/// <see cref="ProcessEnvironmentCollection"/> because every test that starts a session writes the process environment:
/// the strategy's temp-folder lookup reads the CLI temp variables, so those tests set and restore them through
/// <see cref="CliTempVariables"/> and are integration tests (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008;
/// hostile validation finding R6-F1).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGE-003; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (ReadInitialResponseAsync_WhenCodexFails_ReturnsErrorResult, ReadInitialResponseAsync_WhenCodexTimesOut_PreservesPartialStreams, ReadInitialResponseAsync_WithOutputCallback_StreamsAgentOutput and ReadInitialResponseAsync_WithConfiguredSharedTempRoot_UsesSharedOutputPath) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (PrimaryTempName, CreateRequest, CapturingProcessEnvironmentService, CapturingProcessSpawner and FakeSpawnedProcess) moved once into the abstract base class CodexCliAgentExecutionStrategyTestsBase in Services/CodexCliAgentExecutionStrategyTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CodexCliAgentExecutionStrategyTests : CodexCliAgentExecutionStrategyTestsBase
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: launches the strategy against the capturing spawner with the default request, with every
    /// CLI temp variable at a test-owned folder. Shared by the Windows and Linux launch cases, which live in
    /// integration classes because the strategy chooses its launcher by the running OS (TR-MCP-USECASE-018 AC-006).
    /// </summary>
    /// <returns>The spawner holding the captured launch.</returns>
    internal static async Task<CapturingProcessSpawner> LaunchDefaultAsync()
    {
        var spawner = new CapturingProcessSpawner(exitCode: 0, outputBody: """{"title":"triage result"}""");
        await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async _ =>
        {
            var strategy = new CodexCliAgentExecutionStrategy(
                new CapturingProcessEnvironmentService(),
                spawner,
                NullLogger<CodexCliAgentExecutionStrategy>.Instance);

            await using var session = await strategy.CreateSessionAsync(CreateRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            var result = await session.ReadInitialResponseAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("""{"title":"triage result"}""", result.Body);
        }).ConfigureAwait(true);
        Assert.NotNull(spawner.StartInfo);
        return spawner;
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: Codex uses reusable one-shot <c>exec -C ... -o ... -</c> invocation and never
    /// uses Copilot interactive flags or prompt arguments, on every host.
    /// </summary>
    /// <param name="spawner">Spawner holding the captured launch.</param>
    internal static void AssertCodexExecWithoutCopilotInteractiveFlags(CapturingProcessSpawner spawner)
    {
        ArgumentNullException.ThrowIfNull(spawner);
        Assert.NotNull(spawner.StartInfo);
        Assert.Equal("Q:\\__mcp_unit_test__\\QBrainAi", spawner.StartInfo.WorkingDirectory);
        var serializedArguments = string.Join(" ", spawner.StartInfo.ArgumentList);
        Assert.Contains("exec", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("-C", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("Q:\\__mcp_unit_test__\\QBrainAi", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("-o", serializedArguments, StringComparison.Ordinal);
        Assert.True(spawner.HasArgument("-"), "Codex must receive '-' so the prompt is read from stdin.");
        Assert.DoesNotContain("--output-schema", serializedArguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--output-last-message", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--model", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("model-triage", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("-c", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--sandbox", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("read-only", serializedArguments, StringComparison.Ordinal);
        Assert.Contains("--skip-git-repo-check", serializedArguments, StringComparison.Ordinal);
        Assert.DoesNotContain("rendered prompt", serializedArguments, StringComparison.Ordinal);
        Assert.True(spawner.StartInfo.RedirectStandardInput);
        Assert.DoesNotContain("-i", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("-p", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--stream", spawner.StartInfo.ArgumentList);
        Assert.DoesNotContain("--yolo", spawner.StartInfo.ArgumentList);
        Assert.Equal("rendered prompt", spawner.CapturedStandardInput);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: The strategy resolver recognizes the configured
    /// <c>codex-cli</c> strategy name.
    /// </summary>
    [Fact]
    public void AgentExecutionStrategyNames_SupportsCodexCli()
    {
        Assert.True(AgentExecutionStrategyNames.IsSupported("codex-cli"));
        Assert.Contains("codex-cli", AgentExecutionStrategyNames.SupportedNames);
    }
}

/// <summary>
/// TEST-MCP-TRIAGE-003: the Codex CLI strategy on a Windows host launches through <c>cmd.exe /c</c>.
/// The strategy chooses its launcher by the running OS, so these are integration tests on Windows hosts
/// (operator 2026-10-19: "Integration + Platform (Recommended)"; TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019). Each
/// launch sets and restores every CLI temp variable through <see cref="CliTempVariables"/>, so the class runs in the
/// serialized <see cref="ProcessEnvironmentCollection"/> (TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CodexCliAgentExecutionStrategyWindowsTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003: the one-shot exec launch goes through cmd.exe, which needs the TOML quotes escaped.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_OnWindows_WrapsCodexExecThroughCmd()
    {
        var spawner = await CodexCliAgentExecutionStrategyTests.LaunchDefaultAsync().ConfigureAwait(true);

        CodexCliAgentExecutionStrategyTests.AssertCodexExecWithoutCopilotInteractiveFlags(spawner);
        Assert.EndsWith("cmd.exe", spawner.StartInfo!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/c", spawner.StartInfo.ArgumentList);
        Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("codex.cmd", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("exec", StringComparison.Ordinal));
        Assert.Contains("model_reasoning_effort=\\\"xhigh\\\"", string.Join(" ", spawner.StartInfo.ArgumentList), StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-003: Windows npm command shims are wrapped through
    /// <c>cmd.exe</c> so desktop spawning does not execute <c>.cmd</c> directly.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_WithWindowsNpmShim_UsesCmdWrapperAndStdinPrompt()
    {
        var shimDirectory = Path.Combine(Path.GetTempPath(), $"codex-shim-{Guid.NewGuid():N}");
        Directory.CreateDirectory(shimDirectory);
        var cmdPath = Path.Combine(shimDirectory, "codex.cmd");
        File.WriteAllText(cmdPath, "@echo off", Encoding.UTF8);
        try
        {
            var spawner = new CodexCliAgentExecutionStrategyTests.CapturingProcessSpawner(
                exitCode: 0,
                outputBody: """{"title":"triage result"}""",
                stdout: "codex stdout",
                stderr: "codex stderr");
            AgentCliResult? launched = null;
            await CliTempVariables.RunWithTestFolderAsync(CodexCliAgentExecutionStrategyTests.PrimaryTempName, async _ =>
            {
                var strategy = new CodexCliAgentExecutionStrategy(
                    new CodexCliAgentExecutionStrategyTests.CapturingProcessEnvironmentService(),
                    spawner,
                    NullLogger<CodexCliAgentExecutionStrategy>.Instance);

                await using var session = await strategy.CreateSessionAsync(CodexCliAgentExecutionStrategyTests.CreateRequest(cmdPath), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

                launched = await session.ReadInitialResponseAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            }).ConfigureAwait(true);

            var result = Assert.IsType<AgentCliResult>(launched);
            Assert.Equal(AgentCliResultState.Success, result.State);
            Assert.Equal("codex stdout", result.Stdout);
            Assert.Equal("codex stderr", result.Stderr);
            Assert.Equal(0, result.ExitCode);
            Assert.NotNull(spawner.StartInfo);
            Assert.EndsWith("cmd.exe", spawner.StartInfo.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/c", spawner.StartInfo.ArgumentList);
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains(cmdPath, StringComparison.OrdinalIgnoreCase));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("exec", StringComparison.Ordinal));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.Contains("-o", StringComparison.Ordinal));
            Assert.Contains(spawner.StartInfo.ArgumentList, argument => argument.EndsWith(" -", StringComparison.Ordinal));
            Assert.True(spawner.StartInfo.RedirectStandardInput);
            Assert.DoesNotContain(spawner.StartInfo.ArgumentList, argument => argument.Contains("rendered prompt", StringComparison.Ordinal));
            Assert.Equal("rendered prompt", spawner.CapturedStandardInput);
        }
        finally
        {
            Directory.Delete(shimDirectory, recursive: true);
        }
    }
}

/// <summary>
/// TEST-MCP-TRIAGE-003: the Codex CLI strategy off Windows launches the agent directly. The strategy
/// chooses its launcher by the running OS, so this is an integration test on Linux hosts (operator
/// 2026-10-19: "Integration + Platform (Recommended)"; TR-MCP-USECASE-018 AC-006, TR-MCP-NUKE-019). The launch sets
/// and restores every CLI temp variable through <see cref="CliTempVariables"/>, so the class runs in the serialized
/// <see cref="ProcessEnvironmentCollection"/> (TR-MCP-USECASE-019 AC-008; hostile validation finding R6-F1).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class CodexCliAgentExecutionStrategyLinuxTests
{
    /// <summary>
    /// TEST-MCP-TRIAGE-003, BUG-BASELINE-004: codex is started directly and receives the TOML literal as
    /// one argv element.
    /// </summary>
    [Fact]
    public async Task ReadInitialResponseAsync_OffWindows_RunsCodexExecDirectly()
    {
        var spawner = await CodexCliAgentExecutionStrategyTests.LaunchDefaultAsync().ConfigureAwait(true);

        CodexCliAgentExecutionStrategyTests.AssertCodexExecWithoutCopilotInteractiveFlags(spawner);
        Assert.Equal("codex.cmd", spawner.StartInfo!.FileName);
        Assert.Contains("model_reasoning_effort=\"xhigh\"", spawner.StartInfo.ArgumentList);
    }
}
