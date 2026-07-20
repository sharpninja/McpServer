using System.Diagnostics;

using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Tests.TestSupport;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TUN-004 (under FR-MCP-141): verifies <see cref="NgrokTunnelProvider"/> reproduces the interactive
/// user's process environment for the ngrok child process when the host runs under a service account, and
/// that the ngrok-not-found path is reported as a warning with actionable remedies.
/// Deliberately kept out of <c>TunnelProviderTests</c> because that class carries
/// <c>[Trait("Category", "Integration")]</c>, which the default Nuke <c>Test</c> target excludes.
/// </summary>
public sealed class NgrokTunnelProviderEnvironmentTests
{
    private const string UserProfile = @"C:\Users\interactive";
    private const string UserPath = @"C:\Users\interactive\tools\ngrok";
    private const string ResolvedNgrok = @"C:\Users\interactive\tools\ngrok\ngrok.exe";

    private static IOptions<TunnelOptions> CreateOptions(Action<TunnelOptions>? configure = null)
    {
        var opts = new TunnelOptions { Port = 7147 };
        configure?.Invoke(opts);
        return Microsoft.Extensions.Options.Options.Create(opts);
    }

    /// <summary>
    /// TR-MCP-TUN-004: the ngrok child <see cref="ProcessStartInfo"/> carries the interactive user's
    /// <c>USERPROFILE</c>, <c>HOME</c>, <c>APPDATA</c>, and <c>PATH</c> so ngrok can locate <c>ngrok.yml</c>.
    /// Fixture: a hand-written <see cref="StubProcessEnvironmentService"/> standing in for the registry/WTS
    /// lookups, plus default <see cref="TunnelOptions"/> on port 7147.
    /// </summary>
    [Fact]
    public void BuildNgrokStartInfo_AppliesInteractiveUserEnvironment()
    {
        var env = new StubProcessEnvironmentService();
        using var sut = new NgrokTunnelProvider(
            CreateOptions(),
            Substitute.For<IProcessRunner>(),
            env,
            new TestLogger<NgrokTunnelProvider>());

        var startInfo = sut.BuildNgrokStartInfo(ResolvedNgrok, "http 7147");

        Assert.Equal(ResolvedNgrok, startInfo.FileName);
        Assert.Equal(UserProfile, startInfo.Environment["USERPROFILE"]);
        Assert.Equal(UserProfile, startInfo.Environment["HOME"]);
        Assert.Equal(Path.Combine(UserProfile, "AppData", "Roaming"), startInfo.Environment["APPDATA"]);
        Assert.Equal(UserPath, startInfo.Environment["PATH"]);
    }

    /// <summary>
    /// TR-MCP-TUN-004: the auth token is still injected via the environment (never the command line) after
    /// the user environment is applied. Fixture: <see cref="TunnelOptions"/> with
    /// <c>Ngrok.AuthToken = "token-123"</c> and the stub environment service.
    /// </summary>
    [Fact]
    public void BuildNgrokStartInfo_StillInjectsAuthTokenViaEnvironment()
    {
        var env = new StubProcessEnvironmentService();
        using var sut = new NgrokTunnelProvider(
            CreateOptions(o => o.Ngrok.AuthToken = "token-123"),
            Substitute.For<IProcessRunner>(),
            env,
            new TestLogger<NgrokTunnelProvider>());

        var startInfo = sut.BuildNgrokStartInfo(ResolvedNgrok, "http 7147");

        Assert.Equal("token-123", startInfo.Environment["NGROK_AUTHTOKEN"]);
    }

    /// <summary>
    /// TR-MCP-TUN-004: with no configured <c>ExecutablePath</c>, the provider resolves the ngrok binary
    /// through <see cref="IProcessEnvironmentService.ResolveExecutable"/> against the enriched PATH instead
    /// of handing the literal string <c>ngrok</c> to <see cref="Process"/>.
    /// Fixture: default <see cref="TunnelOptions"/> plus the stub environment service that injects
    /// <c>C:\Users\interactive\tools\ngrok</c> on PATH and resolves <c>ngrok</c> from it.
    /// </summary>
    [Fact]
    public void ResolveExecutablePath_WithoutConfiguredPath_ResolvesAgainstEnrichedPath()
    {
        var env = new StubProcessEnvironmentService();
        using var sut = new NgrokTunnelProvider(
            CreateOptions(),
            Substitute.For<IProcessRunner>(),
            env,
            new TestLogger<NgrokTunnelProvider>());

        var resolved = sut.ResolveExecutablePath();

        Assert.Equal(ResolvedNgrok, resolved);
        Assert.Equal("ngrok", Assert.Single(env.ResolveRequests).FileName);
        Assert.Equal(UserPath, Assert.Single(env.ResolveRequests).Path);
    }

