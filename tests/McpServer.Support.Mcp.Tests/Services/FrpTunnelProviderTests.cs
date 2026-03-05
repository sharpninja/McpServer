using System.Diagnostics;
using System.Reflection;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for FRP tunnel provider helper logic and failure paths.</summary>
public sealed class FrpTunnelProviderTests
{
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();

    [Fact]
    public async Task StartAsync_WhenProxyTypeUnsupported_SetsValidationErrorAndSkipsCliCheck()
    {
        var sut = CreateSut(o => o.Frp.ProxyType = "udp");

        await sut.StartAsync(CancellationToken.None).ConfigureAwait(true);

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("ProxyType 'udp' is not supported yet", status.Error, StringComparison.Ordinal);
        _ = _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default);
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

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
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

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
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
    public async Task GetStatusAsync_WhenTrackedProcessExited_ReturnsExitError()
    {
        var sut = CreateSut();
        using var exitedProcess = StartExitedDotnetProcess();

        SetPrivateField(sut, "_process", exitedProcess);
        SetPrivateField(sut, "_lastStderrLine", "simulated frpc startup failure");

        var status = await sut.GetStatusAsync().ConfigureAwait(true);

        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("exit code", status.Error, StringComparison.Ordinal);
        Assert.Contains("simulated frpc startup failure", status.Error, StringComparison.Ordinal);
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

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
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

        var status = await sut.GetStatusAsync().ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.NotNull(status.Error);
        Assert.Contains("either RemotePort or TcpPortRangeStart/End", status.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_WhenTempConfigExists_DeletesFileAndClearsConfigPath()
    {
        var sut = CreateSut();
        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"frpc-test-{Guid.NewGuid():N}.toml");
        File.WriteAllText(tempConfigPath, "test");
        Assert.True(File.Exists(tempConfigPath));

        SetPrivateField(sut, "_configPath", tempConfigPath);

        sut.Dispose();

        Assert.False(File.Exists(tempConfigPath));
        Assert.Null(GetPrivateField<string?>(sut, "_configPath"));
    }

    private FrpTunnelProvider CreateSut(Action<TunnelOptions>? configure = null)
        => new(CreateOptions(configure), _processRunner, NullLogger<FrpTunnelProvider>.Instance);

    private static IOptions<TunnelOptions> CreateOptions(Action<TunnelOptions>? configure = null)
    {
        var options = new TunnelOptions { Port = 7147 };
        configure?.Invoke(options);
        return Microsoft.Extensions.Options.Options.Create(options);
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

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        field.SetValue(target, value);
    }

    private static T? GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        return (T?)field.GetValue(target);
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
