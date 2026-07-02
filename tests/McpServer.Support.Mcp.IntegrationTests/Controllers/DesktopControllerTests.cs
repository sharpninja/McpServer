using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies the authenticated HTTP desktop-launch endpoint that
/// fronts the shared desktop-launch service for workspace-scoped callers.
/// The tests replace <see cref="IProcessRunner"/> with a substitute so the controller contract,
/// request routing, and launcher-result normalization can be exercised without starting real
/// desktop programs during integration test execution.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DesktopControllerTests
{
    private const string DesktopLaunchToken = "desktop-launch-test-token";

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that <c>POST /mcpserver/desktop/launch</c>
    /// returns the normalized launcher result and forwards the structured launch payload to the
    /// shared process-runner abstraction.
    /// The test uses an existing launcher-path override plus a substituted process runner that
    /// returns a deterministic success payload so the HTTP contract stays stable and safe.
    /// </summary>
    [Fact]
    public async Task Launch_ReturnsOkAndNormalizedResult()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner
            .RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, """{"success":true,"processId":4242,"exitCode":0}""", null)));

        var launcherPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Expected the integration test host to expose an executable path.");
        var workingDirectory = Path.GetDirectoryName(launcherPath)
            ?? throw new InvalidOperationException("Expected a launcher working directory.");

        using var factory = new CustomWebApplicationFactory(
            services =>
            {
                services.RemoveAll<IProcessRunner>();
                services.AddSingleton(processRunner);
            },
            new Dictionary<string, string?>
            {
                ["Mcp:LauncherPath"] = launcherPath,
                ["Mcp:DesktopLaunch:Enabled"] = "true",
                ["Mcp:DesktopLaunch:AccessToken"] = DesktopLaunchToken,
                ["Mcp:DesktopLaunch:AllowedExecutables:0"] = $"**/{Path.GetFileName(launcherPath)}"
            });

        var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);
        client.DefaultRequestHeaders.Add("X-Desktop-Launch-Token", DesktopLaunchToken);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/desktop/launch", UriKind.Relative),
            new DesktopLaunchRequest
            {
                ExecutablePath = launcherPath,
                Arguments = "/c exit 0",
                WorkingDirectory = workingDirectory,
                EnvironmentVariables = new Dictionary<string, string> { ["TEST_ENV"] = "true" },
                CreateNoWindow = true,
                WindowStyle = "Hidden",
                WaitForExit = true,
                TimeoutMs = 5000
            }).ConfigureAwait(true);

        var result = await response.Content.ReadFromJsonAsync<DesktopLaunchResult>().ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal(4242, result.ProcessId);
        Assert.Equal(0, result.ExitCode);
        await processRunner.Received(1).RunAsync(
            launcherPath,
            Arg.Is<string>(arguments =>
                arguments != null
                && arguments.Contains(Path.GetFileName(launcherPath), StringComparison.Ordinal)
                && arguments.Contains("TEST_ENV", StringComparison.Ordinal)
                && arguments.Contains("Hidden", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that the controller rejects a missing request
    /// body with HTTP 400 instead of attempting a launch.
    /// The test uses the default authenticated integration-test host because only request-body
    /// validation behavior matters for this failure path.
    /// </summary>
    [Fact]
    public async Task Launch_WithoutBody_ReturnsBadRequest()
    {
        using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(new Uri("/mcpserver/desktop/launch", UriKind.Relative), value: (object?)null).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Verifies that the HTTP desktop-launch endpoint rejects
    /// authenticated workspace-key callers that do not also present the privileged desktop-launch
    /// token header.
    /// The test uses the real HTTP pipeline plus a substituted process runner so the stronger
    /// authorization tier can be asserted without starting any local desktop program.
    /// </summary>
    [Fact]
    public async Task Launch_WithoutDesktopLaunchToken_ReturnsForbidden()
    {
        var launcherPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("Expected the integration test host to expose an executable path.");
        var processRunner = Substitute.For<IProcessRunner>();
        using var factory = new CustomWebApplicationFactory(
            services =>
            {
                services.RemoveAll<IProcessRunner>();
                services.AddSingleton(processRunner);
            },
            new Dictionary<string, string?>
            {
                ["Mcp:LauncherPath"] = launcherPath,
                ["Mcp:DesktopLaunch:Enabled"] = "true",
                ["Mcp:DesktopLaunch:AccessToken"] = DesktopLaunchToken,
                ["Mcp:DesktopLaunch:AllowedExecutables:0"] = $"**/{Path.GetFileName(launcherPath)}"
            });

        var client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(client, factory.Services);

        var response = await client.PostAsJsonAsync(
            new Uri("/mcpserver/desktop/launch", UriKind.Relative),
            new DesktopLaunchRequest { ExecutablePath = launcherPath }).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await processRunner.DidNotReceive().RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
