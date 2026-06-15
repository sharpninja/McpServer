using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
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
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WeightUpdatedAtUtc",
                table: "BrainSlotDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightVersion",
                table: "BrainSlotDefinitions",
                type: "integer",
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
