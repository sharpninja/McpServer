using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <summary>TR-MCP-MEMORY-SEARCH-002: Dedicated memory ANN/FTS side table (forward-only).</summary>
    public partial class AddMemoryIndexStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryIndexes",
                columns: table => new
                {
                    MemoryId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EmbeddingJson = table.Column<string>(type: "text", nullable: false),
                    IndexedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryIndexes", x => x.MemoryId);
                    table.ForeignKey(
                        name: "FK_MemoryIndexes_Memories_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "Memories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryIndexes_WorkspaceId",
                table: "MemoryIndexes",
                column: "WorkspaceId");
            migrationBuilder.CreateIndex(
                name: "IX_MemoryIndexes_ContentHash",
                table: "MemoryIndexes",
                column: "ContentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Memory index storage is forward-only.");
        }
    }
}
