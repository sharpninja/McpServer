using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolDefinitionTags",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolDefinitionTags",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolDefinitionTags",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolDefinitionTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitions",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolDefinitions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolDefinitions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolDefinitions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolBuckets",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "ToolBuckets",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "ToolBuckets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ToolBuckets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ToolBuckets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoItems",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoDocumentMetadata",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoDocumentMetadata",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoDocumentMetadata",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoDocumentMetadata",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "TodoAuditHistory",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "TodoAuditHistory",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "TodoAuditHistory",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TodoAuditHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnTags",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnTags",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnTags",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnTags",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnStringLists",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnStringLists",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnStringLists",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnStringLists",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnStringLists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurns",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurns",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurns",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnContexts",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogTurnContexts",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogTurnContexts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogTurnContexts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogTurnContexts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogs",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogs",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogProcessingDialogs",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogProcessingDialogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogProcessingDialogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogProcessingDialogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogCommits",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogCommits",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogCommits",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogCommits",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogCommits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogActions",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "SessionLogActions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "SessionLogActions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "SessionLogActions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SessionLogActions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "RequirementTraceabilityLinks",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "RequirementTraceabilityLinks",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RequirementTraceabilityLinks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RequirementTraceabilityLinks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                table: "RequirementTraceabilityLinks",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Requirements",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Requirements",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Requirements",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Requirements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphRelationships",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "GraphRelationships",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "GraphRelationships",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GraphRelationships",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GraphRelationships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphEntities",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "GraphEntities",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "GraphEntities",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "GraphEntities",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GraphEntities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalWorkspaceId",
                table: "FederationWorkspaces",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationWorkspaces",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationWorkspaces",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationWorkspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationWorkspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationProxies",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationProxies",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationProxies",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationProxies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationOutbox",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationOutbox",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationOutbox",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationOutbox",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationOperations",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationOperations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationOperations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "FederationConflicts",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "FederationConflicts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FederationConflicts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FederationConflicts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Documents",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Documents",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Documents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Documents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Documents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Chunks",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "Chunks",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "Chunks",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Chunks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Chunks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentWorkspaces",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentWorkspaces",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentWorkspaces",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentWorkspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentWorkspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentEventLogs",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentEventLogs",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentEventLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentEventLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentEventLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentDefinitions",
                type: "nvarchar(1024)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "AgentDefinitions",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "AgentDefinitions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "AgentDefinitions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AgentDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    WorkspacePath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TodoPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DataDirectory = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    TunnelProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RunAs = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedLicensesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedCountriesOfOriginJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedOrganizationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BannedIndividualsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentPath = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateTimeModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.WorkspaceId);
                });

            migrationBuilder.CreateTable(
                name: "DataAuditLogs",
                columns: table => new
                {
                    AuditId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    EntityKind = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FederationOperationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiffJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequirementKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoRequirementLinks", x => new { x.WorkspaceId, x.TodoId, x.RequirementKind, x.RequirementId });
                    table.ForeignKey(
                        name: "FK_TodoRequirementLinks_Requirements_WorkspaceId_RequirementKind_RequirementId",
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
                INSERT INTO [Workspaces] ([WorkspaceId], [WorkspacePath], [Name], [TodoPath], [IsEnabled], [IsPrimary], [DateTimeCreated], [DateTimeModified], [IsDeleted])
                SELECT DISTINCT ws.[WorkspaceId], ws.[WorkspaceId], CASE WHEN ws.[WorkspaceId] = N'' THEN N'global' ELSE LEFT(ws.[WorkspaceId], 512) END, N'docs/todo.yaml', CAST(1 AS bit), CAST(0 AS bit), CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CAST(0 AS bit)
                FROM (
                    SELECT [WorkspaceId] FROM [AgentDefinitions] UNION SELECT [WorkspaceId] FROM [AgentEventLogs] UNION SELECT [WorkspaceId] FROM [AgentWorkspaces] UNION SELECT [WorkspaceId] FROM [Chunks] UNION SELECT [WorkspaceId] FROM [Documents] UNION SELECT [WorkspaceId] FROM [GraphEntities] UNION SELECT [WorkspaceId] FROM [GraphRelationships] UNION SELECT [WorkspaceId] FROM [Requirements] UNION SELECT [WorkspaceId] FROM [RequirementTraceabilityLinks] UNION SELECT [WorkspaceId] FROM [SessionLogActions] UNION SELECT [WorkspaceId] FROM [SessionLogCommits] UNION SELECT [WorkspaceId] FROM [SessionLogProcessingDialogs] UNION SELECT [WorkspaceId] FROM [SessionLogs] UNION SELECT [WorkspaceId] FROM [SessionLogTurnContexts] UNION SELECT [WorkspaceId] FROM [SessionLogTurns] UNION SELECT [WorkspaceId] FROM [SessionLogTurnStringLists] UNION SELECT [WorkspaceId] FROM [SessionLogTurnTags] UNION SELECT [WorkspaceId] FROM [TodoAuditHistory] UNION SELECT [WorkspaceId] FROM [TodoDocumentMetadata] UNION SELECT [WorkspaceId] FROM [TodoItems] UNION SELECT [WorkspaceId] FROM [ToolBuckets] UNION SELECT [WorkspaceId] FROM [ToolDefinitions] UNION SELECT [WorkspaceId] FROM [ToolDefinitionTags]
                ) ws
                WHERE ws.[WorkspaceId] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [Workspaces] w WHERE w.[WorkspaceId] = ws.[WorkspaceId]);

                UPDATE [FederationWorkspaces]
                SET [CanonicalWorkspaceId] = COALESCE(NULLIF([CanonicalWorkspaceId], N''), [GlobalWorkspaceId]);

                INSERT INTO [Workspaces] ([WorkspaceId], [WorkspacePath], [Name], [TodoPath], [IsEnabled], [IsPrimary], [DateTimeCreated], [DateTimeModified], [IsDeleted])
                SELECT DISTINCT fw.[CanonicalWorkspaceId], fw.[CanonicalWorkspaceId], COALESCE(NULLIF(fw.[WorkspaceName], N''), LEFT(fw.[CanonicalWorkspaceId], 512)), N'docs/todo.yaml', fw.[IsEnabled], CAST(0 AS bit), CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CAST(0 AS bit)
                FROM [FederationWorkspaces] fw
                WHERE fw.[CanonicalWorkspaceId] IS NOT NULL
                  AND fw.[CanonicalWorkspaceId] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [Workspaces] w WHERE w.[WorkspaceId] = fw.[CanonicalWorkspaceId]);

                INSERT INTO [TodoRecords] ([WorkspaceId], [TodoId], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                SELECT ti.[WorkspaceId], ti.[Id], CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CAST(0 AS bit)
                FROM [TodoItems] ti
                WHERE NOT EXISTS (SELECT 1 FROM [TodoRecords] tr WHERE tr.[WorkspaceId] = ti.[WorkspaceId] AND tr.[TodoId] = ti.[Id]);

                INSERT INTO [Requirements] ([WorkspaceId], [Kind], [Id], [Title], [Body], [Priority], [Status], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                SELECT DISTINCT ti.[WorkspaceId], N'fr', fr.[value], fr.[value], N'Placeholder requirement backfilled by DB-FK-001.', N'medium', N'pending', N'1970-01-01T00:00:00Z', N'1970-01-01T00:00:00Z', CAST(0 AS bit)
                FROM [TodoItems] ti
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(ti.[FunctionalRequirementsJson]) = 1 THEN ti.[FunctionalRequirementsJson] ELSE N'[]' END) fr
                WHERE fr.[value] IS NOT NULL AND fr.[value] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [Requirements] r WHERE r.[WorkspaceId] = ti.[WorkspaceId] AND r.[Kind] = N'fr' AND r.[Id] = fr.[value]);

                INSERT INTO [Requirements] ([WorkspaceId], [Kind], [Id], [Title], [Body], [Priority], [Status], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                SELECT DISTINCT ti.[WorkspaceId], N'tr', tr.[value], tr.[value], N'Placeholder requirement backfilled by DB-FK-001.', N'medium', N'pending', N'1970-01-01T00:00:00Z', N'1970-01-01T00:00:00Z', CAST(0 AS bit)
                FROM [TodoItems] ti
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(ti.[TechnicalRequirementsJson]) = 1 THEN ti.[TechnicalRequirementsJson] ELSE N'[]' END) tr
                WHERE tr.[value] IS NOT NULL AND tr.[value] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [Requirements] r WHERE r.[WorkspaceId] = ti.[WorkspaceId] AND r.[Kind] = N'tr' AND r.[Id] = tr.[value]);

                INSERT INTO [Requirements] ([WorkspaceId], [Kind], [Id], [Title], [Body], [Priority], [Status], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                SELECT DISTINCT rtl.[WorkspaceId], N'fr', rtl.[FrId], rtl.[FrId], N'Placeholder requirement backfilled by DB-FK-001.', N'medium', N'pending', N'1970-01-01T00:00:00Z', N'1970-01-01T00:00:00Z', CAST(0 AS bit)
                FROM [RequirementTraceabilityLinks] rtl
                WHERE rtl.[FrId] IS NOT NULL AND rtl.[FrId] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [Requirements] r WHERE r.[WorkspaceId] = rtl.[WorkspaceId] AND r.[Kind] = N'fr' AND r.[Id] = rtl.[FrId]);

                INSERT INTO [Requirements] ([WorkspaceId], [Kind], [Id], [Title], [Body], [Priority], [Status], [CreatedAtUtc], [UpdatedAtUtc], [IsDeleted])
                SELECT DISTINCT rtl.[WorkspaceId], rtl.[TargetKind], rtl.[TargetId], rtl.[TargetId], N'Placeholder requirement backfilled by DB-FK-001.', N'medium', N'pending', N'1970-01-01T00:00:00Z', N'1970-01-01T00:00:00Z', CAST(0 AS bit)
                FROM [RequirementTraceabilityLinks] rtl
                WHERE rtl.[TargetKind] IS NOT NULL AND rtl.[TargetKind] <> N'' AND rtl.[TargetId] IS NOT NULL AND rtl.[TargetId] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [Requirements] r WHERE r.[WorkspaceId] = rtl.[WorkspaceId] AND r.[Kind] = rtl.[TargetKind] AND r.[Id] = rtl.[TargetId]);

                INSERT INTO [TodoRequirementLinks] ([WorkspaceId], [TodoId], [RequirementKind], [RequirementId], [CreatedAtUtc], [IsDeleted])
                SELECT ti.[WorkspaceId], ti.[Id], N'fr', fr.[value], CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CAST(0 AS bit)
                FROM [TodoItems] ti
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(ti.[FunctionalRequirementsJson]) = 1 THEN ti.[FunctionalRequirementsJson] ELSE N'[]' END) fr
                WHERE fr.[value] IS NOT NULL AND fr.[value] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [TodoRequirementLinks] l WHERE l.[WorkspaceId] = ti.[WorkspaceId] AND l.[TodoId] = ti.[Id] AND l.[RequirementKind] = N'fr' AND l.[RequirementId] = fr.[value]);

                INSERT INTO [TodoRequirementLinks] ([WorkspaceId], [TodoId], [RequirementKind], [RequirementId], [CreatedAtUtc], [IsDeleted])
                SELECT ti.[WorkspaceId], ti.[Id], N'tr', tr.[value], CONVERT(datetimeoffset, '1970-01-01T00:00:00+00:00'), CAST(0 AS bit)
                FROM [TodoItems] ti
                CROSS APPLY OPENJSON(CASE WHEN ISJSON(ti.[TechnicalRequirementsJson]) = 1 THEN ti.[TechnicalRequirementsJson] ELSE N'[]' END) tr
                WHERE tr.[value] IS NOT NULL AND tr.[value] <> N''
                  AND NOT EXISTS (SELECT 1 FROM [TodoRequirementLinks] l WHERE l.[WorkspaceId] = ti.[WorkspaceId] AND l.[TodoId] = ti.[Id] AND l.[RequirementKind] = N'tr' AND l.[RequirementId] = tr.[value]);
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
                name: "IX_TodoRequirementLinks_WorkspaceId_RequirementKind_RequirementId",
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
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_SourceKind_FrId",
                table: "RequirementTraceabilityLinks",
                columns: new[] { "WorkspaceId", "SourceKind", "FrId" },
                principalTable: "Requirements",
                principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_TargetKind_TargetId",
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
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_SourceKind_FrId",
                table: "RequirementTraceabilityLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_RequirementTraceabilityLinks_Requirements_WorkspaceId_TargetKind_TargetId",
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
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "ToolBuckets",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnTags",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnStringLists",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurns",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogTurnContexts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogCommits",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogActions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphRelationships",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "GraphEntities",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Documents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "Chunks",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentWorkspaces",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentEventLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

            migrationBuilder.AlterColumn<string>(
                name: "WorkspaceId",
                table: "AgentDefinitions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)");

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
