using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for FRP tunnel provider helper logic and failure paths.</summary>
/// <remarks>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (GetStatusAsync_WhenTrackedProcessExited_ReturnsExitError and Dispose_WhenTempConfigExists_DeletesFileAndClearsConfigPath) and the helpers only they use (SetPrivateField, GetPrivateField and StartExitedDotnetProcess) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_processRunner, CreateSut and CreateOptions) moved once into the abstract base class FrpTunnelProviderTestsBase in Services/FrpTunnelProviderTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</para>
/// <para>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the blank line that separated this documentation from the class declaration was removed (slice S8, hostile validation finding R14-F4), so the documentation sits directly on the declaration and the documentation scan finds it; the text is unchanged.</para>
/// </remarks>
public sealed class FrpTunnelProviderTests : FrpTunnelProviderTestsBase
{
    [Fact]
    public async Task StartAsync_WhenProxyTypeUnsupported_SetsValidationErrorAndSkipsCliCheck()
    {
        var sut = CreateSut(o => o.Frp.ProxyType = "udp");

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("ProxyType 'udp' is not supported yet", status.Error, StringComparison.Ordinal);
        _ = _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, ct: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_WhenSubdomainAndCustomDomainConfigured_SetsValidationError()
    {
        var sut = CreateSut(o =>
        {
            o.Frp.Subdomain = "mcp";
            o.Frp.CustomDomain = "mcp.example.com";
        });

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("either Subdomain or CustomDomain", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartAsync_WhenCliMissing_SetsError()
    {
        _processRunner
            .RunAsync("frpc", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var sut = CreateSut();

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("frpc CLI not found", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateConfig_IncludesExpectedHttpFieldsAndToken()
    {
        var sut = CreateSut(o => o.Port = 7788);
        var frp = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            ServerPort = 7000,
            Token = "secret-token",
            CustomDomain = "mcp.example.com",
        };

        var config = Assert.IsType<string>(InvokePrivateInstance(sut, "GenerateConfig", frp, "http"));

        Assert.Contains("[common]", config, StringComparison.Ordinal);
        Assert.Contains("serverAddr = \"frps.example.com\"", config, StringComparison.Ordinal);
        Assert.Contains("serverPort = 7000", config, StringComparison.Ordinal);
        Assert.Contains("auth.token = \"secret-token\"", config, StringComparison.Ordinal);
        Assert.Contains("[[proxies]]", config, StringComparison.Ordinal);
        Assert.Contains("name = \"mcp-http\"", config, StringComparison.Ordinal);
        Assert.Contains("type = \"http\"", config, StringComparison.Ordinal);
        Assert.Contains("localPort = 7788", config, StringComparison.Ordinal);
        Assert.Contains("customDomains = [\"mcp.example.com\"]", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateConfig_WhenSubdomainConfigured_UsesSubdomainInsteadOfCustomDomain()
    {
        var sut = CreateSut();
        var frp = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            Subdomain = "my-mcp",
            CustomDomain = "ignored.example.com",
        };

        var config = Assert.IsType<string>(InvokePrivateInstance(sut, "GenerateConfig", frp, "http"));

        Assert.Contains("subdomain = \"my-mcp\"", config, StringComparison.Ordinal);
        Assert.DoesNotContain("customDomains =", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateConfig_TcpRange_CreatesOneToOneMappings()
    {
        var sut = CreateSut(o => o.Port = 7147);
        var frp = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            ProxyType = "tcp",
            TcpPortRangeStart = 7147,
            TcpPortRangeEnd = 7149,
        };

        var config = Assert.IsType<string>(InvokePrivateInstance(sut, "GenerateConfig", frp, "tcp"));

        Assert.Contains("type = \"tcp\"", config, StringComparison.Ordinal);
        Assert.Contains("name = \"mcp-tcp-7147\"", config, StringComparison.Ordinal);
        Assert.Contains("localPort = 7147", config, StringComparison.Ordinal);
        Assert.Contains("remotePort = 7147", config, StringComparison.Ordinal);
        Assert.Contains("name = \"mcp-tcp-7147\"", config, StringComparison.Ordinal);
        Assert.Contains("localPort = 7147", config, StringComparison.Ordinal);
        Assert.Contains("remotePort = 7147", config, StringComparison.Ordinal);
        Assert.Contains("name = \"mcp-tcp-7149\"", config, StringComparison.Ordinal);
        Assert.Contains("localPort = 7149", config, StringComparison.Ordinal);
        Assert.Contains("remotePort = 7149", config, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateConfig_TcpSinglePort_UsesLocalTunnelPortAndConfiguredRemotePort()
    {
        var sut = CreateSut(o => o.Port = 7147);
        var frp = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            ProxyType = "tcp",
            RemotePort = 17147,
        };

        var config = Assert.IsType<string>(InvokePrivateInstance(sut, "GenerateConfig", frp, "tcp"));

        Assert.Contains("name = \"mcp-tcp-17147\"", config, StringComparison.Ordinal);
        Assert.Contains("localPort = 7147", config, StringComparison.Ordinal);
        Assert.Contains("remotePort = 17147", config, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPublicUrl_PrefersPublicBaseUrl_ThenCustomDomain_ThenSubdomain()
    {
        var withPublicBaseUrl = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            PublicBaseUrl = " https://mcp.up.railway.app/ ",
            CustomDomain = "mcp.example.com",
            Subdomain = "mcp",
        };
        var withCustomDomain = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            CustomDomain = "mcp.example.com",
            Subdomain = "mcp",
        };
        var withSubdomain = new FrpTunnelOptions
        {
            ServerAddress = "frps.example.com",
            Subdomain = "mcp",
        };

        var publicBaseUrl = (string?)InvokePrivateStatic(typeof(FrpTunnelProvider), "BuildPublicUrl", withPublicBaseUrl);
        var customDomainUrl = (string?)InvokePrivateStatic(typeof(FrpTunnelProvider), "BuildPublicUrl", withCustomDomain);
        var subdomainUrl = (string?)InvokePrivateStatic(typeof(FrpTunnelProvider), "BuildPublicUrl", withSubdomain);

        Assert.Equal("https://mcp.up.railway.app", publicBaseUrl);
        Assert.Equal("http://mcp.example.com", customDomainUrl);
        Assert.Equal("http://mcp.frps.example.com", subdomainUrl);
    }

    [Fact]
    public async Task StartAsync_WhenTcpRangeMissingEnd_SetsValidationError()
    {
        var sut = CreateSut(o =>
        {
            o.Frp.ProxyType = "tcp";
            o.Frp.TcpPortRangeStart = 7147;
        });

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("TcpPortRangeStart and TcpPortRangeEnd must be set together", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartAsync_WhenTcpRangeAndRemotePortBothConfigured_SetsValidationError()
    {
        var sut = CreateSut(o =>
        {
            o.Frp.ProxyType = "tcp";
            o.Frp.RemotePort = 17147;
            o.Frp.TcpPortRangeStart = 7147;
            o.Frp.TcpPortRangeEnd = 7160;
        });

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("either RemotePort or TcpPortRangeStart/End", status.Error, StringComparison.Ordinal);
    }

    private static object? InvokePrivateInstance(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{methodName}' not found on {target.GetType().FullName}.");
        return method.Invoke(target, args);
    }

    private static object? InvokePrivateStatic(Type targetType, string methodName, params object?[] args)
    {
        var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Static method '{methodName}' not found on {targetType.FullName}.");
        return method.Invoke(null, args);
    }
}
