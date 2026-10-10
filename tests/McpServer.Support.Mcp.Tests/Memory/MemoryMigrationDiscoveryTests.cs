using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using PostgreSqlMemoryIndex = McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddMemoryIndexStorage;
using PostgreSqlMemoryVersion = McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddMemoryVersionAndEdgeStorage;
using PostgreSqlSoftDelete = McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddMemoryVersionSoftDeleteColumns;
using SqliteMemoryIndex = McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations.AddMemoryIndexStorage;
using SqliteMemoryVersion = McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations.AddMemoryVersionAndEdgeStorage;
using SqliteSoftDelete = McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations.AddMemoryVersionSoftDeleteColumns;
using SqlServerMemoryIndex = McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddMemoryIndexStorage;
using SqlServerMemoryVersion = McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddMemoryVersionAndEdgeStorage;
using SqlServerSoftDelete = McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddMemoryVersionSoftDeleteColumns;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TR-MCP-MEMORY-MODEL-002: EF discovers September memory migrations
/// on all three provider assemblies via <see cref="DbContextAttribute"/> and <see cref="MigrationAttribute"/>.
/// </summary>
public sealed class MemoryMigrationDiscoveryTests
{
    /// <summary>Canonical migration ids that Legion and CI must discover.</summary>
    public static readonly string[] RequiredMigrationIds =
    [
        "20260918223000_AddMemoryVersionAndEdgeStorage",
        "20260919060000_AddMemoryIndexStorage",
        "20260919193000_AddMemoryVersionSoftDeleteColumns",
    ];

    /// <summary>SQLite, PostgreSQL, and SQL Server assemblies all publish the three memory migration ids.</summary>
    [Fact]
    public void Discovers_RequiredMemoryMigrations_OnAllProviders()
    {
        var assemblies = new[]
        {
            typeof(SqliteMemoryVersion).Assembly,
            typeof(PostgreSqlMemoryVersion).Assembly,
            typeof(SqlServerMemoryVersion).Assembly,
        };

        foreach (var assembly in assemblies)
        {
            var ids = DiscoverMigrationIds(assembly);
            foreach (var required in RequiredMigrationIds)
            {
                Assert.Contains(required, ids);
            }
        }

        Assert.Equal(
            "20260918223000_AddMemoryVersionAndEdgeStorage",
            typeof(SqliteMemoryVersion).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            "20260919060000_AddMemoryIndexStorage",
            typeof(SqliteMemoryIndex).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            "20260919193000_AddMemoryVersionSoftDeleteColumns",
            typeof(SqliteSoftDelete).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteMemoryVersion).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(PostgreSqlMemoryVersion).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteMemoryIndex).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(PostgreSqlMemoryIndex).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteSoftDelete).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(PostgreSqlSoftDelete).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteMemoryVersion).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(SqlServerMemoryVersion).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteMemoryIndex).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(SqlServerMemoryIndex).GetCustomAttribute<MigrationAttribute>()!.Id);
        Assert.Equal(
            typeof(SqliteSoftDelete).GetCustomAttribute<MigrationAttribute>()!.Id,
            typeof(SqlServerSoftDelete).GetCustomAttribute<MigrationAttribute>()!.Id);
    }

    private static string[] DiscoverMigrationIds(Assembly assembly)
    {
        return assembly.GetTypes()
            .Select(type => type.GetCustomAttribute<MigrationAttribute>())
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.Id)
            .ToArray();
    }
}
