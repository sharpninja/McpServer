using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(McpDbContext))]
    [Migration("20260302235000_FixPostgresAgentWorkspaceBannedBoolean")]
    public class FixPostgresAgentWorkspaceBannedBoolean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
                return;

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'AgentWorkspaces'
                          AND column_name = 'Banned'
                          AND data_type = 'integer'
                    ) THEN
                        EXECUTE 'ALTER TABLE "AgentWorkspaces" ALTER COLUMN "Banned" TYPE boolean USING CASE WHEN "Banned" IS NULL THEN NULL ELSE "Banned" <> 0 END';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
                return;

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'AgentWorkspaces'
                          AND column_name = 'Banned'
                          AND data_type = 'boolean'
                    ) THEN
                        EXECUTE 'ALTER TABLE "AgentWorkspaces" ALTER COLUMN "Banned" TYPE integer USING CASE WHEN "Banned" THEN 1 ELSE 0 END';
                    END IF;
                END $$;
                """);
        }
    }
}
