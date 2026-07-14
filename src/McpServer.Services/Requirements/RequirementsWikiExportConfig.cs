using McpServer.Support.Mcp.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>TR-MCP-WIKIEXPORT-001: Typed, validated docs/wiki.yaml export definition.</summary>
internal sealed class RequirementsWikiExportConfig
{
    public required string ConfigPath { get; init; }

    public required string WorkspaceRoot { get; init; }

    public string? HomeTemplatePath { get; init; }

    public IReadOnlyList<RequirementsWikiExportDocument> Documents { get; init; } = [];

    public IReadOnlyList<RequirementsWikiExportNavigationItem> Navigation { get; init; } = [];

    public IReadOnlyList<RequirementsDocFxWorkflow> DocFxWorkflows { get; init; } = [];

    public IReadOnlyDictionary<string, RequirementsWikiExportDocument> DocumentsById { get; init; }
        = new Dictionary<string, RequirementsWikiExportDocument>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Configured wiki document entry.</summary>
internal sealed class RequirementsWikiExportDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Source { get; init; }

    public required string Target { get; init; }

    public string? SourcePath { get; init; }

    public IReadOnlySet<string> Platforms { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool AppliesTo(string platform) => Platforms.Contains(platform);
}

/// <summary>Configured DocFX workflow entry.</summary>
internal sealed class RequirementsDocFxWorkflow
{
    public required string Id { get; init; }

    public required string Executable { get; init; }

    public IReadOnlyList<string> Arguments { get; init; } = [];

    public required string WorkingDirectory { get; init; }

    public required string WorkingDirectoryPath { get; init; }

    public required string OutputRoot { get; init; }

    public required string OutputRootPath { get; init; }

    public required string TargetRoot { get; init; }

    public IReadOnlySet<string> Platforms { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public int TimeoutSeconds { get; init; }

    public bool AppliesTo(string platform) => Platforms.Contains(platform);
}

/// <summary>Configured wiki navigation node.</summary>
internal sealed class RequirementsWikiExportNavigationItem
{
    public string? Document { get; init; }

    public string? Title { get; init; }

    public string? Path { get; init; }

    public IReadOnlyList<RequirementsWikiExportNavigationItem> Children { get; init; } = [];
}

/// <summary>TR-MCP-WIKIEXPORT-001: Loads and validates docs/wiki.yaml using YamlDotNet object deserialization.</summary>
internal static class RequirementsWikiExportConfigLoader
{
    internal const string Schema = "mcp-wiki-export/v1";
    private const string GitHubPlatform = RequirementsWikiDocumentRenderer.GitHubFolder;
    private const string AzurePlatform = RequirementsWikiDocumentRenderer.AzureFolder;

    private static readonly IDeserializer s_deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly HashSet<string> s_generatedSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "generated:home",
        "generated:functional",
        "generated:technical",
        "generated:testing",
        "generated:mapping",
        "generated:matrix"
    };

    private static readonly HashSet<string> s_reservedTargetNames = new(StringComparer.OrdinalIgnoreCase)
    {
        RequirementsWikiDocumentRenderer.ManifestFileName,
        "_Sidebar.md",
        "_Footer.md",
        ".order"
    };

    internal static RequirementsWikiExportConfig? Load(string? workspaceRoot, RequirementsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var resolvedWorkspace = ResolveWorkspaceRoot(workspaceRoot, options);
        var configPath = ResolveConfigPath(resolvedWorkspace, options.WikiConfigPath);
        if (!File.Exists(configPath))
            return null;

        WikiConfigFile? file;
        try
        {
            file = s_deserializer.Deserialize<WikiConfigFile>(File.ReadAllText(configPath));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid docs/wiki.yaml: YAML deserialization failed: {ex.Message}", ex);
        }

        return Validate(file, configPath, resolvedWorkspace);
    }

