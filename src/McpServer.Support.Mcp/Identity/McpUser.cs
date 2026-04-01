using Microsoft.AspNetCore.Identity;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Application user for IdentityServer authentication.
/// </summary>
public sealed class McpUser : IdentityUser
{
    /// <summary>Display name shown in tokens and UI.</summary>
    public string? DisplayName { get; set; }
}
