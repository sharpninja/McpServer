using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using QBrainAi.Tests.Shared;
using Xunit;
using PostgreSqlRecovery = QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddRequirementsRecoveryRuns;
using SqliteRecovery = QBrainAi.Support.Mcp.Storage.SqliteMigrations.Migrations.AddRequirementsRecoveryRuns;
using SqlServerRecovery = QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddRequirementsRecoveryRuns;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: SQLite, SQL Server, and PostgreSQL each publish the recovery migration.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-REQRECOVERY-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Requirements/RequirementsRecoveryMigrationTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the shared TestRepositoryRoot locator is linked into this assembly; only the namespace changed.</remarks>
public sealed class RequirementsRecoveryMigrationTests
{
    /// <summary>The three provider assemblies discover the same recovery migration id.</summary>
    /// <remarks>Integration: reads each provider migration source file from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void Discovers_AddRequirementsRecoveryRuns_OnAllProviders()
    {
        const string id = "20260929020000_AddRequirementsRecoveryRuns";
        foreach (var type in new[] { typeof(SqliteRecovery), typeof(SqlServerRecovery), typeof(PostgreSqlRecovery) })
        {
            Assert.Equal(id, type.GetCustomAttribute<MigrationAttribute>()!.Id);
            var source = type.Assembly.GetTypes().Single(candidate => candidate == type);
            Assert.Contains("RequirementsRecoveryRuns", ReadUpSource(source), StringComparison.Ordinal);
        }
    }

    private static string ReadUpSource(Type migration)
    {
        var path = Path.Combine(RepositoryRoot(), RelativeMigrationPath(migration));
        return File.ReadAllText(path);
    }

    private static string RelativeMigrationPath(Type migration)
    {
        var ns = migration.Namespace ?? string.Empty;
        if (ns.Contains("Sqlite", StringComparison.Ordinal))
            return Path.Combine("src", "QBrainAi.Storage.SqliteMigrations", "Migrations", "20260929020000_AddRequirementsRecoveryRuns.cs");
        if (ns.Contains("SqlServer", StringComparison.Ordinal))
            return Path.Combine("src", "QBrainAi.Storage.SqlServerMigrations", "Migrations", "20260929020000_AddRequirementsRecoveryRuns.cs");
        return Path.Combine("src", "QBrainAi.Storage.PostgreSqlMigrations", "Migrations", "20260929020000_AddRequirementsRecoveryRuns.cs");
    }

    private static string RepositoryRoot() => TestRepositoryRoot.Resolve();
}
