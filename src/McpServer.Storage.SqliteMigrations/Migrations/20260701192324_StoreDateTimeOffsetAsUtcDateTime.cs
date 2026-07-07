using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class StoreDateTimeOffsetAsUtcDateTime : Migration
    {
        // Every column whose CLR type changed from DateTimeOffset to DateTime(UTC). SQLite stores
        // both as TEXT (no column type change), but pre-existing rows carry the legacy
        // "yyyy-MM-dd HH:mm:ss.fffffff+HH:MM" offset text that the DateTime reader cannot use.
        private static readonly (string Table, string Column)[] DateTimeOffsetColumns =
        {
            ("Workspaces", "DeletedAtUtc"),
            ("Workspaces", "DateTimeModified"),
            ("Workspaces", "DateTimeCreated"),
            ("TriageResearchRuns", "StartedUtc"),
            ("TriageResearchRuns", "DeletedAtUtc"),
            ("TriageResearchRuns", "CompletedUtc"),
            ("TriageReports", "DeletedAtUtc"),
            ("TriageReports", "CreatedUtc"),
            ("TriageGroups", "QuietDeadlineUtc"),
            ("TriageGroups", "LastReportAtUtc"),
            ("TriageGroups", "FirstReportAtUtc"),
            ("TriageGroups", "DeletedAtUtc"),
            ("ToolDefinitionTags", "DeletedAtUtc"),
            ("ToolDefinitions", "DeletedAtUtc"),
            ("ToolDefinitions", "DateTimeModified"),
            ("ToolDefinitions", "DateTimeCreated"),
            ("ToolBuckets", "DeletedAtUtc"),
            ("ToolBuckets", "DateTimeLastSynced"),
            ("ToolBuckets", "DateTimeCreated"),
            ("TodoRequirementLinks", "DeletedAtUtc"),
            ("TodoRequirementLinks", "CreatedAtUtc"),
            ("TodoItems", "DeletedAtUtc"),
            ("TodoDocumentMetadata", "DeletedAtUtc"),
            ("TodoAuditHistory", "DeletedAtUtc"),
            ("SessionLogTurnTags", "DeletedAtUtc"),
            ("SessionLogTurnStringLists", "DeletedAtUtc"),
            ("SessionLogTurns", "Timestamp"),
            ("SessionLogTurns", "DeletedAtUtc"),
            ("SessionLogTurnContexts", "DeletedAtUtc"),
            ("SessionLogs", "Started"),
            ("SessionLogs", "LastUpdated"),
            ("SessionLogs", "DeletedAtUtc"),
            ("SessionLogProcessingDialogs", "Timestamp"),
            ("SessionLogProcessingDialogs", "DeletedAtUtc"),
            ("SessionLogCommits", "DeletedAtUtc"),
            ("SessionLogCommits", "CommitTimestamp"),
            ("SessionLogActions", "DeletedAtUtc"),
            ("RequirementTraceabilityLinks", "DeletedAtUtc"),
            ("RequirementScopeLayers", "UpdatedAtUtc"),
            ("RequirementScopeLayers", "DeletedAtUtc"),
            ("RequirementScopeLayers", "CreatedAtUtc"),
            ("Requirements", "DeletedAtUtc"),
            ("Memories", "UpdatedAtUtc"),
            ("Memories", "DeletedAtUtc"),
            ("Memories", "CreatedAtUtc"),
            ("GraphRelationships", "DeletedAtUtc"),
            ("GraphEntities", "DeletedAtUtc"),
            ("FederationWorkspaces", "LastSeenUtc"),
            ("FederationWorkspaces", "DeletedAtUtc"),
            ("FederationWorkspaces", "CreatedAtUtc"),
            ("FederationProxies", "UpdatedAtUtc"),
            ("FederationProxies", "LastHeartbeatUtc"),
            ("FederationProxies", "DeletedAtUtc"),
            ("FederationProxies", "CreatedAtUtc"),
            ("FederationOutbox", "DeletedAtUtc"),
            ("FederationOutbox", "CreatedAtUtc"),
            ("FederationOutbox", "AcknowledgedAtUtc"),
            ("FederationOperations", "UpdatedAtUtc"),
            ("FederationOperations", "DeletedAtUtc"),
            ("FederationOperations", "CreatedAtUtc"),
            ("FederationOperations", "AcknowledgedAtUtc"),
            ("FederationConflicts", "ResolvedAtUtc"),
            ("FederationConflicts", "DeletedAtUtc"),
            ("FederationConflicts", "CreatedAtUtc"),
            ("Documents", "DeletedAtUtc"),
            ("DataAuditLogs", "OccurredAtUtc"),
            ("Chunks", "DeletedAtUtc"),
            ("BrainSlotInvocations", "StartedAtUtc"),
            ("BrainSlotInvocations", "DeletedAtUtc"),
            ("BrainSlotInvocations", "CompletedAtUtc"),
            ("BrainSlotDefinitions", "WeightUpdatedAtUtc"),
            ("BrainSlotDefinitions", "UpdatedAtUtc"),
            ("BrainSlotDefinitions", "DeletedAtUtc"),
            ("BrainSlotDefinitions", "CreatedAtUtc"),
            ("AgentWorkspaces", "DeletedAtUtc"),
            ("AgentEventLogs", "DeletedAtUtc"),
            ("AgentDefinitions", "DeletedAtUtc"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TR-MCP-DB-DTO-001 data migration: DateTimeOffset columns now persist as offset-less
            // UTC DateTime so SQLite can translate timestamp predicates/ordering to SQL. Existing
            // rows are normalized in place: strftime() shifts any legacy offset text to the UTC
            // instant and drops the offset (millisecond precision retained). Only rows still
            // carrying an offset suffix are touched.
            foreach (var (table, column) in DateTimeOffsetColumns)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"{column}\" = strftime('%Y-%m-%d %H:%M:%f', \"{column}\") " +
                    $"WHERE \"{column}\" IS NOT NULL AND \"{column}\" GLOB '*[+-][0-9][0-9]:[0-9][0-9]';");
            }

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "DateTimeCreated", "DateTimeModified", "DeletedAtUtc" },
                values: new object[] { new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-append the UTC offset so the legacy DateTimeOffset reader can parse the text.
            foreach (var (table, column) in DateTimeOffsetColumns)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"{table}\" SET \"{column}\" = \"{column}\" || '+00:00' " +
                    $"WHERE \"{column}\" IS NOT NULL AND NOT (\"{column}\" GLOB '*[+-][0-9][0-9]:[0-9][0-9]');");
            }

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "DateTimeCreated", "DateTimeModified", "DeletedAtUtc" },
                values: new object[] { new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }
    }
}
