using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelDriftTestStoragePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requirements",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UpdatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requirements", x => new { x.WorkspaceId, x.Kind, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "RequirementTraceabilityLinks",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FrId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementTraceabilityLinks", x => new { x.WorkspaceId, x.FrId, x.TargetKind, x.TargetId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_Kind",
                table: "Requirements",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_WorkspaceId",
                table: "Requirements",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_WorkspaceId_Id",
                table: "Requirements",
                columns: new[] { "WorkspaceId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementTraceabilityLinks_WorkspaceId",
                table: "RequirementTraceabilityLinks",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementTraceabilityLinks_WorkspaceId_TargetKind_TargetId",
                table: "RequirementTraceabilityLinks",
                columns: new[] { "WorkspaceId", "TargetKind", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Requirements");

            migrationBuilder.DropTable(
                name: "RequirementTraceabilityLinks");
        }
    }
}
