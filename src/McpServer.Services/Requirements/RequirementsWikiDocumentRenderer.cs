using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

internal static class RequirementsWikiDocumentRenderer
{
    internal const string ManifestFileName = ".mcp-requirements-manifest.json";
    internal const string AzureFolder = "azure";
    internal const string GitHubFolder = "github";

    private static readonly UTF8Encoding s_utf8NoBom = new(false);
    private static readonly string[] s_documentFiles =
    [
        "Home.md",
        RequirementsDocumentRenderer.FunctionalFileName,
        RequirementsDocumentRenderer.TechnicalFileName,
        RequirementsDocumentRenderer.TestingFileName,
        RequirementsDocumentRenderer.MappingFileName,
        RequirementsDocumentRenderer.MatrixFileName
    ];

    private static readonly JsonSerializerOptions s_manifestOptions = new()
    {
        WriteIndented = true
    };

    internal static IReadOnlyList<RequirementsRenderedDocument> RenderCanonicalFiles(
        IEnumerable<FrEntry> functional,
        IEnumerable<TrEntry> technical,
        IEnumerable<TestEntry> testing,
        IEnumerable<FrTrMapping> mappings,
        string? existingMatrixMarkdown = null) =>
        [
            new(RequirementsDocumentRenderer.FunctionalFileName, RequirementsDocumentRenderer.RenderFunctional(functional), "text/markdown"),
            new(RequirementsDocumentRenderer.TechnicalFileName, RequirementsDocumentRenderer.RenderTechnical(technical), "text/markdown"),
            new(RequirementsDocumentRenderer.TestingFileName, RequirementsDocumentRenderer.RenderTesting(testing), "text/markdown"),
            new(RequirementsDocumentRenderer.MappingFileName, RequirementsDocumentRenderer.RenderMapping(mappings), "text/markdown"),
            new(RequirementsDocumentRenderer.MatrixFileName, RequirementsDocumentRenderer.RenderMatrix(functional, technical, testing, existingMatrixMarkdown), "text/markdown")
        ];

    internal static IReadOnlyList<RequirementsRenderedDocument> RenderWikiFiles(
        IEnumerable<FrEntry> functional,
        IEnumerable<TrEntry> technical,
        IEnumerable<TestEntry> testing,
        IEnumerable<FrTrMapping> mappings,
        DateTimeOffset generatedAtUtc,
        string? existingMatrixMarkdown = null,
        RequirementsWikiExportConfig? config = null)
    {
        var documents = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home.md"] = RenderHome(),
            [RequirementsDocumentRenderer.FunctionalFileName] = RequirementsDocumentRenderer.RenderFunctional(functional),
            [RequirementsDocumentRenderer.TechnicalFileName] = RequirementsDocumentRenderer.RenderTechnical(technical),
            [RequirementsDocumentRenderer.TestingFileName] = RenderTesting(testing),
            [RequirementsDocumentRenderer.MappingFileName] = RequirementsDocumentRenderer.RenderMapping(mappings),
            [RequirementsDocumentRenderer.MatrixFileName] = RequirementsDocumentRenderer.RenderMatrix(functional, technical, testing, existingMatrixMarkdown)
        };

        if (config is not null)
            return RenderConfiguredWikiFiles(config, generatedAtUtc, documents);

