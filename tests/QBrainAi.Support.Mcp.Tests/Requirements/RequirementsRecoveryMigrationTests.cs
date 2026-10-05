using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using PostgreSqlRecovery = QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddRequirementsRecoveryRuns;
using SqliteRecovery = QBrainAi.Support.Mcp.Storage.SqliteMigrations.Migrations.AddRequirementsRecoveryRuns;
using SqlServerRecovery = QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddRequirementsRecoveryRuns;

namespace QBrainAi.Support.Mcp.Tests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: SQLite, SQL Server, and PostgreSQL each publish the recovery migration.
/// </summary>
public sealed class RequirementsRecoveryMigrationTests
{
    /// <summary>The three provider assemblies discover the same recovery migration id.</summary>
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

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "QBrainAi.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("QBrainAi.sln was not found above the test base directory.");
    }
}
