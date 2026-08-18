using System.Security.Cryptography;
using System.Text;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-MODES-001: Provider-portable replay identity for durable unique enforcement.</summary>
public static class HandoffReplayKeys
{
    /// <summary>
    /// Builds a fixed-length SHA-256 replay identity over a length-prefixed canonical payload.
    /// Force includes the run id so a new durable row is allowed; non-force ignores run id.
    /// </summary>
    public static string Create(string workspacePath, string contentSha256, string promptVersion, bool force, string runId)
    {
        var payload = new StringBuilder(256);
        payload.Append("v1");
        AppendPart(payload, workspacePath);
        AppendPart(payload, contentSha256);
        AppendPart(payload, promptVersion);
        AppendPart(payload, force ? runId ?? string.Empty : string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));
    }

    private static void AppendPart(StringBuilder payload, string? value)
    {
        value ??= string.Empty;
        payload.Append('\n');
        payload.Append(value.Length);
        payload.Append(':');
        payload.Append(value);
    }
}
