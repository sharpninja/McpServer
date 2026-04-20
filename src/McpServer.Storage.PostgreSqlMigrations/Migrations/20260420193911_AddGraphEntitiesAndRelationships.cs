using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGraphEntitiesAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GraphEntities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraphRelationships",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SourceEntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetEntityId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RelationshipType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GraphRelationships_GraphEntities_SourceEntityId",
                        column: x => x.SourceEntityId,
                        principalTable: "GraphEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GraphRelationships_GraphEntities_TargetEntityId",
                        column: x => x.TargetEntityId,
                        principalTable: "GraphEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GraphEntities_EntityType",
                table: "GraphEntities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEntities_Name",
                table: "GraphEntities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEntities_WorkspaceId",
                table: "GraphEntities",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphRelationships_RelationshipType",
                table: "GraphRelationships",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_GraphRelationships_SourceEntityId",
                table: "GraphRelationships",
                column: "SourceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphRelationships_TargetEntityId",
                table: "GraphRelationships",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphRelationships_WorkspaceId",
                table: "GraphRelationships",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GraphRelationships");

            migrationBuilder.DropTable(
                name: "GraphEntities");
        }
    }
}