    private static RequirementsWikiExportConfig Validate(WikiConfigFile? file, string configPath, string workspaceRoot)
    {
        var errors = new List<string>();
        if (file is null)
        {
            errors.Add("schema is required.");
            throw BuildException(errors);
        }

        if (!string.Equals(file.Schema, Schema, StringComparison.Ordinal))
            errors.Add($"schema must be {Schema}.");

        if (file.Documents.Count == 0)
            errors.Add("documents must contain at least one entry.");

        if (file.Navigation.Count == 0)
            errors.Add("navigation must contain at least one entry.");

        var documents = ValidateDocuments(file.Documents, workspaceRoot, errors);
        var documentMap = documents
            .GroupBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.OrdinalIgnoreCase);

        var navigation = ValidateNavigation(file.Navigation, documentMap, errors);
        ValidateHome(file.Home, documentMap, workspaceRoot, errors, out var homeTemplatePath);
        ValidateNavigationCoverage(documentMap, navigation, errors);
        var docFxWorkflows = ValidateDocFxWorkflows(file.DocFx?.Workflows ?? [], workspaceRoot, errors);

        if (errors.Count > 0)
            throw BuildException(errors);

        return new RequirementsWikiExportConfig
        {
            ConfigPath = configPath,
            WorkspaceRoot = workspaceRoot,
            HomeTemplatePath = homeTemplatePath,
            Documents = documents,
            Navigation = navigation,
            DocFxWorkflows = docFxWorkflows,
            DocumentsById = documentMap
        };
    }

    private static List<RequirementsWikiExportDocument> ValidateDocuments(
        IReadOnlyList<WikiDocument> rawDocuments,
        string workspaceRoot,
        ICollection<string> errors)
    {
        var documents = new List<RequirementsWikiExportDocument>(rawDocuments.Count);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetsByPlatform = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rawDocuments.Count; index++)
        {
            var raw = rawDocuments[index];
            var label = $"documents[{index}]";
            if (string.IsNullOrWhiteSpace(raw.Id))
                errors.Add($"{label}.id is required.");
            else if (!ids.Add(raw.Id.Trim()))
                errors.Add($"{label}.id duplicates document id '{raw.Id}'.");

            if (string.IsNullOrWhiteSpace(raw.Title))
                errors.Add($"{label}.title is required.");

            var target = NormalizeMarkdownTarget(raw.Target, $"{label}.target", errors);
            var platforms = NormalizePlatforms(raw.Platforms, $"{label}.platforms", errors);
            foreach (var platform in platforms)
            {
                if (!string.IsNullOrWhiteSpace(target) && !targetsByPlatform.Add(platform + ":" + target))
                    errors.Add($"{label}.target duplicate target '{target}' for platform '{platform}'.");
            }

            var source = raw.Source?.Trim() ?? string.Empty;
            string? sourcePath = null;
            if (string.IsNullOrWhiteSpace(source))
            {
                errors.Add($"{label}.source is required.");
            }
            else if (source.StartsWith("generated:", StringComparison.OrdinalIgnoreCase))
            {
                source = source.ToLowerInvariant();
                if (!s_generatedSources.Contains(source))
                    errors.Add($"{label}.source has unsupported generated source '{raw.Source}'.");
            }
            else
            {
                sourcePath = ResolveWorkspaceMarkdownPath(workspaceRoot, source, $"{label}.source", errors);
            }

            if (!string.IsNullOrWhiteSpace(raw.Id)
                && !string.IsNullOrWhiteSpace(raw.Title)
                && !string.IsNullOrWhiteSpace(source)
                && !string.IsNullOrWhiteSpace(target))
            {
                documents.Add(new RequirementsWikiExportDocument
                {
                    Id = raw.Id.Trim(),
                    Title = raw.Title.Trim(),
                    Source = source,
                    SourcePath = sourcePath,
                    Target = target!,
                    Platforms = platforms
                });
            }
        }

