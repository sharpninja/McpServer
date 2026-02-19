using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FWH.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1062:Validate arguments of public methods", Justification = "Auto-generated EF Core migration")]
    public partial class AddProcessingDialog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionLogProcessingDialogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
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
                        name: "FK_SessionLogProcessingDialogs_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogProcessingDialogs_SessionLogEntryId",
                table: "SessionLogProcessingDialogs",
                column: "SessionLogEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogProcessingDialogs");
        }
    }
}
