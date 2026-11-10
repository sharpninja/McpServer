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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: <see cref="WorkspaceService"/> registers, finds and initializes
/// workspaces by the path's own syntax, never by the host OS. BDP v4 (plan section 2a):
/// MockData fixture (scripted normalizer data) then Real fixture (production normalizer over a
/// simulated host).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration contract of this pair, split from QBrainAi.Support.Mcp.Tests (Services/WorkspaceServiceCrossPlatformContractTests.cs), where the unit cases stay in the contract of the same name (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"). It holds only the Category=Integration case Init_ForeignPlatformWorkspace_CreatesNothingOnHost, moved unchanged, because that case checks the real filesystem under both fixtures; Normalizer, CreateOptions and CreateSut come from the base class WorkspaceServiceCrossPlatformContractTestsBase of QBrainAi.Support.Mcp.Tests, linked into this project with SimulatedWorkspaceHosts (DRY).</remarks>
public abstract class WorkspaceServiceCrossPlatformContractTests : WorkspaceServiceCrossPlatformContractTestsBase
{
    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: initializing a foreign-platform workspace fails without creating
    /// any directory on the host.
    /// </summary>
    /// <remarks>Integration: uses the actual host boundary to reject a foreign-platform workspace without filesystem creation (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Init_ForeignPlatformWorkspace_CreatesNothingOnHost()
    {
        const string leaf = "__mcp_unit_test__fed_init";
        var hostRewrite = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, leaf);
        Assert.False(Directory.Exists(hostRewrite) || File.Exists(hostRewrite),
            "Refusal-test target already exists; preserving it and refusing to enter the SUT.");
        await using var db = new McpDbContext(CreateOptions());
        var sut = CreateSut(db, Normalizer(WorkspacePathPlatform.Windows));
        var created = await sut.CreateAsync(
            new WorkspaceCreateRequest { WorkspacePath = "/" + leaf, Name = leaf },
            ct: TestContext.Current.CancellationToken);
        Assert.True(created.Success, created.Error);

        var init = await sut.InitAsync("/" + leaf, TestContext.Current.CancellationToken);

        Assert.False(init.Success);
        Assert.Contains("not accessible on this host", init.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(hostRewrite), $"Init created {hostRewrite}; retained as failure evidence.");
    }
}

/// <summary>BDP v4 step 2: WorkspaceService path contract against scripted normalizer data.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the fixture of the same name as the one in QBrainAi.Support.Mcp.Tests, over the integration contract of this project (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4): its normalizer is the same one-line call to the linked SimulatedWorkspaceHosts, and it runs the one Category=Integration case, which keeps its identity (this class name and the method name).</remarks>
public sealed class WorkspaceServiceCrossPlatformMockDataTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: WorkspaceService path contract against the production normalizer.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the fixture of the same name as the one in QBrainAi.Support.Mcp.Tests, over the integration contract of this project (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4): its normalizer is the same one-line call to the linked SimulatedWorkspaceHosts, and it runs the one Category=Integration case, which keeps its identity (this class name and the method name).</remarks>
public sealed class WorkspaceServiceCrossPlatformRealTests : WorkspaceServiceCrossPlatformContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
