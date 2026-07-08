using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-002: Deterministic inbound guard for Agent Help user messages.
/// TR-MCP-HELP-004: Applies stable injection and bypass rules before helper execution.
/// </summary>
public sealed partial class AgentHelpInboundGuard
{
    private sealed record GuardRule(string RuleId, Regex Pattern, string Reason);

    private static readonly GuardRule[] s_rules =
    [
        new(
            "injection.ignore-instructions",
            IgnoreInstructionsPattern(),
            "Inbound message attempts to override prior instructions."),
        new(
            "injection.api-key-exfiltration",
            ApiKeyExfiltrationPattern(),
            "Inbound message attempts to exfiltrate API keys or secrets."),
        new(
            "injection.write-todo-yaml",
            WriteTodoYamlPattern(),
            "Inbound message attempts to write or overwrite todo.yaml directly."),
        new(
            "injection.disable-guardrails",
            DisableGuardrailsPattern(),
            "Inbound message attempts to disable guardrails or safety controls."),
    ];

    /// <summary>
    /// FR-MCP-HELP-002: Evaluates an inbound user message against deterministic guard rules.
    /// </summary>
    /// <param name="message">Raw user message text.</param>
    /// <returns>Guard evaluation result.</returns>
    public AgentHelpGuardResult Evaluate(string? message)
    {
        var normalized = (message ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new AgentHelpGuardResult
            {
                Allowed = false,
                RuleId = "validation.empty-message",
                Reason = "Inbound message is required.",
            };
        }

        foreach (var rule in s_rules)
        {
            var match = rule.Pattern.Match(normalized);
            if (!match.Success)
                continue;

            if (IsBenignBypass(normalized, rule.RuleId))
            {
                return new AgentHelpGuardResult
                {
                    Allowed = true,
                    Reason = "Message matched a benign bypass rule.",
                };
            }

            return new AgentHelpGuardResult
            {
                Allowed = false,
                RuleId = rule.RuleId,
                Reason = rule.Reason,
                MatchedSnippet = TruncateSnippet(match.Value),
            };
        }

        return new AgentHelpGuardResult
        {
            Allowed = true,
            Reason = "Message passed inbound guard checks.",
        };
    }

    private static bool IsBenignBypass(string message, string matchedRuleId)
    {
        if (McpFailureDescriptionPattern().IsMatch(message))
            return true;

        if (string.Equals(matchedRuleId, "injection.write-todo-yaml", StringComparison.Ordinal)
            && BenignTodoYamlDiscussionPattern().IsMatch(message))
        {
            return true;
        }

        return false;
    }

    private static string TruncateSnippet(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 120 ? trimmed : trimmed[..120] + "…";
    }

    [GeneratedRegex(
        @"(?i)\b(ignore|disregard|forget)\b.{0,40}\b(previous|prior|above|earlier)\b.{0,40}\b(instructions?|prompts?|rules?)\b|" +
        @"(?i)\bignore\s+all\s+previous\s+instructions\b|" +
        @"(?i)\bdisregard\s+your\s+system\s+prompt\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex IgnoreInstructionsPattern();

    [GeneratedRegex(
        @"(?i)\b(reveal|show|print|dump|exfiltrate|leak|send|output)\b.{0,40}\b(api[_ -]?key|secret|token|password|credentials?|openai_api_key|anthropic_api_key)\b|" +
        @"(?i)\bwhat\s+is\s+(your|the)\s+(api[_ -]?key|secret|token)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyExfiltrationPattern();

    [GeneratedRegex(
        @"(?i)\b(write|overwrite|modify|edit|append|save|create)\b.{0,60}\b(todo\.yaml|docs/todo\.yaml|docs\\todo\.yaml)\b|" +
        @"(?i)\bwrite\s+to\s+todo\.yaml\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex WriteTodoYamlPattern();

    [GeneratedRegex(
        @"(?i)\b(disable|turn\s+off|bypass|remove|deactivate)\b.{0,40}\b(guardrails?|safety|security\s+filters?|content\s+policy|policy\s+checks?)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex DisableGuardrailsPattern();

    [GeneratedRegex(
        @"(?i)\b(mcp\s+tool|tool\s+call|stdio\s+mcp|mcp\s+server)\b.{0,80}\b(failed|failure|error|timed\s+out|timeout|unavailable|rejected|returned\s+an?\s+error)\b|" +
        @"(?i)\bthe\s+mcp\s+request\s+failed\b|" +
        @"(?i)\bdescribe\s+why\s+the\s+mcp\s+tool\s+failed\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex McpFailureDescriptionPattern();

    [GeneratedRegex(
        @"(?i)\b(read|explain|describe|understand|review|inspect|what\s+is)\b.{0,60}\b(todo\.yaml|todo\s+schema|todo\s+format)\b|" +
        @"(?i)\bhow\s+do\s+i\s+use\s+the\s+todo\s+api\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex BenignTodoYamlDiscussionPattern();

}