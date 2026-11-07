using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-006 / TR-MCP-USECASE-001: Apply real EF migrations for Use Case support.
/// Proves empty-DB apply and upgrade from the migration before AddUseCaseSupport.
/// Does not use EnsureCreated as the storage gate.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-006; TR-MCP-USECASE-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/UseCaseMigrationApplyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class UseCaseMigrationApplyTestsBase in Storage/UseCaseMigrationApplyTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class UseCaseMigrationApplyTests : UseCaseMigrationApplyTestsBase, IDisposable
{
    /// <summary>AddUseCaseSupport migration sources must not alter SessionLogs.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void AddUseCaseSupportMigration_Source_HasNoSessionLogsTableOps()
    {
        var repoSrc = QBrainAi.Tests.Shared.TestRepositoryRoot.Combine("src");
        var roots = new[]
        {
            Path.Combine(repoSrc, "QBrainAi.Storage.SqliteMigrations", "Migrations"),
            Path.Combine(repoSrc, "QBrainAi.Storage.SqlServerMigrations", "Migrations"),
            Path.Combine(repoSrc, "QBrainAi.Storage.PostgreSqlMigrations", "Migrations"),
        };

        foreach (var root in roots)
        {
            Assert.True(Directory.Exists(root), $"Missing migrations root {root}");
            var files = Directory.GetFiles(root, "*AddUseCaseSupport.cs")
                .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("table: \"SessionLogs\"", text, StringComparison.Ordinal);
            }
        }
    }
}
