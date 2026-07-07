using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfRequirementAcceptanceCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequirementAcceptanceCriteria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RequirementKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    CriterionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSatisfied = table.Column<bool>(type: "bit", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Requirements_WorkspaceId_RequirementKind_RequirementId",
                        columns: x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId },
                        principalTable: "Requirements",
                        principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementAcceptanceCriteria_WorkspaceId_RequirementKind_RequirementId_Ordinal",
                table: "RequirementAcceptanceCriteria",
                columns: new[] { "WorkspaceId", "RequirementKind", "RequirementId", "Ordinal" });

            // TR-MCP-REQAC-001 data migration: backfill AcceptanceCriteriaJson (array of
            // {id, text, isSatisfied, evidence} objects) into ordered 4NF child rows before the
            // source column is dropped. Two-level OPENJSON preserves array order (arr.[key]) and
            // reads text/evidence as nvarchar(max) (JSON_VALUE would truncate at 4000 chars).
            migrationBuilder.Sql(@"
INSERT INTO [RequirementAcceptanceCriteria] ([WorkspaceId], [RequirementKind], [RequirementId], [Ordinal], [CriterionId], [Text], [IsSatisfied], [Evidence])
SELECT r.[WorkspaceId], r.[Kind], r.[Id], CAST(arr.[key] AS int),
       COALESCE(crit.[id], ''), COALESCE(crit.[text], ''), COALESCE(crit.[isSatisfied], 0), crit.[evidence]
FROM [Requirements] r
CROSS APPLY OPENJSON(r.[AcceptanceCriteriaJson]) arr
CROSS APPLY OPENJSON(arr.[value]) WITH (
    [id] nvarchar(128) '$.id',
    [text] nvarchar(max) '$.text',
    [isSatisfied] bit '$.isSatisfied',
    [evidence] nvarchar(max) '$.evidence'
) crit
WHERE r.[AcceptanceCriteriaJson] IS NOT NULL AND ISJSON(r.[AcceptanceCriteriaJson]) = 1;");

            migrationBuilder.DropColumn(
                name: "AcceptanceCriteriaJson",
                table: "Requirements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteriaJson",
                table: "Requirements",
                type: "nvarchar(max)",
                nullable: true);

            // Reconstruct the JSON object array (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql(@"
UPDATE r SET [AcceptanceCriteriaJson] = (
    SELECT c.[CriterionId] AS [id], c.[Text] AS [text], c.[IsSatisfied] AS [isSatisfied], c.[Evidence] AS [evidence]
    FROM [RequirementAcceptanceCriteria] c
    WHERE c.[WorkspaceId] = r.[WorkspaceId] AND c.[RequirementKind] = r.[Kind] AND c.[RequirementId] = r.[Id] AND c.[IsDeleted] = 0
    ORDER BY c.[Ordinal]
    FOR JSON PATH, INCLUDE_NULL_VALUES
)
FROM [Requirements] r
WHERE EXISTS (
    SELECT 1 FROM [RequirementAcceptanceCriteria] c
    WHERE c.[WorkspaceId] = r.[WorkspaceId] AND c.[RequirementKind] = r.[Kind] AND c.[RequirementId] = r.[Id] AND c.[IsDeleted] = 0);");

            migrationBuilder.DropTable(
                name: "RequirementAcceptanceCriteria");
        }
    }
}
