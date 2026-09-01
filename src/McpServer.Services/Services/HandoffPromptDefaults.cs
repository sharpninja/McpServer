namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-AGENT-001: Versioned strict-JSON prompt for handoff TODO extraction.</summary>
public static class HandoffPromptDefaults
{
    /// <summary>Canonical prompt version. Changing this starts a new replay identity.</summary>
    public const string PromptVersion = "handoff-todo-draft/v1";

    /// <summary>Well-known prompt template identifier.</summary>
    public const string TemplateId = "handoff-todo-draft";

    /// <summary>Minimum confidence required by CreateWhenConfident.</summary>
    public const double CreateWhenConfidentThreshold = 0.75;

    /// <summary>Maximum decoded source size in bytes.</summary>
    public const int MaxDecodedBytes = 8 * 1024 * 1024;

    /// <summary>Versioned extraction prompt. Requires a single JSON object and forbids compatibility wrapping.</summary>
    public const string Prompt =
        """
        Extract one MCP TODO draft from the handoff document in {handoffText}.
        Return only a single JSON object. Do not wrap it in markdown. Do not emit prose.

        Required properties:
        - id: canonical TODO id matching ^[A-Z]+-[A-Z0-9]+-\d{3}$ or ^ISSUE-\d+$
        - title: short title
        - section: existing or proposed section name
        - priority: critical, high, medium, or low
        - confidence: number from 0.0 to 1.0

        Optional properties:
        - estimate
        - description: string array
        - technicalDetails: string array
        - implementationTasks: array of { "task": string, "done": boolean }
        - dependsOn: string array of TODO ids
        - functionalRequirements: string array of FR ids
        - technicalRequirements: string array of TR ids
        - unknownSourceNotes: string array. Put unknown, missing, or ambiguous source facts here. Never omit them.

        If the handoff is ambiguous, lower confidence and populate unknownSourceNotes.
        """;
}
