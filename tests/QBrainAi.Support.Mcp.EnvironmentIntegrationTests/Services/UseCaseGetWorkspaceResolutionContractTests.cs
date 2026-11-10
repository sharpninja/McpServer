using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Xunit;
using Xunit.Sdk;
using static QBrainAi.Support.Mcp.Tests.Services.UseCaseGetWorkspaceResolutionContractTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001 / TR-MCP-USECASE-002:
/// proves the get-by-id workspace contract rejects raw-path lookup behavior before production binding.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/UseCaseGetWorkspaceResolutionContractTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the helpers both halves use come from Services/UseCaseGetWorkspaceResolutionContractTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class UseCaseGetWorkspaceResolutionContractTests
{
    /// <summary>Production binding resolves an equivalent dot-segment spelling of its own temporary workspace.</summary>
    /// <remarks>Integration: the real handler resolves physical workspace paths (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), "usecase-owned-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspaceRoot);
        try
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(connection)
                .Options;
            // Seeded with the caller's spelling on purpose: the store canonicalizes workspace keys on save.
            var workspaceContext = new WorkspaceContext
            {
                WorkspacePath = workspaceRoot,
                WorkspaceName = "Fixture",
                DataDirectory = workspaceRoot,
            };

            await using var db = new McpDbContext(options, workspaceContext);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            db.OverrideWorkspaceId(workspaceRoot);
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspaceRoot,
                WorkspacePath = workspaceRoot,
                Name = "Fixture",
            });
            db.UseCases.Add(new UseCaseEntity
            {
                UseCaseId = 125,
                WorkspaceId = workspaceRoot,
                Title = "Certify fixture services",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            var equivalentRawPath = workspaceRoot + "/.";
            var handler = new GetUseCaseQueryHandler(db, workspaceContext);

            await AssertCanonicalWorkspaceContractAsync(
                () => handler.HandleAsync(
                    new GetUseCaseQuery(equivalentRawPath, 125),
                    new CallContext { CancellationToken = TestContext.Current.CancellationToken }), ExpectedWorkspaceKey.Of(workspaceRoot)).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(workspaceRoot, recursive: true);
        }
    }
}
