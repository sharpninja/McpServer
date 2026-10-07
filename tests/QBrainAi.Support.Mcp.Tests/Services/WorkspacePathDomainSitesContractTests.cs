using QBrainAi.Client;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.Storage;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001 (plan P1.R R4): the workspace-identity computations
/// in domain services that resolve request-supplied workspace paths. Each case uses a simulated
/// Linux hub or Windows host. BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// </summary>
public abstract class WorkspacePathDomainSitesContractTests
{
    private const WorkspacePathPlatform LinuxHub = WorkspacePathPlatform.CaseSensitive;
    private const WorkspacePathPlatform WindowsHost = WorkspacePathPlatform.Windows;

    /// <summary>Normalizer for a simulated host of the given platform.</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);

    /// <summary>
    /// <see cref="TodoServiceResolver"/>: an equivalent spelling of the primary workspace resolves
    /// to the primary service without creating a per-workspace store.
    /// </summary>
    [Fact]
    public void TodoResolver_LinuxHub_PrimaryMatchedByEquivalentSpelling()
    {
        var primary = Substitute.For<ITodoService>();
        var factory = Substitute.For<ITodoServiceFactory>();
        using var sut = new TodoServiceResolver(
            primary,
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "/home/sharpninja/github/McpServer" }),
            factory,
            Normalizer(LinuxHub));

        var resolved = sut.Resolve(new WorkspaceContext { WorkspacePath = "/home/sharpninja/github/McpServer/" });

        Assert.Same(primary, resolved);
        factory.DidNotReceiveWithAnyArgs().CreateForWorkspace(default!, default!);
    }

    /// <summary>
    /// <see cref="TodoServiceResolver"/>: on a Linux hub a Windows workspace store is created for
    /// the canonical Windows path, not a hub-rooted rewrite.
    /// </summary>
    [Fact]
    public void TodoResolver_LinuxHub_WindowsWorkspace_CreatesStoreForCanonicalPath()
    {
        var factory = Substitute.For<ITodoServiceFactory>();
        using var sut = new TodoServiceResolver(
            Substitute.For<ITodoService>(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "/home/sharpninja/github/McpServer" }),
            factory,
            Normalizer(LinuxHub));
        var context = new WorkspaceContext { WorkspacePath = "C:/Users/kingd/repo" };

        sut.Resolve(context);

        factory.Received(1).CreateForWorkspace(@"C:\Users\kingd\repo", context);
    }

    /// <summary><see cref="TodoServiceFactory"/>: workspace and default roots by path syntax.</summary>
    [Fact]
    public void TodoFactory_WindowsHost_WorkspaceRoots()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal("/home/sharpninja/github/RideAudit", TodoServiceFactory.NormalizeWorkspaceRoot("/home/sharpninja/github/RideAudit/", windows));
        Assert.Equal(@"C:\svc", TodoServiceFactory.NormalizeWorkspaceRoot(null, windows));
    }

    /// <summary><see cref="EfTodoService"/>: fixed workspace path by path syntax; blank stays unset.</summary>
    [Fact]
    public void EfTodo_LinuxHub_FixedWorkspacePath()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal(@"C:\Users\kingd\repo", EfTodoService.NormalizeFixedWorkspacePath("C:/Users/kingd/repo", linux));
        Assert.Null(EfTodoService.NormalizeFixedWorkspacePath("  ", linux));
    }

    /// <summary><see cref="TodoExecutionService"/>: execution workspace by path syntax; blank rejected.</summary>
    [Fact]
    public void TodoExecution_LinuxHub_WorkspacePath()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal(@"C:\Users\kingd\repo", TodoExecutionService.NormalizeWorkspacePath("C:/Users/kingd/repo", linux));
        Assert.Throws<ArgumentException>(() => TodoExecutionService.NormalizeWorkspacePath(" ", linux));
    }

    /// <summary><see cref="RequirementsDatabaseDocumentService"/>: request workspace scope by path syntax.</summary>
    [Fact]
    public void Requirements_WindowsHost_RequestWorkspacePath()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal(
            "/home/sharpninja/github/RideAudit",
            RequirementsDatabaseDocumentService.NormalizeRequestWorkspacePath("/home/sharpninja/github/RideAudit/", windows));
        Assert.Null(RequirementsDatabaseDocumentService.NormalizeRequestWorkspacePath(null, windows));
    }

    /// <summary><see cref="HostileReviewService"/>: workspace match by path syntax.</summary>
    [Fact]
    public void HostileReview_WindowsHost_WorkspaceMatch()
    {
        var windows = Normalizer(WindowsHost);

        Assert.True(HostileReviewService.IsSameWorkspace("/home/sharpninja/github/RideAudit/", "/home/sharpninja/github/RideAudit", windows));
        Assert.False(HostileReviewService.IsSameWorkspace("/home/sharpninja/github/RideAuditOther", "/home/sharpninja/github/RideAudit", windows));
    }

    /// <summary>
    /// <see cref="AgentPoolService"/>: the pooled device id hashes the canonical workspace path, so
    /// equivalent spellings share an id and distinct workspaces do not.
    /// </summary>
    [Fact]
    public void AgentPool_DeviceId_HashesCanonicalWorkspace()
    {
        Assert.Equal("agent-pool-codex@cc136810e4f7", AgentPoolService.BuildPooledDeviceId("codex", "C:/Users/kingd/repo", Normalizer(LinuxHub)));
        Assert.Equal("agent-pool-codex@cc136810e4f7", AgentPoolService.BuildPooledDeviceId("codex", @"C:\Users\kingd\repo", Normalizer(LinuxHub)));
        Assert.Equal("agent-pool-codex@d5731a58e39a", AgentPoolService.BuildPooledDeviceId("codex", "/home/sharpninja/github/RideAudit/", Normalizer(WindowsHost)));
    }

    /// <summary><see cref="FileGitHubWorkspaceTokenStore"/>: token key by path syntax; blank rejected.</summary>
    [Fact]
    public void GitHubTokenStore_WindowsHost_WorkspaceKey()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal("/home/sharpninja/github/RideAudit", FileGitHubWorkspaceTokenStore.NormalizeWorkspacePath("/home/sharpninja/github/RideAudit/", windows));
        Assert.Throws<ArgumentException>(() => FileGitHubWorkspaceTokenStore.NormalizeWorkspacePath("", windows));
    }
}

/// <summary>BDP v4 step 2: domain-site identity contracts against scripted normalizer data.</summary>
public sealed class WorkspacePathDomainSitesMockDataTests : WorkspacePathDomainSitesContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: domain-site identity contracts against the production normalizer.</summary>
public sealed class WorkspacePathDomainSitesRealTests : WorkspacePathDomainSitesContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
