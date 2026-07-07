using System.Collections.Generic;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that <see cref="DesktopLaunchService"/> enforces the
/// desktop-launch feature gate and executable allowlist before invoking the launcher process.
/// The tests use an in-memory configuration root plus a substituted <see cref="IProcessRunner"/>
/// so privileged launch decisions can be asserted without starting real desktop programs.
/// </summary>
public sealed class DesktopLaunchServiceTests
{
    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that the service fails closed when desktop launch
    /// is disabled, even if the launcher path and request payload are otherwise valid.
    /// The test uses the current process path as a deterministic absolute executable fixture and a
    /// substituted runner so the denial path can prove no launcher invocation occurs.
    /// </summary>
    [Fact]
    public async Task LaunchAsync_WhenDesktopLaunchDisabled_ReturnsFailureWithoutInvokingRunner()
    {
        var executablePath = GetExistingExecutablePath();
        var processRunner = Substitute.For<IProcessRunner>();
        var service = CreateService(
            processRunner,
            new DesktopLaunchOptions
            {
                Enabled = false,
                AllowedExecutables = { $"**/{Path.GetFileName(executablePath)}" }
            });

        var result = await service.LaunchAsync(
            Path.GetDirectoryName(executablePath)!,
            new DesktopLaunchRequest { ExecutablePath = executablePath }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("disabled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await processRunner.DidNotReceive().RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that the service rejects executables that do not
    /// match the configured allowlist, even when desktop launch is enabled.
    /// The test uses the current process path plus a deliberately non-matching pattern so the
    /// allowlist check proves the runner stays untouched on denied launches.
    /// </summary>
    [Fact]
    public async Task LaunchAsync_WhenExecutableDoesNotMatchAllowlist_ReturnsFailureWithoutInvokingRunner()
    {
        var executablePath = GetExistingExecutablePath();
        var processRunner = Substitute.For<IProcessRunner>();
        var service = CreateService(
            processRunner,
            new DesktopLaunchOptions
            {
                Enabled = true,
                AllowedExecutables = { "**/not-allowed.exe" }
            });

        var result = await service.LaunchAsync(
            Path.GetDirectoryName(executablePath)!,
            new DesktopLaunchRequest { ExecutablePath = executablePath }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("allowlist", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        await processRunner.DidNotReceive().RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that an enabled service forwards an allowlisted
    /// executable to the launcher after normalizing the payload.
    /// The test uses the current process path for both the launcher fixture and the executable
    /// fixture so the runner can return a deterministic JSON success payload without external files.
    /// </summary>
    [Fact]
    public async Task LaunchAsync_WhenExecutableMatchesAllowlist_InvokesRunner()
    {
        var executablePath = GetExistingExecutablePath();
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner
            .RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, """{"success":true,"processId":4242,"exitCode":0}""", null)));

        var service = CreateService(
            processRunner,
            new DesktopLaunchOptions
            {
                Enabled = true,
                AllowedExecutables = { $"**/{Path.GetFileName(executablePath)}" }
            });

        var result = await service.LaunchAsync(
            Path.GetDirectoryName(executablePath)!,
            new DesktopLaunchRequest
            {
                ExecutablePath = executablePath,
                CreateNoWindow = true,
                WaitForExit = true
            }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(4242, result.ProcessId);
        await processRunner.Received(1).RunAsync(
            executablePath,
            Arg.Is<string>(arguments => arguments != null && arguments.Contains(Path.GetFileName(executablePath), StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    private static DesktopLaunchService CreateService(IProcessRunner processRunner, DesktopLaunchOptions options)
    {
        var launcherPath = GetExistingExecutablePath();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mcp:LauncherPath"] = launcherPath
                })
            .Build();
        return new DesktopLaunchService(
            configuration,
            Microsoft.Extensions.Options.Options.Create(options),
            processRunner,
            NullLogger<DesktopLaunchService>.Instance);
    }

    private static string GetExistingExecutablePath()
        => Environment.ProcessPath
           ?? throw new InvalidOperationException("Expected the test host to expose an executable path.");
}
