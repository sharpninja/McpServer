using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Options for voice conversation orchestration endpoints.
/// </summary>
public sealed class VoiceConversationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "VoiceConversation";

    /// <summary>
    /// Enables the voice conversation endpoints.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Copilot model identifier passed to Copilot CLI via <c>--model</c>.
    /// </summary>
    public string CopilotModel { get; set; } = "gpt-5.3-codex";

    /// <summary>
    /// Default execution strategy used for voice sessions when callers do not explicitly choose one.
    /// Supported values are <c>copilot-cli</c> and <c>hosted-agentframework</c>.
    /// </summary>
    public string DefaultExecutionStrategy { get; set; } = AgentExecutionStrategyNames.CopilotCli;

    /// <summary>
    /// Optional API key injected into the underlying agent/model process for voice sessions.
    /// When set, the key is exposed to the process through
    /// <see cref="ModelApiKeyEnvironmentVariableName"/>.
    /// </summary>
    public string? ModelApiKey { get; set; }

    /// <summary>
    /// Environment variable name used to pass <see cref="ModelApiKey"/> to the underlying
    /// agent/model process.
    /// </summary>
    public string ModelApiKeyEnvironmentVariableName { get; set; } = "OPENAI_API_KEY";

    /// <summary>
    /// Maximum number of tool-call loop iterations per turn.
    /// </summary>
    public int MaxToolSteps { get; set; } = 6;

    /// <summary>
    /// Timeout for a single Copilot CLI invocation in seconds.
    /// </summary>
    public int CopilotTimeoutSeconds { get; set; } = 14400;

    /// <summary>
    /// Maximum number of todo write mutations allowed in a single turn.
    /// </summary>
    public int MaxWritesPerTurn { get; set; } = 3;

    /// <summary>
    /// Maximum number of todo delete operations allowed in a single turn.
    /// </summary>
    public int MaxDeletesPerTurn { get; set; } = 1;

    /// <summary>
    /// Working directory for Copilot CLI. Empty means use the host content root.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Whether transcripts should be retained in the in-memory session store.
    /// </summary>
    public bool LogTranscripts { get; set; } = true;

    /// <summary>
    /// Whether tool-call records should be retained in the in-memory session store.
    /// </summary>
    public bool LogToolCalls { get; set; } = true;

    /// <summary>
    /// Maximum number of transcript entries included in the model context prompt.
    /// </summary>
    public int TranscriptContextEntryLimit { get; set; } = 20;

    /// <summary>
    /// When <see langword="true"/>, voice chat launches the Copilot CLI on the interactive desktop
    /// using <c>CreateProcessWithTokenW</c> instead of <see cref="System.Diagnostics.Process.Start(System.Diagnostics.ProcessStartInfo)"/>.
    /// This is required when the MCP server runs as a Windows service.
    /// </summary>
    public bool UseDesktopLaunch { get; set; } = true;

    /// <summary>
    /// Minutes of inactivity before a voice session is considered idle and eligible for cleanup.
    /// </summary>
    public TimeSpan SessionIdleTimeoutMinutes { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Command sent to the Copilot subprocess before terminating an idle session.
    /// The subprocess is expected to respond with the <see cref="IdleShutdownSentinel"/> text.
    /// </summary>
    public string IdleShutdownCommand { get; set; } = "Commit changes and update session log, then announce 'Ready to shut down'";

    /// <summary>
    /// Sentinel text the server waits for in the Copilot response before terminating an idle session.
    /// </summary>
    public string IdleShutdownSentinel { get; set; } = "Ready to shut down";
}
