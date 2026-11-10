using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: architecture ratchet against host-OS resolution of
/// request-supplied workspace paths. Workspace-identity files must route paths through
/// <c>IWorkspacePathNormalizer</c> and contain no direct <c>Path.GetFullPath(</c>. Every other file
/// keeps its reviewed count of host-native file-system uses; a new call anywhere fails until a
/// reviewer decides whether it touches a request path and updates the allowlist.
/// BDP v4 (plan section 2a): <c>WorkspacePathGetFullPathRatchetMockDataTests</c> proves the
/// evaluation against scripted sources; <c>WorkspacePathGetFullPathRatchetRealTests</c> scans
/// the repository.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; TR-MCP-FED-PATH-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the ratchet contract moved unchanged from Storage/WorkspacePathGetFullPathRatchetContractTests.cs into this file, which QBrainAi.Support.Mcp.Tests compiles and QBrainAi.Support.Mcp.EnvironmentIntegrationTests links, because its scripted fixture stays a unit fixture in QBrainAi.Support.Mcp.Tests while its repository-scanning fixture moved to the integration-owned project (slice S7, hostile validation finding R14-F4); one definition for both projects (DRY); the crefs to the fixtures are now text, since each fixture is in one project only.</remarks>
public abstract class WorkspacePathGetFullPathRatchetContractTests
{
    internal const string Marker = "Path.GetFullPath(";

    /// <summary>
    /// Files that resolve request-supplied workspace identities. Zero direct host resolution.
    /// </summary>
    internal static readonly string[] IdentityFiles =
    [
        "src/QBrainAi.Client/WorkspacePathNormalizer.cs",
        "src/QBrainAi.Services/Services/TriageService.cs",
        "src/QBrainAi.Services/Services/HandoffWorkspacePaths.cs",
        "src/QBrainAi.Services/Services/WorkspaceTokenService.cs",
        "src/QBrainAi.Services/Services/TodoServiceResolver.cs",
        "src/QBrainAi.Services/Services/TodoServiceFactory.cs",
        "src/QBrainAi.Services/Services/TodoExecutionService.cs",
        "src/QBrainAi.Services/Services/HostileReviewService.cs",
        "src/QBrainAi.Services/Services/AgentPoolService.cs",
        "src/QBrainAi.Services/Services/SessionLogWorkspaceAttributionValidator.cs",
        // Plan P1.4d: tool scope, agent workspaces and the accessor default route through the normalizer.
        "src/QBrainAi.Services/Services/ToolRegistryService.cs",
        "src/QBrainAi.Services/Services/AgentService.cs",
        "src/QBrainAi.Services/Services/WorkspaceServiceAccessor.cs",
    ];

