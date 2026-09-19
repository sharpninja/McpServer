using System;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <summary>
    /// TR-MCP-MEMORY-MODEL-002: Adds the global soft-delete shadow columns to MemoryVersions
    /// (and the other September memory side tables) so EF inserts match the applied SQL Server schema.
    /// Forward-only; does not rewrite 20260918223000 or 20260919060000.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260919193000_AddMemoryVersionSoftDeleteColumns")]
    public class AddMemoryVersionSoftDeleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            AddSoftDeleteColumns(migrationBuilder, "MemoryVersions");
            AddSoftDeleteColumns(migrationBuilder, "MemoryEdges");
            AddSoftDeleteColumns(migrationBuilder, "MemoryIndexes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Memory version soft-delete columns are forward-only.");
        }

        private static void AddSoftDeleteColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: table,
                type: "bit",
                nullable: false,
                defaultValue: false);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: table,
                type: "datetimeoffset",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: table,
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: table,
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
