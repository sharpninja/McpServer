using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RepairTriageCreatedTodoWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                WITH candidates AS (
                    SELECT
                        [CreatedTodoId] AS [TodoId],
                        [WorkspaceId] AS [TargetWorkspaceId],
                        [Title],
                        [LastReportAtUtc] AS [SortUtc],
                        0 AS [SourcePriority]
                    FROM [TriageGroups]
                    WHERE [CreatedTodoId] IS NOT NULL
                      AND [IsDeleted] = CAST(0 AS bit)
                    UNION ALL
                    SELECT
                        run.[CreatedTodoId] AS [TodoId],
                        run.[WorkspaceId] AS [TargetWorkspaceId],
                        COALESCE([group].[Title], 'Recovered TODO ' + run.[CreatedTodoId]) AS [Title],
                        COALESCE(run.[CompletedUtc], run.[StartedUtc]) AS [SortUtc],
                        1 AS [SourcePriority]
                    FROM [TriageResearchRuns] AS run
                    LEFT JOIN [TriageGroups] AS [group]
                        ON [group].[WorkspaceId] = run.[WorkspaceId]
                       AND [group].[GroupId] = run.[GroupId]
                    WHERE run.[CreatedTodoId] IS NOT NULL
                      AND run.[IsDeleted] = CAST(0 AS bit)
                ),
                targets AS (
                    SELECT *,
                        ROW_NUMBER() OVER (
                            PARTITION BY [TodoId]
                            ORDER BY [SourcePriority], [SortUtc] DESC
                        ) AS [TargetRank]
                    FROM candidates
                ),
                source AS (
                    SELECT
                        target.[TargetWorkspaceId],
                        target.[TodoId],
                        target.[Title] AS [TargetTitle],
                        item.[Title],
                        item.[Section],
                        item.[Priority],
                        item.[Done],
                        item.[Estimate],
                        item.[Note],
                        item.[DescriptionJson],
                        item.[TechnicalDetailsJson],
                        item.[ImplementationTasksJson],
                        item.[CompletedDate],
                        item.[DoneSummary],
                        item.[Remaining],
                        item.[PriorityNote],
                        item.[Reference],
                        item.[DependsOnJson],
                        item.[FunctionalRequirementsJson],
                        item.[TechnicalRequirementsJson],
                        item.[ItemKind],
                        item.[SectionOrder],
                        item.[ItemOrder],
                        item.[PhaseLabel],
                        ROW_NUMBER() OVER (
                            PARTITION BY target.[TodoId]
                            ORDER BY CASE WHEN item.[WorkspaceId] = target.[TargetWorkspaceId] THEN 0 ELSE 1 END
                        ) AS [SourceRank]
                    FROM targets AS target
                    LEFT JOIN [TodoItems] AS item
                        ON item.[Id] = target.[TodoId]
                    WHERE target.[TargetRank] = 1
                )
                INSERT INTO [TodoItems] (
                    [WorkspaceId],
                    [Id],
                    [Title],
                    [Section],
                    [Priority],
                    [Done],
                    [Estimate],
                    [Note],
                    [DescriptionJson],
                    [TechnicalDetailsJson],
                    [ImplementationTasksJson],
                    [CompletedDate],
                    [DoneSummary],
                    [Remaining],
                    [PriorityNote],
                    [Reference],
                    [DependsOnJson],
                    [FunctionalRequirementsJson],
                    [TechnicalRequirementsJson],
                    [ItemKind],
                    [SectionOrder],
                    [ItemOrder],
                    [PhaseLabel]
                )
                SELECT
                    source.[TargetWorkspaceId],
                    source.[TodoId],
                    COALESCE(source.[Title], source.[TargetTitle]),
                    COALESCE(source.[Section], 'Backlog'),
                    COALESCE(source.[Priority], 'medium'),
                    COALESCE(source.[Done], CAST(0 AS bit)),
                    source.[Estimate],
                    source.[Note],
                    source.[DescriptionJson],
                    source.[TechnicalDetailsJson],
                    source.[ImplementationTasksJson],
                    source.[CompletedDate],
                    source.[DoneSummary],
                    source.[Remaining],
                    source.[PriorityNote],
                    source.[Reference],
                    source.[DependsOnJson],
                    source.[FunctionalRequirementsJson],
                    source.[TechnicalRequirementsJson],
                    COALESCE(source.[ItemKind], 'standard'),
                    COALESCE(source.[SectionOrder], 0),
                    COALESCE(source.[ItemOrder], 0),
                    source.[PhaseLabel]
                FROM source
                WHERE source.[SourceRank] = 1
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [TodoItems] AS existing
                      WHERE existing.[WorkspaceId] = source.[TargetWorkspaceId]
                        AND existing.[Id] = source.[TodoId]
                  );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
