using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>TR-MCP-DOCFXWIKI-001: Executes configured DocFX workflows and projects staged output into wiki export documents.</summary>
internal interface IRequirementsDocFxWorkflowRunner
{
    /// <summary>Runs configured DocFX workflows and returns projected wiki documents.</summary>
    /// <param name="config">Validated wiki export configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Projected DocFX wiki documents.</returns>
    Task<IReadOnlyList<RequirementsRenderedDocument>> RunAsync(RequirementsWikiExportConfig config, CancellationToken ct = default);
}

/// <summary>TR-MCP-DOCFXWIKI-001: Default DocFX workflow runner implementation.</summary>
internal sealed class RequirementsDocFxWorkflowRunner : IRequirementsDocFxWorkflowRunner
{
    private const int MaxDiagnosticCharacters = 2048;

    private static readonly IReadOnlyDictionary<string, string> s_contentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".md"] = "text/markdown",
        [".markdown"] = "text/markdown",
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "text/javascript",
        [".json"] = "application/json",
        [".map"] = "application/json",
        [".txt"] = "text/plain",
        [".xml"] = "application/xml",
        [".yaml"] = "application/yaml",
        [".yml"] = "application/yaml",
        [".svg"] = "image/svg+xml"
    };

    private readonly IProcessRunner _processRunner;
    private readonly ILogger<RequirementsDocFxWorkflowRunner> _logger;

    public RequirementsDocFxWorkflowRunner(IProcessRunner processRunner, ILogger<RequirementsDocFxWorkflowRunner> logger)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<RequirementsRenderedDocument>> RunAsync(RequirementsWikiExportConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.DocFxWorkflows.Count == 0)
            return [];

        var documents = new List<RequirementsRenderedDocument>();
        var seenPublicationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var workflow in config.DocFxWorkflows)
            await RunWorkflowAsync(config.WorkspaceRoot, workflow, documents, seenPublicationPaths, ct).ConfigureAwait(false);

        return documents
            .OrderBy(static document => document.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    private async Task RunWorkflowAsync(
        string workspaceRoot,
        RequirementsDocFxWorkflow workflow,
        ICollection<RequirementsRenderedDocument> documents,
        ISet<string> seenPublicationPaths,
        CancellationToken ct)
    {
        _logger.LogDebug("Running DocFX workflow {WorkflowId} into {OutputRoot}.", workflow.Id, workflow.OutputRootPath);
        DeleteDirectoryTreeWithoutFollowingLinks(workflow.OutputRootPath);

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(workflow.TimeoutSeconds));
            var request = new ProcessRunRequest(
                workflow.Executable,
                string.Empty,
                WorkingDirectory: workflow.WorkingDirectoryPath,
                ArgumentList: workflow.Arguments);

            var result = await _processRunner.RunAsync(request, timeout.Token).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw BuildProcessFailure(workflow, result);

            if (!Directory.Exists(workflow.OutputRootPath))
                throw new InvalidOperationException($"DocFX workflow '{workflow.Id}' completed but output root '{workflow.OutputRoot}' does not exist.");

            RequirementsWikiPathSecurity.ThrowIfPathEscapesRoot(workspaceRoot, workflow.OutputRootPath, $"DocFX workflow '{workflow.Id}' output root");
            var stagedFiles = EnumerateStagedFiles(workspaceRoot, workflow.OutputRootPath, workflow.Id, ct);
            foreach (var filePath in stagedFiles)
            {
                ct.ThrowIfCancellationRequested();
                var artifactRelativePath = NormalizeRelativePath(Path.GetRelativePath(workflow.OutputRootPath, filePath));
                if (IsIgnoredDocFxTemplateArtifact(artifactRelativePath))
                    continue;

                var contentType = ResolveContentType(workflow.Id, artifactRelativePath);
                var content = await File.ReadAllTextAsync(filePath, RequirementsWikiDocumentRenderer.Utf8NoBom, ct).ConfigureAwait(false);

                foreach (var platform in workflow.Platforms.Order(StringComparer.OrdinalIgnoreCase))
                {
                    var publicationPath = NormalizeRelativePath(Path.Combine(platform, workflow.TargetRoot, artifactRelativePath));
                    if (!seenPublicationPaths.Add(publicationPath))
                        throw new InvalidOperationException($"DocFX workflow '{workflow.Id}' produced duplicate publication path '{publicationPath}'.");

                    documents.Add(new RequirementsRenderedDocument(publicationPath, content, contentType));
                }
            }
        }
        finally
        {
            DeleteDirectoryTreeWithoutFollowingLinks(workflow.OutputRootPath);
        }
    }

    private static InvalidOperationException BuildProcessFailure(RequirementsDocFxWorkflow workflow, ProcessRunResult result)
    {
        var stdout = BoundDiagnostic(result.Stdout);
        var stderr = BoundDiagnostic(result.Stderr);
        return new InvalidOperationException(
            $"DocFX workflow '{workflow.Id}' exited with code {result.ExitCode}. Stdout: {stdout} Stderr: {stderr}");
    }

    private static string BoundDiagnostic(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "<empty>";

        var trimmed = value.Trim();
        return trimmed.Length <= MaxDiagnosticCharacters
            ? trimmed
            : trimmed[..MaxDiagnosticCharacters] + "...<truncated>";
    }

    private static IReadOnlyList<string> EnumerateStagedFiles(
        string workspaceRoot,
        string outputRoot,
        string workflowId,
        CancellationToken ct)
    {
        var files = new List<string>();
        VisitDirectory(workspaceRoot, outputRoot, outputRoot, workflowId, files, ct);
        return files
            .OrderBy(path => NormalizeRelativePath(Path.GetRelativePath(outputRoot, path)), StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => NormalizeRelativePath(Path.GetRelativePath(outputRoot, path)), StringComparer.Ordinal)
            .ToList();
    }

    private static void VisitDirectory(
        string workspaceRoot,
        string outputRoot,
        string currentDirectory,
        string workflowId,
        ICollection<string> files,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        RequirementsWikiPathSecurity.ThrowIfPathEscapesRoot(outputRoot, currentDirectory, $"DocFX workflow '{workflowId}' staged directory");
        RequirementsWikiPathSecurity.ThrowIfPathEscapesRoot(workspaceRoot, currentDirectory, $"DocFX workflow '{workflowId}' staged directory");

        foreach (var entry in Directory.EnumerateFileSystemEntries(currentDirectory).Order(StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            var attributes = File.GetAttributes(entry);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new InvalidOperationException($"DocFX workflow '{workflowId}' staged output contains a reparse point: {entry}");

            if (attributes.HasFlag(FileAttributes.Directory))
            {
                VisitDirectory(workspaceRoot, outputRoot, entry, workflowId, files, ct);
            }
            else
            {
                RequirementsWikiPathSecurity.ThrowIfPathEscapesRoot(outputRoot, entry, $"DocFX workflow '{workflowId}' staged file");
                RequirementsWikiPathSecurity.ThrowIfPathEscapesRoot(workspaceRoot, entry, $"DocFX workflow '{workflowId}' staged file");
                files.Add(Path.GetFullPath(entry));
            }
        }
    }

    private static bool IsIgnoredDocFxTemplateArtifact(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        if (normalized.Equals("favicon.ico", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!normalized.StartsWith("styles/", StringComparison.OrdinalIgnoreCase))
            return false;

        return Path.GetExtension(normalized) is var extension
            && (extension.Equals(".eot", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".woff", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".woff2", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveContentType(string workflowId, string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        if (s_contentTypes.TryGetValue(extension, out var contentType))
            return contentType;

        throw new InvalidOperationException($"DocFX workflow '{workflowId}' staged output contains unsupported file '{relativePath}'.");
    }

    private static string NormalizeRelativePath(string path)
        => path.Replace('\\', '/').TrimStart('/');

    private static void DeleteDirectoryTreeWithoutFollowingLinks(string path)
    {
        if (!Directory.Exists(path))
            return;

        DeleteDirectoryTreeWithoutFollowingLinks(new DirectoryInfo(path));
    }

    private static void DeleteDirectoryTreeWithoutFollowingLinks(DirectoryInfo directory)
    {
        if (!directory.Exists)
            return;

        if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            directory.Delete();
            return;
        }

        foreach (var file in directory.EnumerateFiles())
        {
            file.Attributes &= ~FileAttributes.ReadOnly;
            file.Delete();
        }

        foreach (var child in directory.EnumerateDirectories())
            DeleteDirectoryTreeWithoutFollowingLinks(child);

        directory.Delete();
    }
}
