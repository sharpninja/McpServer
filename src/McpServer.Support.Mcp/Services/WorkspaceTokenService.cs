using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Generates and validates per-workspace auth tokens that rotate on every service restart.
/// Two token tiers are managed:
/// <list type="bullet">
///   <item><description><strong>Full-access tokens</strong> — published in the
///     <c>AGENTS-README-FIRST.yaml</c> marker file. Grant unrestricted access to all
///     <c>/mcp/*</c> endpoints.</description></item>
///   <item><description><strong>Default (anonymous) tokens</strong> — returned by the
///     unprotected <c>GET /api-key</c> endpoint. Grant <em>read-only</em> access to all
///     endpoints <strong>except</strong> TODO routes (<c>/mcp/todo*</c>), which are
///     read-write.</description></item>
/// </list>
/// Tokens are held in memory only (never persisted) and rotate on every service restart.
/// </summary>
public sealed class WorkspaceTokenService
{
    private const int TokenByteLength = 32;
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _defaultTokens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Generates a new cryptographic random full-access token for the given workspace and stores it.
    /// If a token already exists for the workspace it is replaced.
    /// </summary>
    /// <returns>The generated base64url token.</returns>
    public string GenerateToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        var token = MakeToken();
        _tokens[key] = token;
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
        _defaultTokens[key] = token;
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

    private static string MakeToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
