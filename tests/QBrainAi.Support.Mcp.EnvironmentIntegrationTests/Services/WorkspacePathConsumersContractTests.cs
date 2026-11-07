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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-FED-PATH-001: contract tests for the smaller request-path consumers
/// (<see cref="HandoffWorkspacePaths"/>, <see cref="WorkspaceTokenService"/>,
/// <see cref="SessionLogWorkspaceAttributionValidator"/>, <see cref="WorkspaceContext.SetDerivedPaths"/>,
/// <see cref="WorkspaceResolutionMiddleware"/>, <see cref="RepoFileService"/>). Every case uses a
/// simulated Linux hub or Windows host, so results do not depend on the test runner's OS.
/// BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration contract of this pair, split from QBrainAi.Support.Mcp.Tests (Services/WorkspacePathConsumersContractTests.cs), where the unit cases stay in the contract of the same name (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"). It holds only the Category=Integration case RepoFiles_ForeignWorkspace_WriteFailsAndCreatesNothing and its RepoFileService section, moved unchanged, because that case checks the real filesystem under both fixtures; WindowsHost and Normalizer come from the base class WorkspacePathConsumersContractTestsBase of QBrainAi.Support.Mcp.Tests, linked into this project with SimulatedWorkspaceHosts (DRY).</remarks>
public abstract class WorkspacePathConsumersContractTests : WorkspacePathConsumersContractTestsBase
{
    // ---- RepoFileService ----

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: repo writes for a foreign-platform workspace fail and create
    /// nothing at a host re-rooting of the path.
    /// Integration: observes a physical host-root spelling. A unique name and precondition ensure cleanup can
    /// remove only a folder this invocation could have caused, never a pre-existing one (slice M SVC-13).
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task RepoFiles_ForeignWorkspace_WriteFailsAndCreatesNothing()
    {
        const string leaf = "__mcp_unit_test__repo";
        var hostRewrite = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, leaf);
        Assert.False(Directory.Exists(hostRewrite) || File.Exists(hostRewrite),
            "Refusal-test target already exists; preserving it and refusing to enter the SUT.");
        var sut = new RepoFileService(
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = Path.GetTempPath() }),
            new WorkspaceContext { WorkspacePath = "/" + leaf },
            Substitute.For<IWriteAuditLog>(),
            NullLogger<RepoFileService>.Instance,
            Substitute.For<IChangeEventBus>(),
            Normalizer(WindowsHost));

        var write = await sut.WriteAsync("notes.md", "x", TestContext.Current.CancellationToken);
        var read = await sut.ReadAsync("notes.md", TestContext.Current.CancellationToken);

        Assert.False(write.Written);
        Assert.Null(read);
        Assert.False(Directory.Exists(hostRewrite), $"Repo write created {hostRewrite}; retained as failure evidence.");
    }
}

/// <summary>BDP v4 step 2: consumer path contracts against scripted normalizer data.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the fixture of the same name as the one in QBrainAi.Support.Mcp.Tests, over the integration contract of this project (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4): its normalizer is the same one-line call to the linked SimulatedWorkspaceHosts, and it runs the one Category=Integration case, which keeps its identity (this class name and the method name).</remarks>
public sealed class WorkspacePathConsumersMockDataTests : WorkspacePathConsumersContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Scripted(hostPlatform);
}

/// <summary>BDP v4 step 3: consumer path contracts against the production normalizer.</summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-FED-PATH-001-AC4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the fixture of the same name as the one in QBrainAi.Support.Mcp.Tests, over the integration contract of this project (ClaudeCode decision on the S7 leftover; slice S8, hostile validation finding R14-F4): its normalizer is the same one-line call to the linked SimulatedWorkspaceHosts, and it runs the one Category=Integration case, which keeps its identity (this class name and the method name).</remarks>
public sealed class WorkspacePathConsumersRealTests : WorkspacePathConsumersContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform) =>
        SimulatedWorkspaceHosts.Real(hostPlatform);
}
