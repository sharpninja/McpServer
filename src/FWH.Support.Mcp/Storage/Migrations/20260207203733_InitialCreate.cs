using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FWH.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceKey = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Chunks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChunkIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_DocumentId",
                table: "Chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_IngestedAt",
                table: "Documents",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceKey",
                table: "Documents",
                column: "SourceKey");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceType",
                table: "Documents",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.DropTable(
                name: "Chunks");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
