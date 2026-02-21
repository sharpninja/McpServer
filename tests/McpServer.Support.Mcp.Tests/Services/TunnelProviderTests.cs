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
}
