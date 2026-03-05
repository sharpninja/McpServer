namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-GH-002: Per-workspace GitHub token persistence contract.
/// </summary>
public interface IGitHubWorkspaceTokenStore
{
    /// <summary>
    /// Gets the stored token record for the specified workspace.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The token record, or <see langword="null"/> if none exists.</returns>
    Task<GitHubWorkspaceTokenRecord?> GetAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the stored token for the specified workspace.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="accessToken">OAuth or PAT token value.</param>
    /// <param name="expiresAtUtc">Optional token expiration timestamp.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(string workspacePath, string accessToken, DateTimeOffset? expiresAtUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes a stored token for the specified workspace.
    /// </summary>
    /// <param name="workspacePath">Workspace root path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when a token was removed; otherwise <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(string workspacePath, CancellationToken ct = default);
}

/// <summary>
/// TR-MCP-GH-002: Decrypted GitHub token record returned from token storage.
/// </summary>
/// <param name="WorkspacePath">Normalized workspace path key.</param>
/// <param name="AccessToken">Access token value.</param>
/// <param name="UpdatedAtUtc">Last update timestamp.</param>
/// <param name="ExpiresAtUtc">Optional expiration timestamp.</param>
public sealed record GitHubWorkspaceTokenRecord(
    string WorkspacePath,
    string AccessToken,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ExpiresAtUtc);
