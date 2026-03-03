using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations;

/// <summary>
/// TR-PLANNED-013: Add nullable Embedding BLOB column to Chunks table for vector search.
/// </summary>
public partial class AddEmbeddingColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.AddColumn<byte[]>(
            name: "Embedding",
            table: "Chunks",
            type: "BLOB",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.DropColumn(
            name: "Embedding",
            table: "Chunks");
    }
}
