using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: <see cref="WorkspaceService"/> registers, finds and initializes
/// workspaces by the path's own syntax, never by the host OS. Hosts are simulated with a mocked
/// <see cref="IWorkspaceHostEnvironment"/> so both directions run on any test runner.
/// </summary>
public sealed class WorkspaceServiceCrossPlatformPathTests
{
    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: A Windows host registering a POSIX workspace (a Linux proxy's
    /// workspace on a Windows hub) stores the POSIX path, not a drive-rooted rewrite.
    /// </summary>
    [Fact]
    public async Task Create_WindowsHost_PosixWorkspace_StoresPosixIdentity()
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, SimulatedHost(WorkspacePathPlatform.Windows));
        const string posix = "/home/sharpninja/github/RideAudit";

        var result = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = posix, Name = "RideAudit" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "RideAudit", TestContext.Current.CancellationToken);
        Assert.Equal(posix, stored.WorkspaceId);
        Assert.Equal(posix, stored.WorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: A Linux host accepts a Windows drive path as absolute (host
    /// <c>Path.IsPathRooted</c> would reject it) and stores it without the host working directory.
    /// </summary>
    [Fact]
    public async Task Create_LinuxHost_WindowsWorkspace_IsAcceptedAndUnmangled()
    {
        await using var db = new McpDbContext(CreateOptions());
        var linux = SimulatedHost(WorkspacePathPlatform.CaseSensitive);
        var sut = CreateSut(db, linux);
        const string windows = @"C:\Users\kingd\repo";

        var result = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = windows, Name = "repo" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "repo", TestContext.Current.CancellationToken);
        Assert.Equal(linux.Normalize(windows), stored.WorkspaceId);
        Assert.DoesNotContain("/opt/mcpserver/app", stored.WorkspaceId, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1/AC2: A registered foreign-platform workspace is found again by
    /// any equivalent spelling of its path.
    /// </summary>
    [Theory]
    [InlineData("/home/sharpninja/github/RideAudit", "/home/sharpninja/github/RideAudit/")]
    [InlineData("/home/sharpninja/github/RideAudit", "/home/sharpninja/github/./RideAudit")]
    public async Task Get_WindowsHost_PosixWorkspace_FoundByEquivalentSpelling(string registered, string lookup)
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, SimulatedHost(WorkspacePathPlatform.Windows));
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = registered, Name = "RideAudit" },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        var found = await sut.GetAsync(lookup, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(registered, found.WorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: Initializing a workspace whose path belongs to another platform
    /// fails without creating any directory on the host (no drive-rooted or cwd-rooted rewrite).
    /// </summary>
    [Fact]
    public async Task Init_ForeignPlatformWorkspace_CreatesNothingOnHost()
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, SimulatedHost(WorkspacePathPlatform.Windows));
        var leaf = $"__mcp_unit_test__fed_init_{Guid.NewGuid():N}";
        var posix = "/" + leaf;
        var hostRewrite = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, leaf);
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = posix, Name = leaf },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        try
        {
            var init = await sut.InitAsync(posix, TestContext.Current.CancellationToken);

            Assert.False(init.Success);
            Assert.Contains("not accessible on this host", init.Error, StringComparison.OrdinalIgnoreCase);
            Assert.False(Directory.Exists(hostRewrite), $"Init created {hostRewrite}");
        }
        finally
        {
            if (Directory.Exists(hostRewrite))
                Directory.Delete(hostRewrite, recursive: true);
        }
    }

    private static IWorkspacePathNormalizer SimulatedHost(WorkspacePathPlatform platform)
    {
        const string linuxCwd = "/opt/mcpserver/app";
        const string windowsCwd = @"C:\svc";
        var host = Substitute.For<IWorkspaceHostEnvironment>();
        host.Platform.Returns(platform);
        host.CurrentDirectory.Returns(platform == WorkspacePathPlatform.Windows ? windowsCwd : linuxCwd);
        host.NormalizeNativePath(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>()!;
            var full = platform == WorkspacePathPlatform.Windows
                ? (path.Length >= 2 && path[1] == ':' ? path : windowsCwd + @"\" + path)
                : (path.StartsWith('/') ? path : linuxCwd + "/" + path);
            return WorkspaceIdentityPath.NormalizeLexicalPath(full, platform);
        });
        return new WorkspacePathNormalizer(host);
    }

    private static DbContextOptions<McpDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"workspace-xplat-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static WorkspaceService CreateSut(McpDbContext db, IWorkspacePathNormalizer normalizer)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mcp:Workspaces"] = "[]" })
            .Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(Path.GetTempPath());

        return new WorkspaceService(
            configuration,
            environment,
            Substitute.For<IProcessRunner>(),
            db,
            Substitute.For<IWorkspaceProjectionWriter>(),
            NullLogger<WorkspaceService>.Instance,
            pathNormalizer: normalizer);
    }
}