    /// <summary>
    /// Reviewed host-native uses (configuration files, process launch, export roots, physical
    /// resolution, and file access after a host-native check). Counts may only go down.
    /// </summary>
    internal static readonly Dictionary<string, int> HostNativeAllowlist = new(StringComparer.Ordinal)
    {
        ["src/QBrainAi.Client/BoundedFileSystemPolicy.cs"] = 3,
        ["src/QBrainAi.Client/LinuxPhysicalPathResolver.cs"] = 9,
        ["src/QBrainAi.Client/WorkspaceIdentityPath.cs"] = 2,
        ["src/QBrainAi.Common.AgentCli/Processes/ProcessRunner.Windows.cs"] = 1,
        ["src/QBrainAi.GraphRag/Services/GraphRagService.cs"] = 5,
        ["src/QBrainAi.McpAgent.SampleHost/SampleHostPreviewFactory.cs"] = 5,
        ["src/QBrainAi.McpAgent/McpAgentOptionsValidator.cs"] = 1,
        ["src/QBrainAi.McpAgent/PowerShellSessions/HostedPowerShellSessionManager.cs"] = 1,
        ["src/QBrainAi.Repl.Core/SessionLogPersistence.cs"] = 2,
        ["src/QBrainAi.Repl.Core/TodoSelectionStore.cs"] = 2,
        ["src/QBrainAi.Repl.Core/TranscriptIngestionWorkflow.cs"] = 1,
        ["src/QBrainAi.Services/Ingestion/ExternalDocsIngestor.cs"] = 5,
        ["src/QBrainAi.Services/Ingestion/RepoIngestor.cs"] = 1,
        ["src/QBrainAi.Services/Ingestion/SessionLogFileWatcher.cs"] = 3,
        ["src/QBrainAi.Services/Ingestion/SessionLogIngestor.cs"] = 5,
        ["src/QBrainAi.Services/Ingestion/WebsiteIngestor.cs"] = 1,
        ["src/QBrainAi.Services/Native/DesktopProcessLauncher.cs"] = 1,
        ["src/QBrainAi.Services/Options/McpInstanceResolver.cs"] = 1,
        ["src/QBrainAi.Services/Requirements/RequirementsDatabaseDocumentService.cs"] = 2,
        ["src/QBrainAi.Services/Requirements/RequirementsDocFxWorkflowRunner.cs"] = 1,
        ["src/QBrainAi.Services/Requirements/RequirementsDocumentService.cs"] = 3,
        ["src/QBrainAi.Services/Requirements/RequirementsWikiExportConfig.cs"] = 5,
        ["src/QBrainAi.Services/Requirements/RequirementsWikiPathSecurity.cs"] = 8,
        ["src/QBrainAi.Services/Services/AgentHelp/AgentHelpPinnedPathResolver.cs"] = 5,
        ["src/QBrainAi.Services/Services/AgentProcessManager.cs"] = 1,
        ["src/QBrainAi.Services/Services/AuditedAgentCliClient.cs"] = 4,
        ["src/QBrainAi.Services/Services/CloneAgentIsolationStrategy.cs"] = 1,
        ["src/QBrainAi.Services/Services/EfTodoService.cs"] = 1,
        ["src/QBrainAi.Services/Services/FeatureAgentBranchStrategy.cs"] = 1,
        ["src/QBrainAi.Services/Services/FileGitHubWorkspaceTokenStore.cs"] = 2,
        ["src/QBrainAi.Services/Services/GitHubCliService.cs"] = 4,
        ["src/QBrainAi.Services/Services/HandoffSourceResolver.cs"] = 3,
        ["src/QBrainAi.Services/Services/LegacyTodoSqliteMigrator.cs"] = 1,
        ["src/QBrainAi.Services/Services/MarkerDiagnosticsEndpointHelper.cs"] = 2,
        ["src/QBrainAi.Services/Services/MarkerFileService.cs"] = 2,
        ["src/QBrainAi.Services/Services/NoneAgentIsolationStrategy.cs"] = 1,
        ["src/QBrainAi.Services/Services/RepoFileService.cs"] = 3,
        ["src/QBrainAi.Services/Services/TodoBootstrapImporter.cs"] = 6,
        ["src/QBrainAi.Services/Services/TodoYamlFileSerializer.cs"] = 1,
        ["src/QBrainAi.Services/Services/WikiDumpService.cs"] = 3,
        ["src/QBrainAi.Services/Services/WorkspacePolicyDirectiveParser.cs"] = 3,
        ["src/QBrainAi.Services/Services/WorkspaceProcessManager.cs"] = 1,
        ["src/QBrainAi.Services/Services/WorkspaceService.cs"] = 2,
        ["src/QBrainAi.Services/Services/WorktreeAgentIsolationStrategy.cs"] = 1,
        ["src/QBrainAi.SessionLog.Transcripts/OpenCodeSqliteUtilities.cs"] = 1,
        ["src/QBrainAi.SessionLog.Transcripts/TranscriptBundleDetector.cs"] = 1,
        ["src/QBrainAi.SessionLog.Transcripts/TranscriptIngestionService.cs"] = 3,
        ["src/QBrainAi.SessionLog.Transcripts/TranscriptPathSecurity.cs"] = 3,
        ["src/QBrainAi.SessionLog.Transcripts/TranscriptRunArtifactWriter.cs"] = 3,
        ["src/QBrainAi.SessionLog.Transcripts/TranscriptUtilities.cs"] = 1,
        ["src/QBrainAi.Storage/SqliteBoundedConnectionOpener.cs"] = 2,
        ["src/QBrainAi.Storage/WorkspaceContainedFileSystem.BoundedTraversal.cs"] = 6,
        ["src/QBrainAi.Storage/WorkspaceContainedFileSystem.cs"] = 5,
        ["src/QBrainAi.Storage/WorkspaceContainedFileSystem.ExportRoot.cs"] = 3,
        ["src/QBrainAi.Support.Mcp/Controllers/RequirementsController.cs"] = 2,
        ["src/QBrainAi.Support.Mcp/Controllers/SessionLogTranscriptIngestionController.cs"] = 2,
        ["src/QBrainAi.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionCommand.cs"] = 1,
        ["src/QBrainAi.Support.Mcp/DatabaseMaintenance/McpDatabaseEncryptionTransitionRunner.cs"] = 3,
        ["src/QBrainAi.Support.Mcp/McpStdio/McpStdioHost.cs"] = 1,
        ["src/QBrainAi.Support.Mcp/Program.cs"] = 9,
        ["src/QBrainAi.Support.Mcp/Services/AppSettingsFileService.cs"] = 2,
        ["src/QBrainAi.Support.Mcp/Services/DesktopLaunchService.cs"] = 1,
        ["src/QBrainAi.Support.Mcp/Services/GraphRagGlobalCorpusStartupSeeder.cs"] = 5,
        ["src/QBrainAi.Support.Mcp/Services/TransactionGatedRequirementsDocumentService.cs"] = 2,
        ["src/QBrainAi.TransactionSecurity/Services/TransactionSecurityStateStores.cs"] = 1,
    };

