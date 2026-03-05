using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for public auth configuration.</summary>
public interface IAuthConfigApiClient
{
    /// <summary>Gets the server's public auth/OIDC configuration.</summary>
    Task<AuthConfigSnapshot> GetAuthConfigAsync(CancellationToken cancellationToken = default);
}
