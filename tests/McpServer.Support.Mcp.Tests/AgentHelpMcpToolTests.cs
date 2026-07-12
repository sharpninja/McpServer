using McpServer.Support.Mcp.McpStdio;
using ModelContextProtocol.Server;

namespace McpServer.Support.Mcp.Tests;

/// <summary>
/// TEST-MCP-BUGTRIAGE-045: Validates native Agent Help MCP tool discovery names.
/// </summary>
public sealed class AgentHelpMcpToolTests
{
    /// <summary>Native MCP assembly discovery exposes create, submit, status, and transcript tool names.</summary>
    [Fact]
    public void FwhMcpTools_ExposesAgentHelpToolNames()
    {
        var toolNames = typeof(FwhMcpTools)
            .GetMethods()
            .Select(method => method.GetCustomAttributes(typeof(McpServerToolAttribute), inherit: true)
                .OfType<McpServerToolAttribute>()
                .FirstOrDefault()?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("agent_help_create_session", toolNames);
        Assert.Contains("agent_help_submit_turn", toolNames);
        Assert.Contains("agent_help_get_status", toolNames);
        Assert.Contains("agent_help_get_transcript", toolNames);
    }
}