        return documents;
    }

    private static IReadOnlyList<RequirementsDocFxWorkflow> ValidateDocFxWorkflows(
        IReadOnlyList<WikiDocFxWorkflow> rawWorkflows,
        string workspaceRoot,
        ICollection<string> errors)
    {
        var workflows = new List<RequirementsDocFxWorkflow>(rawWorkflows.Count);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetRootsByPlatform = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < rawWorkflows.Count; index++)
        {
            var raw = rawWorkflows[index];
            var label = $"docfx.workflows[{index}]";
            var id = raw.Id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                errors.Add($"{label}.id is required.");
            else if (!ids.Add(id))
                errors.Add($"{label}.id duplicates workflow id '{raw.Id}'.");

            var executable = raw.Executable?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(executable))
                errors.Add($"{label}.executable is required.");

            var arguments = NormalizeArguments(raw.Arguments, $"{label}.arguments", errors);
            var platforms = NormalizePlatforms(raw.Platforms, $"{label}.platforms", errors);
            var workingDirectoryPath = ResolveWorkspaceDirectoryPath(workspaceRoot, raw.WorkingDirectory, $"{label}.workingDirectory", errors, out var workingDirectory);
            var outputRootPath = ResolveWorkspaceDirectoryPath(workspaceRoot, raw.OutputRoot, $"{label}.outputRoot", errors, out var outputRoot);
            var targetRoot = NormalizeSectionPath(raw.TargetRoot, $"{label}.targetRoot", errors);
            foreach (var platform in platforms)
            {
                if (!string.IsNullOrWhiteSpace(targetRoot) && !targetRootsByPlatform.Add(platform + ":" + targetRoot))
                    errors.Add($"{label}.targetRoot duplicate target root '{targetRoot}' for platform '{platform}'.");
            }

            if (raw.TimeoutSeconds is < 1 or > 3600)
                errors.Add($"{label}.timeoutSeconds must be between 1 and 3600.");

            if (!string.IsNullOrWhiteSpace(id)
                && !string.IsNullOrWhiteSpace(executable)
                && arguments.Count > 0
                && workingDirectoryPath is not null
                && outputRootPath is not null
                && targetRoot is not null
                && raw.TimeoutSeconds is >= 1 and <= 3600)
            {
                workflows.Add(new RequirementsDocFxWorkflow
                {
                    Id = id,
                    Executable = executable,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory!,
                    WorkingDirectoryPath = workingDirectoryPath,
                    OutputRoot = outputRoot!,
                    OutputRootPath = outputRootPath,
                    TargetRoot = targetRoot,
                    Platforms = platforms,
                    TimeoutSeconds = raw.TimeoutSeconds
                });
            }
        }

        return workflows;
    }

    private static IReadOnlyList<string> NormalizeArguments(IReadOnlyList<string> arguments, string label, ICollection<string> errors)
    {
        if (arguments.Count == 0)
        {
            errors.Add($"{label} must contain at least one argument.");
            return [];
        }

        var normalized = new List<string>(arguments.Count);
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (string.IsNullOrWhiteSpace(argument))
            {
                errors.Add($"{label}[{index}] must not be empty.");
                continue;
            }

            normalized.Add(argument);
        }

        return normalized;
    }

    private static IReadOnlyList<RequirementsWikiExportNavigationItem> ValidateNavigation(
        IReadOnlyList<WikiNavigationItem> rawItems,
        IReadOnlyDictionary<string, RequirementsWikiExportDocument> documentMap,
        ICollection<string> errors)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return rawItems
            .Select((item, index) => ValidateNavigationItem(item, $"navigation[{index}]", documentMap, paths, errors))
            .ToList();
    }

    private static RequirementsWikiExportNavigationItem ValidateNavigationItem(
        WikiNavigationItem raw,
        string label,
        IReadOnlyDictionary<string, RequirementsWikiExportDocument> documentMap,
        ISet<string> paths,
        ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(raw.Document))
        {
            var document = raw.Document.Trim();
            if (!documentMap.ContainsKey(document))
                errors.Add($"{label}.document references unknown document '{document}'.");

            if (raw.Children.Count > 0)
                errors.Add($"{label}.children is not allowed on document navigation entries.");

            return new RequirementsWikiExportNavigationItem { Document = document };
        }

        if (string.IsNullOrWhiteSpace(raw.Title))
            errors.Add($"{label}.title is required for section navigation entries.");

        var sectionPath = NormalizeSectionPath(raw.Path, $"{label}.path", errors);
        if (!string.IsNullOrWhiteSpace(sectionPath) && !paths.Add(sectionPath!))
            errors.Add($"{label}.path duplicates navigation path '{sectionPath}'.");

        if (raw.Children.Count == 0)
            errors.Add($"{label}.children must contain at least one item for section navigation entries.");

        var children = raw.Children
            .Select((child, index) => ValidateNavigationItem(child, $"{label}.children[{index}]", documentMap, paths, errors))
            .ToList();

        return new RequirementsWikiExportNavigationItem
        {
            Title = raw.Title?.Trim(),
            Path = sectionPath,
            Children = children
        };
    }

    private static void ValidateHome(
        WikiHome? home,
        IReadOnlyDictionary<string, RequirementsWikiExportDocument> documentMap,
        string workspaceRoot,
        ICollection<string> errors,
        out string? homeTemplatePath)
    {
        homeTemplatePath = null;
        if (home is null)
            return;

        if (!string.IsNullOrWhiteSpace(home.Document) && !documentMap.ContainsKey(home.Document.Trim()))
            errors.Add($"home.document references unknown document '{home.Document}'.");

        if (!string.IsNullOrWhiteSpace(home.Template))
            homeTemplatePath = ResolveWorkspaceMarkdownPath(workspaceRoot, home.Template, "home.template", errors);
    }

    private static void ValidateNavigationCoverage(
        IReadOnlyDictionary<string, RequirementsWikiExportDocument> documentMap,
        IReadOnlyList<RequirementsWikiExportNavigationItem> navigation,
        ICollection<string> errors)
    {
        var references = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in FlattenNavigation(navigation))
        {
            if (string.IsNullOrWhiteSpace(item.Document))
                continue;

            references.TryGetValue(item.Document, out var count);
            references[item.Document] = count + 1;
        }

        foreach (var id in documentMap.Keys.Order(StringComparer.OrdinalIgnoreCase))
        {
            references.TryGetValue(id, out var count);
            if (count == 0)
                errors.Add($"navigation does not reference document '{id}'.");
            else if (count > 1)
                errors.Add($"navigation references document '{id}' more than once.");
        }
    }

    private static IEnumerable<RequirementsWikiExportNavigationItem> FlattenNavigation(IEnumerable<RequirementsWikiExportNavigationItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in FlattenNavigation(item.Children))
                yield return child;
        }
    }

    private static IReadOnlySet<string> NormalizePlatforms(IReadOnlyList<string> platforms, string label, ICollection<string> errors)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (platforms.Count == 0)
        {
            normalized.Add(GitHubPlatform);
            normalized.Add(AzurePlatform);
            return normalized;
        }

        foreach (var platform in platforms)
        {
            var value = platform?.Trim().ToLowerInvariant();
            if (value is GitHubPlatform or AzurePlatform)
            {
                normalized.Add(value);
            }
            else
            {
                errors.Add($"{label} contains unsupported platform '{platform}'.");
            }
        }

        return normalized;
    }

    private static string? NormalizeMarkdownTarget(string? target, string label, ICollection<string> errors)
    {
        var normalized = NormalizeRelativePath(target, label, requireMarkdown: true, errors);
        if (normalized is not null && s_reservedTargetNames.Contains(Path.GetFileName(normalized)))
            errors.Add($"{label} targets reserved exporter-managed file '{normalized}'.");

        return normalized;
    }

    private static string? NormalizeSectionPath(string? path, string label, ICollection<string> errors)
        => NormalizeRelativePath(path, label, requireMarkdown: false, errors);

    private static string? NormalizeRelativePath(string? path, string label, bool requireMarkdown, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{label} is required.");
            return null;
        }

        var normalized = path.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
            errors.Add($"{label} must be relative.");

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(static segment => segment is "." or ".."))
            errors.Add($"{label} must not contain path traversal.");

        if (requireMarkdown && !normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            errors.Add($"{label} must target a Markdown file.");

        if (!requireMarkdown && normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            errors.Add($"{label} must be a section path, not a Markdown file.");

        return normalized;
    }

    private static string? ResolveWorkspaceMarkdownPath(string workspaceRoot, string relativePath, string label, ICollection<string> errors)
    {
        var normalized = NormalizeRelativePath(relativePath, label, requireMarkdown: true, errors);
        if (normalized is null)
            return null;

        var fullPath = ResolveWorkspaceContainedPath(workspaceRoot, normalized, label, errors);
        if (fullPath is null)
            return null;

        if (!File.Exists(fullPath))
            errors.Add($"{label} source file '{normalized}' does not exist.");

        return fullPath;
    }

    private static string? ResolveWorkspaceDirectoryPath(
        string workspaceRoot,
        string? relativePath,
        string label,
        ICollection<string> errors,
        out string? normalized)
    {
        normalized = NormalizeSectionPath(relativePath, label, errors);
        if (normalized is null)
            return null;

        return ResolveWorkspaceContainedPath(workspaceRoot, normalized, label, errors);
    }

    private static string? ResolveWorkspaceContainedPath(
        string workspaceRoot,
        string normalizedRelativePath,
        string label,
        ICollection<string> errors)
        => RequirementsWikiPathSecurity.ResolveWorkspaceContainedPath(workspaceRoot, normalizedRelativePath, label, errors);

    private static bool EscapesWorkspaceThroughReparsePoint(string workspaceRoot, string fullPath)
        => RequirementsWikiPathSecurity.EscapesWorkspaceThroughReparsePoint(workspaceRoot, fullPath);

    private static string EnsureTrailingSeparator(string path)
        => RequirementsWikiPathSecurity.EnsureTrailingSeparator(path);

    private static string ResolveWorkspaceRoot(string? workspaceRoot, RequirementsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
            return Path.GetFullPath(workspaceRoot);

        var functional = options.FunctionalRequirementsPath;
        if (!string.IsNullOrWhiteSpace(functional))
        {
            var projectDir = Path.GetDirectoryName(Path.GetFullPath(functional));
            var docsDir = projectDir is null ? null : Directory.GetParent(projectDir)?.FullName;
            var inferred = docsDir is null ? null : Directory.GetParent(docsDir)?.FullName;
            if (!string.IsNullOrWhiteSpace(inferred))
                return Path.GetFullPath(inferred);
        }

        return Directory.GetCurrentDirectory();
    }

    private static string ResolveConfigPath(string workspaceRoot, string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("docs", "wiki.yaml")
            : configuredPath;
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(workspaceRoot, path));
    }

    private static InvalidOperationException BuildException(IEnumerable<string> errors)
        => new("Invalid docs/wiki.yaml: " + string.Join("; ", errors));

    private sealed class WikiConfigFile
    {
        public string? Schema { get; set; }
        public WikiHome? Home { get; set; }
        [YamlMember(Alias = "docfx")]
        public WikiDocFx? DocFx { get; set; }
        public List<WikiDocument> Documents { get; set; } = [];
        public List<WikiNavigationItem> Navigation { get; set; } = [];
    }

    private sealed class WikiHome
    {
        public string? Document { get; set; }
        public string? Template { get; set; }
    }

    private sealed class WikiDocFx
    {
        public List<WikiDocFxWorkflow> Workflows { get; set; } = [];
    }

    private sealed class WikiDocFxWorkflow
    {
        public string? Id { get; set; }
        public string? Executable { get; set; }
        public List<string> Arguments { get; set; } = [];
        public string? WorkingDirectory { get; set; }
        public string? OutputRoot { get; set; }
        public string? TargetRoot { get; set; }
        public List<string> Platforms { get; set; } = [];
        public int TimeoutSeconds { get; set; } = 120;
    }

    private sealed class WikiDocument
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Source { get; set; }
        public string? Target { get; set; }
        public List<string> Platforms { get; set; } = [];
    }

    private sealed class WikiNavigationItem
    {
        public string? Document { get; set; }
        public string? Title { get; set; }
        public string? Path { get; set; }
        public List<WikiNavigationItem> Children { get; set; } = [];
    }
}
