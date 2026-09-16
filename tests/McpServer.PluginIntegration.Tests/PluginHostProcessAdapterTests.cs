using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P7: process-adapter contract for each PluginHostKind.
/// Tests TR-MCP-PLUGININT-001 AC3 (production entrypoints via process I/O, not raw REST).
/// Fixture: FakePluginProcessRunner plus catalog rows from plugin-sessionlog-scenarios.json.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginHostProcessAdapterTests
{
    /// <summary>
    /// P7 red: adapter captures executable, argument list, stdin, environment, working directory,
    /// timeout, exit code, stdout, and stderr for each PluginHostKind.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr(PluginHostKind hostKind)
    {
        var repoRoot = FindRepositoryRoot();
        var scenario = PluginSessionLogCatalog.LoadAndValidate(repoRoot)
            .Single(row => row.HostKind == hostKind);
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException("Cannot resolve sibling plugin parent directory.");
        var pluginRoot = Path.GetFullPath(Path.Combine(parent, scenario.RepositoryName));
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        var stdin = "{\"hostKind\":\"" + hostKind + "\",\"method\":\"workflow.sessionlog.beginTurn\"}";
        var timeout = TimeSpan.FromSeconds(20 + (int)hostKind);
        var expectedExit = 100 + (int)hostKind;
        var expectedStdout = "stdout-" + hostKind;
        var expectedStderr = "stderr-" + hostKind;

        var fake = new FakePluginProcessRunner
        {
            NextResult = new PluginProcessLaunchResult
            {
                ExitCode = expectedExit,
                StandardOutput = expectedStdout,
                StandardError = expectedStderr,
            },
        };
        var adapter = new PluginHostProcessAdapter(fake);

        var result = await adapter.LaunchAsync(scenario, stdin, timeout, TestContext.Current.CancellationToken);

        Assert.NotNull(fake.LastRequest);
        Assert.Equal(ExpectedExecutable(hostKind), fake.LastRequest.Executable, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(entrypointPath, fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        if (IsPowerShellHost(hostKind))
        {
            Assert.Contains("-NoProfile", fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("-NonInteractive", fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("-File", fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        }

        Assert.Equal(stdin, fake.LastRequest.StandardInput);
        Assert.Equal(pluginRoot, fake.LastRequest.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(timeout, fake.LastRequest.Timeout);
        Assert.Equal(expectedExit, result.ExitCode);
        Assert.Equal(expectedStdout, result.StandardOutput);
        Assert.Equal(expectedStderr, result.StandardError);
        Assert.All(scenario.RequiredEnvironmentVariables, name =>
        {
            Assert.True(fake.LastRequest.Environment.ContainsKey(name), hostKind + " missing env " + name);
            Assert.False(string.IsNullOrWhiteSpace(fake.LastRequest.Environment[name]));
        });
        Assert.Equal(scenario.AgentSourceType, fake.LastRequest.Environment["PLUGIN_AGENT_NAME"]);
        var rootVariable = scenario.RequiredEnvironmentVariables.First(name => name.EndsWith("_PLUGIN_ROOT", StringComparison.Ordinal));
        Assert.Equal(pluginRoot, fake.LastRequest.Environment[rootVariable], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// P8 green: Codex and PowerShell-hook hosts launch the catalog-declared entrypoint via pwsh.exe, not raw REST or repl-invoke.ps1.
    /// </summary>
    /// <param name="hostKind">Codex or a PowerShell-hook host kind.</param>
    [Theory]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    public async Task Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint(PluginHostKind hostKind)
    {
        var (scenario, fake, _, pluginRoot) = await LaunchWithFakeAsync(hostKind);
        Assert.Equal("pwsh.exe", fake.LastRequest!.Executable, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("-File", fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        Assert.Contains(entrypointPath, fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(fake.LastRequest.Arguments, argument => argument.Contains("repl-invoke.ps1", StringComparison.OrdinalIgnoreCase));
        if (hostKind == PluginHostKind.Codex)
        {
            Assert.EndsWith("Invoke-CodexMcpPlugin.ps1", entrypointPath, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains("Invoke-McpPlugin.ps1", scenario.Entrypoint, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// P9 green: Cline v1 starts catalog dist/index.js through node for session-log stdin, not repl-invoke.ps1.
    /// </summary>
    [Fact]
    public async Task Adapter_ClineV1_StdioSessionLogTools()
    {
        var stdin = "{\"method\":\"sessionlog_begin_turn\",\"jsonrpc\":\"2.0\",\"id\":1}";
        var (scenario, fake, _, pluginRoot) = await LaunchWithFakeAsync(PluginHostKind.Cline, stdin);
        Assert.Equal("Cline", scenario.Name, StringComparer.Ordinal);
        Assert.Equal("node", fake.LastRequest!.Executable, StringComparer.OrdinalIgnoreCase);
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        Assert.EndsWith("dist" + Path.DirectorySeparatorChar + "index.js", entrypointPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(entrypointPath, fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(stdin, fake.LastRequest.StandardInput);
        Assert.Contains("sessionlog", fake.LastRequest.StandardInput, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fake.LastRequest.Arguments, argument => argument.Contains("repl-invoke.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(pluginRoot, fake.LastRequest.Environment["CLINE_PLUGIN_ROOT"], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// P10 green: Cline v2 and OpenCode use the catalog production export (dist/index.js), not lib/repl-invoke.ps1.
    /// </summary>
    /// <param name="hostKind">ClineV2 or OpenCode.</param>
    [Theory]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task Adapter_ClineV2AndOpenCode_UseProductionExport(PluginHostKind hostKind)
    {
        var (scenario, fake, _, pluginRoot) = await LaunchWithFakeAsync(hostKind);
        Assert.Equal("node", fake.LastRequest!.Executable, StringComparer.OrdinalIgnoreCase);
        var entrypointPath = Path.Combine(pluginRoot, scenario.Entrypoint.Replace('/', Path.DirectorySeparatorChar));
        Assert.EndsWith("dist" + Path.DirectorySeparatorChar + "index.js", entrypointPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(entrypointPath, fake.LastRequest.Arguments, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(fake.LastRequest.Arguments, argument => argument.Contains("repl-invoke.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fake.LastRequest.Arguments, argument => argument.Contains("Invoke-McpPlugin.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(pluginRoot, fake.LastRequest.WorkingDirectory, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<(PluginSessionLogScenario Scenario, FakePluginProcessRunner Fake, PluginProcessLaunchResult Result, string PluginRoot)> LaunchWithFakeAsync(
        PluginHostKind hostKind,
        string? stdin = null)
    {
        var repoRoot = FindRepositoryRoot();
        var scenario = PluginSessionLogCatalog.LoadAndValidate(repoRoot)
            .Single(row => row.HostKind == hostKind);
        var parent = Directory.GetParent(repoRoot)?.FullName
            ?? throw new InvalidOperationException("Cannot resolve sibling plugin parent directory.");
        var pluginRoot = Path.GetFullPath(Path.Combine(parent, scenario.RepositoryName));
        var envelope = stdin ?? "{\"hostKind\":\"" + hostKind + "\",\"method\":\"workflow.sessionlog.beginTurn\"}";
        var fake = new FakePluginProcessRunner
        {
            NextResult = new PluginProcessLaunchResult
            {
                ExitCode = 0,
                StandardOutput = "ok",
                StandardError = string.Empty,
            },
        };
        var adapter = new PluginHostProcessAdapter(fake);
        var result = await adapter.LaunchAsync(scenario, envelope, TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        Assert.NotNull(fake.LastRequest);
        return (scenario, fake, result, pluginRoot);
    }

    private static bool IsPowerShellHost(PluginHostKind hostKind) =>
        hostKind is PluginHostKind.Codex
            or PluginHostKind.ClaudeCode
            or PluginHostKind.ClaudeCowork
            or PluginHostKind.Copilot
            or PluginHostKind.Grok;

    private static string ExpectedExecutable(PluginHostKind hostKind) =>
        IsPowerShellHost(hostKind) ? "pwsh.exe" : "node";

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
