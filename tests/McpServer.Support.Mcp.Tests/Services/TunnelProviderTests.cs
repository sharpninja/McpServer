using System.Diagnostics;
using System.Reflection;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for tunnel provider implementations.</summary>
public sealed class TunnelProviderTests
{
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();

    private static Microsoft.Extensions.Options.IOptions<TunnelOptions> CreateOptions(Action<TunnelOptions>? configure = null)
    {
        var opts = new TunnelOptions { Port = 7147 };
        configure?.Invoke(opts);
        return Microsoft.Extensions.Options.Options.Create(opts);
    }

    // --- NgrokTunnelProvider ---

    [Fact]
    public void NgrokProvider_ProviderName_IsNgrok()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, NullLogger<NgrokTunnelProvider>.Instance);
        Assert.Equal("ngrok", sut.ProviderName);
    }

    [Fact]
    public async Task NgrokProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("ngrok", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, NullLogger<NgrokTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("ngrok CLI not found", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NgrokProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, NullLogger<NgrokTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    [Fact]
    public async Task NgrokProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError()
    {
        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, NullLogger<NgrokTunnelProvider>.Instance);
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated ngrok failure");
        SetPrivateField(sut, "_startupCompleted", true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("ngrok process exited after startup", status.Error, StringComparison.Ordinal);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated ngrok failure", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NgrokProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError()
    {
        _processRunner.RunAsync("curl", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(-1, null, "curl not found"));

        var sut = new NgrokTunnelProvider(CreateOptions(), _processRunner, NullLogger<NgrokTunnelProvider>.Instance);
        SetPrivateField(sut, "_process", Process.GetCurrentProcess());
        SetPrivateField(sut, "_error", "ngrok startup timed out after 8s waiting for a public URL.");

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.True(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("startup timed out", status.Error, StringComparison.Ordinal);
    }

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

    [Fact]
    public void CloudflareProvider_ProviderName_IsCloudflare()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        Assert.Equal("cloudflare", sut.ProviderName);
    }

    [Fact]
    public async Task CloudflareProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("cloudflared", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("cloudflared CLI not found", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_WhenTrackedProcessExited_ReturnsDetailedExitError()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated cloudflared failure");
        SetPrivateField(sut, "_startupCompleted", true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("cloudflared process exited after startup", status.Error, StringComparison.Ordinal);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated cloudflared failure", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CloudflareProvider_GetStatusAsync_WhenRunningWithStartupTimeoutError_PreservesDiagnosticError()
    {
        var sut = new CloudflareTunnelProvider(CreateOptions(), _processRunner, NullLogger<CloudflareTunnelProvider>.Instance);
        SetPrivateField(sut, "_process", Process.GetCurrentProcess());
        SetPrivateField(sut, "_error", "cloudflared startup timed out after 8s waiting for a public URL.");

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.True(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("startup timed out", status.Error, StringComparison.Ordinal);
    }

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

    [Fact]
    public void FrpProvider_ProviderName_IsFrp()
    {
        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        Assert.Equal("frp", sut.ProviderName);
    }

    [Fact]
    public async Task FrpProvider_StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner.RunAsync("frpc", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Contains("frpc CLI not found", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FrpProvider_GetStatusAsync_BeforeStart_ReturnsNotRunning()
    {
        var sut = new FrpTunnelProvider(CreateOptions(), _processRunner, NullLogger<FrpTunnelProvider>.Instance);
        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
    }

    private static object? InvokePrivateStatic(Type targetType, string methodName, params object?[] args)
    {
        var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Static method '{methodName}' not found on {targetType.FullName}.");
        return method.Invoke(null, args);
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
