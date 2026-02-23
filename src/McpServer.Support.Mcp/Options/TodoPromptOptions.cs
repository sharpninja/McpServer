namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Per-workspace Copilot prompt overrides for TODO operations.
/// When a property is <see langword="null"/>, the built-in default from
/// <see cref="Services.TodoPromptDefaults"/> is used.
/// </summary>
public sealed class TodoPromptOptions
{
    /// <summary>Configuration section name under Mcp.</summary>
    public const string SectionName = "Mcp:TodoPrompts";

    /// <summary>Override for the status prompt template. Null = use default.</summary>
    public string? StatusPrompt { get; set; }

    /// <summary>Override for the implement prompt template. Null = use default.</summary>
    public string? ImplementPrompt { get; set; }

    /// <summary>Override for the plan prompt template. Null = use default.</summary>
    public string? PlanPrompt { get; set; }

    /// <summary>
    /// Base URL for API calls embedded in prompts (e.g. <c>http://localhost:7147</c>).
    /// Resolved at startup from the workspace port.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:7147";
}
