using System.Collections.Generic;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Configuration that gates privileged desktop-launch access,
/// remote authorization, and executable allowlisting.
/// </summary>
public sealed class DesktopLaunchOptions
{
    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Configuration section containing desktop-launch controls.
    /// </summary>
    public const string SectionName = "Mcp:DesktopLaunch";

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: HTTP header carrying the privileged desktop-launch token.
    /// </summary>
    public const string AccessTokenHeaderName = "X-Desktop-Launch-Token";

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Gets or sets a value indicating whether desktop launch is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Gets or sets the privileged HTTP token required for
    /// remote desktop-launch requests.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Gets or sets the executable allowlist patterns that
    /// remote and local desktop-launch requests must match.
    /// </summary>
    public List<string> AllowedExecutables { get; set; } = [];
}
