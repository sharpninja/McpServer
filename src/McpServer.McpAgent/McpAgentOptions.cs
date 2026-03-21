using McpServer.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;

namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Configuration for the scaffolded MCP Agent host integration.
/// </summary>
public sealed class McpAgentOptions
{
    /// <summary>
    /// Configuration section name reserved for the MCP Agent integration.
    /// </summary>
    public const string SectionName = "McpServer:McpAgent";

    /// <summary>
    /// Base URL for the target MCP Server workspace host.
    /// </summary>
    public Uri BaseUrl { get; set; } = new("http://localhost:7147");

    /// <summary>
    /// Optional API key used to authenticate requests against the target workspace.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional bearer token used to authenticate requests against the target workspace.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the hosted-agent registration should require an API key or bearer token.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Optional workspace path header value used for multi-workspace routing.
    /// </summary>
    public string? WorkspacePath { get; set; }

    /// <summary>
    /// Optional privileged token used only for remote desktop-launch requests.
    /// </summary>
    public string? DesktopLaunchToken { get; set; }

    /// <summary>
    /// Stable identifier projected into <see cref="ChatClientAgentOptions.Id"/> for host-side agent construction.
    /// </summary>
    public string AgentId { get; set; } = McpHostedAgentDefaults.DefaultAgentId;

    /// <summary>
    /// Friendly host-facing name for the scaffolded hosted agent.
    /// </summary>
    public string AgentName { get; set; } = McpHostedAgentDefaults.DefaultAgentName;

    /// <summary>
    /// Optional human-readable description projected into <see cref="ChatClientAgentOptions.Description"/>.
    /// </summary>
    public string Description { get; set; } = McpHostedAgentDefaults.DefaultAgentDescription;

    /// <summary>
    /// Canonical source type reserved for later hosted-agent session-log workflow integration.
    /// </summary>
    public string SourceType { get; set; } = McpHostedAgentDefaults.DefaultSourceType;

    /// <summary>
    /// HTTP timeout applied to the transport client created for the hosted agent scaffold.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(300);

    internal McpServerClientOptions ToClientOptions(ILoggerFactory? loggerFactory = null)
        => new()
        {
            ApiKey = ApiKey,
            BearerToken = BearerToken,
            BaseUrl = BaseUrl,
            DesktopLaunchToken = DesktopLaunchToken,
            LoggerFactory = loggerFactory,
            Timeout = Timeout,
            WorkspacePath = WorkspacePath,
        };

    internal ChatClientAgentOptions ToAgentOptions()
        => new()
        {
            Description = Description,
            Id = AgentId,
            Name = AgentName,
        };
}

