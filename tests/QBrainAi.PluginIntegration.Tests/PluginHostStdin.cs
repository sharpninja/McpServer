using System.Text;

namespace QBrainAi.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC-003 (PLAN-SESSIONTRIAGEFIX-001 E4, triage-report-fbac91dc): the one stdin encoding the harness gives every
/// plugin host process (NodeMcpStdioSession for the Node MCP stdio host, RealPluginProcessRunner for the others), UTF-8 without a byte
/// order mark, so the first JSON-RPC line a host reads is plain JSON.
/// </summary>
internal static class PluginHostStdin
{
    /// <summary>UTF-8 that writes no preamble; Encoding.UTF8 writes EF BB BF before the first line on a pipe.</summary>
    internal static Encoding Encoding { get; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
