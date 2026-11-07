using System.Diagnostics;
using System.Reflection;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>Unit tests for tunnel provider implementations.</summary>
/// <remarks>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/TunnelProviderTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 4 Category=Integration cases and the helpers only they use (SetPrivateField and StartExitedDotnetProcess) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class TunnelProviderTestsBase in Services/TunnelProviderTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</para>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the blank line that separated this documentation from the class declaration was removed (slice S8, hostile validation finding R14-F4), so the documentation sits directly on the declaration and the documentation scan finds it; the text is unchanged.</para>
/// </remarks>
public sealed class TunnelProviderTests : TunnelProviderTestsBase
{
    /// <summary>Verifies NgrokProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError using its native fixture.</summary>
    /// <remarks>Integration: starts and observes a native child process (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task NgrokProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, _processEnvironment, NullLogger<NgrokTunnelProvider>.Instance);
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated ngrok failure");
        SetPrivateField(sut, "_startupCompleted", true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("ngrok process exited after startup", status.Error, StringComparison.Ordinal);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated ngrok failure", status.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies NgrokProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError using its native fixture.</summary>
    /// <remarks>Integration: binds status to the actual current process (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task NgrokProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError()
    {
        _processRunner.RunAsync("curl", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(-1, null, "curl not found"));

        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, _processEnvironment, NullLogger<NgrokTunnelProvider>.Instance);
        SetPrivateField(sut, "_process", Process.GetCurrentProcess());
        SetPrivateField(sut, "_error", "ngrok startup timed out after 8s waiting for a public URL.");

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("startup timed out", status.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies CloudflareProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError using its native fixture.</summary>
    /// <remarks>Integration: starts and observes a native child process (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated cloudflared failure");
        SetPrivateField(sut, "_startupCompleted", true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("cloudflared process exited after startup", status.Error, StringComparison.Ordinal);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated cloudflared failure", status.Error, StringComparison.Ordinal);
    }

    /// <summary>Verifies CloudflareProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError using its native fixture.</summary>
    /// <remarks>Integration: binds status to the actual current process (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        SetPrivateField(sut, "_process", Process.GetCurrentProcess());
        SetPrivateField(sut, "_error", "cloudflared startup timed out after 8s waiting for a public URL.");

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("startup timed out", status.Error, StringComparison.Ordinal);
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        field.SetValue(target, value);
    }

    private static Process StartExitedDotnetProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "__mcpserver_unknown_test_command__",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start test process for exited-process status path.");

        Assert.True(process.WaitForExit(10_000), "Timed out waiting for test process to exit.");
        return process;
    }
}
