using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingDialog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "SessionLogProcessingDialogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogTurnId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogProcessingDialogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogProcessingDialogs_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogProcessingDialogs_SessionLogTurnId",
                table: "SessionLogProcessingDialogs",
                column: "SessionLogTurnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "SessionLogProcessingDialogs");
        }
    }
}
