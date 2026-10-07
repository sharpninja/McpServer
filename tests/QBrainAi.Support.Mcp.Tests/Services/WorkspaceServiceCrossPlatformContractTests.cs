using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: <see cref="WorkspaceService"/> registers, finds and initializes
/// workspaces by the path's own syntax, never by the host OS. BDP v4 (plan section 2a):
/// MockData fixture (scripted normalizer data) then Real fixture (production normalizer over a
/// simulated host).
/// </summary>
public abstract class WorkspaceServiceCrossPlatformContractTests
{
    /// <summary>Normalizer for a simulated host of the given platform.</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: a Windows host registering a POSIX workspace stores the POSIX
    /// path, not a drive-rooted rewrite.
    /// </summary>
    [Fact]
    public async Task Create_WindowsHost_PosixWorkspace_StoresPosixIdentity()
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.Windows));

        var result = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = "/home/sharpninja/github/RideAudit", Name = "RideAudit" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "RideAudit", TestContext.Current.CancellationToken);
        Assert.Equal("/home/sharpninja/github/RideAudit", stored.WorkspaceId);
        Assert.Equal("/home/sharpninja/github/RideAudit", stored.WorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: a Linux hub accepts a Windows drive path as absolute and stores it
    /// without the hub working directory.
    /// </summary>
    [Fact]
    public async Task Create_LinuxHost_WindowsWorkspace_IsAcceptedAndUnmangled()
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.CaseSensitive));

        var result = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = @"C:\Users\kingd\repo", Name = "repo" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "repo", TestContext.Current.CancellationToken);
        Assert.Equal(@"C:\Users\kingd\repo", stored.WorkspaceId);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: a registered foreign-platform workspace is found again by any
    /// equivalent spelling of its path.
    /// </summary>
    [Theory]
    [InlineData("/home/sharpninja/github/RideAudit/")]
    [InlineData("/home/sharpninja/github/./RideAudit")]
    public async Task Get_WindowsHost_PosixWorkspace_FoundByEquivalentSpelling(string lookup)
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.Windows));
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = "/home/sharpninja/github/RideAudit", Name = "RideAudit" },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        var found = await sut.GetAsync(lookup, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("/home/sharpninja/github/RideAudit", found.WorkspacePath);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: initializing a foreign-platform workspace fails without creating
    /// any directory on the host.
    /// </summary>
    [Fact]
    public async Task Init_ForeignPlatformWorkspace_CreatesNothingOnHost()
    {
        const string leaf = "__mcp_unit_test__fed_init";
        var hostRewrite = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, leaf);
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.Windows));
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = "/" + leaf, Name = leaf },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        try
        {
            var init = await sut.InitAsync("/" + leaf, TestContext.Current.CancellationToken);

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

/// <summary>BDP v4 step 2: WorkspaceService path contract against scripted normalizer data.</summary>
public sealed class WorkspaceServiceCrossPlatformMockDataTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: WorkspaceService path contract against the production normalizer.</summary>
public sealed class WorkspaceServiceCrossPlatformRealTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
