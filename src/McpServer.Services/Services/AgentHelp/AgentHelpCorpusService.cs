using System.Text;
using McpServer.Support.Mcp.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-005: Corpus bootstrap for Agent Help sessions.
/// TR-MCP-HELP-006: Loads pinned workspace docs and optional indexed search excerpts into a context pack.
/// </summary>
public sealed class AgentHelpCorpusService
{
    private readonly IOptionsMonitor<AgentHelpOptions> _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AgentHelpPinnedPathResolver _pinnedPathResolver;
    private readonly IGlobalGraphRagCorpusSource? _globalGraphRagCorpusSource;
    private readonly ILogger<AgentHelpCorpusService> _logger;

    /// <summary>
    /// TR-MCP-HELP-006: Creates a new corpus bootstrap service.
    /// </summary>
    public AgentHelpCorpusService(
        IOptionsMonitor<AgentHelpOptions> options,
        IHttpContextAccessor httpContextAccessor,
        AgentHelpPinnedPathResolver pinnedPathResolver,
        ILogger<AgentHelpCorpusService> logger,
        IGlobalGraphRagCorpusSource? globalGraphRagCorpusSource = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _pinnedPathResolver = pinnedPathResolver ?? throw new ArgumentNullException(nameof(pinnedPathResolver));
        _globalGraphRagCorpusSource = globalGraphRagCorpusSource;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-005: Bootstraps a context pack for the workspace and topic.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="topic">Optional topic label.</param>
    /// <param name="issueSummary">Optional issue summary used to refine search queries.</param>
    /// <param name="todoId">Optional active TODO id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<AgentHelpCorpusBootstrapResult> BootstrapAsync(
        string workspacePath,
        string? topic = null,
        string? issueSummary = null,
        string? todoId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        var opts = _options.CurrentValue;
        if (!opts.CorpusBootstrapEnabled)
        {
            return new AgentHelpCorpusBootstrapResult
            {
                WorkspacePath = workspacePath,
                DocumentCount = 0,
                ChunkCount = 0,
                Topics = [],
                Summary = "Corpus bootstrap is disabled.",
                BootstrappedUtc = DateTimeOffset.UtcNow.ToString("O"),
                ContextPackText = string.Empty,
                SourceKeys = [],
            };
        }

        var topics = BuildTopics(workspacePath, topic, todoId);
        var sections = new List<(string SourceKey, string Text)>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (opts.PreferGlobalGraphRag && _globalGraphRagCorpusSource is not null)
        {
            var globalSections = await TryQueryGlobalGraphRagAsync(
                    BuildSearchQuery(topic, issueSummary, todoId),
                    opts.ContextSearchChunkLimit,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var section in globalSections)
            {
                if (seenPaths.Add(section.SourceKey))
                    sections.Add((section.SourceKey, section.Text));
            }
        }

        foreach (var token in AgentHelpPinnedPathResolver.GetPinnedPathTokens(opts))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolved = _pinnedPathResolver.TryResolve(token, workspacePath);
            if (resolved is null || !seenPaths.Add(resolved.Value.FullPath))
                continue;

            var excerpt = TryReadPinnedExcerpt(
                resolved.Value.FullPath,
                resolved.Value.SourceKey,
                topic,
                issueSummary,
                todoId);
            if (!string.IsNullOrWhiteSpace(excerpt))
                sections.Add((resolved.Value.SourceKey, excerpt!));
        }

        var searchQuery = BuildSearchQuery(topic, issueSummary, todoId);
        var searchSections = await TrySearchIndexedContextAsync(searchQuery, opts.ContextSearchChunkLimit, cancellationToken)
            .ConfigureAwait(false);
        sections.AddRange(searchSections);

        var sourceKeys = sections.Select(section => section.SourceKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var contextPackText = BuildContextPackText(sections, opts.MaxContextCharacters);
        var chunkCount = sections.Count;
        var documentCount = EstimateDocumentCount(workspacePath);
        var summary = chunkCount == 0
            ? $"No Agent Help context excerpts were loaded for '{Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}'."
            : $"Loaded {chunkCount} context excerpt(s) from {sourceKeys.Count} source(s) for topic '{topics[0]}'.";

        _logger.LogInformation(
            "Bootstrapped Agent Help corpus: Workspace={WorkspacePath}; Excerpts={ChunkCount}; Sources={SourceCount}",
            workspacePath,
            chunkCount,
            sourceKeys.Count);

        return new AgentHelpCorpusBootstrapResult
        {
            WorkspacePath = workspacePath,
            DocumentCount = documentCount,
            ChunkCount = chunkCount,
            Topics = topics,
            Summary = summary,
            BootstrappedUtc = DateTimeOffset.UtcNow.ToString("O"),
            ContextPackText = contextPackText,
            SourceKeys = sourceKeys,
        };
    }

