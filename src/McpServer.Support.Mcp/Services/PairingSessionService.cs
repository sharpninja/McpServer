using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Issues and validates short-lived opaque tokens for the <c>/pair</c> web login session.
/// Tokens expire after one hour. Expired tokens are pruned on each new issue.
/// </summary>
internal sealed class PairingSessionService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();

    /// <summary>Creates a new random URL-safe token valid for one hour.</summary>
    public string CreateToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
                        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        _tokens[token] = DateTimeOffset.UtcNow.AddHours(1);
        PurgeExpired();
        return token;
    }

    /// <summary>Returns <c>true</c> if the token exists and has not expired.</summary>
    public bool Validate(string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        return _tokens.TryGetValue(token, out var exp) && exp > DateTimeOffset.UtcNow;
    }

    private void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _tokens.Where(x => x.Value < now).ToList())
            _tokens.TryRemove(kv.Key, out _);
    }
}
