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
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "/fixture/workspaces/McpServer" }),
            factory,
            Normalizer(LinuxHub));

        var resolved = sut.Resolve(new WorkspaceContext { WorkspacePath = "/fixture/workspaces/McpServer/" });

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
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "/fixture/workspaces/McpServer" }),
            factory,
            Normalizer(LinuxHub));
        var context = new WorkspaceContext { WorkspacePath = "C:/Users/fixture-user/repo" };

        sut.Resolve(context);

        factory.Received(1).CreateForWorkspace("C:/Users/fixture-user/repo", context);
    }

    /// <summary><see cref="TodoServiceFactory"/>: workspace and default roots by path syntax.</summary>
    [Fact]
    public void TodoFactory_WindowsHost_WorkspaceRoots()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal("/fixture/workspaces/ExampleWorkspace", TodoServiceFactory.NormalizeWorkspaceRoot("/fixture/workspaces/ExampleWorkspace/", windows));
        Assert.Equal("C:/svc", TodoServiceFactory.NormalizeWorkspaceRoot(null, windows));
    }

    /// <summary><see cref="EfTodoService"/>: fixed workspace path by path syntax; blank stays unset.</summary>
    [Fact]
    public void EfTodo_LinuxHub_FixedWorkspacePath()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal("C:/Users/fixture-user/repo", EfTodoService.NormalizeFixedWorkspacePath("C:/Users/fixture-user/repo", linux));
        Assert.Null(EfTodoService.NormalizeFixedWorkspacePath("  ", linux));
    }

    /// <summary><see cref="TodoExecutionService"/>: execution workspace by path syntax; blank rejected.</summary>
    [Fact]
    public void TodoExecution_LinuxHub_WorkspacePath()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal("C:/Users/fixture-user/repo", TodoExecutionService.NormalizeWorkspacePath("C:/Users/fixture-user/repo", linux));
        Assert.Throws<ArgumentException>(() => TodoExecutionService.NormalizeWorkspacePath(" ", linux));
    }

    /// <summary><see cref="RequirementsDatabaseDocumentService"/>: request workspace scope by path syntax.</summary>
    [Fact]
    public void Requirements_WindowsHost_RequestWorkspacePath()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal(
            "/fixture/workspaces/ExampleWorkspace",
            RequirementsDatabaseDocumentService.NormalizeRequestWorkspacePath("/fixture/workspaces/ExampleWorkspace/", windows));
        Assert.Null(RequirementsDatabaseDocumentService.NormalizeRequestWorkspacePath(null, windows));
    }

    /// <summary><see cref="HostileReviewService"/>: workspace match by path syntax.</summary>
    [Fact]
    public void HostileReview_WindowsHost_WorkspaceMatch()
    {
        var windows = Normalizer(WindowsHost);

        Assert.True(HostileReviewService.IsSameWorkspace("/fixture/workspaces/ExampleWorkspace/", "/fixture/workspaces/ExampleWorkspace", windows));
        Assert.False(HostileReviewService.IsSameWorkspace("/fixture/workspaces/ExampleWorkspaceOther", "/fixture/workspaces/ExampleWorkspace", windows));
    }

    /// <summary>
    /// <see cref="AgentPoolService"/>: the pooled device id hashes the canonical workspace path, so
    /// equivalent spellings share an id and distinct workspaces do not.
    /// </summary>
    [Fact]
    public void AgentPool_DeviceId_HashesCanonicalWorkspace()
    {
        Assert.Equal("agent-pool-codex@f0e75d9e17fa", AgentPoolService.BuildPooledDeviceId("codex", "C:/Users/fixture-user/repo", Normalizer(LinuxHub)));
        Assert.Equal("agent-pool-codex@f0e75d9e17fa", AgentPoolService.BuildPooledDeviceId("codex", @"C:\Users\fixture-user\repo", Normalizer(LinuxHub)));
        Assert.Equal("agent-pool-codex@68b5017a3bb9", AgentPoolService.BuildPooledDeviceId("codex", "/fixture/workspaces/ExampleWorkspace/", Normalizer(WindowsHost)));
    }

    /// <summary><see cref="FileGitHubWorkspaceTokenStore"/>: token key by path syntax; blank rejected.</summary>
    [Fact]
    public void GitHubTokenStore_WindowsHost_WorkspaceKey()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal("/fixture/workspaces/ExampleWorkspace", FileGitHubWorkspaceTokenStore.NormalizeWorkspacePath("/fixture/workspaces/ExampleWorkspace/", windows));
        Assert.Throws<ArgumentException>(() => FileGitHubWorkspaceTokenStore.NormalizeWorkspacePath("", windows));
    }

    /// <summary>
    /// Plan P1.4d: <see cref="ToolRegistryService"/> scopes a tool to the canonical workspace path; a
    /// Windows workspace on a Linux hub is not resolved against the hub's working directory.
    /// </summary>
    [Fact]
    public void ToolRegistry_LinuxHub_WindowsWorkspacePath()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal("C:/Users/fixture-user/repo", ToolRegistryService.NormalizeWorkspacePath(@"C:\Users\fixture-user\repo\", linux));
        Assert.Null(ToolRegistryService.NormalizeWorkspacePath("  ", linux));
    }

    /// <summary>Plan P1.4d: <see cref="AgentService"/> agent-workspace paths use the canonical form.</summary>
    [Fact]
    public void AgentWorkspace_LinuxHub_WindowsWorkspacePath()
    {
        Assert.Equal("C:/Users/fixture-user/repo", AgentService.NormalizeWorkspacePath(@"C:\Users\fixture-user\repo\", Normalizer(LinuxHub)));
    }

    /// <summary>
    /// Plan P1.4d: <see cref="WorkspaceService"/> synthesizes the RepoRoot workspace in canonical form
    /// (absolute as given, relative under the content root), so it matches configured entries.
    /// </summary>
    [Fact]
    public void WorkspaceRepoRoot_WindowsHost_CanonicalPath()
    {
        var windows = Normalizer(WindowsHost);

        Assert.Equal("C:/Users/fixture-user/repo", WorkspaceService.ResolveRepoRootWorkspacePath(@"C:\Users\fixture-user\repo\", @"C:\svc", windows));
        Assert.Equal("C:/svc/repo", WorkspaceService.ResolveRepoRootWorkspacePath("repo", @"C:\svc", windows));
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
