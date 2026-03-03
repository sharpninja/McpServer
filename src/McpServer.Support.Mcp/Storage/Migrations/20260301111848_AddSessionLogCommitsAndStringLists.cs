using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogCommitsAndStringLists : Migration
    {
        private static readonly string[] s_postgresIdentityTables =
        {
            "SessionLogs",
            "SessionLogEntries",
            "SessionLogActions",
            "SessionLogEntryContexts",
            "SessionLogEntryTags",
            "SessionLogProcessingDialogs",
            "ToolBuckets",
            "ToolDefinitions",
            "ToolDefinitionTags",
            "AgentEventLogs",
            "AgentWorkspaces",
            "SessionLogCommits",
            "SessionLogEntryStringLists",
        };

        private static readonly (string Table, string Column)[] s_postgresTimestampColumns =
        {
            ("AgentEventLogs", "Timestamp"),
            ("SessionLogCommits", "CommitTimestamp"),
            ("SessionLogEntries", "Timestamp"),
            ("SessionLogProcessingDialogs", "Timestamp"),
            ("SessionLogs", "Started"),
            ("SessionLogs", "LastUpdated"),
            ("ToolBuckets", "DateTimeCreated"),
            ("ToolDefinitions", "DateTimeCreated"),
            ("ToolDefinitions", "DateTimeModified"),
            ("Workspaces", "DateTimeCreated"),
            ("Workspaces", "DateTimeModified"),
        };

        private static readonly (string Table, string Column)[] s_postgresBooleanColumns =
        {
            ("AgentDefinitions", "IsBuiltIn"),
            ("AgentWorkspaces", "Enabled"),
            ("SessionLogEntries", "IsPremium"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionLogCommits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Sha = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CommitTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FilesChangedJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogCommits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogCommits_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogEntryStringLists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    ListType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntryStringLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntryStringLists_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommits_SessionLogEntryId",
                table: "SessionLogCommits",
                column: "SessionLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryStringLists_SessionLogEntryId_ListType",
                table: "SessionLogEntryStringLists",
                columns: new[] { "SessionLogEntryId", "ListType" });

            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                foreach (var table in s_postgresIdentityTables)
                {
                    EnsurePostgresIdentity(migrationBuilder, table);
                }

                foreach (var (table, column) in s_postgresTimestampColumns)
                {
                    EnsurePostgresTimestampColumn(migrationBuilder, table, column);
                }

                foreach (var (table, column) in s_postgresBooleanColumns)
                {
                    EnsurePostgresBooleanColumn(migrationBuilder, table, column);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogCommits");

            migrationBuilder.DropTable(
                name: "SessionLogEntryStringLists");
        }

        private static void EnsurePostgresIdentity(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($$"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{{tableName}}'
                          AND column_name = 'Id'
                          AND column_default IS NULL
                    ) THEN
                        EXECUTE 'ALTER TABLE "' || '{{tableName}}' || '" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{{tableName}}'
                          AND column_name = 'Id'
                    ) THEN
                        EXECUTE 'SELECT setval(pg_get_serial_sequence(''"' || '{{tableName}}' || '"'', ''Id''), COALESCE(MAX("Id"), 0) + 1, false) FROM "' || '{{tableName}}' || '"';
                    END IF;
                END $$;
                """);
        }

        private static void EnsurePostgresTimestampColumn(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            migrationBuilder.Sql($$"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{{tableName}}'
                          AND column_name = '{{columnName}}'
                          AND data_type = 'text'
                    ) THEN
                        EXECUTE 'ALTER TABLE "' || '{{tableName}}' || '" ALTER COLUMN "' || '{{columnName}}' || '" TYPE timestamp with time zone USING NULLIF("' || '{{columnName}}' || '", '''')::timestamp with time zone';
                    END IF;
                END $$;
                """);
        }

        private static void EnsurePostgresBooleanColumn(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            migrationBuilder.Sql($$"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{{tableName}}'
                          AND column_name = '{{columnName}}'
                          AND data_type = 'integer'
                    ) THEN
                        EXECUTE 'ALTER TABLE "' || '{{tableName}}' || '" ALTER COLUMN "' || '{{columnName}}' || '" TYPE boolean USING CASE WHEN "' || '{{columnName}}' || '" IS NULL THEN NULL ELSE "' || '{{columnName}}' || '" <> 0 END';
                    END IF;
                END $$;
                """);
        }
    }
}
