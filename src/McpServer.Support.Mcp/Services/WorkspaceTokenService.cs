using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Generates and validates per-workspace auth tokens that rotate on every service restart.
/// Tokens are held in memory only (never persisted) and auto-discovered by agents
/// via the <c>ApiKey</c> field in the <c>AGENTS-README-FIRST.yaml</c> marker file.
/// </summary>
public sealed class WorkspaceTokenService
{
    private const int TokenByteLength = 32;
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Generates a new cryptographic random token for the given workspace and stores it.
    /// If a token already exists for the workspace it is replaced.
    /// </summary>
    /// <returns>The generated base64url token.</returns>
    public string GenerateToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenByteLength))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        _tokens[key] = token;
        return token;
    }

    /// <summary>
    /// Validates that <paramref name="candidate"/> matches the current token for the workspace.
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

    /// <summary>Returns the current token for the workspace, or <c>null</c> if none exists.</summary>
    public string? GetToken(string workspacePath)
    {
        var key = Normalize(workspacePath);
        return _tokens.TryGetValue(key, out var token) ? token : null;
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
