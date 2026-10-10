using System;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <summary>
    /// TR-MCP-MEMORY-MODEL-002: Adds the global soft-delete shadow columns to MemoryVersions
    /// (and the other September memory side tables) so EF inserts match the applied PostgreSQL schema.
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
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            DropSoftDeleteColumns(migrationBuilder, "MemoryIndexes");
            DropSoftDeleteColumns(migrationBuilder, "MemoryEdges");
            DropSoftDeleteColumns(migrationBuilder, "MemoryVersions");
        }

        private static void DropSoftDeleteColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(name: "DeleteReason", table: table);
            migrationBuilder.DropColumn(name: "DeletedBy", table: table);
            migrationBuilder.DropColumn(name: "DeletedAtUtc", table: table);
            migrationBuilder.DropColumn(name: "IsDeleted", table: table);
        }

        private static void AddSoftDeleteColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: table,
                type: "boolean",
                nullable: false,
                defaultValue: false);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: table,
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: table,
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: table,
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
