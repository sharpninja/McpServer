namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-005: Outcome analysis with triage and documentation TODO recommendations.
/// TR-MCP-HELP-008: Derives follow-up actions from help session transcripts and incidents.
/// </summary>
public sealed class AgentHelpOutcomeService
{
    private readonly HelpTranscriptWriter _transcriptWriter;
    private readonly AgentHelpIncidentLogger _incidentLogger;
    private readonly ILogger<AgentHelpOutcomeService> _logger;

    /// <summary>
    /// TR-MCP-HELP-008: Creates a new outcome service.
    /// </summary>
    public AgentHelpOutcomeService(
        HelpTranscriptWriter transcriptWriter,
        AgentHelpIncidentLogger incidentLogger,
        ILogger<AgentHelpOutcomeService> logger)
    {
        _transcriptWriter = transcriptWriter ?? throw new ArgumentNullException(nameof(transcriptWriter));
        _incidentLogger = incidentLogger ?? throw new ArgumentNullException(nameof(incidentLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-005: Analyzes a completed help session and returns triage plus doc TODO recommendations.
    /// </summary>
    public async Task<AgentHelpOutcomeAnalysis> AnalyzeAsync(
        string workspacePath,
        string sessionId,
        string? topic = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();

        var dataRoot = Path.Combine(workspacePath, ".mcpServer");
        var transcript = await _transcriptWriter.ReadAllAsync(dataRoot, sessionId, cancellationToken)
            .ConfigureAwait(false);
        var incidents = await _incidentLogger.ReadBySessionAsync(dataRoot, sessionId, cancellationToken)
            .ConfigureAwait(false);

        var guardBlocks = incidents.Count;
        var userTurns = transcript.Count(entry => string.Equals(entry.Role, "user", StringComparison.OrdinalIgnoreCase));
        var assistantTurns = transcript.Count(entry => string.Equals(entry.Role, "assistant", StringComparison.OrdinalIgnoreCase));
        var summary = BuildSummary(userTurns, assistantTurns, guardBlocks, topic);

        var triage = BuildTriageRecommendations(sessionId, guardBlocks, topic, transcript);
        var docTodos = BuildDocTodoRecommendations(sessionId, topic, transcript, incidents);

        _logger.LogInformation(
            "Analyzed Agent Help outcome: Session={SessionId}; UserTurns={UserTurns}; GuardBlocks={GuardBlocks}; DocTodos={DocTodoCount}",
            sessionId,
            userTurns,
            guardBlocks,
            docTodos.Count);

        return new AgentHelpOutcomeAnalysis
        {
            SessionId = sessionId,
            Summary = summary,
            TriageRecommendations = triage,
            DocTodoRecommendations = docTodos,
            AnalyzedUtc = DateTimeOffset.UtcNow.ToString("O"),
        };
    }

    private static string BuildSummary(int userTurns, int assistantTurns, int guardBlocks, string? topic)
    {
        var topicLabel = string.IsNullOrWhiteSpace(topic) ? "unspecified topic" : topic.Trim();
        if (guardBlocks > 0)
        {
            return $"Help session for '{topicLabel}' completed with {userTurns} user turn(s), {assistantTurns} assistant turn(s), and {guardBlocks} guard block(s). Review incidents before continuing.";
        }

        return $"Help session for '{topicLabel}' completed with {userTurns} user turn(s) and {assistantTurns} assistant turn(s).";
    }

    private static IReadOnlyList<AgentHelpTriageRecommendation> BuildTriageRecommendations(
        string sessionId,
        int guardBlocks,
        string? topic,
        IReadOnlyList<AgentHelpTranscriptEntry> transcript)
    {
        var recommendations = new List<AgentHelpTriageRecommendation>();

        if (guardBlocks > 0)
        {
            recommendations.Add(new AgentHelpTriageRecommendation
            {
                Id = $"{sessionId}-triage-guard-review",
                Category = "security",
                Title = "Review blocked inbound prompts",
                Detail = $"Investigate {guardBlocks} guard incident(s) and confirm whether follow-up user education or policy updates are required.",
                Priority = "high",
            });
        }

        if (transcript.Any(entry => entry.Text.Contains("mcp", StringComparison.OrdinalIgnoreCase)
            && entry.Text.Contains("fail", StringComparison.OrdinalIgnoreCase)))
        {
            recommendations.Add(new AgentHelpTriageRecommendation
            {
                Id = $"{sessionId}-triage-mcp-failure",
                Category = "operations",
                Title = "Investigate MCP tool failure context",
                Detail = "The session discussed MCP failures. Capture reproduction steps and verify tool registry, workspace routing, and auth readiness.",
                Priority = "medium",
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new AgentHelpTriageRecommendation
            {
                Id = $"{sessionId}-triage-closeout",
                Category = "documentation",
                Title = "Capture resolved help guidance",
                Detail = string.IsNullOrWhiteSpace(topic)
                    ? "No guard incidents were recorded. Consider documenting the resolved guidance for future sessions."
                    : $"No guard incidents were recorded for topic '{topic}'. Consider documenting the resolved guidance for future sessions.",
                Priority = "low",
            });
        }

        return recommendations;
    }

    private static IReadOnlyList<AgentHelpDocTodoRecommendation> BuildDocTodoRecommendations(
        string sessionId,
        string? topic,
        IReadOnlyList<AgentHelpTranscriptEntry> transcript,
        IReadOnlyList<AgentHelpIncidentRecord> incidents)
    {
        var recommendations = new List<AgentHelpDocTodoRecommendation>();
        var slug = CreateSlug(string.IsNullOrWhiteSpace(topic) ? sessionId : topic!);

        if (incidents.Count > 0)
        {
            recommendations.Add(new AgentHelpDocTodoRecommendation
            {
                SuggestedTodoId = $"DOC-HELP-GUARD-{slug}",
                Title = "Document Agent Help guard incident handling",
                Section = "Documentation",
                Description =
                [
                    "Add operator guidance for reviewing Agent Help guard incidents.",
                    "Document the blocked rule identifiers and expected user remediation steps.",
                ],
                FunctionalRequirements = ["FR-MCP-HELP-002"],
                TechnicalRequirements = ["TR-MCP-HELP-004", "TR-MCP-HELP-005"],
            });
        }

        var hasRequirementsDiscussion = transcript.Any(entry =>
            entry.Text.Contains("requirement", StringComparison.OrdinalIgnoreCase)
            || entry.Text.Contains("FR-MCP", StringComparison.OrdinalIgnoreCase)
            || entry.Text.Contains("TR-MCP", StringComparison.OrdinalIgnoreCase));

        if (hasRequirementsDiscussion)
        {
            recommendations.Add(new AgentHelpDocTodoRecommendation
            {
                SuggestedTodoId = $"DOC-HELP-REQ-{slug}",
                Title = "Update requirements traceability from Agent Help session",
                Section = "Documentation",
                Description =
                [
                    "Capture FR/TR references discussed during the help session.",
                    "Add or update requirements mappings and focused tests as needed.",
                ],
                FunctionalRequirements = ["FR-MCP-HELP-005"],
                TechnicalRequirements = ["TR-MCP-HELP-008"],
            });
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(new AgentHelpDocTodoRecommendation
            {
                SuggestedTodoId = $"DOC-HELP-SUMMARY-{slug}",
                Title = "Publish Agent Help session summary",
                Section = "Documentation",
                Description =
                [
                    "Summarize the resolved help guidance for the workspace.",
                    "Link the help transcript and any follow-up validation steps.",
                ],
                FunctionalRequirements = ["FR-MCP-HELP-005"],
                TechnicalRequirements = ["TR-MCP-HELP-003", "TR-MCP-HELP-008"],
            });
        }

        return recommendations;
    }

    private static string CreateSlug(string value)
    {
        var chars = value
            .Trim()
            .ToUpperInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return string.IsNullOrWhiteSpace(slug) ? "SESSION" : slug[..Math.Min(slug.Length, 24)];
    }
}