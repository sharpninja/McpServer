using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WorkspaceProcessManager"/> hosted-service startup behavior.
/// Validates TR-MCP-WS-003: workspace registration loop resilience.
/// </summary>
public sealed class WorkspaceProcessManagerStartupTests : IDisposable
{
    private readonly string _tempRoot;

    /// <summary>Initializes a new instance; creates a temp directory for test workspaces.</summary>
    public WorkspaceProcessManagerStartupTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"wpm_startup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// When a workspace with an empty WorkspacePath (e.g. the "global" pseudo-workspace) appears
    /// in the middle of the list, startup must skip it and continue registering subsequent
    /// workspaces rather than throwing and aborting the loop.
    /// Validates the fix for the RiskyStars marker-file regression: the global workspace entry
    /// caused Path.GetFullPath("") to throw, stopping all registrations that followed it.
    /// Uses fixture: two temp directories as valid workspaces, one empty-path workspace between them.
    /// Requirement: TR-MCP-WS-003.
    /// </summary>
    [Fact]
    public async Task StartAsync_EmptyWorkspacePathInMiddle_SkipsItAndRegistersSubsequentWorkspaces()
    {
        var workspaceA = Path.Combine(_tempRoot, "workspace-a");
        var workspaceB = Path.Combine(_tempRoot, "workspace-b");
        Directory.CreateDirectory(workspaceA);
        Directory.CreateDirectory(workspaceB);

        var tokenService = new WorkspaceTokenService();
        var sut = BuildSut(tokenService, workspaces:
        [
            MakeWorkspace(workspaceA, "A"),
            MakeWorkspace("", "global"),
            MakeWorkspace(workspaceB, "B"),
        ]);

        await ((IHostedService)sut).StartAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.NotNull(tokenService.GetToken(workspaceA));
        Assert.NotNull(tokenService.GetToken(workspaceB));
    }

    /// <summary>
    /// When a workspace with a null WorkspacePath appears in the list, startup skips it
    /// without throwing.
    /// Uses fixture: single valid workspace before the null-path entry.
    /// Requirement: TR-MCP-WS-003.
    /// </summary>
    [Fact]
    public async Task StartAsync_NullWorkspacePathEntry_SkipsWithoutThrowing()
    {
        var workspaceA = Path.Combine(_tempRoot, "workspace-only");
        Directory.CreateDirectory(workspaceA);

        var tokenService = new WorkspaceTokenService();
        var sut = BuildSut(tokenService, workspaces:
        [
            MakeWorkspace(workspaceA, "Only"),
            MakeWorkspace(null!, "null-path"),
        ]);

        var ex = await Record.ExceptionAsync(() =>
            ((IHostedService)sut).StartAsync(CancellationToken.None)).ConfigureAwait(true);

        Assert.Null(ex);
        Assert.NotNull(tokenService.GetToken(workspaceA));
    }

    private WorkspaceProcessManager BuildSut(
        WorkspaceTokenService tokenService,
        IReadOnlyList<WorkspaceDto> workspaces)
    {
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(workspaces, workspaces.Count));

        var agentService = Substitute.For<IAgentService>();
        agentService.ListWorkspaceAgentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentWorkspaceListResult { Items = [], TotalCount = 0 });

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IWorkspaceService)).Returns(workspaceService);
        scope.ServiceProvider.GetService(typeof(IAgentService)).Returns(agentService);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var promptOptions = Substitute.For<IOptionsMonitor<MarkerPromptOptions>>();
        promptOptions.CurrentValue.Returns(new MarkerPromptOptions
        {
            MarkerPromptTemplate = "You are connected at {{baseUrl}}.",
        });

        var markerPromptProvider = Substitute.For<IMarkerPromptProvider>();
        markerPromptProvider.GetGlobalPromptTemplateAsync(Arg.Any<CancellationToken>())
            .Returns("You are connected at {{baseUrl}}.");

        var runtime = new ServerRuntimeInfo(DateTimeOffset.UtcNow, 7147);

        return new WorkspaceProcessManager(
            NullLogger<WorkspaceProcessManager>.Instance,
            NullLoggerFactory.Instance,
            serviceProvider,
            promptOptions,
            markerPromptProvider,
            tokenService,
            runtime);
    }

    private static WorkspaceDto MakeWorkspace(string path, string name) => new()
    {
        WorkspacePath = path,
        Name = name,
        TodoPath = "docs/todo.yaml",
        StatusPrompt = string.Empty,
        ImplementPrompt = string.Empty,
        PlanPrompt = string.Empty,
        IsEnabled = true,
    };
}
