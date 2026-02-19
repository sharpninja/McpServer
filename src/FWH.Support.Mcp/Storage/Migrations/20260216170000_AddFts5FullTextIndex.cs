using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FWH.Support.Mcp.Storage.Migrations;

/// <summary>
/// TR-PLANNED-013: FTS5 full-text index over ContextChunkEntity.Content for BM25-ranked search.
/// </summary>
public partial class AddFts5FullTextIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        // Create FTS5 virtual table (external content mode is not used to keep it simple and portable)
        migrationBuilder.Sql("""
            CREATE VIRTUAL TABLE IF NOT EXISTS chunks_fts USING fts5(
                ChunkId UNINDEXED,
                Content
            );
            """);

        // Populate from existing data
        migrationBuilder.Sql("""
            INSERT INTO chunks_fts(ChunkId, Content)
            SELECT Id, Content FROM Chunks;
            """);

        // Trigger: after INSERT on Chunks -> insert into FTS
        migrationBuilder.Sql("""
            CREATE TRIGGER IF NOT EXISTS chunks_fts_insert
            AFTER INSERT ON Chunks
            BEGIN
                INSERT INTO chunks_fts(ChunkId, Content) VALUES(NEW.Id, NEW.Content);
            END;
            """);

        // Trigger: after DELETE on Chunks -> delete from FTS
        migrationBuilder.Sql("""
            CREATE TRIGGER IF NOT EXISTS chunks_fts_delete
            AFTER DELETE ON Chunks
            BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, ChunkId, Content)
                VALUES('delete', OLD.rowid, OLD.Id, OLD.Content);
            END;
            """);

        // Trigger: after UPDATE on Chunks -> delete old + insert new in FTS
        migrationBuilder.Sql("""
            CREATE TRIGGER IF NOT EXISTS chunks_fts_update
            AFTER UPDATE ON Chunks
            BEGIN
                INSERT INTO chunks_fts(chunks_fts, rowid, ChunkId, Content)
                VALUES('delete', OLD.rowid, OLD.Id, OLD.Content);
                INSERT INTO chunks_fts(ChunkId, Content) VALUES(NEW.Id, NEW.Content);
            END;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.Sql("DROP TRIGGER IF EXISTS chunks_fts_update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS chunks_fts_delete;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS chunks_fts_insert;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS chunks_fts;");
    }
}
