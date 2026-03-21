using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-GH-006: Validates GitHub controller boundary hardening for state and close-reason query
/// parameters so only canonical values reach <see cref="IGitHubCliService"/> and the gh CLI layer.
/// </summary>
public sealed class GitHubControllerTests
{
    /// <summary>
    /// TEST-MCP-GH-006: Verifies that invalid close reasons are rejected at the controller boundary before
    /// the GitHub CLI service is invoked, preventing query-string flag injection in issue-close requests.
    /// </summary>
    [Fact]
    public async Task CloseIssueAsync_WithInvalidReason_ReturnsBadRequest()
    {
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var controller = CreateController(gitHubCliService);

        var result = await controller.CloseIssueAsync(42, "completed --repo other/repo", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await gitHubCliService.DidNotReceive().CloseIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-GH-006: Verifies that list-state query parameters are normalized to canonical lowercase
    /// values before forwarding them to the GitHub CLI service, preventing raw user input from reaching gh.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithMixedCaseState_NormalizesBeforeCallingService()
    {
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        gitHubCliService.ListIssuesAsync("open", 30, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, Array.Empty<GitHubIssueItem>()));

        var controller = CreateController(gitHubCliService);
        var result = await controller.ListIssuesAsync(" Open ", 30, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<OkObjectResult>(result.Result);
        await gitHubCliService.Received(1).ListIssuesAsync("open", 30, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-GH-006: Verifies that invalid list-state query parameters are rejected before the GitHub CLI
    /// service is invoked, blocking raw multi-token input from being incorporated into gh list commands.
    /// </summary>
    [Fact]
    public async Task ListPullsAsync_WithInvalidState_ReturnsBadRequest()
    {
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var controller = CreateController(gitHubCliService);

        var result = await controller.ListPullsAsync("open --repo other/repo", 30, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        await gitHubCliService.DidNotReceive().ListPullsAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static GitHubController CreateController(IGitHubCliService gitHubCliService)
    {
        var tokenStore = Substitute.For<IGitHubWorkspaceTokenStore>();
        var gitHubOptions = Substitute.For<IOptionsMonitor<GitHubIntegrationOptions>>();
        gitHubOptions.CurrentValue.Returns(new GitHubIntegrationOptions());

        return new GitHubController(
            gitHubCliService,
            tokenStore,
            gitHubOptions,
            syncService: null,
            eventBus: null,
            logger: NullLogger<GitHubController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
