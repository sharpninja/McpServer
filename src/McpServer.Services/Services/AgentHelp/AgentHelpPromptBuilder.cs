using System.Text;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-005: Builds structured Agent Help system prompts and echo-fallback guidance.
/// TR-MCP-HELP-006: Composes caller linkage, seeded context, and turn prompts for helper execution.
/// </summary>
public static class AgentHelpPromptBuilder
{
    private const string DefaultRolePrompt = """
        You are the MCP Server Agent Help expert for this workspace.
        Diagnose MCP Server marker trust, plugin bootstrap, session log, TODO, requirements, triage, memory, context, federation, hooks, and API behavior.
        Answer with concrete plugin or REPL steps (workflow.* method names, MCP tool names, REST paths only when no plugin route exists).
        Separate observation from inference. Never instruct callers to bypass guardrails, ignore triage, or mutate TODO/session-log/requirements storage directly.
        Prefer the required agent plugin route over raw REST for normal MCP work.
        """;

    /// <summary>
    /// FR-MCP-HELP-005: Builds the system prompt from session context and seeded corpus excerpts.
    /// </summary>
    /// <param name="context">Prompt context assembled during session bootstrap.</param>
    /// <returns>System prompt text for helper execution.</returns>
    public static string BuildSystemPrompt(AgentHelpPromptContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sb = new StringBuilder();
        sb.AppendLine(DefaultRolePrompt.Trim());
        sb.AppendLine();
        sb.AppendLine("## Session linkage");
        sb.AppendLine($"workspacePath: {context.WorkspacePath}");
        if (!string.IsNullOrWhiteSpace(context.Topic))
            sb.AppendLine($"topic: {context.Topic.Trim()}");
        if (!string.IsNullOrWhiteSpace(context.TodoId))
            sb.AppendLine($"activeTodoId: {context.TodoId.Trim()}");
        if (!string.IsNullOrWhiteSpace(context.CallerAgent))
            sb.AppendLine($"callerAgent: {context.CallerAgent.Trim()}");
        if (!string.IsNullOrWhiteSpace(context.CallerSessionId))
            sb.AppendLine($"callerSessionId: {context.CallerSessionId.Trim()}");
        if (!string.IsNullOrWhiteSpace(context.CallerRequestId))
            sb.AppendLine($"callerRequestId: {context.CallerRequestId.Trim()}");
        if (!string.IsNullOrWhiteSpace(context.IssueSummary))
        {
            sb.AppendLine("issueSummary:");
            sb.AppendLine(context.IssueSummary.Trim());
        }

        if (!string.IsNullOrWhiteSpace(context.CustomSeed))
        {
            sb.AppendLine();
            sb.AppendLine("## Operator seed");
            sb.AppendLine(context.CustomSeed.Trim());
        }

        if (!string.IsNullOrWhiteSpace(context.ContextPackText))
        {
            sb.AppendLine();
            sb.AppendLine("## Workspace context (read-only excerpts)");
            sb.AppendLine(context.ContextPackText.Trim());
            if (context.SourceKeys.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"sources: {string.Join(", ", context.SourceKeys)}");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// FR-MCP-HELP-001: Builds a single-turn prompt combining system context and the user question.
    /// </summary>
    /// <param name="context">Prompt context assembled during session bootstrap.</param>
    /// <param name="userMessage">Caller question for this turn.</param>
    /// <returns>Combined prompt suitable for one-shot CLI strategies.</returns>
    public static string BuildTurnPrompt(AgentHelpPromptContext context, string userMessage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);
        return $"SYSTEM:{Environment.NewLine}{BuildSystemPrompt(context)}{Environment.NewLine}{Environment.NewLine}USER:{Environment.NewLine}{userMessage.Trim()}";
    }

    /// <summary>
    /// FR-MCP-HELP-005: Synthesizes actionable guidance when the helper model is unavailable.
    /// </summary>
    /// <param name="context">Prompt context with seeded excerpts.</param>
    /// <param name="userMessage">Caller question.</param>
    /// <returns>Deterministic guidance derived from seeded context.</returns>
    public static string SynthesizeEchoResponse(AgentHelpPromptContext context, string userMessage)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        var sb = new StringBuilder();
        sb.AppendLine("Agent Help (context-guided fallback): the helper model is unavailable, so this answer is synthesized from seeded workspace context.");
        sb.AppendLine();
        sb.AppendLine($"Question: {userMessage.Trim()}");

        if (!string.IsNullOrWhiteSpace(context.ContextPackText))
        {
            var excerpts = ExtractRelevantExcerpts(context.ContextPackText, userMessage, maxExcerpts: 3);
            if (excerpts.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Relevant workspace excerpts:");
                foreach (var excerpt in excerpts)
                    sb.AppendLine(excerpt);
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Seeded context (truncated):");
                sb.AppendLine(Truncate(context.ContextPackText, 1200));
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("No seeded context was loaded. Re-create the session with a topic and issueSummary so corpus bootstrap can load pinned docs.");
        }

        if (context.SourceKeys.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Consult: {string.Join(", ", context.SourceKeys)}");
        }

        sb.AppendLine();
        sb.AppendLine("Next step: use the required agent plugin (workflow.* or MCP tools) rather than raw REST for TODO, session log, and requirements mutations.");
        return sb.ToString().Trim();
    }

    private static List<string> ExtractRelevantExcerpts(string contextPackText, string userMessage, int maxExcerpts)
    {
        var keywords = userMessage
            .Split([' ', '\t', '\r', '\n', ',', '.', '?', '!', ':', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length >= 4)
            .Select(word => word.ToLowerInvariant())
            .Distinct()
            .ToList();

        var sections = contextPackText.Split("### ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scored = new List<(int Score, string Text)>();
        foreach (var section in sections)
        {
            var lines = section.Split('\n', 2);
            var heading = lines[0].Trim();
            var body = lines.Length > 1 ? lines[1] : string.Empty;
            var combined = $"### {heading}{Environment.NewLine}{body}".Trim();
            var lower = combined.ToLowerInvariant();
            var score = keywords.Count(keyword => lower.Contains(keyword, StringComparison.Ordinal));
            if (score > 0)
                scored.Add((score, Truncate(combined, 600)));
        }

        return scored
            .OrderByDescending(pair => pair.Score)
            .Take(maxExcerpts)
            .Select(pair => pair.Text)
            .ToList();
    }

    private static string Truncate(string value, int maxChars)
        => value.Length <= maxChars ? value : value[..maxChars] + "...";
}

/// <summary>
/// FR-MCP-HELP-005: Mutable prompt context carried on an Agent Help session.
/// TR-MCP-HELP-006: Caller linkage and seeded corpus text for prompt construction.
/// </summary>
public sealed class AgentHelpPromptContext
{
    /// <summary>Workspace root path.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Optional topic label.</summary>
    public string? Topic { get; init; }

    /// <summary>Optional active TODO id.</summary>
    public string? TodoId { get; init; }

    /// <summary>Optional caller agent identity.</summary>
    public string? CallerAgent { get; init; }

    /// <summary>Optional caller session id.</summary>
    public string? CallerSessionId { get; init; }

    /// <summary>Optional caller request/turn id.</summary>
    public string? CallerRequestId { get; init; }

    /// <summary>Optional factual issue summary.</summary>
    public string? IssueSummary { get; init; }

    /// <summary>Optional custom operator seed text.</summary>
    public string? CustomSeed { get; init; }

    /// <summary>Seeded context pack text injected into prompts.</summary>
    public string ContextPackText { get; init; } = string.Empty;

    /// <summary>Source keys represented in <see cref="ContextPackText"/>.</summary>
    public IReadOnlyList<string> SourceKeys { get; init; } = [];
}