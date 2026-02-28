using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Generates and validates per-workspace auth tokens that rotate on every service restart.
/// Two token tiers are managed:
/// <list type="bullet">
///   <item><description><strong>Full-access tokens</strong> — published in the
///     <c>AGENTS-README-FIRST.yaml</c> marker file. Grant unrestricted access to all
///     <c>/mcpserver/*</c> endpoints.</description></item>
///   <item><description><strong>Default (anonymous) tokens</strong> — returned by the
///     unprotected <c>GET /api-key</c> endpoint. Grant <em>read-only</em> access to all
///     endpoints <strong>except</strong> TODO routes (<c>/mcpserver/todo*</c>), which are
///     read-write.</description></item>
/// </list>
/// Tokens are held in memory only (never persisted) and rotate on every service restart.
/// </summary>
public sealed class WorkspaceTokenService
{
    private const int TokenByteLength = 32;
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _defaultTokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _tokenToWorkspace = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _defaultTokenToWorkspace = new(StringComparer.Ordinal);

    /// <summary>
    /// Generates a new cryptographic random full-access token for the given workspace and stores it.
    /// If a token already exists for the workspace it is replaced.
    /// </summary>
    /// <returns>The generated base64url token.</returns>
    public string GenerateToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        var token = MakeToken();

        // Remove old reverse mapping if a previous token exists
        if (_tokens.TryGetValue(key, out var oldToken))
            _tokenToWorkspace.TryRemove(oldToken, out _);

        _tokens[key] = token;
        _tokenToWorkspace[token] = key;
        return token;
    }

    /// <summary>
    /// Validates that <paramref name="candidate"/> matches the current full-access token for the workspace.
    /// Returns <c>false</c> if no token has been generated for the workspace.
    /// </summary>
    public bool ValidateToken(string workspacePath, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var key = Normalize(workspacePath);
        return _tokens.TryGetValue(key, out var expected)
               && string.Equals(expected, candidate, StringComparison.Ordinal);
    }

    /// <summary>Returns the current full-access token for the workspace, or <c>null</c> if none exists.</summary>
    public string? GetToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        return _tokens.TryGetValue(key, out var token) ? token : null;
    }

    /// <summary>
    /// Generates a new default (anonymous) token for the given workspace and stores it.
    /// If a default token already exists for the workspace it is replaced.
    /// Default tokens grant read-only access to all endpoints except TODO routes which are read-write.
    /// </summary>
    /// <returns>The generated base64url token.</returns>
    public string GenerateDefaultToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        var token = MakeToken();

        // Remove old reverse mapping if a previous default token exists
        if (_defaultTokens.TryGetValue(key, out var oldToken))
            _defaultTokenToWorkspace.TryRemove(oldToken, out _);

        _defaultTokens[key] = token;
        _defaultTokenToWorkspace[token] = key;
        return token;
    }

    /// <summary>
    /// Validates that <paramref name="candidate"/> matches the current default (anonymous)
    /// token for the workspace. Returns <c>false</c> if no default token exists.
    /// </summary>
    public bool ValidateDefaultToken(string workspacePath, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var key = Normalize(workspacePath);
        return _defaultTokens.TryGetValue(key, out var expected)
               && string.Equals(expected, candidate, StringComparison.Ordinal);
    }

    /// <summary>Returns the current default (anonymous) token for the workspace, or <c>null</c> if none exists.</summary>
    public string? GetDefaultToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        return _defaultTokens.TryGetValue(key, out var token) ? token : null;
    }

    /// <summary>
    /// Checks whether the given <paramref name="candidate"/> is a default (anonymous) token
    /// for the specified workspace. Useful for middleware to determine the access tier.
    /// </summary>
    public bool IsDefaultToken(string workspacePath, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var key = Normalize(workspacePath);
        return _defaultTokens.TryGetValue(key, out var expected)
               && string.Equals(expected, candidate, StringComparison.Ordinal);
    }

    /// <summary>
    /// TR-MCP-MT-002: Resolves a workspace path from a token (full-access or default).
    /// Returns <c>null</c> if the token is unknown.
    /// Also indicates via <paramref name="isDefault"/> whether the matched token is a default (anonymous) token.
    /// </summary>
    public string? ResolveWorkspaceByToken(string? token, out bool isDefault)
    {
        isDefault = false;

        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (_tokenToWorkspace.TryGetValue(token, out var fullPath))
            return fullPath;

        if (_defaultTokenToWorkspace.TryGetValue(token, out var defaultPath))
        {
            isDefault = true;
            return defaultPath;
        }

        return null;
    }

    /// <summary>
    /// TR-MCP-MT-002: Resolves a workspace path from a token (full-access or default).
    /// Returns <c>null</c> if the token is unknown.
    /// </summary>
    public string? ResolveWorkspaceByToken(string? token)
        => ResolveWorkspaceByToken(token, out _);

    private static string MakeToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
