using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUseCaseSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use Case tables only. SessionLogs agent-runtime columns are already applied
            // by prior migrations on production; do not re-add them here.

            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    ActorId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.ActorId);
                    table.ForeignKey(
                        name: "FK_Actors_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCases",
                columns: table => new
                {
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BriefDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Precondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Postcondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "Draft"),
                    ProductKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCases", x => x.UseCaseId);
                    table.ForeignKey(
                        name: "FK_UseCases_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseActors",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseActors", x => new { x.WorkspaceId, x.UseCaseId, x.ActorId });
                    table.ForeignKey(
                        name: "FK_UseCaseActors_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "ActorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseActors_UseCases_UseCaseId",
                        column: x => x.UseCaseId,
                        principalTable: "UseCases",
                        principalColumn: "UseCaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseActors_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseExtensionPoints",
                columns: table => new
                {
                    ExtensionPointId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseExtensionPoints", x => x.ExtensionPointId);
                    table.ForeignKey(
                        name: "FK_UseCaseExtensionPoints_UseCases_UseCaseId",
                        column: x => x.UseCaseId,
                        principalTable: "UseCases",
                        principalColumn: "UseCaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseExtensionPoints_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseFlows",
                columns: table => new
                {
                    FlowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false),
                    FlowType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseFlows", x => x.FlowId);
                    table.ForeignKey(
                        name: "FK_UseCaseFlows_UseCases_UseCaseId",
                        column: x => x.UseCaseId,
                        principalTable: "UseCases",
                        principalColumn: "UseCaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseFlows_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseFrLinks",
                columns: table => new
                {
                    LinkId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false),
                    FrId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FrKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "fr"),
                    LinkType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Realizes"),
                    LinkOrder = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseFrLinks", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_UseCaseFrLinks_Requirements_WorkspaceId_FrKind_FrId",
                        columns: x => new { x.WorkspaceId, x.FrKind, x.FrId },
                        principalTable: "Requirements",
                        principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseFrLinks_UseCases_UseCaseId",
                        column: x => x.UseCaseId,
                        principalTable: "UseCases",
                        principalColumn: "UseCaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseFrLinks_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseSpecialRequirements",
                columns: table => new
                {
                    SpecialReqId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UseCaseId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequirementText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseSpecialRequirements", x => x.SpecialReqId);
                    table.ForeignKey(
                        name: "FK_UseCaseSpecialRequirements_UseCases_UseCaseId",
                        column: x => x.UseCaseId,
                        principalTable: "UseCases",
                        principalColumn: "UseCaseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseSpecialRequirements_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UseCaseSteps",
                columns: table => new
                {
                    StepId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    FlowId = table.Column<long>(type: "bigint", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEntities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseCaseSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_UseCaseSteps_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "ActorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseSteps_UseCaseFlows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "UseCaseFlows",
                        principalColumn: "FlowId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UseCaseSteps_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actors_WorkspaceId",
                table: "Actors",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Actors_WorkspaceId_Name",
                table: "Actors",
                columns: new[] { "WorkspaceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseActors_ActorId",
                table: "UseCaseActors",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseActors_UseCaseId",
                table: "UseCaseActors",
                column: "UseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseExtensionPoints_UseCaseId",
                table: "UseCaseExtensionPoints",
                column: "UseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseExtensionPoints_WorkspaceId_UseCaseId",
                table: "UseCaseExtensionPoints",
                columns: new[] { "WorkspaceId", "UseCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseFlows_UseCaseId",
                table: "UseCaseFlows",
                column: "UseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseFlows_WorkspaceId_UseCaseId_SequenceNumber",
                table: "UseCaseFlows",
                columns: new[] { "WorkspaceId", "UseCaseId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseFrLinks_UseCaseId",
                table: "UseCaseFrLinks",
                column: "UseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseFrLinks_WorkspaceId_FrKind_FrId",
                table: "UseCaseFrLinks",
                columns: new[] { "WorkspaceId", "FrKind", "FrId" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseFrLinks_WorkspaceId_UseCaseId_FrId",
                table: "UseCaseFrLinks",
                columns: new[] { "WorkspaceId", "UseCaseId", "FrId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UseCases_WorkspaceId",
                table: "UseCases",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCases_WorkspaceId_ProductKey",
                table: "UseCases",
                columns: new[] { "WorkspaceId", "ProductKey" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCases_WorkspaceId_Title",
                table: "UseCases",
                columns: new[] { "WorkspaceId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseSpecialRequirements_UseCaseId",
                table: "UseCaseSpecialRequirements",
                column: "UseCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseSpecialRequirements_WorkspaceId_UseCaseId",
                table: "UseCaseSpecialRequirements",
                columns: new[] { "WorkspaceId", "UseCaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseSteps_ActorId",
                table: "UseCaseSteps",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseSteps_FlowId",
                table: "UseCaseSteps",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_UseCaseSteps_WorkspaceId_FlowId_StepNumber",
                table: "UseCaseSteps",
                columns: new[] { "WorkspaceId", "FlowId", "StepNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UseCaseActors");

            migrationBuilder.DropTable(
                name: "UseCaseExtensionPoints");

            migrationBuilder.DropTable(
                name: "UseCaseFrLinks");

            migrationBuilder.DropTable(
                name: "UseCaseSpecialRequirements");

            migrationBuilder.DropTable(
                name: "UseCaseSteps");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropTable(
                name: "UseCaseFlows");

            migrationBuilder.DropTable(
                name: "UseCases");
        }
    }
}
