using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddBrainSlotStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrainSlotDefinitions",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SlotId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CredentialReference = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PartyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrainSlotDefinitions", x => new { x.WorkspaceId, x.SlotId });
                    table.ForeignKey(
                        name: "FK_BrainSlotDefinitions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrainSlotInvocations",
                columns: table => new
                {
                    InvocationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SlotId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderKind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TurnId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DiffgramId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PromptSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OutputSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AdmitToGraphRag = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrainSlotInvocations", x => x.InvocationId);
                    table.ForeignKey(
                        name: "FK_BrainSlotInvocations_BrainSlotDefinitions_WorkspaceId_SlotId",
                        columns: x => new { x.WorkspaceId, x.SlotId },
                        principalTable: "BrainSlotDefinitions",
                        principalColumns: new[] { "WorkspaceId", "SlotId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BrainSlotInvocations_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotDefinitions_WorkspaceId_PartyId",
                table: "BrainSlotDefinitions",
                columns: new[] { "WorkspaceId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotDefinitions_WorkspaceId_Role",
                table: "BrainSlotDefinitions",
                columns: new[] { "WorkspaceId", "Role" },
                unique: true,
                filter: "\"Enabled\" = TRUE AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotDefinitions_WorkspaceId_Role_Enabled",
                table: "BrainSlotDefinitions",
                columns: new[] { "WorkspaceId", "Role", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotInvocations_WorkspaceId",
                table: "BrainSlotInvocations",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotInvocations_WorkspaceId_SlotId_StartedAtUtc",
                table: "BrainSlotInvocations",
                columns: new[] { "WorkspaceId", "SlotId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BrainSlotInvocations_WorkspaceId_TransactionId",
                table: "BrainSlotInvocations",
                columns: new[] { "WorkspaceId", "TransactionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrainSlotInvocations");

            migrationBuilder.DropTable(
                name: "BrainSlotDefinitions");
        }
    }
}
