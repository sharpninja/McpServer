using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-WIKIEXPORT-003: One policy per current McpDbContext DbSet.</summary>
public static class WikiDumpTablePolicyRegistry
{
    /// <summary>Returns every DbSet name on <see cref="McpDbContext"/> exactly once.</summary>
    public static IReadOnlyList<string> GetDbSetNames()
        => typeof(McpDbContext).GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Policy for a DbSet name. Unknown names throw so the registry cannot silently skip.</summary>
    public static string PolicyFor(string dbSetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbSetName);
        if (!GetDbSetNames().Contains(dbSetName, StringComparer.Ordinal))
            throw new ArgumentException($"No dump policy registered for DbSet '{dbSetName}'.", nameof(dbSetName));

        return dbSetName switch
        {
            "Workspaces" => "ROOT",
            "DataAuditLogs" => "COPY-SANITIZE+HISTORY",
            "SessionLogs" or "SessionLogTurns" or "SessionLogActions" or "SessionLogTurnTags" or "SessionLogTags"
                or "SessionLogTurnContexts" or "SessionLogProcessingDialogs" or "SessionLogCommits"
                or "SessionLogCommitFiles" or "SessionLogTurnStringLists" => "COPY-SANITIZE+HISTORY",
            "FederationProxies" => "SHARED-CLOSURE+INERT+OMIT-SECRET",
            "Products" => "SHARED-CLOSURE",
            "ProductWorkspaceMemberships" => "SHARED-CLOSURE+COPY",
            "HostileReviewRequests" or "HostileReviewArtifactLinks" or "HostileReviewExecutions"
                or "HostileReviewFindings" or "HostileReviewDiagnostics" => "COPY-SANITIZE+HISTORY+INERT",
            "HandoffIngestionRuns" => "COPY-SANITIZE+HISTORY+INERT",
            "TriageReports" or "TriageReportListItems" or "TriageGroups" or "TriageResearchRuns" => "COPY-SANITIZE+HISTORY+INERT",
            "BrainSlotDefinitions" => "COPY-SANITIZE+INERT+OMIT-SECRET",
            "BrainSlotInvocations" => "COPY-SANITIZE+HISTORY+INERT",
            "AgentWorkspaces" or "AgentWorkspaceListItems" => "COPY-SANITIZE+INERT",
            _ => "COPY-SANITIZE",
        };
    }
}
