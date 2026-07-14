using System.Diagnostics;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: ProcessRunner argument-list coverage for DocFX workflow execution.</summary>
public sealed class ProcessRunnerTests
{
    /// <summary>ArgumentList preserves spaces, quotes, and shell metacharacters without shell execution.</summary>
    [Fact]
    public async Task RunAsync_WithArgumentList_PopulatesStartInfoArgumentListAndDisablesShellExecution()
    {
        var processEnvironment = new CapturingProcessEnvironmentService();
        var runner = new ProcessRunner(
            processEnvironment,
            Microsoft.Extensions.Options.Options.Create(new ProcessRunnerOptions()),
            NullLogger<ProcessRunner>.Instance);
        string[] arguments = ["docfx", "metadata file.json", "literal\"quote", "a&b|c;d", "$(not-a-shell)"];

        _ = await runner.RunAsync(
            new ProcessRunRequest("definitely-missing-docfx-test-exe", string.Empty, ArgumentList: arguments),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var startInfo = Assert.IsType<ProcessStartInfo>(processEnvironment.ResolvedStartInfo);
        Assert.False(startInfo.UseShellExecute);
        Assert.Empty(startInfo.Arguments);
        Assert.Equal(arguments, startInfo.ArgumentList);
    }

    private sealed class CapturingProcessEnvironmentService : IProcessEnvironmentService
    {
        public ProcessStartInfo? ResolvedStartInfo { get; private set; }

        public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
        {
        }

        public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
        {
        }

        public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
        {
        }

        public string ResolveExecutable(ProcessStartInfo psi, string fileName)
        {
            ResolvedStartInfo = psi;
            return fileName;
        }
    }
}
