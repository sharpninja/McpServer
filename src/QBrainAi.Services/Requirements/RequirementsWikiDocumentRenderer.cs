using System.ComponentModel;
using System.Text;
using System.Text.Json;
using QBrainAi.Support.Mcp.Requirements.Models;
using QBrainAi.Support.Mcp.Storage;

namespace QBrainAi.Support.Mcp.Requirements;

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

    internal static IReadOnlyList<RequirementsRenderedDocument> RenderCanonicalFiles(
        IEnumerable<FrEntry> functional,
        IEnumerable<TrEntry> technical,
        IEnumerable<TestEntry> testing,
        IEnumerable<FrTrMapping> mappings,
        string? existingMatrixMarkdown = null) =>
        [
            new(RequirementsDocumentRenderer.FunctionalFileName, RequirementsDocumentRenderer.RenderFunctional(functional), "text/markdown"),
            new(RequirementsDocumentRenderer.TechnicalFileName, RequirementsDocumentRenderer.RenderTechnical(technical, mappings), "text/markdown"),
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
        RequirementsWikiExportConfig? config = null,
        IReadOnlyList<RequirementsRenderedDocument>? extensionDocuments = null)
    {
        var documents = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Home.md"] = RenderHome(),
            [RequirementsDocumentRenderer.FunctionalFileName] = RequirementsDocumentRenderer.RenderFunctional(functional),
            [RequirementsDocumentRenderer.TechnicalFileName] = RequirementsDocumentRenderer.RenderTechnical(technical, mappings),
            [RequirementsDocumentRenderer.TestingFileName] = RenderTesting(testing),
            [RequirementsDocumentRenderer.MappingFileName] = RequirementsDocumentRenderer.RenderMapping(mappings),
            [RequirementsDocumentRenderer.MatrixFileName] = RequirementsDocumentRenderer.RenderMatrix(functional, technical, testing, existingMatrixMarkdown)
        };

        var extensions = extensionDocuments ?? [];
        if (config is not null)
        {
            var groups = new Dictionary<string, WikiRequirementGroup[]>(StringComparer.OrdinalIgnoreCase);
            if (config.RequirementGroupsEnabled)
            {
                groups["generated:functional"] = RenderGroups(functional, "FR", entry => entry.Id, RequirementsDocumentRenderer.RenderFunctional);
                groups["generated:technical"] = RenderGroups(technical, "TR", entry => entry.Id,
                    entries => RequirementsDocumentRenderer.RenderTechnical(entries, mappings));
                groups["generated:testing"] = RenderGroups(testing, "TEST", entry => entry.Id, RenderTesting);
            }

            return RenderConfiguredWikiFiles(config, generatedAtUtc, documents, extensions, groups);
        }

        if (extensions.Count > 0)
            throw new InvalidOperationException("Extension wiki documents require docs/wiki.yaml configuration.");

        var files = new List<RequirementsRenderedDocument>();
        AddPlatform(files, AzureFolder, generatedAtUtc, documents);
        AddPlatform(files, GitHubFolder, generatedAtUtc, documents);
        return files;
    }

    private static IReadOnlyList<RequirementsRenderedDocument> RenderConfiguredWikiFiles(
        RequirementsWikiExportConfig config,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> generatedDocuments,
        IReadOnlyList<RequirementsRenderedDocument> extensionDocuments,
        IReadOnlyDictionary<string, WikiRequirementGroup[]> groups)
    {
        var files = new List<RequirementsRenderedDocument>();
        AddConfiguredPlatform(files, config, AzureFolder, generatedAtUtc, generatedDocuments, extensionDocuments, groups);
        AddConfiguredPlatform(files, config, GitHubFolder, generatedAtUtc, generatedDocuments, extensionDocuments, groups);
        return files;
    }

    private static void AddConfiguredPlatform(
        ICollection<RequirementsRenderedDocument> files,
        RequirementsWikiExportConfig config,
        string platform,
        DateTimeOffset generatedAtUtc,
        IReadOnlyDictionary<string, string> generatedDocuments,
        IReadOnlyList<RequirementsRenderedDocument> extensionDocuments,
        IReadOnlyDictionary<string, WikiRequirementGroup[]> groups)
    {
        var platformDocuments = config.Documents
            .Where(document => document.AppliesTo(platform))
            .ToList();
        var platformDocumentIds = platformDocuments
            .Select(static document => document.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var platformExtensions = GetPlatformExtensionDocuments(extensionDocuments, platform);
        var groupPages = platformDocuments
            .Where(document => groups.TryGetValue(document.Source, out var matches) && matches.Length > 0)
            .ToDictionary(document => document.Id, document => PlanGroupPages(document, groups[document.Source]), StringComparer.OrdinalIgnoreCase);
        var groupTargets = groupPages.Values.SelectMany(static pages => pages).Select(static page => page.Target).ToArray();
        var inlineFooter = platform.Equals(AzureFolder, StringComparison.Ordinal) ? config.FooterContent : null;
        var manifestTargets = platformDocuments
            .Select(static document => document.Target)
            .Concat(platformExtensions.Select(document => GetPlatformRelativePath(document.RelativePath, platform)))
            .Concat(groupTargets)
            .ToArray();
        var managedDocuments = GetPlatformManagedDocuments(config, platform, platformDocumentIds, groupPages);
        var reservedTargets = manifestTargets
            .Concat(managedDocuments.Select(document => GetPlatformRelativePath(document.RelativePath, platform)))
            .Prepend(ManifestFileName)
            .ToArray();
        ValidateUniquePlatformTargets(platform, reservedTargets);
        if (platform.Equals(GitHubFolder, StringComparison.Ordinal))
            ValidateGroupRoutes(groupTargets, reservedTargets);

        files.Add(new(
            $"{platform}/{ManifestFileName}",
            RenderManifest(platform, generatedAtUtc, manifestTargets, groupTargets.Length == 0 ? null : groupTargets, inlineFooter is not null),
            "application/json"));

        foreach (var document in platformDocuments)
        {
            var content = ResolveConfiguredDocumentContent(document, config, generatedAtUtc, generatedDocuments);
            if (groupPages.TryGetValue(document.Id, out var children))
            {
                content = AddGroupLinks(content, children);
                foreach (var child in children)
                {
                    files.Add(ApplyFooter(new($"{platform}/{child.Target}", child.Markdown, "text/markdown"), inlineFooter));
                }
            }

            files.Add(ApplyFooter(new($"{platform}/{document.Target}", content, "text/markdown"), inlineFooter));
        }

        foreach (var document in platformExtensions)
            files.Add(ApplyFooter(document, inlineFooter));

        foreach (var document in managedDocuments)
            files.Add(document);
    }

    private static IReadOnlyList<RequirementsRenderedDocument> GetPlatformManagedDocuments(
        RequirementsWikiExportConfig config,
        string platform,
        ISet<string> platformDocumentIds,
        IReadOnlyDictionary<string, WikiGroupPage[]> groupPages)
    {
        if (platform.Equals(AzureFolder, StringComparison.Ordinal))
        {
            return RenderAzureOrderFiles(config.Navigation, config, platformDocumentIds, groupPages)
                .Select(orderFile => new RequirementsRenderedDocument($"{platform}/{orderFile.RelativePath}", orderFile.Content, "text/plain"))
                .ToList();
        }

        return
        [
            new RequirementsRenderedDocument($"{platform}/_Sidebar.md", RenderMarkdownNavigation(config.Navigation, config, platformDocumentIds, groupPages), "text/markdown"),
            new RequirementsRenderedDocument($"{platform}/_Footer.md", (config.FooterContent ?? "Generated from MCP requirements wiki export.") + "\n", "text/markdown")
        ];
    }

    private static void ValidateUniquePlatformTargets(string platform, IEnumerable<string> platformRelativePaths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in platformRelativePaths)
        {
            var normalized = relativePath.Replace('\\', '/');
            if (!seen.Add(normalized))
                throw new InvalidOperationException($"Duplicate wiki export path '{platform}/{normalized}'.");
        }
    }

    private static IReadOnlyList<RequirementsRenderedDocument> GetPlatformExtensionDocuments(
        IEnumerable<RequirementsRenderedDocument> extensionDocuments,
        string platform)
        => extensionDocuments
            .Where(document => HasPlatformPrefix(document.RelativePath, platform))
            .OrderBy(static document => document.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static document => document.RelativePath, StringComparer.Ordinal)
            .ToList();

    private static bool HasPlatformPrefix(string relativePath, string platform)
    {
        var normalized = relativePath.Replace('\\', '/');
        return normalized.StartsWith(platform + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPlatformRelativePath(string relativePath, string platform)
    {
        var normalized = relativePath.Replace('\\', '/');
        var prefix = platform + "/";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Extension document '{relativePath}' does not target platform '{platform}'.");

        return normalized[prefix.Length..];
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

    private static string RenderManifest(
        string platform,
        DateTimeOffset generatedAtUtc,
        IEnumerable<string>? documents = null,
        string[]? groupDocuments = null,
        bool inlineFooter = false)
    {
        var manifest = new RequirementsWikiManifest(
            "mcp-requirements-wiki/v1",
            platform,
            generatedAtUtc,
            documents?.ToArray() ?? s_documentFiles,
            groupDocuments,
            inlineFooter);

        return JsonSerializer.Serialize(manifest, RequirementsWikiJsonContext.Default.RequirementsWikiManifest) + "\n";
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
        sb.AppendLine("# Testing Requirements (QBrain.AI)");
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

    private static string RenderMarkdownNavigation(
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string>? platformDocumentIds,
        IReadOnlyDictionary<string, WikiGroupPage[]>? groupPages = null)
    {
        var sb = new StringBuilder();
        AppendMarkdownNavigation(sb, navigation, config, platformDocumentIds, depth: 0, groupPages);
        return sb.ToString();
    }

    private static void AppendMarkdownNavigation(
        StringBuilder sb,
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string>? platformDocumentIds,
        int depth,
        IReadOnlyDictionary<string, WikiGroupPage[]>? groupPages)
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
                if (groupPages?.TryGetValue(item.Document, out var children) == true)
                {
                    foreach (var child in children)
                        sb.Append(indent).Append("  ").AppendLine(GroupLink(child, fromSidebar: true));
                }
                continue;
            }

            var visibleChildren = FilterVisibleNavigation(item.Children, platformDocumentIds).ToList();
            if (visibleChildren.Count == 0)
                continue;

            sb.Append(indent).Append("- ").AppendLine(item.Title);
            AppendMarkdownNavigation(sb, visibleChildren, config, platformDocumentIds, depth + 1, groupPages);
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
        ISet<string> platformDocumentIds,
        IReadOnlyDictionary<string, WikiGroupPage[]> groupPages)
    {
        var files = new List<RequirementsRenderedDocument>();
        AddAzureOrderFile(files, string.Empty, navigation, config, platformDocumentIds, groupPages);
        return files;
    }

    private static void AddAzureOrderFile(
        ICollection<RequirementsRenderedDocument> files,
        string sectionPath,
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        RequirementsWikiExportConfig config,
        ISet<string> platformDocumentIds,
        IReadOnlyDictionary<string, WikiGroupPage[]> groupPages)
    {
        var lines = new List<string>();
        foreach (var item in navigation)
        {
            if (!string.IsNullOrWhiteSpace(item.Document))
            {
                if (!platformDocumentIds.Contains(item.Document))
                    continue;

                lines.Add(Path.GetFileNameWithoutExtension(config.DocumentsById[item.Document].Target));
                if (groupPages.TryGetValue(item.Document, out var children))
                    lines.AddRange(children.Select(static child => Path.GetFileNameWithoutExtension(child.Target)));
                continue;
            }

            var visibleChildren = FilterVisibleNavigation(item.Children, platformDocumentIds).ToList();
            if (visibleChildren.Count == 0 || string.IsNullOrWhiteSpace(item.Path))
                continue;

            lines.Add(Path.GetFileName(item.Path));
            AddAzureOrderFile(files, item.Path, visibleChildren, config, platformDocumentIds, groupPages);
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

    /// <summary>Builds deterministic wiki views from the same records and formatters as their parent.</summary>
    /// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiGroupDetection and ac-WikiGroupRoundTrip.</remarks>
    private static WikiRequirementGroup[] RenderGroups<T>(
        IEnumerable<T> entries,
        string kind,
        Func<T, string> id,
        Func<IEnumerable<T>, string> render)
        => entries
            .Select(entry => (Entry: entry, Key: IdentifyGroup(id(entry), kind)))
            .Where(static candidate => candidate.Key is not null)
            .GroupBy(static candidate => candidate.Key!, StringComparer.Ordinal)
            .Where(group => group.Select(candidate => id(candidate.Entry)).Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any())
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(group => new WikiRequirementGroup(group.Key,
                render(group.Select(static candidate => candidate.Entry).OrderBy(id, StringComparer.Ordinal))))
            .ToArray();

    /// <summary>Extracts whole leading ID segments before the first digit-leading segment.</summary>
    private static string? IdentifyGroup(string id, string kind)
    {
        var segments = id.Split('-');
        if (!segments[0].Equals(kind, StringComparison.OrdinalIgnoreCase) ||
            segments.Any(static segment => segment.Length == 0 || !segment.All(char.IsAsciiLetterOrDigit)))
        {
            return null;
        }

        var firstNumber = Array.FindIndex(segments, static segment => char.IsAsciiDigit(segment[0]));
        return firstNumber < 2 ? null : string.Join('-', segments.Take(firstNumber)).ToUpperInvariant();
    }

    /// <summary>Derives portable sibling paths and back-links for the configured parent.</summary>
    /// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiGroupNavigation and ac-WikiGroupInventory.</remarks>
    private static WikiGroupPage[] PlanGroupPages(RequirementsWikiExportDocument parent, WikiRequirementGroup[] groups)
        => groups.Select(group => new WikiGroupPage(
            group.Key,
            parent.Target[..^3] + "--" + group.Key + ".md",
            $"# {group.Key}\n\n[Back to {parent.Title}]({SiblingLink(parent.Target)})\n" +
            group.Markdown[(group.Markdown.IndexOf('\n', StringComparison.Ordinal) + 1)..])).ToArray();

    /// <summary>Adds a links section before the canonical requirement entries.</summary>
    private static string AddGroupLinks(string content, IEnumerable<WikiGroupPage> children)
    {
        var afterTitle = content.IndexOf('\n', StringComparison.Ordinal) + 1;
        var links = string.Join('\n', children.Select(static child => GroupLink(child)));
        return content.Insert(afterTitle, "\n## Requirement groups\n\n" + links + "\n");
    }

    /// <summary>Links to a group from its sibling parent or the wiki-root sidebar.</summary>
    /// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiGroupNavigation: keep the directory in sidebar links.</remarks>
    private static string GroupLink(WikiGroupPage page, bool fromSidebar = false)
    {
        var route = fromSidebar ? ToWikiLink(page.Target) : SiblingLink(page.Target);
        return $"- [{page.Key}]({route})";
    }

    private static string SiblingLink(string target) => Uri.EscapeDataString(Path.GetFileNameWithoutExtension(target));

    /// <summary>Rejects a generated GitHub page slug that another exported Markdown file would shadow.</summary>
    private static void ValidateGroupRoutes(string[] groupTargets, string[] allTargets)
    {
        if (groupTargets.Length == 0)
            return;

        var groupSlugs = groupTargets.Select(SiblingLink).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var duplicates = allTargets.Where(static target => target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(SiblingLink).GroupBy(static slug => slug, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => groupSlugs.Contains(group.Key) && group.Skip(1).Any());
        if (duplicates is not null)
            throw new InvalidOperationException($"Duplicate GitHub wiki group route '{duplicates.Key}'.");
    }

    /// <summary>Applies the inline footer to content pages while leaving extension assets byte-stable.</summary>
    /// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiFooterCoverage.</remarks>
    private static RequirementsRenderedDocument ApplyFooter(RequirementsRenderedDocument document, string? footer)
        => footer is not null && document.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? document with { Content = RequirementsWikiFooter.Append(document.Content, footer) }
            : document;

    private sealed record WikiRequirementGroup(string Key, string Markdown);

    private sealed record WikiGroupPage(string Key, string Target, string Markdown);

    internal static UTF8Encoding Utf8NoBom => s_utf8NoBom;
}

internal sealed record RequirementsRenderedDocument(string RelativePath, string Content, string ContentType);

internal static class RequirementsDocumentExportWriter
{
    private static readonly WorkspaceTraversalLimits s_exportTraversalLimits = new()
    {
        MaxFiles = 10_000,
        MaxFileBytes = 64L * 1024 * 1024,
        MaxAggregateBytes = 256L * 1024 * 1024,
        MaxDepth = 64,
    };

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

        using var exportRoot = WorkspaceContainedFileSystem.OpenExportRoot(outputRoot);
        return await WriteAsync(
                exportRoot,
                format,
                docType,
                generatedAtUtc,
                documents,
                cleanRelativeDirectories,
                ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes one requirements export through an already-pinned root capability.
    /// </summary>
    internal static async Task<RequirementsDocumentExportResult> WriteAsync(
        WorkspaceContainedFileSystem.WorkspaceExportRoot exportRoot,
        string format,
        string docType,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<RequirementsRenderedDocument> documents,
        IReadOnlyCollection<string>? cleanRelativeDirectories = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(exportRoot);
        ArgumentNullException.ThrowIfNull(documents);

        // Cleanup is entirely planned and validated before the first generated file becomes
        // externally visible. The same pinned capability then owns cleanup and every write.
        if (cleanRelativeDirectories is { Count: > 0 })
        {
            await DeleteStaleFilesAsync(
                    exportRoot,
                    cleanRelativeDirectories,
                    documents.Select(document => NormalizeRelativePath(exportRoot, document.RelativePath)),
                    ct)
                .ConfigureAwait(false);
        }

        var written = new List<RequirementsDocumentExportFile>(documents.Count);
        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();
            var relativePath = NormalizeRelativePath(exportRoot, document.RelativePath);
            var fullPath = exportRoot.ResolvePath(relativePath);
            await exportRoot.WriteTextFileAsync(
                    relativePath,
                    document.Content,
                    RequirementsWikiDocumentRenderer.Utf8NoBom,
                    generatedAtUtc.UtcDateTime,
                    readOnly: true,
                    ct)
                .ConfigureAwait(false);

            written.Add(new RequirementsDocumentExportFile
            {
                RelativePath = relativePath,
                FullPath = fullPath,
                ContentType = document.ContentType,
                LastModifiedUtc = generatedAtUtc
            });
        }

        return new RequirementsDocumentExportResult
        {
            Success = true,
            Format = format,
            DocType = docType,
            GeneratedAtUtc = generatedAtUtc,
            OutputRoot = exportRoot.RootPath,
            Files = written
        };
    }

    private static async Task DeleteStaleFilesAsync(
        WorkspaceContainedFileSystem.WorkspaceExportRoot exportRoot,
        IReadOnlyCollection<string> cleanRelativeDirectories,
        IEnumerable<string> expectedFiles,
        CancellationToken ct)
    {
        var expected = new HashSet<string>(expectedFiles, exportRoot.NameComparer);
        var staleFiles = new HashSet<string>(exportRoot.NameComparer);
        var knownDirectories = new HashSet<string>(exportRoot.NameComparer);
        var staleDirectories = new List<string>();

        foreach (var requestedDirectory in cleanRelativeDirectories)
        {
            ct.ThrowIfCancellationRequested();
            var relativeDirectory = NormalizeRelativePath(exportRoot, requestedDirectory);
            var files = await exportRoot.EnumerateFilesBoundedAsync(
                    relativeDirectory,
                    s_exportTraversalLimits,
                    TimeSpan.FromSeconds(5),
                    ct)
                .ConfigureAwait(false);
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var relativeFile = NormalizeRelativePath(exportRoot, file.FullPath);
                if (!expected.Contains(relativeFile))
                    staleFiles.Add(relativeFile);
            }

            var directories = await exportRoot.EnumerateDirectoriesBoundedAsync(
                    relativeDirectory,
                    s_exportTraversalLimits,
                    TimeSpan.FromSeconds(5),
                    ct)
                .ConfigureAwait(false);
            foreach (var directory in directories)
            {
                ct.ThrowIfCancellationRequested();
                var relativeDirectoryPath = NormalizeRelativePath(exportRoot, directory);
                if (knownDirectories.Add(relativeDirectoryPath))
                    staleDirectories.Add(relativeDirectoryPath);
            }
        }

        // Linux offers no unlinkat operation with openat2's RESOLVE_IN_ROOT guarantees for a
        // nested path; nested deletion is safe only where Landlock confines it to the root
        // (BUG-BASELINE-001). Otherwise reject every nested deletion before any stale file is mutated.
        if (!exportRoot.SupportsNestedDeletion &&
            (staleFiles.Any(static path => path.Contains('/')) ||
             staleDirectories.Any(static path => path.Contains('/'))))
        {
            var nested = staleFiles.FirstOrDefault(static path => path.Contains('/'))
                ?? staleDirectories.First(static path => path.Contains('/'));
            throw new NotSupportedException(
                "Linux requirements export cleanup rejects nested stale paths before mutation because the kernel lacks a contained atomic delete: '" +
                nested +
                "'.");
        }

        foreach (var staleFile in staleFiles.Order(exportRoot.NameComparer))
        {
            ct.ThrowIfCancellationRequested();
            _ = exportRoot.SetFileReadOnlyIfExists(staleFile, readOnly: false);
            _ = exportRoot.DeleteFileIfExists(staleFile);
        }

        // EnumerateDirectoriesBoundedAsync returns deterministic post-order; retain it while
        // de-duplicating scopes, rather than re-sorting by path.
        foreach (var staleDirectory in staleDirectories)
        {
            ct.ThrowIfCancellationRequested();
            _ = exportRoot.DeleteEmptyDirectory(staleDirectory);
        }
    }

    private static string NormalizeRelativePath(
        WorkspaceContainedFileSystem.WorkspaceExportRoot exportRoot,
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (Path.IsPathFullyQualified(path))
            return exportRoot.GetRelativePathFromFullPath(path);

        var fullPath = exportRoot.ResolvePath(path);
        return Path.GetRelativePath(exportRoot.RootPath, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/');
    }
}

/// <summary>
/// Reads the optional existing matrix through a contained native file-open boundary and limits
/// fallback to that boundary's exact absent-file outcomes.
/// </summary>
internal static class RequirementsExportMatrixReader
{
    /// <summary>
    /// Reads a contained matrix stream or obtains a configured fallback only when that contained
    /// operation reports a native absent file or path.
    /// </summary>
    /// <param name="openContainedMatrix">Opens the exact contained matrix stream.</param>
    /// <param name="readFallback">Reads the configured non-export fallback matrix.</param>
    /// <param name="ct">The caller cancellation token.</param>
    /// <returns>The contained matrix text or the exact fallback value.</returns>
    internal static async Task<string?> ReadExistingAsync(
        Func<CancellationToken, Task<Stream>> openContainedMatrix,
        Func<string?> readFallback,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(openContainedMatrix);
        ArgumentNullException.ThrowIfNull(readFallback);

        try
        {
            await using var stream = await openContainedMatrix(ct).ConfigureAwait(false);
            using var reader = new StreamReader(
                stream,
                RequirementsWikiDocumentRenderer.Utf8NoBom,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 64 * 1024,
                leaveOpen: false);
            return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return readFallback();
        }
        catch (DirectoryNotFoundException)
        {
            return readFallback();
        }
        catch (IOException exception) when (IsNativeAbsentFileOrPath(exception))
        {
            return readFallback();
        }
    }

    private static bool IsNativeAbsentFileOrPath(IOException exception) =>
        exception.InnerException is Win32Exception { NativeErrorCode: 2 or 3 };
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
        var (manifestGeneratedAt, groupDocuments, inlineFooter) = ReadManifest(platformDocuments);
        var latestModified = platformDocuments
            .Where(document => !groupDocuments.Contains(document.Key) && document.Value.LastModifiedUtc is not null)
            .Select(static document => document.Value.LastModifiedUtc!.Value.ToUniversalTime())
            .DefaultIfEmpty()
            .Max();

        if (latestModified == default)
            latestModified = default;

        return new WikiPlatformDocuments(
            platform,
            exists,
            GetContent(platformDocuments, RequirementsDocumentRenderer.FunctionalFileName, inlineFooter),
            GetContent(platformDocuments, RequirementsDocumentRenderer.TechnicalFileName, inlineFooter),
            GetContent(platformDocuments, RequirementsDocumentRenderer.TestingFileName, inlineFooter),
            GetContent(platformDocuments, RequirementsDocumentRenderer.MappingFileName, inlineFooter),
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

    /// <summary>Reads export metadata while accepting older manifests without group or footer ownership.</summary>
    /// <remarks>2026-11-26, TR-MCP-WIKIEXPORT-001 ac-WikiGroupRoundTrip and ac-WikiFooterRoundTrip.</remarks>
    private static (DateTimeOffset? GeneratedAt, HashSet<string> GroupDocuments, bool InlineFooter) ReadManifest(
        IReadOnlyDictionary<string, RequirementsIngestDocument> documents)
    {
        var groupDocuments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!documents.TryGetValue(RequirementsWikiDocumentRenderer.ManifestFileName, out var manifestDocument))
            return (null, groupDocuments, false);

        var manifestContent = ReadContent(manifestDocument);
        if (string.IsNullOrWhiteSpace(manifestContent))
            return (null, groupDocuments, false);

        using var json = JsonDocument.Parse(manifestContent);
        var root = json.RootElement;
        if (root.TryGetProperty("groupDocuments", out var groups) && groups.ValueKind == JsonValueKind.Array)
        {
            foreach (var group in groups.EnumerateArray())
            {
                if (group.ValueKind == JsonValueKind.String && group.GetString() is { } path &&
                    path.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileNameWithoutExtension(path).Contains("--", StringComparison.Ordinal))
                {
                    groupDocuments.Add(path.Replace('\\', '/'));
                }
            }
        }

        var generated = root.TryGetProperty("generatedAtUtc", out var generatedAt)
            && generatedAt.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(generatedAt.GetString(), out var parsed)
            ? (DateTimeOffset?)parsed.ToUniversalTime()
            : null;
        var inlineFooter = root.TryGetProperty("inlineFooter", out var footer) && footer.ValueKind == JsonValueKind.True;
        return (generated, groupDocuments, inlineFooter);
    }

    /// <summary>Reads a canonical page, omitting the inline footer when the exporter declared one.</summary>
    private static string? GetContent(
        IReadOnlyDictionary<string, RequirementsIngestDocument> documents,
        string fileName,
        bool inlineFooter)
    {
        var content = documents.TryGetValue(fileName, out var document) ? ReadContent(document) : null;
        return inlineFooter ? RequirementsWikiFooter.Remove(content) : content;
    }

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
