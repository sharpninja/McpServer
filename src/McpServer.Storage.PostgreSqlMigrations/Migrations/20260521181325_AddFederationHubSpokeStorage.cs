using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFederationHubSpokeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FederationProxies",
                columns: table => new
                {
                    ProxyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationProxies", x => x.ProxyId);
                });

            migrationBuilder.CreateTable(
                name: "FederationOperations",
                columns: table => new
                {
                    OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProxyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceOperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    GlobalWorkspaceId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Domain = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Method = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    BodyBase64 = table.Column<string>(type: "text", nullable: true),
                    BaseVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HubVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationOperations", x => x.OperationId);
                    table.ForeignKey(
                        name: "FK_FederationOperations_FederationProxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "FederationProxies",
                        principalColumn: "ProxyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FederationWorkspaces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GlobalWorkspaceId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ProxyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkspaceName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    WorkspacePath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederationWorkspaces_FederationProxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "FederationProxies",
                        principalColumn: "ProxyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FederationConflicts",
                columns: table => new
                {
                    ConflictId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProxyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Domain = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ProxyVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    HubVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResolutionStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationConflicts", x => x.ConflictId);
                    table.ForeignKey(
                        name: "FK_FederationConflicts_FederationOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "FederationOperations",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FederationConflicts_FederationProxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "FederationProxies",
                        principalColumn: "ProxyId");
                });

            migrationBuilder.CreateTable(
                name: "FederationOutbox",
                columns: table => new
                {
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProxyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederationOutbox", x => x.Sequence);
                    table.ForeignKey(
                        name: "FK_FederationOutbox_FederationOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "FederationOperations",
                        principalColumn: "OperationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FederationOutbox_FederationProxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "FederationProxies",
                        principalColumn: "ProxyId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FederationConflicts_Domain_ResourceId",
                table: "FederationConflicts",
                columns: new[] { "Domain", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationConflicts_OperationId",
                table: "FederationConflicts",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationConflicts_ProxyId_ResolutionStatus",
                table: "FederationConflicts",
                columns: new[] { "ProxyId", "ResolutionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationOperations_CreatedAtUtc",
                table: "FederationOperations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FederationOperations_Domain_ResourceId",
                table: "FederationOperations",
                columns: new[] { "Domain", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationOperations_ProxyId_Status",
                table: "FederationOperations",
                columns: new[] { "ProxyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationOperations_SourceOperationId",
                table: "FederationOperations",
                column: "SourceOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationOutbox_OperationId",
                table: "FederationOutbox",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationOutbox_ProxyId_Sequence",
                table: "FederationOutbox",
                columns: new[] { "ProxyId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationProxies_LastHeartbeatUtc",
                table: "FederationProxies",
                column: "LastHeartbeatUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FederationProxies_Status",
                table: "FederationProxies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FederationWorkspaces_GlobalWorkspaceId",
                table: "FederationWorkspaces",
                column: "GlobalWorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FederationWorkspaces_ProxyId",
                table: "FederationWorkspaces",
                column: "ProxyId");

            migrationBuilder.CreateIndex(
                name: "IX_FederationWorkspaces_ProxyId_WorkspacePath",
                table: "FederationWorkspaces",
                columns: new[] { "ProxyId", "WorkspacePath" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FederationConflicts");

            migrationBuilder.DropTable(
                name: "FederationOutbox");

            migrationBuilder.DropTable(
                name: "FederationWorkspaces");

            migrationBuilder.DropTable(
                name: "FederationOperations");

            migrationBuilder.DropTable(
                name: "FederationProxies");
        }
    }
}