    /// <summary>Per-file <c>Path.GetFullPath(</c> counts for the sources under test.</summary>
    protected abstract IReadOnlyDictionary<string, int> Counts();

    /// <summary>TR-MCP-FED-PATH-001: identity files contain no direct host path resolution.</summary>
    [Fact]
    public void IdentityFiles_HaveNoDirectGetFullPath()
    {
        var offenders = IdentityOffenders(Counts());

        Assert.True(offenders.Count == 0, "Direct Path.GetFullPath in workspace-identity files:\n" + string.Join('\n', offenders));
    }

    /// <summary>
    /// TR-MCP-FED-PATH-001: no file exceeds its reviewed host-native count and no new file starts
    /// calling Path.GetFullPath without review.
    /// </summary>
    [Fact]
    public void AllOtherFiles_StayWithinReviewedAllowlist()
    {
        var violations = AllowlistViolations(Counts());

        Assert.True(
            violations.Count == 0,
            "New or increased Path.GetFullPath use. If it resolves a request-supplied workspace path, route it through IWorkspacePathNormalizer; if it is host-native file access, review and update the allowlist:\n" +
            string.Join('\n', violations));
    }

    /// <summary>Identity files with any direct call.</summary>
    internal static IReadOnlyList<string> IdentityOffenders(IReadOnlyDictionary<string, int> counts) =>
        IdentityFiles
            .Where(file => counts.TryGetValue(file, out var count) && count > 0)
            .Select(file => $"{file}: {counts[file]}")
            .ToList();

    /// <summary>Non-identity files above their allowlisted count, or not allowlisted at all.</summary>
    internal static IReadOnlyList<string> AllowlistViolations(IReadOnlyDictionary<string, int> counts) =>
        counts
            .Where(pair => !IdentityFiles.Contains(pair.Key, StringComparer.Ordinal))
            .Where(pair => !HostNativeAllowlist.TryGetValue(pair.Key, out var allowed) || pair.Value > allowed)
            .Select(pair => $"{pair.Key}: {pair.Value} (allowed {(HostNativeAllowlist.TryGetValue(pair.Key, out var a) ? a : 0)})")
            .ToList();

    /// <summary>Counts <see cref="Marker"/> occurrences per source (relative path to text).</summary>
    internal static Dictionary<string, int> Count(IEnumerable<KeyValuePair<string, string>> sources)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (path, text) in sources)
        {
            var count = 0;
            for (var index = text.IndexOf(Marker, StringComparison.Ordinal);
                 index >= 0;
                 index = text.IndexOf(Marker, index + Marker.Length, StringComparison.Ordinal))
            {
                count++;
            }

            if (count > 0)
                result[path] = count;
        }

        return result;
    }
}
