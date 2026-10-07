using QBrainAi.Client;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Middleware;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: contract tests for the smaller request-path consumers
/// (<see cref="HandoffWorkspacePaths"/>, <see cref="WorkspaceTokenService"/>,
/// <see cref="SessionLogWorkspaceAttributionValidator"/>, <see cref="WorkspaceContext.SetDerivedPaths"/>,
/// <see cref="WorkspaceResolutionMiddleware"/>, <see cref="RepoFileService"/>). Every case uses a
/// simulated Linux hub or Windows host, so results do not depend on the test runner's OS.
/// BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// </summary>
public abstract class WorkspacePathConsumersContractTests
{
    private const WorkspacePathPlatform LinuxHub = WorkspacePathPlatform.CaseSensitive;
    private const WorkspacePathPlatform WindowsHost = WorkspacePathPlatform.Windows;

    /// <summary>Normalizer for a simulated host of the given platform.</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);

    // ---- HandoffWorkspacePaths (every MCP tool workspace override flows through it) ----

    /// <summary>FR-MCP-FED-PATH-001-AC2: a POSIX workspace on a Windows host is not re-rooted.</summary>
    [Fact]
    public void Handoff_WindowsHost_PosixPath_IsNotReRooted()
    {
        Assert.Equal(
            "/home/sharpninja/github/McpServer",
            HandoffWorkspacePaths.Canonicalize("/home/sharpninja/github/McpServer", Normalizer(WindowsHost)));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: on a Linux hub a Windows path stays unmangled and a relative path
    /// anchors at the hub working directory.
    /// </summary>
    [Fact]
    public void Handoff_LinuxHub_UsesPathSyntax()
    {
        var linux = Normalizer(LinuxHub);

        Assert.Equal(@"C:\Users\kingd", HandoffWorkspacePaths.Canonicalize(@"C:\Users\kingd", linux));
        Assert.Equal("/opt/mcpserver/app/ws", HandoffWorkspacePaths.Canonicalize("ws", linux));
    }

    // ---- WorkspaceTokenService ----

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: a token for a foreign-platform workspace resolves back to that
    /// workspace's own path and validates for an equivalent spelling.
    /// </summary>
    [Fact]
    public void Token_WindowsHost_PosixWorkspace_ResolvesUnmangled()
    {
        var sut = new WorkspaceTokenService(Normalizer(WindowsHost));
        var token = sut.GenerateToken("/home/sharpninja/github/McpServer");

        Assert.Equal("/home/sharpninja/github/McpServer", sut.ResolveWorkspaceByToken(token));
        Assert.True(sut.ValidateToken("/home/sharpninja/github/McpServer/", token));
    }

    // ---- SessionLogWorkspaceAttributionValidator ----

    /// <summary>
    /// FR-MCP-FED-PATH-001: a host path that only "matches" a foreign root after the host re-roots
    /// the root is outside the workspace (Windows host: C:\home\...; Linux hub: /opt/.../C:\...).
    /// </summary>
    [Theory]
    [InlineData(WindowsHost, "/home/sharpninja/github/RideAudit", @"C:\home\sharpninja\github\RideAudit\src\a.cs")]
    [InlineData(LinuxHub, @"C:\Users\kingd\repo", @"/opt/mcpserver/app/C:\Users\kingd\repo/src/a.cs")]
    public void Attribution_HostRewriteOfForeignRoot_IsOutside(WorkspacePathPlatform host, string root, string hostRewrite)
    {
        Assert.Throws<ArgumentException>(() =>
            SessionLogWorkspaceAttributionValidator.ValidatePaths([hostRewrite], null, root, "filesModified", Normalizer(host)));
    }

    /// <summary>FR-MCP-FED-PATH-001: relative and absolute paths under a foreign root are inside.</summary>
    [Theory]
    [InlineData(WindowsHost, "/home/sharpninja/github/RideAudit", "/home/sharpninja/github/RideAudit/src/a.cs")]
    [InlineData(LinuxHub, @"C:\Users\kingd\repo", "C:/Users/kingd/repo/src/a.cs")]
    public void Attribution_PathsUnderForeignRoot_AreInside(WorkspacePathPlatform host, string root, string absolute)
    {
        SessionLogWorkspaceAttributionValidator.ValidatePaths(
            ["docs/readme.md", "src/../src/b.cs", absolute],
            null,
            root,
            "filesModified",
            Normalizer(host));
    }

    /// <summary>FR-MCP-FED-PATH-001: a sibling of a foreign root is outside.</summary>
    [Theory]
    [InlineData(WindowsHost, "/home/sharpninja/github/RideAudit", "/home/sharpninja/github/RideAuditOther/a.cs")]
    [InlineData(LinuxHub, @"C:\Users\kingd\repo", @"C:\Users\kingd\repoOther\a.cs")]
    public void Attribution_SiblingOfForeignRoot_IsOutside(WorkspacePathPlatform host, string root, string sibling)
    {
        Assert.Throws<ArgumentException>(() =>
            SessionLogWorkspaceAttributionValidator.ValidatePaths([sibling], null, root, "filesModified", Normalizer(host)));
    }

    // ---- WorkspaceContext.SetDerivedPaths ----

    /// <summary>
    /// TR-MCP-FED-PATH-001: derived paths for a foreign-platform workspace use the workspace's own
    /// separators (no "/home/x\docs\sessions", no "C:\x/docs/sessions").
    /// </summary>
    [Theory]
    [InlineData(WindowsHost, "/home/sharpninja/github/RideAudit", "/home/sharpninja/github/RideAudit/docs/sessions", "/home/sharpninja/github/RideAudit/docs/external")]
    [InlineData(LinuxHub, @"C:\Users\kingd\repo", @"C:\Users\kingd\repo\docs\sessions", @"C:\Users\kingd\repo\docs\external")]
    public void DerivedPaths_ForeignWorkspace_UseWorkspaceSyntax(WorkspacePathPlatform host, string root, string sessions, string external)
    {
        var ctx = new WorkspaceContext();

        ctx.SetDerivedPaths(root, Normalizer(host));

        Assert.Equal(sessions, ctx.SessionsPath);
        Assert.Equal(external, ctx.ExternalDocsPath);
    }

    // ---- WorkspaceResolutionMiddleware ----

    /// <summary>
    /// TR-MCP-FED-PATH-001: a foreign-platform workspace resolved by X-Workspace-Path gets derived
    /// paths in its own syntax.
    /// </summary>
    [Fact]
    public async Task Middleware_ForeignWorkspaceHeader_DerivesPathsInWorkspaceSyntax()
    {
        const string foreign = "/home/sharpninja/github/RideAudit";
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.GetAsync(foreign, Arg.Any<CancellationToken>()).Returns(new WorkspaceDto
        {
            WorkspacePath = foreign,
            Name = "RideAudit",
            TodoPath = "docs/todo.yaml",
            StatusPrompt = "",
            ImplementPrompt = "",
            PlanPrompt = "",
        });
        var wsContext = new WorkspaceContext();
        var http = new DefaultHttpContext { Request = { Method = "GET", Path = "/mcpserver/todo" } };
        http.Request.Headers[WorkspaceResolutionMiddleware.WorkspacePathHeader] = foreign;
        var middleware = new WorkspaceResolutionMiddleware(_ => Task.CompletedTask, NullLogger<WorkspaceResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(http, wsContext, new WorkspaceTokenService(Normalizer(WindowsHost)), workspaceService, null, Normalizer(WindowsHost));

        Assert.Equal(foreign, wsContext.WorkspacePath);
        Assert.Equal("/home/sharpninja/github/RideAudit/docs/sessions", wsContext.SessionsPath);
        Assert.Equal("/home/sharpninja/github/RideAudit/docs/external", wsContext.ExternalDocsPath);
    }

    // ---- RepoFileService ----

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: repo writes for a foreign-platform workspace fail and create
    /// nothing at a host re-rooting of the path.
    /// </summary>
    [Fact]
    public async Task RepoFiles_ForeignWorkspace_WriteFailsAndCreatesNothing()
    {
        const string leaf = "__mcp_unit_test__repo";
        var hostRewrite = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, leaf);
        var sut = new RepoFileService(
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Path.GetTempPath() }),
            new WorkspaceContext { WorkspacePath = "/" + leaf },
            Substitute.For<IWriteAuditLog>(),
            NullLogger<RepoFileService>.Instance,
            Substitute.For<IChangeEventBus>(),
            Normalizer(WindowsHost));

        try
        {
            var write = await sut.WriteAsync("notes.md", "x", TestContext.Current.CancellationToken);
            var read = await sut.ReadAsync("notes.md", TestContext.Current.CancellationToken);

            Assert.False(write.Written);
            Assert.Null(read);
            Assert.False(Directory.Exists(hostRewrite), $"Repo write created {hostRewrite}");
        }
        finally
        {
            if (Directory.Exists(hostRewrite))
                Directory.Delete(hostRewrite, recursive: true);
        }
    }
}

/// <summary>BDP v4 step 2: consumer path contracts against scripted normalizer data.</summary>
public sealed class WorkspacePathConsumersMockDataTests : WorkspacePathConsumersContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: consumer path contracts against the production normalizer.</summary>
public sealed class WorkspacePathConsumersRealTests : WorkspacePathConsumersContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
