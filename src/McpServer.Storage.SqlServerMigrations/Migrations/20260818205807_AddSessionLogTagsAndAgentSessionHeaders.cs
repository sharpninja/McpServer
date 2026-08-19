using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTagsAndAgentSessionHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'SessionLogs', N'AgentSessionId') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentSessionId] nvarchar(256) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentSessionTranscriptFile') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentSessionTranscriptFile] nvarchar(2048) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentExecutablePath') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentExecutablePath] nvarchar(2048) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentExecutableVersion') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentExecutableVersion] nvarchar(128) NULL;

                IF OBJECT_ID(N'SessionLogTags', N'U') IS NULL
                BEGIN
                    CREATE TABLE [SessionLogTags] (
                        [Id] bigint NOT NULL IDENTITY,
                        [WorkspaceId] nvarchar(1024) NOT NULL,
                        [SessionLogId] bigint NOT NULL,
                        [Tag] nvarchar(256) NOT NULL,
                        [DeleteReason] nvarchar(1024) NULL,
                        [DeletedAtUtc] datetime2 NULL,
                        [DeletedBy] nvarchar(256) NULL,
                        [IsDeleted] bit NOT NULL CONSTRAINT [DF_SessionLogTags_IsDeleted] DEFAULT CAST(0 AS bit),
                        CONSTRAINT [PK_SessionLogTags] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_SessionLogTags_SessionLogs_SessionLogId] FOREIGN KEY ([SessionLogId]) REFERENCES [SessionLogs] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_SessionLogTags_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([WorkspaceId]) ON DELETE NO ACTION
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SessionLogTags_SessionLogId_Tag' AND object_id = OBJECT_ID(N'SessionLogTags'))
                    CREATE UNIQUE INDEX [IX_SessionLogTags_SessionLogId_Tag] ON [SessionLogTags] ([SessionLogId], [Tag]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SessionLogTags_WorkspaceId' AND object_id = OBJECT_ID(N'SessionLogTags'))
                    CREATE INDEX [IX_SessionLogTags_WorkspaceId] ON [SessionLogTags] ([WorkspaceId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogTags");
        }
    }
}
