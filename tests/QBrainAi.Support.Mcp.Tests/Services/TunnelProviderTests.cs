using System.Diagnostics;
using System.Reflection;
using QBrainAi.Common.AgentCli;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for tunnel provider implementations.</summary>
/// <remarks>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (NgrokProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError, NgrokProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError, CloudflareProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError and CloudflareProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError) and the helpers only they use (SetPrivateField and StartExitedDotnetProcess) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_processRunner, _processEnvironment, .ctor and CreateOptions) moved once into the abstract base class TunnelProviderTestsBase in Services/TunnelProviderTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</para>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the blank line that separated this documentation from the class declaration was removed (slice S8, hostile validation finding R14-F4), so the documentation sits directly on the declaration and the documentation scan finds it; the text is unchanged.</para>
/// </remarks>
public sealed class TunnelProviderTests : TunnelProviderTestsBase
{
    // --- NgrokTunnelProvider ---

    /// <summary>
    /// FR-MCP-015: the ngrok tunnel provider reports the provider name ngrok. Data: an NgrokTunnelProvider over
    /// CreateOptions (port 7147) and the substituted IProcessRunner and IProcessEnvironmentService of
    /// TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void NgrokProvider_ProviderName_IsNgrok()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, _processEnvironment, NullLogger<NgrokTunnelProvider>.Instance);
        Assert.Equal("ngrok", sut.ProviderName);
    }

    /// <summary>
    /// FR-MCP-015: when the ngrok CLI check fails (exit code 1, not found) the ngrok provider does not start and
    /// reports a status error that the ngrok CLI was not found. Data: an NgrokTunnelProvider over CreateOptions, the
    /// substituted IProcessRunner of TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs) scripted to return
    /// exit code 1 for ngrok, and its IProcessEnvironmentService substitute whose ResolveExecutable returns the
    /// requested name.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task NgrokProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("ngrok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, _processEnvironment, NullLogger<NgrokTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("ngrok CLI not found", status.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-015: an ngrok provider that was never started reports a status that is not running. Data: an
    /// NgrokTunnelProvider over CreateOptions (port 7147) and the substitutes of TunnelProviderTestsBase
    /// (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task NgrokProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, _processEnvironment, NullLogger<NgrokTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    /// <summary>
    /// FR-MCP-015: the private static BuildStartupTimeoutError method of the ngrok provider names the 8 s timeout, the
    /// local ngrok API address 127.0.0.1:4040/api/tunnels, the last API query error and the last stderr and stdout
    /// lines. Data: the inline values 8, curl exited with code -1, stderr line and stdout line, passed by reflection.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void NgrokProvider_BuildStartupTimeoutError_IncludesApiAndOutputContext()
    {
        var message = Assert.IsType<string>(InvokePrivateStatic(
            typeof(NgrokTunnelProvider),
            "BuildStartupTimeoutError",
            8,
            "curl exited with code -1",
            "stderr line",
            "stdout line"));

        Assert.Contains("ngrok startup timed out after 8s", message, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:4040/api/tunnels", message, StringComparison.Ordinal);
        Assert.Contains("Last ngrok API query error", message, StringComparison.Ordinal);
        Assert.Contains("Last stderr: stderr line", message, StringComparison.Ordinal);
        Assert.Contains("Last stdout: stdout line", message, StringComparison.Ordinal);
    }

    // --- CloudflareTunnelProvider ---

    /// <summary>
    /// FR-MCP-015: the Cloudflare tunnel provider reports the provider name cloudflare. Data: a
    /// CloudflareTunnelProvider over CreateOptions (port 7147) and the substituted IProcessRunner of
    /// TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void CloudflareProvider_ProviderName_IsCloudflare()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        Assert.Equal("cloudflare", sut.ProviderName);
    }

    /// <summary>
    /// FR-MCP-015: when the cloudflared CLI check fails (exit code 1, not found) the Cloudflare provider does not start
    /// and reports a status error that the cloudflared CLI was not found. Data: a CloudflareTunnelProvider over
    /// CreateOptions and the substituted IProcessRunner of TunnelProviderTestsBase
    /// (Services/TunnelProviderTestsBase.cs) scripted to return exit code 1 for cloudflared.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CloudflareProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("cloudflared", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("cloudflared CLI not found", status.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-015: a Cloudflare provider that was never started reports a status that is not running. Data: a
    /// CloudflareTunnelProvider over CreateOptions (port 7147) and the substituted IProcessRunner of
    /// TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    /// <summary>
    /// FR-MCP-015: the private static BuildStartupTimeoutError method of the Cloudflare provider names the 8 s timeout
    /// and the last stderr and stdout lines. Data: the inline values 8, stderr line and stdout line, passed by
    /// reflection.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void CloudflareProvider_BuildStartupTimeoutError_IncludesOutputContext()
    {
        var message = Assert.IsType<string>(InvokePrivateStatic(
            typeof(CloudflareTunnelProvider),
            "BuildStartupTimeoutError",
            8,
            "stderr line",
            "stdout line"));

        Assert.Contains("cloudflared startup timed out after 8s", message, StringComparison.Ordinal);
        Assert.Contains("Last stderr: stderr line", message, StringComparison.Ordinal);
        Assert.Contains("Last stdout: stdout line", message, StringComparison.Ordinal);
    }

    // --- FrpTunnelProvider ---

    /// <summary>
    /// FR-MCP-015: the FRP tunnel provider reports the provider name frp. Data: a FrpTunnelProvider over CreateOptions
    /// (port 7147) and the substituted IProcessRunner of TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public void FrpProvider_ProviderName_IsFrp()
    {
        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        Assert.Equal("frp", sut.ProviderName);
    }

    /// <summary>
    /// FR-MCP-015: when the frpc CLI check fails (exit code 1, not found) the FRP provider does not start and reports a
    /// status error that the frpc CLI was not found. Data: a FrpTunnelProvider over CreateOptions and the substituted
    /// IProcessRunner of TunnelProviderTestsBase (Services/TunnelProviderTestsBase.cs) scripted to return exit code 1
    /// for frpc.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task FrpProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("frpc", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("frpc CLI not found", status.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-015: an FRP provider that was never started reports a status that is not running. Data: a
    /// FrpTunnelProvider over CreateOptions (port 7147) and the substituted IProcessRunner of TunnelProviderTestsBase
    /// (Services/TunnelProviderTestsBase.cs).
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-015: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task FrpProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    private static object? InvokePrivateStatic(Type targetType, string methodName, params object?[] args)
    {
        var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Static method '{methodName}' not found on {targetType.FullName}.");
        return method.Invoke(null, args);
    }
}
