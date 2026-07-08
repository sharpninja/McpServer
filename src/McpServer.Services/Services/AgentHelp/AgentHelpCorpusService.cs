using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-005: Stub corpus bootstrap for Agent Help sessions.
/// TR-MCP-HELP-006: Returns a deterministic context pack summary without full indexing.
/// </summary>
public sealed class AgentHelpCorpusService
{
    private readonly IOptionsMonitor<AgentHelpOptions> _options;
    private readonly ILogger<AgentHelpCorpusService> _logger;

    /// <summary>
    /// TR-MCP-HELP-006: Creates a new corpus bootstrap service.
    /// </summary>
    public AgentHelpCorpusService(
        IOptionsMonitor<AgentHelpOptions> options,
        ILogger<AgentHelpCorpusService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-005: Bootstraps a stub context pack summary for the workspace.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="topic">Optional topic label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<AgentHelpCorpusSummary> BootstrapAsync(
        string workspacePath,
        string? topic = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);

        if (!_options.CurrentValue.CorpusBootstrapEnabled)
        {
            return Task.FromResult(new AgentHelpCorpusSummary
            {
                WorkspacePath = workspacePath,
                DocumentCount = 0,
                Topics = [],
                Summary = "Corpus bootstrap is disabled.",
                BootstrappedUtc = DateTimeOffset.UtcNow.ToString("O"),
            });
        }

        var topics = BuildTopics(workspacePath, topic);
        var documentCount = EstimateDocumentCount(workspacePath);
        var summary = $"Stub context pack for '{Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}' with {documentCount} document(s) across {topics.Count} topic(s).";

        _logger.LogInformation(
            "Bootstrapped Agent Help corpus summary: Workspace={WorkspacePath}; Documents={DocumentCount}; Topics={TopicCount}",
            workspacePath,
            documentCount,
            topics.Count);

        return Task.FromResult(new AgentHelpCorpusSummary
        {
            WorkspacePath = workspacePath,
            DocumentCount = documentCount,
            Topics = topics,
            Summary = summary,
            BootstrappedUtc = DateTimeOffset.UtcNow.ToString("O"),
        });
    }

    private static IReadOnlyList<string> BuildTopics(string workspacePath, string? topic)
    {
        var topics = new List<string>();
        if (!string.IsNullOrWhiteSpace(topic))
            topics.Add(topic.Trim());

        var docsDir = Path.Combine(workspacePath, "docs");
        if (Directory.Exists(docsDir))
            topics.Add("workspace-docs");

        var projectDir = Path.Combine(workspacePath, "docs", "Project");
        if (Directory.Exists(projectDir))
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
}