    private async Task<IReadOnlyList<(string SourceKey, string Text)>> TryQueryGlobalGraphRagAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || _globalGraphRagCorpusSource is null)
            return [];

        try
        {
            var excerpts = await _globalGraphRagCorpusSource
                .QueryAsync(query, Math.Clamp(limit, 1, 20), cancellationToken)
                .ConfigureAwait(false);
            return excerpts
                .Select(excerpt => (excerpt.SourceKey, Truncate(excerpt.Text, 900)))
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Item2))
                .ToList();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            _logger.LogDebug(ex, "Agent Help global GraphRAG query unavailable during corpus bootstrap.");
            return [];
        }
    }

    private async Task<IReadOnlyList<(string SourceKey, string Text)>> TrySearchIndexedContextAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var search = _httpContextAccessor.HttpContext?.RequestServices.GetService<IContextSearchService>();
        if (search is null)
            return [];

        try
        {
            var result = await search.SearchAsync(query, Math.Clamp(limit, 1, 20), sourceType: null, cancellationToken)
                .ConfigureAwait(false);
            return result.Chunks
                .Select((chunk, index) =>
                {
                    var sourceKey = result.SourceKeys.ElementAtOrDefault(index) ?? chunk.DocumentId;
                    var text = string.IsNullOrWhiteSpace(chunk.Snippet) ? chunk.Content : chunk.Snippet;
                    return (SourceKey: $"search:{sourceKey}", Text: Truncate(text?.Trim() ?? string.Empty, 900));
                })
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Text))
                .ToList();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            _logger.LogDebug(ex, "Agent Help indexed search unavailable during corpus bootstrap.");
            return [];
        }
    }

    private static string BuildSearchQuery(string? topic, string? issueSummary, string? todoId)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(topic))
            parts.Add(topic.Trim());
        if (!string.IsNullOrWhiteSpace(issueSummary))
            parts.Add(issueSummary.Trim());
        if (!string.IsNullOrWhiteSpace(todoId))
            parts.Add(todoId.Trim());
        return string.Join(' ', parts);
    }

    private static string BuildContextPackText(IReadOnlyList<(string SourceKey, string Text)> sections, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var (sourceKey, text) in sections)
        {
            var block = $"### {sourceKey}{Environment.NewLine}{text.Trim()}{Environment.NewLine}";
            if (sb.Length + block.Length > maxChars)
                break;
            sb.AppendLine(block);
        }

        return sb.ToString().Trim();
    }

    private static string? TryReadPinnedExcerpt(
        string fullPath,
        string sourceKey,
        string? topic,
        string? issueSummary,
        string? todoId)
    {
        try
        {
            var content = File.ReadAllText(fullPath);
            if (string.IsNullOrWhiteSpace(content))
                return null;

            content = ExtractRelevantSections(content, sourceKey, topic, issueSummary, todoId);
            return string.IsNullOrWhiteSpace(content) ? null : Truncate(content.Trim(), 2500);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string ExtractRelevantSections(
        string content,
        string relativePath,
        string? topic,
        string? issueSummary,
        string? todoId)
    {
        var lowerPath = relativePath.Replace('\\', '/').ToLowerInvariant();
        if (lowerPath.Contains("agents-readme-first", StringComparison.Ordinal)
            || lowerPath.Contains("prompt-templates", StringComparison.Ordinal))
        {
            return ExtractMarkdownSection(content, "Agent Help")
                ?? ExtractKeywordWindow(content, ["agent help", "workflow.agenthelp", "agent_help_create_session"])
                ?? Truncate(content.Trim(), 1200);
        }

        if (lowerPath.Contains("todo-schema", StringComparison.Ordinal)
            || (topic?.Contains("todo", StringComparison.OrdinalIgnoreCase) ?? false)
            || (issueSummary?.Contains("todo", StringComparison.OrdinalIgnoreCase) ?? false)
            || !string.IsNullOrWhiteSpace(todoId))
        {
            var todoSection = ExtractKeywordWindow(content, ["workflow.todo", "done:", "doneSummary", "todo.update"]);
            if (!string.IsNullOrWhiteSpace(todoSection))
                return todoSection;
        }

        if (lowerPath.Contains("session-log", StringComparison.Ordinal)
            || (topic?.Contains("session", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            var sessionSection = ExtractKeywordWindow(content, ["workflow.sessionlog", "session log", "beginTurn"]);
            if (!string.IsNullOrWhiteSpace(sessionSection))
                return sessionSection;
        }

        if (lowerPath.Contains("mcp-server", StringComparison.Ordinal)
            || lowerPath.Contains("client-integration", StringComparison.Ordinal))
        {
            var helpSection = ExtractKeywordWindow(content, ["agent-help", "agent help", "/mcpserver/agent-help"]);
            if (!string.IsNullOrWhiteSpace(helpSection))
                return helpSection;
        }

        return Truncate(content.Trim(), 1200);
    }

    private static string? ExtractMarkdownSection(string content, string heading)
    {
        var lines = content.Split('\n');
        var capture = new List<string>();
        var inSection = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("# ", StringComparison.Ordinal))
            {
                if (inSection)
                    break;
                if (line.Contains(heading, StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    capture.Add(line);
                    continue;
                }
            }

            if (inSection)
                capture.Add(line);
        }

        return capture.Count == 0 ? null : string.Join(Environment.NewLine, capture).Trim();
    }

    private static string? ExtractKeywordWindow(string content, IReadOnlyList<string> keywords)
    {
        var lower = content.ToLowerInvariant();
        var index = -1;
        foreach (var keyword in keywords)
        {
            var found = lower.IndexOf(keyword.ToLowerInvariant(), StringComparison.Ordinal);
            if (found >= 0 && (index < 0 || found < index))
                index = found;
        }

        if (index < 0)
            return null;

        var start = Math.Max(0, index - 400);
        var length = Math.Min(content.Length - start, 1600);
        return content.Substring(start, length).Trim();
    }

    private static IReadOnlyList<string> BuildTopics(string workspacePath, string? topic, string? todoId)
    {
        var topics = new List<string>();
        if (!string.IsNullOrWhiteSpace(topic))
            topics.Add(topic.Trim());
        if (!string.IsNullOrWhiteSpace(todoId))
            topics.Add($"todo:{todoId.Trim()}");

        if (Directory.Exists(Path.Combine(workspacePath, "docs", "Project")))
            topics.Add("requirements");

        if (topics.Count == 0)
            topics.Add("general");

        return topics.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static int EstimateDocumentCount(string workspacePath)
    {
        var docsDir = Path.Combine(workspacePath, "docs");
        if (!Directory.Exists(docsDir))
            return 0;

        try
        {
            return Directory.EnumerateFiles(docsDir, "*.*", SearchOption.AllDirectories)
                .Count(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private static string Truncate(string value, int maxChars)
        => value.Length <= maxChars ? value : value[..maxChars] + "...";
}

/// <summary>
/// FR-MCP-HELP-005: Corpus bootstrap result including prompt-ready context text.
/// TR-MCP-HELP-006: Context pack summary contract with excerpt payload.
/// </summary>
public sealed record AgentHelpCorpusBootstrapResult
{
    /// <summary>Workspace path used for corpus bootstrap.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Estimated markdown/yaml document count under docs/.</summary>
    public int DocumentCount { get; init; }

    /// <summary>Number of context excerpts loaded into the pack.</summary>
    public int ChunkCount { get; init; }

    /// <summary>Topic labels associated with the bootstrap.</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>Short human-readable summary.</summary>
    public required string Summary { get; init; }

    /// <summary>Bootstrap timestamp in ISO 8601 UTC format.</summary>
    public required string BootstrappedUtc { get; init; }

    /// <summary>Prompt-ready context pack text.</summary>
    public required string ContextPackText { get; init; }

    /// <summary>Source keys represented in the context pack.</summary>
    public IReadOnlyList<string> SourceKeys { get; init; } = [];

    /// <summary>
    /// FR-MCP-HELP-005: Maps bootstrap result to API corpus summary DTO.
    /// </summary>
    public AgentHelpCorpusSummary ToSummary()
        => new()
        {
            WorkspacePath = WorkspacePath,
            DocumentCount = DocumentCount,
            ChunkCount = ChunkCount,
            Topics = Topics,
            Summary = Summary,
            BootstrappedUtc = BootstrappedUtc,
            SourceKeys = SourceKeys,
            ContextCharacterCount = ContextPackText.Length,
        };
}