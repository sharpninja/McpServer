using System;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations;

/// <summary>Adds lossless compressed audit payloads without rewriting legacy JSON rows.</summary>
[DbContext(typeof(McpDbContext))]
[Migration("20260929140000_VersionedAuditPayloads")]
public sealed class VersionedAuditPayloads : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.AddColumn<int>(
            name: "PayloadEncodingVersion",
            table: "DataAuditLogs",
            type: "int",
            nullable: true);
        foreach (var column in new[] { "PreviousSnapshotPayload", "CurrentSnapshotPayload", "DiffPayload", "MetadataPayload" })
        {
            migrationBuilder.AddColumn<byte[]>(
                name: column,
                table: "DataAuditLogs",
                type: "varbinary(max)",
                nullable: true);
        }

        migrationBuilder.Sql("""
            IF DATABASE_PRINCIPAL_ID(N'mcp_runtime') IS NULL
                CREATE ROLE [mcp_runtime] AUTHORIZATION [dbo];
            GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [mcp_runtime];
            DENY UPDATE, DELETE ON OBJECT::[dbo].[DataAuditLogs] TO [mcp_runtime];
            DENY UPDATE, DELETE ON OBJECT::[dbo].[TodoAuditHistory] TO [mcp_runtime];
            DENY INSERT, UPDATE, DELETE ON OBJECT::[dbo].[__EFMigrationsHistory] TO [mcp_runtime];
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Permanent audit payload migration is forward-only.");
}
