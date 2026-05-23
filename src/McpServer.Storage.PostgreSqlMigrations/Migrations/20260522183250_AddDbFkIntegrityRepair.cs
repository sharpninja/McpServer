using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDbFkIntegrityRepair : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                table: "AgentWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Chunks_Documents_DocumentId",
                table: "Chunks");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationConflicts_FederationOperations_OperationId",
                table: "FederationConflicts");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationOperations_FederationProxies_ProxyId",
                table: "FederationOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationOutbox_FederationOperations_OperationId",
                table: "FederationOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationWorkspaces_FederationProxies_ProxyId",
                table: "FederationWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphRelationships_GraphEntities_SourceEntityId",
                table: "GraphRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphRelationships_GraphEntities_TargetEntityId",
                table: "GraphRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogActions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogCommits_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogCommits");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogProcessingDialogs_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
                table: "SessionLogTurns");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnStringLists_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                table: "ToolDefinitionTags");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitionTags",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolDefinitionTags",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolDefinitionTags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolDefinitionTags",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolDefinitionTags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitions",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolDefinitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolDefinitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolBuckets",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolBuckets",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolBuckets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolBuckets",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolBuckets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoItems",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoItems",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoDocumentMetadata",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoDocumentMetadata",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoDocumentMetadata",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoDocumentMetadata",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoAuditHistory",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoAuditHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoAuditHistory",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoAuditHistory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnTags",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnTags",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnTags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnTags",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnTags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnStringLists",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnStringLists",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnStringLists",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnStringLists",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnStringLists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurns",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurns",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurns",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnContexts",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnContexts",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnContexts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnContexts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnContexts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogs",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogs",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogProcessingDialogs",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogProcessingDialogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogProcessingDialogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogProcessingDialogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogCommits",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogCommits",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogCommits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogCommits",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogCommits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogActions",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogActions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogActions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogActions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogActions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "RequirementTraceabilityLinks",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "RequirementTraceabilityLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RequirementTraceabilityLinks",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RequirementTraceabilityLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                table: "RequirementTraceabilityLinks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Requirements",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Requirements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Requirements",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Requirements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphRelationships",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "GraphRelationships",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "GraphRelationships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GraphRelationships",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GraphRelationships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphEntities",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "GraphEntities",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "GraphEntities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GraphEntities",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GraphEntities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalWorkspaceId",
                table: "FederationWorkspaces",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationWorkspaces",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationWorkspaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationWorkspaces",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationWorkspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationProxies",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationProxies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationProxies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationProxies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationOutbox",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationOutbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationOutbox",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationOutbox",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationOperations",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationOperations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationOperations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationOperations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationConflicts",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationConflicts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationConflicts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationConflicts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Documents",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Documents",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Documents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Chunks",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Chunks",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Chunks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Chunks",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Chunks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentWorkspaces",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentWorkspaces",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentWorkspaces",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentWorkspaces",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentWorkspaces",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentEventLogs",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentEventLogs",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentEventLogs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentEventLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentEventLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentDefinitions",
                type: "character varying(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentDefinitions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentDefinitions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    WorkspacePath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TodoPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    DataDirectory = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TunnelProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RunAs = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PromptTemplate = table.Column<string>(type: "text", nullable: true),
                    StatusPrompt = table.Column<string>(type: "text", nullable: true),
                    ImplementPrompt = table.Column<string>(type: "text", nullable: true),
                    PlanPrompt = table.Column<string>(type: "text", nullable: true),
                    BannedLicensesJson = table.Column<string>(type: "text", nullable: true),
                    BannedCountriesOfOriginJson = table.Column<string>(type: "text", nullable: true),
                    BannedOrganizationsJson = table.Column<string>(type: "text", nullable: true),
                    BannedIndividualsJson = table.Column<string>(type: "text", nullable: true),
                    AgentPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DateTimeModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.WorkspaceId);
                });

            migrationBuilder.CreateTable(
                name: "DataAuditLogs",
                columns: table => new
                {
                    AuditId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EntityKind = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EntityKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FederationOperationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    CurrentSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    DiffJson = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataAuditLogs", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_DataAuditLogs_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoRecords",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoRecords", x => new { x.WorkspaceId, x.TodoId });
                    table.ForeignKey(
                        name: "FK_TodoRecords_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoRequirementLinks",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequirementKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoRequirementLinks", x => new { x.WorkspaceId, x.TodoId, x.RequirementKind, x.RequirementId });
                    table.ForeignKey(
                        name: "FK_TodoRequirementLinks_Requirements_WorkspaceId_RequirementKi~",
                        columns: x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId },
                        principalTable: "Requirements",
                        principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoRequirementLinks_TodoRecords_WorkspaceId_TodoId",
                        columns: x => new { x.WorkspaceId, x.TodoId },
                        principalTable: "TodoRecords",
                        principalColumns: new[] { "WorkspaceId", "TodoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoRequirementLinks_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Workspaces",
                columns: new[] { "WorkspaceId", "AgentPath", "BannedCountriesOfOriginJson", "BannedIndividualsJson", "BannedLicensesJson", "BannedOrganizationsJson", "DataDirectory", "DateTimeCreated", "DateTimeModified", "DeleteReason", "DeletedAtUtc", "DeletedBy", "ImplementPrompt", "IsEnabled", "IsPrimary", "Name", "PlanPrompt", "PromptTemplate", "RunAs", "StatusPrompt", "TodoPath", "TunnelProvider", "WorkspacePath" },
                values: new object[] { "", null, null, null, null, null, null, new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "global", null, null, null, null, "docs/todo.yaml", null, "" });

            migrationBuilder.Sql("""
                INSERT INTO "Workspaces" ("WorkspaceId", "WorkspacePath", "Name", "TodoPath", "IsEnabled", "IsPrimary", "DateTimeCreated", "DateTimeModified", "IsDeleted")
                SELECT DISTINCT ws."WorkspaceId", ws."WorkspaceId", CASE WHEN ws."WorkspaceId" = '' THEN 'global' ELSE left(ws."WorkspaceId", 512) END, 'docs/todo.yaml', true, false, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', false
                FROM (
                    SELECT "WorkspaceId" FROM "AgentDefinitions" UNION SELECT "WorkspaceId" FROM "AgentEventLogs" UNION SELECT "WorkspaceId" FROM "AgentWorkspaces" UNION SELECT "WorkspaceId" FROM "Chunks" UNION SELECT "WorkspaceId" FROM "Documents" UNION SELECT "WorkspaceId" FROM "GraphEntities" UNION SELECT "WorkspaceId" FROM "GraphRelationships" UNION SELECT "WorkspaceId" FROM "Requirements" UNION SELECT "WorkspaceId" FROM "RequirementTraceabilityLinks" UNION SELECT "WorkspaceId" FROM "SessionLogActions" UNION SELECT "WorkspaceId" FROM "SessionLogCommits" UNION SELECT "WorkspaceId" FROM "SessionLogProcessingDialogs" UNION SELECT "WorkspaceId" FROM "SessionLogs" UNION SELECT "WorkspaceId" FROM "SessionLogTurnContexts" UNION SELECT "WorkspaceId" FROM "SessionLogTurns" UNION SELECT "WorkspaceId" FROM "SessionLogTurnStringLists" UNION SELECT "WorkspaceId" FROM "SessionLogTurnTags" UNION SELECT "WorkspaceId" FROM "TodoAuditHistory" UNION SELECT "WorkspaceId" FROM "TodoDocumentMetadata" UNION SELECT "WorkspaceId" FROM "TodoItems" UNION SELECT "WorkspaceId" FROM "ToolBuckets" UNION SELECT "WorkspaceId" FROM "ToolDefinitions" UNION SELECT "WorkspaceId" FROM "ToolDefinitionTags"
                ) ws
                WHERE ws."WorkspaceId" IS NOT NULL
                ON CONFLICT ("WorkspaceId") DO NOTHING;

                UPDATE "FederationWorkspaces"
                SET "CanonicalWorkspaceId" = COALESCE(NULLIF("CanonicalWorkspaceId", ''), "GlobalWorkspaceId");

                INSERT INTO "Workspaces" ("WorkspaceId", "WorkspacePath", "Name", "TodoPath", "IsEnabled", "IsPrimary", "DateTimeCreated", "DateTimeModified", "IsDeleted")
                SELECT DISTINCT "CanonicalWorkspaceId", "CanonicalWorkspaceId", COALESCE(NULLIF("WorkspaceName", ''), left("CanonicalWorkspaceId", 512)), 'docs/todo.yaml', "IsEnabled", false, TIMESTAMPTZ '1970-01-01T00:00:00+00:00', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', false
                FROM "FederationWorkspaces"
                WHERE "CanonicalWorkspaceId" IS NOT NULL AND "CanonicalWorkspaceId" <> ''
                ON CONFLICT ("WorkspaceId") DO NOTHING;

                INSERT INTO "TodoRecords" ("WorkspaceId", "TodoId", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
                SELECT "WorkspaceId", "Id", TIMESTAMPTZ '1970-01-01T00:00:00+00:00', TIMESTAMPTZ '1970-01-01T00:00:00+00:00', false
                FROM "TodoItems"
                ON CONFLICT ("WorkspaceId", "TodoId") DO NOTHING;

                CREATE TEMP TABLE "__todo_requirement_ids" (
                    "WorkspaceId" character varying(1024) NOT NULL,
                    "TodoId" character varying(128) NOT NULL,
                    "RequirementKind" character varying(16) NOT NULL,
                    "RequirementId" character varying(128) NOT NULL
                ) ON COMMIT DROP;

                INSERT INTO "__todo_requirement_ids" ("WorkspaceId", "TodoId", "RequirementKind", "RequirementId")
                SELECT DISTINCT src."WorkspaceId", src."TodoId", src."RequirementKind", left(regexp_replace(btrim(src."RawRequirementId"), '[[:space:]:].*$', ''), 128)
                FROM (
                    SELECT ti."WorkspaceId", ti."Id" AS "TodoId", 'fr' AS "RequirementKind", fr.value AS "RawRequirementId"
                    FROM "TodoItems" ti
                    CROSS JOIN LATERAL jsonb_array_elements_text(CASE WHEN ti."FunctionalRequirementsJson" IS NOT NULL AND ti."FunctionalRequirementsJson" ~ '^\s*\[' THEN ti."FunctionalRequirementsJson"::jsonb ELSE '[]'::jsonb END) AS fr(value)
                    UNION ALL
                    SELECT ti."WorkspaceId", ti."Id" AS "TodoId", 'tr' AS "RequirementKind", tr.value AS "RawRequirementId"
                    FROM "TodoItems" ti
                    CROSS JOIN LATERAL jsonb_array_elements_text(CASE WHEN ti."TechnicalRequirementsJson" IS NOT NULL AND ti."TechnicalRequirementsJson" ~ '^\s*\[' THEN ti."TechnicalRequirementsJson"::jsonb ELSE '[]'::jsonb END) AS tr(value)
                ) src
                WHERE btrim(coalesce(src."RawRequirementId", '')) <> ''
                  AND left(regexp_replace(btrim(src."RawRequirementId"), '[[:space:]:].*$', ''), 128) <> '';

                INSERT INTO "Requirements" ("WorkspaceId", "Kind", "Id", "Title", "Body", "Priority", "Status", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
                SELECT DISTINCT ids."WorkspaceId", ids."RequirementKind", ids."RequirementId", ids."RequirementId", 'Placeholder requirement backfilled by DB-FK-001.', 'medium', 'pending', '1970-01-01T00:00:00Z', '1970-01-01T00:00:00Z', false
                FROM "__todo_requirement_ids" ids
                ON CONFLICT ("WorkspaceId", "Kind", "Id") DO NOTHING;

                INSERT INTO "Requirements" ("WorkspaceId", "Kind", "Id", "Title", "Body", "Priority", "Status", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
                SELECT DISTINCT "WorkspaceId", 'fr', "FrId", "FrId", 'Placeholder requirement backfilled by DB-FK-001.', 'medium', 'pending', '1970-01-01T00:00:00Z', '1970-01-01T00:00:00Z', false
                FROM "RequirementTraceabilityLinks"
                WHERE "FrId" IS NOT NULL AND "FrId" <> ''
                ON CONFLICT ("WorkspaceId", "Kind", "Id") DO NOTHING;

                INSERT INTO "Requirements" ("WorkspaceId", "Kind", "Id", "Title", "Body", "Priority", "Status", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted")
                SELECT DISTINCT "WorkspaceId", "TargetKind", "TargetId", "TargetId", 'Placeholder requirement backfilled by DB-FK-001.', 'medium', 'pending', '1970-01-01T00:00:00Z', '1970-01-01T00:00:00Z', false
                FROM "RequirementTraceabilityLinks"
                WHERE "TargetKind" IS NOT NULL AND "TargetKind" <> '' AND "TargetId" IS NOT NULL AND "TargetId" <> ''
                ON CONFLICT ("WorkspaceId", "Kind", "Id") DO NOTHING;

                INSERT INTO "TodoRequirementLinks" ("WorkspaceId", "TodoId", "RequirementKind", "RequirementId", "CreatedAtUtc", "IsDeleted")
                SELECT ids."WorkspaceId", ids."TodoId", ids."RequirementKind", ids."RequirementId", TIMESTAMPTZ '1970-01-01T00:00:00+00:00', false
                FROM "__todo_requirement_ids" ids
                ON CONFLICT ("WorkspaceId", "TodoId", "RequirementKind", "RequirementId") DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnStringLists_WorkspaceId",
                table: "SessionLogTurnStringLists",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommits_WorkspaceId",
                table: "SessionLogCommits",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementTraceabilityLinks_WorkspaceId_SourceKind_FrId",
                table: "RequirementTraceabilityLinks",
                columns: new[] { "WorkspaceId", "SourceKind", "FrId" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationWorkspaces_CanonicalWorkspaceId",
                table: "FederationWorkspaces",
                column: "CanonicalWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAuditLogs_Action",
                table: "DataAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_DataAuditLogs_CorrelationId",
                table: "DataAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAuditLogs_FederationOperationId",
                table: "DataAuditLogs",
                column: "FederationOperationId");

            migrationBuilder.CreateIndex(
                name: "IX_DataAuditLogs_OccurredAtUtc",
                table: "DataAuditLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DataAuditLogs_WorkspaceId_EntityKind_EntityKey",
                table: "DataAuditLogs",
                columns: new[] { "WorkspaceId", "EntityKind", "EntityKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoRecords_UpdatedAtUtc",
                table: "TodoRecords",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TodoRequirementLinks_WorkspaceId_RequirementKind_Requiremen~",
                table: "TodoRequirementLinks",
                columns: new[] { "WorkspaceId", "RequirementKind", "RequirementId" });

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_IsDeleted",
                table: "Workspaces",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_IsEnabled",
                table: "Workspaces",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_IsPrimary",
                table: "Workspaces",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_WorkspacePath",
                table: "Workspaces",
                column: "WorkspacePath",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentDefinitions_Workspaces_WorkspaceId",
                table: "AgentDefinitions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentEventLogs_Workspaces_WorkspaceId",
                table: "AgentEventLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                table: "AgentWorkspaces",
                column: "AgentDefinitionId",
                principalTable: "AgentDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkspaces_Workspaces_WorkspaceId",
                table: "AgentWorkspaces",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chunks_Documents_DocumentId",
                table: "Chunks",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Chunks_Workspaces_WorkspaceId",
                table: "Chunks",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Workspaces_WorkspaceId",
                table: "Documents",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationConflicts_FederationOperations_OperationId",
                table: "FederationConflicts",
                column: "OperationId",
                principalTable: "FederationOperations",
                principalColumn: "OperationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationOperations_FederationProxies_ProxyId",
                table: "FederationOperations",
                column: "ProxyId",
                principalTable: "FederationProxies",
                principalColumn: "ProxyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationOutbox_FederationOperations_OperationId",
                table: "FederationOutbox",
                column: "OperationId",
                principalTable: "FederationOperations",
                principalColumn: "OperationId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationWorkspaces_FederationProxies_ProxyId",
                table: "FederationWorkspaces",
                column: "ProxyId",
                principalTable: "FederationProxies",
                principalColumn: "ProxyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationWorkspaces_Workspaces_CanonicalWorkspaceId",
                table: "FederationWorkspaces",
                column: "CanonicalWorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphEntities_Workspaces_WorkspaceId",
                table: "GraphEntities",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphRelationships_GraphEntities_SourceEntityId",
                table: "GraphRelationships",
                column: "SourceEntityId",
                principalTable: "GraphEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphRelationships_GraphEntities_TargetEntityId",
                table: "GraphRelationships",
                column: "TargetEntityId",
                principalTable: "GraphEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphRelationships_Workspaces_WorkspaceId",
                table: "GraphRelationships",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requirements_Workspaces_WorkspaceId",
                table: "Requirements",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_Sourc~",
                table: "RequirementTraceabilityLinks",
                columns: new[] { "WorkspaceId", "SourceKind", "FrId" },
                principalTable: "Requirements",
                principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_Targe~",
                table: "RequirementTraceabilityLinks",
                columns: new[] { "WorkspaceId", "TargetKind", "TargetId" },
                principalTable: "Requirements",
                principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequirementTraceabilityLinks_Workspaces_WorkspaceId",
                table: "RequirementTraceabilityLinks",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogActions",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogActions_Workspaces_WorkspaceId",
                table: "SessionLogActions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogCommits_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogCommits",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogCommits_Workspaces_WorkspaceId",
                table: "SessionLogCommits",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogProcessingDialogs_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogProcessingDialogs",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogProcessingDialogs_Workspaces_WorkspaceId",
                table: "SessionLogProcessingDialogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogs_Workspaces_WorkspaceId",
                table: "SessionLogs",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnContexts",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnContexts_Workspaces_WorkspaceId",
                table: "SessionLogTurnContexts",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
                table: "SessionLogTurns",
                column: "SessionLogId",
                principalTable: "SessionLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurns_Workspaces_WorkspaceId",
                table: "SessionLogTurns",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnStringLists_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnStringLists",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnStringLists_Workspaces_WorkspaceId",
                table: "SessionLogTurnStringLists",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnTags",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnTags_Workspaces_WorkspaceId",
                table: "SessionLogTurnTags",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoAuditHistory_Workspaces_WorkspaceId",
                table: "TodoAuditHistory",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoDocumentMetadata_Workspaces_WorkspaceId",
                table: "TodoDocumentMetadata",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_Workspaces_WorkspaceId",
                table: "TodoItems",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToolBuckets_Workspaces_WorkspaceId",
                table: "ToolBuckets",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToolDefinitions_Workspaces_WorkspaceId",
                table: "ToolDefinitions",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                table: "ToolDefinitionTags",
                column: "ToolDefinitionId",
                principalTable: "ToolDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ToolDefinitionTags_Workspaces_WorkspaceId",
                table: "ToolDefinitionTags",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "WorkspaceId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentDefinitions_Workspaces_WorkspaceId",
                table: "AgentDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentEventLogs_Workspaces_WorkspaceId",
                table: "AgentEventLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                table: "AgentWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentWorkspaces_Workspaces_WorkspaceId",
                table: "AgentWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_Chunks_Documents_DocumentId",
                table: "Chunks");

            migrationBuilder.DropForeignKey(
                name: "FK_Chunks_Workspaces_WorkspaceId",
                table: "Chunks");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Workspaces_WorkspaceId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationConflicts_FederationOperations_OperationId",
                table: "FederationConflicts");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationOperations_FederationProxies_ProxyId",
                table: "FederationOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationOutbox_FederationOperations_OperationId",
                table: "FederationOutbox");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationWorkspaces_FederationProxies_ProxyId",
                table: "FederationWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_FederationWorkspaces_Workspaces_CanonicalWorkspaceId",
                table: "FederationWorkspaces");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphEntities_Workspaces_WorkspaceId",
                table: "GraphEntities");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphRelationships_GraphEntities_SourceEntityId",
                table: "GraphRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphRelationships_GraphEntities_TargetEntityId",
                table: "GraphRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_GraphRelationships_Workspaces_WorkspaceId",
                table: "GraphRelationships");

            migrationBuilder.DropForeignKey(
                name: "FK_Requirements_Workspaces_WorkspaceId",
                table: "Requirements");

            migrationBuilder.DropForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_Sourc~",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_Targe~",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequirementTraceabilityLinks_Workspaces_WorkspaceId",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogActions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogActions_Workspaces_WorkspaceId",
                table: "SessionLogActions");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogCommits_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogCommits");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogCommits_Workspaces_WorkspaceId",
                table: "SessionLogCommits");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogProcessingDialogs_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogProcessingDialogs_Workspaces_WorkspaceId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogs_Workspaces_WorkspaceId",
                table: "SessionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnContexts_Workspaces_WorkspaceId",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
                table: "SessionLogTurns");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurns_Workspaces_WorkspaceId",
                table: "SessionLogTurns");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnStringLists_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnStringLists_Workspaces_WorkspaceId",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnTags");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionLogTurnTags_Workspaces_WorkspaceId",
                table: "SessionLogTurnTags");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoAuditHistory_Workspaces_WorkspaceId",
                table: "TodoAuditHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoDocumentMetadata_Workspaces_WorkspaceId",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_Workspaces_WorkspaceId",
                table: "TodoItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ToolBuckets_Workspaces_WorkspaceId",
                table: "ToolBuckets");

            migrationBuilder.DropForeignKey(
                name: "FK_ToolDefinitions_Workspaces_WorkspaceId",
                table: "ToolDefinitions");

            migrationBuilder.DropForeignKey(
                name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                table: "ToolDefinitionTags");

            migrationBuilder.DropForeignKey(
                name: "FK_ToolDefinitionTags_Workspaces_WorkspaceId",
                table: "ToolDefinitionTags");

            migrationBuilder.DropTable(
                name: "DataAuditLogs");

            migrationBuilder.DropTable(
                name: "TodoRequirementLinks");

            migrationBuilder.DropTable(
                name: "TodoRecords");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogTurnStringLists_WorkspaceId",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogCommits_WorkspaceId",
                table: "SessionLogCommits");

            migrationBuilder.DropIndex(
                name: "IX_RequirementTraceabilityLinks_WorkspaceId_SourceKind_FrId",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropIndex(
                name: "IX_FederationWorkspaces_CanonicalWorkspaceId",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ToolDefinitionTags");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ToolDefinitionTags");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ToolDefinitionTags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ToolDefinitionTags");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ToolDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ToolDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ToolDefinitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ToolDefinitions");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "ToolBuckets");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "ToolBuckets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ToolBuckets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ToolBuckets");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "TodoAuditHistory");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "TodoAuditHistory");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "TodoAuditHistory");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TodoAuditHistory");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogTurnTags");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogTurnTags");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogTurnTags");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogTurnTags");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogTurnContexts");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogCommits");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogCommits");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogCommits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogCommits");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropColumn(
                name: "SourceKind",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "GraphRelationships");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "GraphRelationships");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GraphRelationships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GraphRelationships");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "GraphEntities");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "GraphEntities");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "GraphEntities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GraphEntities");

            migrationBuilder.DropColumn(
                name: "CanonicalWorkspaceId",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FederationWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "FederationProxies");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "FederationProxies");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FederationProxies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FederationProxies");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "FederationOutbox");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "FederationOutbox");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FederationOutbox");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FederationOutbox");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "FederationOperations");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "FederationOperations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FederationOperations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FederationOperations");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "FederationConflicts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "FederationConflicts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FederationConflicts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FederationConflicts");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "AgentWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "AgentWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AgentWorkspaces");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AgentWorkspaces");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "AgentEventLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "AgentEventLogs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AgentEventLogs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AgentEventLogs");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "AgentDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "AgentDefinitions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "AgentDefinitions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AgentDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitionTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolBuckets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnTags",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnStringLists",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurns",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnContexts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogCommits",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogActions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphRelationships",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphEntities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Chunks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentWorkspaces",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentEventLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentDefinitions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                table: "AgentWorkspaces",
                column: "AgentDefinitionId",
                principalTable: "AgentDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Chunks_Documents_DocumentId",
                table: "Chunks",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationConflicts_FederationOperations_OperationId",
                table: "FederationConflicts",
                column: "OperationId",
                principalTable: "FederationOperations",
                principalColumn: "OperationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationOperations_FederationProxies_ProxyId",
                table: "FederationOperations",
                column: "ProxyId",
                principalTable: "FederationProxies",
                principalColumn: "ProxyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationOutbox_FederationOperations_OperationId",
                table: "FederationOutbox",
                column: "OperationId",
                principalTable: "FederationOperations",
                principalColumn: "OperationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FederationWorkspaces_FederationProxies_ProxyId",
                table: "FederationWorkspaces",
                column: "ProxyId",
                principalTable: "FederationProxies",
                principalColumn: "ProxyId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphRelationships_GraphEntities_SourceEntityId",
                table: "GraphRelationships",
                column: "SourceEntityId",
                principalTable: "GraphEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GraphRelationships_GraphEntities_TargetEntityId",
                table: "GraphRelationships",
                column: "TargetEntityId",
                principalTable: "GraphEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogActions",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogCommits_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogCommits",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogProcessingDialogs_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogProcessingDialogs",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnContexts",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
                table: "SessionLogTurns",
                column: "SessionLogId",
                principalTable: "SessionLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnStringLists_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnStringLists",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                table: "SessionLogTurnTags",
                column: "SessionLogTurnId",
                principalTable: "SessionLogTurns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                table: "ToolDefinitionTags",
                column: "ToolDefinitionId",
                principalTable: "ToolDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