    /// <summary>
    /// TR-MCP-TUN-004: an explicitly configured <c>Mcp:Tunnel:Ngrok:ExecutablePath</c> wins and the
    /// environment probe is not consulted. Fixture: <see cref="TunnelOptions"/> with
    /// <c>Ngrok.ExecutablePath = @"D:\portable\ngrok.exe"</c>.
    /// </summary>
    [Fact]
    public void ResolveExecutablePath_WithConfiguredPath_UsesConfiguredValue()
    {
        var env = new StubProcessEnvironmentService();
        using var sut = new NgrokTunnelProvider(
            CreateOptions(o => o.Ngrok.ExecutablePath = @"D:\portable\ngrok.exe"),
            Substitute.For<IProcessRunner>(),
            env,
            new TestLogger<NgrokTunnelProvider>());

        var resolved = sut.ResolveExecutablePath();

        Assert.Equal(@"D:\portable\ngrok.exe", resolved);
        Assert.Empty(env.ResolveRequests);
    }

    /// <summary>
    /// TR-MCP-TUN-004: a missing ngrok CLI is a recoverable configuration condition, so it is logged at
    /// <see cref="LogLevel.Warning"/> (never <see cref="LogLevel.Error"/>) and the message names both the
    /// Windows-service and Microsoft-Store-alias causes plus the remedies.
    /// Fixture: <see cref="IProcessRunner"/> substitute returning exit code 1 for <c>ngrok version</c>,
    /// the stub environment service, and a capturing <see cref="TestLogger{T}"/>.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenNgrokMissing_LogsWarningNamingServiceAndStoreCauses()
    {
        var env = new StubProcessEnvironmentService { ResolvedExecutable = "ngrok" };
        var runner = Substitute.For<IProcessRunner>();
        runner.RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));
        var logger = new TestLogger<NgrokTunnelProvider>();

        using var sut = new NgrokTunnelProvider(CreateOptions(), runner, env, logger);
        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);

        Assert.Contains("ngrok CLI not found", warning.Message, StringComparison.Ordinal);
        Assert.Contains("Windows service", warning.Message, StringComparison.Ordinal);
        Assert.Contains("Microsoft Store", warning.Message, StringComparison.Ordinal);
        Assert.Contains("https://ngrok.com/download", warning.Message, StringComparison.Ordinal);
        Assert.Contains("Mcp:Tunnel:Ngrok:ExecutablePath", warning.Message, StringComparison.Ordinal);
        Assert.Contains("Mcp:Tunnel:Provider", warning.Message, StringComparison.Ordinal);

        var status = await sut.GetStatusAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.False(status.IsRunning);
        Assert.Equal(warning.Message, status.Error);
    }

    /// <summary>
    /// TR-MCP-TUN-004: when a configured executable path is present but wrong, the warning points at that
    /// path rather than repeating the PATH-resolution guidance.
    /// Fixture: <c>Ngrok.ExecutablePath = @"D:\missing\ngrok.exe"</c> and a runner returning exit code 1.
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenConfiguredNgrokPathMissing_WarnsAboutConfiguredPath()
    {
        var env = new StubProcessEnvironmentService();
        var runner = Substitute.For<IProcessRunner>();
        runner.RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));
        var logger = new TestLogger<NgrokTunnelProvider>();

        using var sut = new NgrokTunnelProvider(
            CreateOptions(o => o.Ngrok.ExecutablePath = @"D:\missing\ngrok.exe"),
            runner,
            env,
            logger);
        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains(@"D:\missing\ngrok.exe", warning.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Hand-written <see cref="IProcessEnvironmentService"/> double standing in for the Windows
    /// registry and WTS lookups. Applies a fixed interactive-user environment and records every
    /// <see cref="ResolveExecutable"/> probe so tests can assert the PATH it was given.
    /// </summary>
    private sealed class StubProcessEnvironmentService : IProcessEnvironmentService
    {
        /// <summary>Value returned from <see cref="ResolveExecutable"/> for a bare file name.</summary>
        public string ResolvedExecutable { get; init; } = ResolvedNgrok;

        /// <summary>File name and PATH captured for each <see cref="ResolveExecutable"/> call.</summary>
        public List<(string FileName, string? Path)> ResolveRequests { get; } = [];

        /// <inheritdoc />
        public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
        {
        }

        /// <inheritdoc />
        public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
        {
            ArgumentNullException.ThrowIfNull(psi);
            psi.Environment["USERPROFILE"] = UserProfile;
            psi.Environment["HOME"] = UserProfile;
            psi.Environment["APPDATA"] = System.IO.Path.Combine(UserProfile, "AppData", "Roaming");
            psi.Environment["LOCALAPPDATA"] = System.IO.Path.Combine(UserProfile, "AppData", "Local");
            psi.Environment["PATH"] = UserPath;
        }

        /// <inheritdoc />
        public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
        {
            ApplyRunAsEnvironment(psi, runAsUser);
            ApplyGitHubToken(psi, gitHubToken);
        }

        /// <inheritdoc />
        public string ResolveExecutable(ProcessStartInfo psi, string fileName)
        {
            ArgumentNullException.ThrowIfNull(psi);
            ResolveRequests.Add((fileName, psi.Environment.TryGetValue("PATH", out var p) ? p : null));
            return ResolvedExecutable;
        }
    }
}
