using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfAgentModelLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDefinitionModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", nullable: false),
                    AgentDefinitionId = table.Column<string>(type: "nvarchar(64)", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitionModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentDefinitionModels_AgentDefinitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "AgentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentDefinitionModels_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkspaceListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", nullable: false),
                    AgentWorkspaceId = table.Column<int>(type: "int", nullable: false),
                    ListType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkspaceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaceListItems_AgentWorkspaces_AgentWorkspaceId",
                        column: x => x.AgentWorkspaceId,
                        principalTable: "AgentWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaceListItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitionModels_AgentDefinitionId_Ordinal",
                table: "AgentDefinitionModels",
                columns: new[] { "AgentDefinitionId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitionModels_WorkspaceId",
                table: "AgentDefinitionModels",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaceListItems_AgentWorkspaceId_ListType_Ordinal",
                table: "AgentWorkspaceListItems",
                columns: new[] { "AgentWorkspaceId", "ListType", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaceListItems_WorkspaceId",
                table: "AgentWorkspaceListItems",
                column: "WorkspaceId");

            // Data migration: backfill DefaultModelsJson and the two override JSON arrays into
            // ordered 4NF child rows before the source columns are dropped.
            migrationBuilder.Sql(@"
INSERT INTO [AgentDefinitionModels] ([WorkspaceId], [AgentDefinitionId], [Ordinal], [Model])
SELECT a.[WorkspaceId], a.[Id], CAST(j.[key] AS int), j.[value]
FROM [AgentDefinitions] a
CROSS APPLY OPENJSON(a.[DefaultModelsJson]) j
WHERE a.[DefaultModelsJson] IS NOT NULL AND ISJSON(a.[DefaultModelsJson]) = 1;");

            migrationBuilder.Sql(@"
INSERT INTO [AgentWorkspaceListItems] ([WorkspaceId], [AgentWorkspaceId], [ListType], [Ordinal], [Value])
SELECT w.[WorkspaceId], w.[Id], 'ModelOverride', CAST(j.[key] AS int), j.[value]
FROM [AgentWorkspaces] w
CROSS APPLY OPENJSON(w.[ModelsOverrideJson]) j
WHERE w.[ModelsOverrideJson] IS NOT NULL AND ISJSON(w.[ModelsOverrideJson]) = 1;");

            migrationBuilder.Sql(@"
INSERT INTO [AgentWorkspaceListItems] ([WorkspaceId], [AgentWorkspaceId], [ListType], [Ordinal], [Value])
SELECT w.[WorkspaceId], w.[Id], 'InstructionFileOverride', CAST(j.[key] AS int), j.[value]
FROM [AgentWorkspaces] w
CROSS APPLY OPENJSON(w.[InstructionFilesOverrideJson]) j
WHERE w.[InstructionFilesOverrideJson] IS NOT NULL AND ISJSON(w.[InstructionFilesOverrideJson]) = 1;");

            migrationBuilder.DropColumn(name: "InstructionFilesOverrideJson", table: "AgentWorkspaces");
            migrationBuilder.DropColumn(name: "ModelsOverrideJson", table: "AgentWorkspaces");
            migrationBuilder.DropColumn(name: "DefaultModelsJson", table: "AgentDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructionFilesOverrideJson",
                table: "AgentWorkspaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelsOverrideJson",
                table: "AgentWorkspaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultModelsJson",
                table: "AgentDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            // Reconstruct the JSON arrays (ordered) from the child rows.
            migrationBuilder.Sql(@"
UPDATE a SET [DefaultModelsJson] = COALESCE(j.[json], '[]')
FROM [AgentDefinitions] a
OUTER APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(m.[Model], 'json'), '""'), ',') WITHIN GROUP (ORDER BY m.[Ordinal]), ']') AS [json]
    FROM [AgentDefinitionModels] m
    WHERE m.[AgentDefinitionId] = a.[Id] AND m.[IsDeleted] = 0
) j;");

            migrationBuilder.Sql(@"
UPDATE w SET [ModelsOverrideJson] = j.[json]
FROM [AgentWorkspaces] w
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(i.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY i.[Ordinal]), ']') AS [json]
    FROM [AgentWorkspaceListItems] i
    WHERE i.[AgentWorkspaceId] = w.[Id] AND i.[ListType] = 'ModelOverride' AND i.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;");

            migrationBuilder.Sql(@"
UPDATE w SET [InstructionFilesOverrideJson] = j.[json]
FROM [AgentWorkspaces] w
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(i.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY i.[Ordinal]), ']') AS [json]
    FROM [AgentWorkspaceListItems] i
    WHERE i.[AgentWorkspaceId] = w.[Id] AND i.[ListType] = 'InstructionFileOverride' AND i.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;");

            migrationBuilder.DropTable(
                name: "AgentDefinitionModels");

            migrationBuilder.DropTable(
                name: "AgentWorkspaceListItems");
        }
    }
}
