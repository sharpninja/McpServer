using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001 / TR-MCP-FED-PATH-001: architecture ratchet against host-OS resolution of
/// request-supplied workspace paths. Workspace-identity files must route paths through
/// <c>IWorkspacePathNormalizer</c> and contain no direct <c>Path.GetFullPath(</c>. Every other file
/// keeps its reviewed count of host-native file-system uses; a new call anywhere fails until a
/// reviewer decides whether it touches a request path and updates the allowlist.
/// BDP v4 (plan section 2a): <see cref="WorkspacePathGetFullPathRatchetMockDataTests"/> proves the
/// evaluation against scripted sources; <see cref="WorkspacePathGetFullPathRatchetRealTests"/> scans
/// the repository.
/// </summary>
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
    ];

    /// <summary>
    /// Reviewed host-native uses (configuration files, process launch, export roots, physical
    /// resolution, and file access after a host-native check). Counts may only go down.
    /// </summary>
    internal static readonly Dictionary<string, int> HostNativeAllowlist = new(StringComparer.Ordinal)
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

/// <summary>BDP v4 step 3: the ratchet over the repository <c>src/</c> tree.</summary>
public sealed class WorkspacePathGetFullPathRatchetRealTests : WorkspacePathGetFullPathRatchetContractTests
{
    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, int> Counts()
    {
        var root = QBrainAi.Support.Mcp.Tests.Infrastructure.RepositoryEvidenceTestSupport.ResolveRepositoryRoot();
        var excluded = new Regex(@"[\\/](obj|bin|Migrations)[\\/]", RegexOptions.CultureInvariant);
        return Count(Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !excluded.IsMatch(file))
            .Select(file => KeyValuePair.Create(Path.GetRelativePath(root, file).Replace('\\', '/'), File.ReadAllText(file))));
    }
}

/// <summary>
/// BDP v4 step 2: the ratchet evaluation proven against scripted in-memory sources, including each
/// failure path the Real fixture can report.
/// </summary>
public sealed class WorkspacePathGetFullPathRatchetMockDataTests : WorkspacePathGetFullPathRatchetContractTests
{
    private const string Call = "var x = Path.GetFullPath(p);\n";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, int> Counts() => Count(CompliantSources());

    /// <summary>An identity file with a direct call is reported.</summary>
    [Fact]
    public void Evaluator_IdentityFileWithCall_IsReported()
    {
        var sources = CompliantSources().Append(KeyValuePair.Create(IdentityFiles[0], Call)).ToList();

        Assert.Equal([$"{IdentityFiles[0]}: 1"], IdentityOffenders(Count(sources)));
    }

    /// <summary>An allowlisted file above its reviewed count is reported.</summary>
    [Fact]
    public void Evaluator_AllowlistedFileAboveCount_IsReported()
    {
        var (file, allowed) = HostNativeAllowlist.First();
        var sources = CompliantSources()
            .Where(pair => pair.Key != file)
            .Append(KeyValuePair.Create(file, string.Concat(Enumerable.Repeat(Call, allowed + 1))))
            .ToList();

        Assert.Equal([$"{file}: {allowed + 1} (allowed {allowed})"], AllowlistViolations(Count(sources)));
    }

    /// <summary>A file not on the allowlist that starts calling Path.GetFullPath is reported.</summary>
    [Fact]
    public void Evaluator_NewUnlistedFile_IsReported()
    {
        var sources = CompliantSources().Append(KeyValuePair.Create("src/New/Unreviewed.cs", Call)).ToList();

        Assert.Equal(["src/New/Unreviewed.cs: 1 (allowed 0)"], AllowlistViolations(Count(sources)));
    }

    /// <summary>A count that goes down stays within the ratchet.</summary>
    [Fact]
    public void Evaluator_AllowlistedFileBelowCount_IsAccepted()
    {
        var (file, _) = HostNativeAllowlist.First(pair => pair.Value > 1);
        var sources = CompliantSources()
            .Where(pair => pair.Key != file)
            .Append(KeyValuePair.Create(file, Call))
            .ToList();

        Assert.Empty(AllowlistViolations(Count(sources)));
    }

    /// <summary>
    /// Scripted compliant tree: identity files without calls, a sample of allowlisted files at
    /// exactly their reviewed counts, and an unrelated file without calls.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> CompliantSources() =>
        IdentityFiles.Select(file => KeyValuePair.Create(file, "// routes paths through IWorkspacePathNormalizer\n"))
            .Concat(HostNativeAllowlist.Take(5).Select(pair => KeyValuePair.Create(pair.Key, string.Concat(Enumerable.Repeat(Call, pair.Value)))))
            .Append(KeyValuePair.Create("src/Other/NoPaths.cs", "class NoPaths { }\n"));
}