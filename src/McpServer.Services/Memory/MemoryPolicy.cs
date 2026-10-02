using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010-05: Secrets/policy rejection for memory payloads.
/// Keeps existing fail-closed behavior and applies it to remember and compat add/update.
/// </summary>
public static partial class MemoryPolicy
{
    /// <summary>Returns true when <paramref name="content"/> looks like a secret assignment.</summary>
    public static bool RejectsSecrets(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return SecretAssignmentRegex().IsMatch(content) || SkTokenRegex().IsMatch(content);
    }

    [GeneratedRegex(@"\b(?:api[_-]?key|secret|password|token)\s*[:=]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"\bsk-[A-Za-z0-9_-]{8,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SkTokenRegex();
}
