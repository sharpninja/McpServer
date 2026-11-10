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
/// TEST-MCP-FED-PATH-001: the normalizer hook and the helpers that both contracts named WorkspaceServiceCrossPlatformContractTests use (the unit contract in QBrainAi.Support.Mcp.Tests, the integration contract in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from WorkspaceServiceCrossPlatformContractTests (Normalizer, CreateOptions and CreateSut) when that contract split into a unit contract and an integration contract (slice S8, hostile validation finding R14-F4; ClaudeCode decision on the S7 leftover), so both contracts use one definition instead of a copy (DRY); only the accessibility of CreateOptions and CreateSut changed, from private to internal.</remarks>
public abstract class WorkspaceServiceCrossPlatformContractTestsBase
{
    /// <summary>Normalizer for a simulated host of the given platform.</summary>
    protected abstract IWorkspacePathNormalizer Normalizer(WorkspacePathPlatform hostPlatform);

    internal static DbContextOptions<McpDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"workspace-xplat-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    internal static WorkspaceService CreateSut(McpDbContext db, IWorkspacePathNormalizer normalizer)
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
