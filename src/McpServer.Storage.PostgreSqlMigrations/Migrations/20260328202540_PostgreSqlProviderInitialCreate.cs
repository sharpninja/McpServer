using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class PostgreSqlProviderInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DefaultLaunchCommand = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DefaultInstructionFile = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DefaultModelsJson = table.Column<string>(type: "text", nullable: false),
                    DefaultBranchStrategy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DefaultSeedPrompt = table.Column<string>(type: "text", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentEventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkspacePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceKey = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolBuckets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Owner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Repo = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Branch = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ManifestPath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateTimeLastSynced = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolBuckets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ParameterSchema = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    CommandTemplate = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    WorkspacePath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    BucketName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateTimeModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    AgentDefinitionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkspacePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Banned = table.Column<bool>(type: "boolean", nullable: false),
                    BannedReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    BannedUntilPr = table.Column<int>(type: "integer", nullable: true),
                    AgentIsolation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LaunchCommandOverride = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ModelsOverrideJson = table.Column<string>(type: "text", nullable: true),
                    BranchStrategyOverride = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SeedPromptOverride = table.Column<string>(type: "text", nullable: true),
                    MarkerAdditions = table.Column<string>(type: "text", nullable: false),
                    InstructionFilesOverrideJson = table.Column<string>(type: "text", nullable: true),
                    RestartPolicy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLaunchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "AgentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AgentDefinitionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Started = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EntryCount = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: true),
                    CursorSessionLabel = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CopilotAvgSuccessScore = table.Column<double>(type: "double precision", nullable: true),
                    CopilotTotalNetTokens = table.Column<int>(type: "integer", nullable: true),
                    CopilotTotalNetPremiumRequests = table.Column<int>(type: "integer", nullable: true),
                    CopilotCompletedCount = table.Column<int>(type: "integer", nullable: true),
                    CopilotInProgressCount = table.Column<int>(type: "integer", nullable: true),
                    Project = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TargetFramework = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Repository = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Branch = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceFilePath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogs_AgentDefinitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "AgentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Chunks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    DocumentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<byte[]>(type: "bytea", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ToolDefinitionTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    ToolDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalTable: "ToolDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogTurns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogId = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ModelProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QueryText = table.Column<string>(type: "text", nullable: true),
                    QueryTitle = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Response = table.Column<string>(type: "text", nullable: true),
                    Interpretation = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TokenCount = table.Column<int>(type: "integer", nullable: true),
                    FailureNote = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: true),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: true),
                    RawContextJson = table.Column<string>(type: "text", nullable: true),
                    OriginalEntryJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogCommits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Sha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Branch = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CommitTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FilesChangedJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogCommits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogCommits_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogProcessingDialogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "SessionLogTurnContexts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ContextItem = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurnContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogTurnStringLists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    ListType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurnStringLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurnStringLists_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogTurnTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "text", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    Tag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurnTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_IsBuiltIn",
                table: "AgentDefinitions",
                column: "IsBuiltIn");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_WorkspaceId",
                table: "AgentDefinitions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_AgentId",
                table: "AgentEventLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_EventType",
                table: "AgentEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_Timestamp",
                table: "AgentEventLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_WorkspaceId",
                table: "AgentEventLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_WorkspacePath",
                table: "AgentEventLogs",
                column: "WorkspacePath");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_AgentDefinitionId_WorkspacePath",
                table: "AgentWorkspaces",
                columns: new[] { "AgentDefinitionId", "WorkspacePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_WorkspaceId",
                table: "AgentWorkspaces",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_WorkspacePath",
                table: "AgentWorkspaces",
                column: "WorkspacePath");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_DocumentId",
                table: "Chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_WorkspaceId",
                table: "Chunks",
                column: "WorkspaceId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Documents_WorkspaceId",
                table: "Documents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_SessionLogTurnId",
                table: "SessionLogActions",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_WorkspaceId",
                table: "SessionLogActions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommits_SessionLogTurnId",
                table: "SessionLogCommits",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogProcessingDialogs_SessionLogTurnId",
                table: "SessionLogProcessingDialogs",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogProcessingDialogs_WorkspaceId",
                table: "SessionLogProcessingDialogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_AgentDefinitionId",
                table: "SessionLogs",
                column: "AgentDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_LastUpdated",
                table: "SessionLogs",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_SourceType",
                table: "SessionLogs",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_SourceType_SessionId",
                table: "SessionLogs",
                columns: new[] { "SourceType", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_Started",
                table: "SessionLogs",
                column: "Started");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_WorkspaceId",
                table: "SessionLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnContexts_SessionLogTurnId",
                table: "SessionLogTurnContexts",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnContexts_WorkspaceId",
                table: "SessionLogTurnContexts",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_SessionLogId_RequestId",
                table: "SessionLogTurns",
                columns: new[] { "SessionLogId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_WorkspaceId",
                table: "SessionLogTurns",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnStringLists_SessionLogTurnId_ListType",
                table: "SessionLogTurnStringLists",
                columns: new[] { "SessionLogTurnId", "ListType" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnTags_SessionLogTurnId",
                table: "SessionLogTurnTags",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnTags_WorkspaceId",
                table: "SessionLogTurnTags",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolBuckets_Name",
                table: "ToolBuckets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolBuckets_WorkspaceId",
                table: "ToolBuckets",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_Name_WorkspacePath",
                table: "ToolDefinitions",
                columns: new[] { "Name", "WorkspacePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_WorkspaceId",
                table: "ToolDefinitions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_WorkspacePath",
                table: "ToolDefinitions",
                column: "WorkspacePath");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_Tag",
                table: "ToolDefinitionTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_ToolDefinitionId_Tag",
                table: "ToolDefinitionTags",
                columns: new[] { "ToolDefinitionId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_WorkspaceId",
                table: "ToolDefinitionTags",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentEventLogs");

            migrationBuilder.DropTable(
                name: "AgentWorkspaces");

            migrationBuilder.DropTable(
                name: "Chunks");

            migrationBuilder.DropTable(
                name: "SessionLogActions");

            migrationBuilder.DropTable(
                name: "SessionLogCommits");

            migrationBuilder.DropTable(
                name: "SessionLogProcessingDialogs");

            migrationBuilder.DropTable(
                name: "SessionLogTurnContexts");

            migrationBuilder.DropTable(
                name: "SessionLogTurnStringLists");

            migrationBuilder.DropTable(
                name: "SessionLogTurnTags");

            migrationBuilder.DropTable(
                name: "ToolBuckets");

            migrationBuilder.DropTable(
                name: "ToolDefinitionTags");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "SessionLogTurns");

            migrationBuilder.DropTable(
                name: "ToolDefinitions");

            migrationBuilder.DropTable(
                name: "SessionLogs");

            migrationBuilder.DropTable(
                name: "AgentDefinitions");
        }
    }
}
