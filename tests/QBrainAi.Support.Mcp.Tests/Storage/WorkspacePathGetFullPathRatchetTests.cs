using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: architecture ratchet against host-OS resolution of
/// request-supplied workspace paths. Workspace-identity files must route paths through
/// <c>IWorkspacePathNormalizer</c> and contain no direct <c>Path.GetFullPath(</c>. Every other file
/// keeps its reviewed count of host-native file-system uses; a new call anywhere fails until a
/// reviewer decides whether it touches a request path and updates the allowlist.
/// </summary>
public sealed class WorkspacePathGetFullPathRatchetTests
{
    private const string Marker = "Path.GetFullPath(";

    /// <summary>
    /// Files that resolve request-supplied workspace identities. Zero direct host resolution.
    /// </summary>
    private static readonly string[] IdentityFiles =
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
    ];

    /// <summary>
    /// Reviewed host-native uses (configuration files, process launch, export roots, physical
    /// resolution, and file access after a host-native check). Counts may only go down.
    /// </summary>
    private static readonly Dictionary<string, int> HostNativeAllowlist = new(StringComparer.Ordinal)
    {
        ["src/QBrainAi.Client/BoundedFileSystemPolicy.cs"] = 3,
        ["src/QBrainAi.Client/LinuxPhysicalPathResolver.cs"] = 8,
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
        ["src/QBrainAi.Services/Services/AgentService.cs"] = 1,
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
        ["src/QBrainAi.Services/Services/RepoFileService.cs"] = 2,
        ["src/QBrainAi.Services/Services/TodoBootstrapImporter.cs"] = 6,
        ["src/QBrainAi.Services/Services/TodoYamlFileSerializer.cs"] = 1,
        ["src/QBrainAi.Services/Services/ToolRegistryService.cs"] = 1,
        ["src/QBrainAi.Services/Services/WikiDumpService.cs"] = 3,
        ["src/QBrainAi.Services/Services/WorkspacePolicyDirectiveParser.cs"] = 3,
        ["src/QBrainAi.Services/Services/WorkspaceProcessManager.cs"] = 1,
        ["src/QBrainAi.Services/Services/WorkspaceService.cs"] = 4,
        ["src/QBrainAi.Services/Services/WorkspaceServiceAccessor.cs"] = 1,
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

    /// <summary>TR-MCP-FED-PATH-001: identity files contain no direct host path resolution.</summary>
    [Fact]
    public void IdentityFiles_HaveNoDirectGetFullPath()
    {
        var counts = CountBySourceFile();
        var offenders = IdentityFiles
            .Where(file => counts.TryGetValue(file, out var count) && count > 0)
            .Select(file => $"{file}: {counts[file]}")
            .ToList();

        Assert.True(offenders.Count == 0, "Direct Path.GetFullPath in workspace-identity files:\n" + string.Join('\n', offenders));
    }

    /// <summary>
    /// TR-MCP-FED-PATH-001: no file exceeds its reviewed host-native count and no new file starts
    /// calling Path.GetFullPath without review.
    /// </summary>
    [Fact]
    public void AllOtherFiles_StayWithinReviewedAllowlist()
    {
        var counts = CountBySourceFile();
        var violations = counts
            .Where(pair => !IdentityFiles.Contains(pair.Key, StringComparer.Ordinal))
            .Where(pair => !HostNativeAllowlist.TryGetValue(pair.Key, out var allowed) || pair.Value > allowed)
            .Select(pair => $"{pair.Key}: {pair.Value} (allowed {(HostNativeAllowlist.TryGetValue(pair.Key, out var a) ? a : 0)})")
            .ToList();

        Assert.True(
            violations.Count == 0,
            "New or increased Path.GetFullPath use. If it resolves a request-supplied workspace path, route it through IWorkspacePathNormalizer; if it is host-native file access, review and update the allowlist:\n" +
            string.Join('\n', violations));
    }

    private static Dictionary<string, int> CountBySourceFile()
    {
        var root = QBrainAi.Support.Mcp.Tests.Infrastructure.RepositoryEvidenceTestSupport.ResolveRepositoryRoot();
        var src = Path.Combine(root, "src");
        var excluded = new Regex(@"[\\/](obj|bin|Migrations)[\\/]", RegexOptions.CultureInvariant);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
        {
            if (excluded.IsMatch(file))
                continue;

            var text = File.ReadAllText(file);
            var count = 0;
            for (var index = text.IndexOf(Marker, StringComparison.Ordinal);
                 index >= 0;
                 index = text.IndexOf(Marker, index + Marker.Length, StringComparison.Ordinal))
            {
                count++;
            }

            if (count > 0)
                result[Path.GetRelativePath(root, file).Replace('\\', '/')] = count;
        }

        return result;
    }
}