        var files = new List<RequirementsRenderedDocument>();
        AddPlatform(files, AzureFolder, generatedAtUtc, documents);
        AddPlatform(files, GitHubFolder, generatedAtUtc, documents);
        return files;
    }

    private static IReadOnlyList<RequirementsRenderedDocument> RenderConfiguredWikiFiles(
        RequirementsWikiExportConfig config,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> generatedDocuments)
    {
        var files = new List<RequirementsRenderedDocument>();
        AddConfiguredPlatform(files, config, AzureFolder, generatedAtUtc, generatedDocuments);
        AddConfiguredPlatform(files, config, GitHubFolder, generatedAtUtc, generatedDocuments);
        return files;
    }

    private static void AddConfiguredPlatform(
        ICollection<RequirementsRenderedDocument> files,
        RequirementsWikiExportConfig config,
        string platform,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> generatedDocuments)
    {
        var platformDocuments = config.Documents
            .Where(document => document.AppliesTo(platform))
            .ToList();
        var platformDocumentIds = platformDocuments
            .Select(static document => document.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        files.Add(new(
            $"{platform}/{ManifestFileName}",
            RenderManifest(platform, generatedAtUtc, platformDocuments.Select(static document => document.Target)),
            "application/json"));

        foreach (var document in platformDocuments)
        {
            files.Add(new(
                $"{platform}/{document.Target}",
                ResolveConfiguredDocumentContent(document, config, generatedAtUtc, generatedDocuments),
                "text/markdown"));
        }

        if (platform.Equals(AzureFolder, StringComparison.Ordinal))
        {
            foreach (var orderFile in RenderAzureOrderFiles(config.Navigation, config, platformDocumentIds))
                files.Add(new($"{platform}/{orderFile.RelativePath}", orderFile.Content, "text/plain"));
        }
        else
        {
            files.Add(new($"{platform}/_Sidebar.md", RenderGitHubSidebar(config.Navigation, config, platformDocumentIds), "text/markdown"));
            files.Add(new($"{platform}/_Footer.md", "Generated from MCP requirements wiki export.\n", "text/markdown"));
        }
    }

    private static void AddPlatform(
        ICollection<RequirementsRenderedDocument> files,
        string platform,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> documents)
    {
        files.Add(new($"{platform}/{ManifestFileName}", RenderManifest(platform, generatedAtUtc), "application/json"));

        foreach (var (fileName, content) in documents)
            files.Add(new($"{platform}/{fileName}", content, "text/markdown"));

        if (platform.Equals(AzureFolder, StringComparison.Ordinal))
        {
            files.Add(new($"{platform}/.order", RenderAzureOrder(), "text/plain"));
        }
        else
        {
            files.Add(new($"{platform}/_Sidebar.md", RenderGitHubSidebar(), "text/markdown"));
            files.Add(new($"{platform}/_Footer.md", "Generated from MCP requirements wiki export.\n", "text/markdown"));
        }
    }

    private static string RenderManifest(string platform, DateTimeOffset generatedAtUtc, IEnumerable<string>? documents = null)
    {
        var manifest = new
        {
            schema = "mcp-requirements-wiki/v1",
            platform,
            generatedAtUtc,
            documents = documents?.ToArray() ?? s_documentFiles
        };

        return JsonSerializer.Serialize(manifest, s_manifestOptions) + "\n";
    }

    private static string RenderHome() =>
        """
        # Requirements

        - [Functional Requirements](Functional-Requirements)
        - [Technical Requirements](Technical-Requirements)
        - [Testing Requirements](Testing-Requirements)
        - [Traceability Mapping](TR-per-FR-Mapping)
        - [Requirements Matrix](Requirements-Matrix)
        """;

    private static string RenderTesting(IEnumerable<TestEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Testing Requirements (MCP Server)");
        sb.AppendLine();

        foreach (var group in entries
                     .GroupBy(static entry => GetTestingGroupKey(entry.Id), StringComparer.Ordinal)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            sb.Append("## ").AppendLine(group.Key);
            sb.AppendLine();

            foreach (var entry in group.OrderBy(static item => item.Id, StringComparer.Ordinal))
            {
                sb.Append("### ").AppendLine(entry.Id);
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(entry.Condition))
                {
                    sb.AppendLine(entry.Condition.Trim());
                    sb.AppendLine();
                }

                RequirementsDocumentRenderer.AppendAcceptanceCriteria(sb, entry.AcceptanceCriteria);
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string GetTestingGroupKey(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "TEST";

        var segments = id.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return id.Trim();

        var groupSegments = new List<string>(segments.Length);
        foreach (var segment in segments)
        {
            if (groupSegments.Count > 0 && char.IsDigit(segment[0]))
                break;

            groupSegments.Add(segment);
        }

        return groupSegments.Count == 0
            ? id.Trim()
            : string.Join('-', groupSegments);
    }

    private static string RenderAzureOrder() =>
        """
        Home
        Functional-Requirements
        Technical-Requirements
        Testing-Requirements
        TR-per-FR-Mapping
        Requirements-Matrix
        """;

    private static string RenderGitHubSidebar() =>
        """
        - [Home](Home)
        - [Functional Requirements](Functional-Requirements)
        - [Technical Requirements](Technical-Requirements)
        - [Testing Requirements](Testing-Requirements)
        - [Traceability Mapping](TR-per-FR-Mapping)
        - [Requirements Matrix](Requirements-Matrix)
        """;

    private static string ResolveConfiguredDocumentContent(
        RequirementsWikiExportDocument document,
        RequirementsWikiExportConfig config,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> generatedDocuments)
    {
        if (document.Source.Equals("generated:home", StringComparison.OrdinalIgnoreCase))
            return RenderConfiguredHome(config, generatedAtUtc);

        if (document.Source.StartsWith("generated:", StringComparison.OrdinalIgnoreCase))
            return document.Source.ToLowerInvariant() switch
            {
                "generated:functional" => generatedDocuments[RequirementsDocumentRenderer.FunctionalFileName],
                "generated:technical" => generatedDocuments[RequirementsDocumentRenderer.TechnicalFileName],
                "generated:testing" => generatedDocuments[RequirementsDocumentRenderer.TestingFileName],
                "generated:mapping" => generatedDocuments[RequirementsDocumentRenderer.MappingFileName],
                "generated:matrix" => generatedDocuments[RequirementsDocumentRenderer.MatrixFileName],
                _ => throw new InvalidOperationException($"Unsupported generated wiki source '{document.Source}'.")
            };

        return File.ReadAllText(document.SourcePath!);
    }

    private static string RenderConfiguredHome(RequirementsWikiExportConfig config, DateTimeOffset generatedAtUtc)
    {
        var navigation = RenderMarkdownNavigation(config.Navigation, config, null);
        var documents = RenderDocumentList(config.Documents);
        if (!string.IsNullOrWhiteSpace(config.HomeTemplatePath))
        {
            return File.ReadAllText(config.HomeTemplatePath)
                .Replace("{{generatedAtUtc}}", generatedAtUtc.ToString("O"), StringComparison.Ordinal)
                .Replace("{{navigation}}", navigation.TrimEnd(), StringComparison.Ordinal)
                .Replace("{{documents}}", documents.TrimEnd(), StringComparison.Ordinal);
        }

        return "# Requirements\n\n" + navigation;
    }

    private static string RenderDocumentList(IEnumerable<RequirementsWikiExportDocument> documents)
    {
        var sb = new StringBuilder();
        foreach (var document in documents)
            sb.Append("- [").Append(document.Title).Append("](").Append(ToWikiLink(document.Target)).AppendLine(")");
        return sb.ToString();
    }

    private static string RenderGitHubSidebar(
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string> platformDocumentIds)
        => RenderMarkdownNavigation(navigation, config, platformDocumentIds);

    private static string RenderMarkdownNavigation(
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string>? platformDocumentIds)
    {
        var sb = new StringBuilder();
        AppendMarkdownNavigation(sb, navigation, config, platformDocumentIds, depth: 0);
        return sb.ToString();
    }

    private static void AppendMarkdownNavigation(
        StringBuilder sb,
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string>? platformDocumentIds,
        int depth)
    {
        var indent = new string(' ', depth * 2);
        foreach (var item in navigation)
        {
            if (!string.IsNullOrWhiteSpace(item.Document))
            {
                if (platformDocumentIds is not null && !platformDocumentIds.Contains(item.Document))
                    continue;

                var document = config.DocumentsById[item.Document];
                sb.Append(indent)
                    .Append("- [")
                    .Append(document.Title)
                    .Append("](")
                    .Append(ToWikiLink(document.Target))
                    .AppendLine(")");
                continue;
            }

            var visibleChildren = FilterVisibleNavigation(item.Children, platformDocumentIds).ToList();
            if (visibleChildren.Count == 0)
                continue;

            sb.Append(indent).Append("- ").AppendLine(item.Title);
            AppendMarkdownNavigation(sb, visibleChildren, config, platformDocumentIds, depth + 1);
        }
    }

    private static IEnumerable<RequirementsWikiExportNavigationItem> FilterVisibleNavigation(
        IEnumerable<RequirementsWikiExportNavigationItem> navigation,
        ISet<string>? platformDocumentIds)
    {
        foreach (var item in navigation)
        {
            if (!string.IsNullOrWhiteSpace(item.Document))
            {
                if (platformDocumentIds is null || platformDocumentIds.Contains(item.Document))
                    yield return item;

                continue;
            }

            if (FilterVisibleNavigation(item.Children, platformDocumentIds).Any())
                yield return item;
        }
    }

    private static IReadOnlyList<RequirementsRenderedDocument> RenderAzureOrderFiles(
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string> platformDocumentIds)
    {
        var files = new List<RequirementsRenderedDocument>();
        AddAzureOrderFile(files, string.Empty, navigation, config, platformDocumentIds);
        return files;
    }

    private static void AddAzureOrderFile(
        ICollection<RequirementsRenderedDocument> files,
        string sectionPath,
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string> platformDocumentIds)
    {
        var lines = new List<string>();
        foreach (var item in navigation)
        {
            if (!string.IsNullOrWhiteSpace(item.Document))
            {
                if (!platformDocumentIds.Contains(item.Document))
                    continue;

                lines.Add(Path.GetFileNameWithoutExtension(config.DocumentsById[item.Document].Target));
                continue;
            }

            var visibleChildren = FilterVisibleNavigation(item.Children, platformDocumentIds).ToList();
            if (visibleChildren.Count == 0 || string.IsNullOrWhiteSpace(item.Path))
                continue;

            lines.Add(Path.GetFileName(item.Path));
            AddAzureOrderFile(files, item.Path, visibleChildren, config, platformDocumentIds);
        }

        var relativePath = string.IsNullOrWhiteSpace(sectionPath)
            ? ".order"
            : sectionPath.TrimEnd('/', '\\').Replace('\\', '/') + "/.order";
        files.Add(new(relativePath, string.Join('\n', lines) + "\n", "text/plain"));
    }

    private static string ToWikiLink(string target)
    {
        var normalized = target.Replace('\\', '/');
        return normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^3]
            : normalized;
    }

    internal static UTF8Encoding Utf8NoBom => s_utf8NoBom;
}

internal sealed record RequirementsRenderedDocument(string RelativePath, string Content, string ContentType);

internal static class RequirementsDocumentExportWriter
{
    internal static async Task<RequirementsDocumentExportResult> WriteAsync(
        string outputRoot,
        string format,
        string docType,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<RequirementsRenderedDocument> documents,
        IReadOnlyCollection<string>? cleanRelativeDirectories = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
            throw new ArgumentException("Requirements export output root is required.", nameof(outputRoot));

        var normalizedRoot = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(normalizedRoot);

        var written = new List<RequirementsDocumentExportFile>(documents.Count);
        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = ResolveUnderRoot(normalizedRoot, document.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            var tempPath = Path.Combine(
                Path.GetDirectoryName(fullPath)!,
                $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(tempPath, document.Content, RequirementsWikiDocumentRenderer.Utf8NoBom, ct).ConfigureAwait(false);
            File.SetLastWriteTimeUtc(tempPath, generatedAtUtc.UtcDateTime);
            ClearReadOnly(fullPath);
            try
            {
                File.Move(tempPath, fullPath, overwrite: true);
                File.SetLastWriteTimeUtc(fullPath, generatedAtUtc.UtcDateTime);
            }
            finally
            {
                if (File.Exists(fullPath))
                    SetReadOnly(fullPath);
            }

            written.Add(new RequirementsDocumentExportFile
            {
                RelativePath = document.RelativePath.Replace('\\', '/'),
                FullPath = fullPath,
                ContentType = document.ContentType,
                LastModifiedUtc = generatedAtUtc
            });
        }

        if (cleanRelativeDirectories is { Count: > 0 })
            DeleteStaleFiles(normalizedRoot, cleanRelativeDirectories, written.Select(file => file.FullPath), ct);

        return new RequirementsDocumentExportResult
        {
            Success = true,
            Format = format,
            DocType = docType,
            GeneratedAtUtc = generatedAtUtc,
            OutputRoot = normalizedRoot,
            Files = written
        };
    }

    private static void DeleteStaleFiles(
        string outputRoot,
        IReadOnlyCollection<string> cleanRelativeDirectories,
        IEnumerable<string> expectedFiles,
        CancellationToken ct)
    {
        var expected = new HashSet<string>(
            expectedFiles.Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var relativeDirectory in cleanRelativeDirectories)
        {
            var fullDirectory = ResolveUnderRoot(outputRoot, relativeDirectory);
            if (!Directory.Exists(fullDirectory))
                continue;

            foreach (var file in Directory.EnumerateFiles(fullDirectory, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                if (!expected.Contains(Path.GetFullPath(file)))
                {
                    ClearReadOnly(file);
                    try
                    {
                        File.Delete(file);
                    }
                    finally
                    {
                        if (File.Exists(file))
                            SetReadOnly(file);
                    }
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(fullDirectory, "*", SearchOption.AllDirectories)
                         .OrderByDescending(static path => path.Length))
            {
                ct.ThrowIfCancellationRequested();
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory);
            }
        }
    }

    private static string ResolveUnderRoot(string outputRoot, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(outputRoot, relativePath));
        var rootWithSeparator = outputRoot.EndsWith(Path.DirectorySeparatorChar)
            ? outputRoot
            : outputRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(outputRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException($"Requirements export path escapes output root: {relativePath}");
        }

        return fullPath;
    }

    private static void ClearReadOnly(string path)
    {
        if (!File.Exists(path))
            return;

        var attributes = File.GetAttributes(path);
        if (attributes.HasFlag(FileAttributes.ReadOnly))
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
    }

    private static void SetReadOnly(string path)
    {
        var attributes = File.GetAttributes(path);
        if (!attributes.HasFlag(FileAttributes.ReadOnly))
            File.SetAttributes(path, attributes | FileAttributes.ReadOnly);
    }
}

internal static class RequirementsWikiDocumentSelector
{
    internal static RequirementsWikiSelection Select(
        IReadOnlyDictionary<string, RequirementsIngestDocument> documents,
        string? preferredWikiFormat = null)
    {
        if (documents.Count == 0)
            throw new ArgumentException("Wiki import requires a non-empty documents map.", nameof(documents));

        var preferred = NormalizePreferred(preferredWikiFormat);
        var azure = LoadPlatform(documents, RequirementsWikiDocumentRenderer.AzureFolder);
        var github = LoadPlatform(documents, RequirementsWikiDocumentRenderer.GitHubFolder);

        if (!azure.Exists && !github.Exists)
            throw new ArgumentException("Wiki import did not contain azure/ or github/ document folders.", nameof(documents));

        if (azure.Exists && !github.Exists)
            return azure.ToSelection("Only Azure wiki documents were present.");

        if (github.Exists && !azure.Exists)
            return github.ToSelection("Only GitHub wiki documents were present.");

        var manifestChoice = ChooseByTimestamp(azure.ManifestGeneratedAtUtc, github.ManifestGeneratedAtUtc);
        var modifiedChoice = ChooseByTimestamp(azure.LatestFileModifiedUtc, github.LatestFileModifiedUtc);

        if (manifestChoice is not null && modifiedChoice is not null && !manifestChoice.Equals(modifiedChoice, StringComparison.Ordinal))
        {
            if (preferred is null)
            {
                throw new ArgumentException(
                    "Azure and GitHub wiki timestamps disagree. Supply preferredWikiFormat when manifest.generatedAtUtc and latest file modified time select different platforms.",
                    nameof(preferredWikiFormat));
            }

            var selected = preferred == RequirementsWikiDocumentRenderer.AzureFolder ? azure : github;
            return selected.ToSelection("preferredWikiFormat resolved conflicting manifest and file modified timestamps.",
                ["Wiki manifest and file modified timestamps disagreed; preferredWikiFormat was used."]);
        }

        if (manifestChoice is not null)
        {
            var selected = manifestChoice == RequirementsWikiDocumentRenderer.AzureFolder ? azure : github;
            return selected.ToSelection("Selected by newer manifest.generatedAtUtc.");
        }

        if (modifiedChoice is not null)
        {
            var selected = modifiedChoice == RequirementsWikiDocumentRenderer.AzureFolder ? azure : github;
            return selected.ToSelection("Selected by newer latest file modified UTC.");
        }

        if (preferred is not null)
        {
            var selected = preferred == RequirementsWikiDocumentRenderer.AzureFolder ? azure : github;
            return selected.ToSelection("Timestamps were tied or unavailable; preferredWikiFormat was used.");
        }

        return github.ToSelection("Timestamps were tied or unavailable; GitHub wiki documents were selected by default.");
    }

    private static WikiPlatformDocuments LoadPlatform(
        IReadOnlyDictionary<string, RequirementsIngestDocument> documents,
        string platform)
    {
        var platformDocuments = new Dictionary<string, RequirementsIngestDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, document) in documents)
        {
            var relative = TryGetPlatformRelativePath(path, platform);
            if (relative is not null)
                platformDocuments[relative] = document;
        }

        var exists = platformDocuments.Count > 0;
        var manifestGeneratedAt = TryReadManifestGeneratedAt(platformDocuments);
        var latestModified = platformDocuments.Values
            .Where(static document => document.LastModifiedUtc is not null)
            .Select(static document => document.LastModifiedUtc!.Value.ToUniversalTime())
            .DefaultIfEmpty()
            .Max();

        if (latestModified == default)
            latestModified = default;

        return new WikiPlatformDocuments(
            platform,
            exists,
            GetContent(platformDocuments, RequirementsDocumentRenderer.FunctionalFileName),
            GetContent(platformDocuments, RequirementsDocumentRenderer.TechnicalFileName),
            GetContent(platformDocuments, RequirementsDocumentRenderer.TestingFileName),
            GetContent(platformDocuments, RequirementsDocumentRenderer.MappingFileName),
            manifestGeneratedAt,
            latestModified == default ? null : latestModified);
    }

    private static string? TryGetPlatformRelativePath(string path, string platform)
    {
        var normalized = path.Replace('\\', '/').Trim('/');
        if (normalized.StartsWith(platform + "/", StringComparison.OrdinalIgnoreCase))
            return normalized[(platform.Length + 1)..];

        var marker = "/" + platform + "/";
        var markerIndex = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return markerIndex < 0
            ? null
            : normalized[(markerIndex + marker.Length)..];
    }

    private static DateTimeOffset? TryReadManifestGeneratedAt(IReadOnlyDictionary<string, RequirementsIngestDocument> documents)
    {
        if (!documents.TryGetValue(RequirementsWikiDocumentRenderer.ManifestFileName, out var manifestDocument))
            return null;

        var manifestContent = ReadContent(manifestDocument);
        if (string.IsNullOrWhiteSpace(manifestContent))
            return null;

        using var json = JsonDocument.Parse(manifestContent);
        return json.RootElement.TryGetProperty("generatedAtUtc", out var generatedAt)
            && generatedAt.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(generatedAt.GetString(), out var parsed)
            ? parsed.ToUniversalTime()
            : null;
    }

    private static string? GetContent(IReadOnlyDictionary<string, RequirementsIngestDocument> documents, string fileName)
        => documents.TryGetValue(fileName, out var document) ? ReadContent(document) : null;

    private static string? ReadContent(RequirementsIngestDocument document)
    {
        if (document.Content is not null)
            return document.Content;

        return string.IsNullOrWhiteSpace(document.ContentBase64)
            ? null
            : Encoding.UTF8.GetString(Convert.FromBase64String(document.ContentBase64));
    }

    private static string? ChooseByTimestamp(DateTimeOffset? azure, DateTimeOffset? github)
    {
        if (azure is null || github is null || azure.Value == github.Value)
            return null;

        return azure.Value > github.Value
            ? RequirementsWikiDocumentRenderer.AzureFolder
            : RequirementsWikiDocumentRenderer.GitHubFolder;
    }

    private static string? NormalizePreferred(string? preferred)
    {
        if (string.IsNullOrWhiteSpace(preferred))
            return null;

        var normalized = preferred.Trim().ToLowerInvariant();
        return normalized switch
        {
            RequirementsWikiDocumentRenderer.AzureFolder => RequirementsWikiDocumentRenderer.AzureFolder,
            RequirementsWikiDocumentRenderer.GitHubFolder => RequirementsWikiDocumentRenderer.GitHubFolder,
            _ => throw new ArgumentException("preferredWikiFormat must be azure or github.", nameof(preferred))
        };
    }

    private sealed record WikiPlatformDocuments(
        string Platform,
        bool Exists,
        string? FunctionalMarkdown,
        string? TechnicalMarkdown,
        string? TestingMarkdown,
        string? MappingMarkdown,
        DateTimeOffset? ManifestGeneratedAtUtc,
        DateTimeOffset? LatestFileModifiedUtc)
    {
        public RequirementsWikiSelection ToSelection(string reason, IReadOnlyList<string>? warnings = null)
            => new(
                Platform,
                reason,
                ManifestGeneratedAtUtc,
                LatestFileModifiedUtc,
                FunctionalMarkdown,
                TechnicalMarkdown,
                TestingMarkdown,
                MappingMarkdown,
                warnings ?? []);
    }
}

internal sealed record RequirementsWikiSelection(
    string Platform,
    string Reason,
    DateTimeOffset? ManifestGeneratedAtUtc,
    DateTimeOffset? LatestFileModifiedUtc,
    string? FunctionalMarkdown,
    string? TechnicalMarkdown,
    string? TestingMarkdown,
    string? MappingMarkdown,
    IReadOnlyList<string> Warnings);
