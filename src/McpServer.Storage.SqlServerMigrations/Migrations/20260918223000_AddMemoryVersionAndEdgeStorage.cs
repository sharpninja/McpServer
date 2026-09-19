using System;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <summary>TR-MCP-MEMORY-MODEL-002: Memory version/edge tables and multi-layer columns (forward-only).</summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260918223000_AddMemoryVersionAndEdgeStorage")]
    public partial class AddMemoryVersionAndEdgeStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Title", table: "Memories", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Summary", table: "Memories", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Content", table: "Memories", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Type", table: "Memories", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Tags", table: "Memories", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<double>(name: "Confidence", table: "Memories", type: "float", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SourceKind", table: "Memories", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SourceRef", table: "Memories", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CreatedBy", table: "Memories", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EmbeddingStatus", table: "Memories", type: "nvarchar(32)", maxLength: 32, nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Memories]
                SET [Content] = [Text]
                WHERE [Content] IS NULL OR [Content] = '';
                """);

            migrationBuilder.CreateTable(
                name: "MemoryVersions",
                columns: table => new
                {
                    VersionRowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemoryId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryVersions", x => x.VersionRowId);
                    table.ForeignKey(
                        name: "FK_MemoryVersions_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemoryEdges",
                columns: table => new
                {
                    EdgeRowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromMemoryId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ToMemoryId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EdgeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryEdges", x => x.EdgeRowId);
                });

            migrationBuilder.CreateIndex(name: "IX_Memories_WorkspaceId_Scope", table: "Memories", columns: new[] { "WorkspaceId", "Scope" });
            migrationBuilder.CreateIndex(name: "IX_Memories_EmbeddingStatus", table: "Memories", column: "EmbeddingStatus");
            migrationBuilder.CreateIndex(name: "IX_MemoryVersions_MemoryId_VersionNumber", table: "MemoryVersions", columns: new[] { "MemoryId", "VersionNumber" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_MemoryEdges_FromMemoryId_ToMemoryId_EdgeType", table: "MemoryEdges", columns: new[] { "FromMemoryId", "ToMemoryId", "EdgeType" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Memory version/edge storage is forward-only.");
        }
    }
}
