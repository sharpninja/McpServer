using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Provider migrations add and drop SessionLogTurns.PlanFile and TodoId.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/AddSessionLogTurnPlanFileAndTodoIdMigrationTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (AssertProviderMigrationContainsUpAndDown) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase in Storage/AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class AddSessionLogTurnPlanFileAndTodoIdMigrationTests : AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase, IDisposable
{
    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: SQL Server migration source adds both columns with None default.</summary>
    /// <remarks>Integration: reads the selected provider migration source from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SqlServerMigration_UpAddsColumns_DownDropsThem()
    {
        AssertProviderMigrationContainsUpAndDown("QBrainAi.Storage.SqlServerMigrations");
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: PostgreSQL migration source adds both columns with None default.</summary>
    /// <remarks>Integration: reads the selected provider migration source from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void PostgreSqlMigration_UpAddsColumns_DownDropsThem()
    {
        AssertProviderMigrationContainsUpAndDown("QBrainAi.Storage.PostgreSqlMigrations");
    }

    private static void AssertProviderMigrationContainsUpAndDown(string project)
    {
        var repoSrc = QBrainAi.Tests.Shared.TestRepositoryRoot.Combine("src");
        var root = Path.Combine(repoSrc, project, "Migrations");
        var files = Directory.GetFiles(root, "*AddSessionLogTurnPlanFileAndTodoId.cs")
            .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(files);
        var text = File.ReadAllText(files[0]);
        Assert.Contains("name: \"PlanFile\"", text, StringComparison.Ordinal);
        Assert.Contains("name: \"TodoId\"", text, StringComparison.Ordinal);
        Assert.Contains("defaultValue: \"None\"", text, StringComparison.Ordinal);
        Assert.Contains("DropColumn", text, StringComparison.Ordinal);
        Assert.Contains("DropIndex", text, StringComparison.Ordinal);
    }
}
