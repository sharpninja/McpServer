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
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit contract of this pair (ClaudeCode decision on the S7 leftover). Its Category=Integration case (Init_ForeignPlatformWorkspace_CreatesNothingOnHost), which checks the real filesystem under both fixtures, moved unchanged to the abstract contract of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, whose MockData and Real fixtures of the same names run it there (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"), so this unit-owned project holds only unit cases; the normalizer hook and the helpers both contracts use (Normalizer, CreateOptions and CreateSut) moved once into the abstract base class WorkspaceServiceCrossPlatformContractTestsBase in Services/WorkspaceServiceCrossPlatformContractTestsBase.cs beside this file, which both contracts derive from and the integration project links (DRY); the unit cases are unchanged.</remarks>
public abstract class WorkspaceServiceCrossPlatformContractTests : WorkspaceServiceCrossPlatformContractTestsBase
{
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
            new WorkspaceCreateRequest { WorkspacePath = "/fixture/workspaces/ExampleWorkspace", Name = "ExampleWorkspace" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "ExampleWorkspace", TestContext.Current.CancellationToken);
        Assert.Equal("/fixture/workspaces/ExampleWorkspace", stored.WorkspaceId);
        Assert.Equal("/fixture/workspaces/ExampleWorkspace", stored.WorkspacePath);
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
            new WorkspaceCreateRequest { WorkspacePath = @"C:\Users\fixture-user\repo", Name = "repo" },
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        var stored = await db.Workspaces.SingleAsync(w => w.Name == "repo", TestContext.Current.CancellationToken);
        Assert.Equal("C:/Users/fixture-user/repo", stored.WorkspaceId);
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: a registered foreign-platform workspace is found again by any
    /// equivalent spelling of its path.
    /// </summary>
    [Theory]
    [InlineData("/fixture/workspaces/ExampleWorkspace/")]
    [InlineData("/fixture/workspaces/./ExampleWorkspace")]
    public async Task Get_WindowsHost_PosixWorkspace_FoundByEquivalentSpelling(string lookup)
    {
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.Windows));
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = "/fixture/workspaces/ExampleWorkspace", Name = "ExampleWorkspace" },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        var found = await sut.GetAsync(lookup, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("/fixture/workspaces/ExampleWorkspace", found.WorkspacePath);
    }
}

/// <summary>BDP v4 step 2: WorkspaceService path contract against scripted normalizer data.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration case this fixture inherited (Init_ForeignPlatformWorkspace_CreatesNothingOnHost) now runs from the fixture of the same name in QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4); this fixture runs the unit cases of its contract, unchanged.</remarks>
public sealed class WorkspaceServiceCrossPlatformMockDataTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: WorkspaceService path contract against the production normalizer.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration case this fixture inherited (Init_ForeignPlatformWorkspace_CreatesNothingOnHost) now runs from the fixture of the same name in QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4); this fixture runs the unit cases of its contract, unchanged.</remarks>
public sealed class WorkspaceServiceCrossPlatformRealTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
