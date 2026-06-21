using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBrainSlotWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "OrchestrationWeight",
                table: "BrainSlotDefinitions",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WeightUpdatedAtUtc",
                table: "BrainSlotDefinitions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightVersion",
                table: "BrainSlotDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrchestrationWeight",
                table: "BrainSlotDefinitions");

            migrationBuilder.DropColumn(
                name: "WeightUpdatedAtUtc",
                table: "BrainSlotDefinitions");

            migrationBuilder.DropColumn(
                name: "WeightVersion",
                table: "BrainSlotDefinitions");
        }
    }
}
