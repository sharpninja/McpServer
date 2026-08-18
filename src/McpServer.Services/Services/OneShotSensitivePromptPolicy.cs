using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-HANDOFF-AGENT-001 / TR-HANDOFF-AUDIT-001: Executor receives the raw prompt.
/// Queue state, DTOs, notifications, logs, and persisted jobs expose only a redacted placeholder.
/// </summary>
public static class OneShotSensitivePromptPolicy
{
    /// <summary>Stable published placeholder prefix.</summary>
    public const string RedactedPrefix = "[REDACTED:handoff-source:";

    /// <summary>True when the one-shot context must not retain raw source text.</summary>
    public static bool MustRedact(AgentPoolOneShotContext? context)
        => context == AgentPoolOneShotContext.HandoffTodoDraft;

    /// <summary>Returns the published placeholder, or the original text when retention is allowed.</summary>
    public static string Publish(AgentPoolOneShotContext? context, string? rawPrompt)
    {
        if (!MustRedact(context))
            return rawPrompt ?? string.Empty;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawPrompt ?? string.Empty))).ToLowerInvariant();
        return $"{RedactedPrefix}{hash}]";
    }

    /// <summary>True when published text still contains the raw source.</summary>
    public static bool ContainsRawSource(string? published, string? rawPrompt)
        => !string.IsNullOrEmpty(rawPrompt)
           && !string.IsNullOrEmpty(published)
           && published.Contains(rawPrompt, StringComparison.Ordinal);
}
