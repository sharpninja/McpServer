using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Apply the real SQLite migration that adds SessionLogTurns.PlanFile and TodoId.
/// Does not use EnsureCreated as the storage gate.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/SessionLogTurnPlanFileTodoIdMigrationApplyTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase in Storage/SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogTurnPlanFileTodoIdMigrationApplyTests : SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase, IDisposable
{
    /// <summary>All three provider migration sources add both columns with default None.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ProviderMigrations_AddBothColumnsWithNoneDefault()
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
            var files = Directory.GetFiles(root, "*AddSessionLogTurnPlanFileAndTodoId.cs")
                .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                Assert.Contains("name: \"PlanFile\"", text, StringComparison.Ordinal);
                Assert.Contains("name: \"TodoId\"", text, StringComparison.Ordinal);
                Assert.Contains("defaultValue: \"None\"", text, StringComparison.Ordinal);
            }
        }
    }
}
