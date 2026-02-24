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

    /// <summary>
    /// Windows user identity whose profile environment is loaded when spawning the
    /// Copilot CLI process. This ensures the CLI finds its cached authentication tokens
    /// and is discoverable on the user's PATH (e.g. WinGet links directory).
    /// Null or empty = inherit the service account's environment.
    /// </summary>
    public string? RunAs { get; set; }

    /// <summary>
    /// GitHub personal access token or OAuth token passed as <c>GH_TOKEN</c> to the
    /// Copilot CLI process. Required when running as a Windows service that cannot
    /// access the user's keyring. Null or empty = rely on the CLI's default auth.
    /// </summary>
    public string? GitHubToken { get; set; }

    /// <summary>
    /// Absolute path to the Copilot CLI agent executable.
    /// Null or empty = use the <c>CopilotClientOptions.AgentPath</c> default (<c>copilot</c>).
    /// </summary>
    public string? AgentPath { get; set; }
}
