using System;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations;

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
            type: "integer",
            nullable: true);
        foreach (var column in new[] { "PreviousSnapshotPayload", "CurrentSnapshotPayload", "DiffPayload", "MetadataPayload" })
        {
            migrationBuilder.AddColumn<byte[]>(
                name: column,
                table: "DataAuditLogs",
                type: "bytea",
                nullable: true);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Permanent audit payload migration is forward-only.");
}
