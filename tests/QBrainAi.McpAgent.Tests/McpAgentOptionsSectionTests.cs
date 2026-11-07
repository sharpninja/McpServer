using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QBrainAi.McpAgent.Tests;

/// <summary>
/// TR-MCP-QBRAIN-005: McpAgent configuration section aliases.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-QBRAIN-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: unchanged; the integration class McpAgentOptionsLegacySectionHostTests, which shared this file, moved to the integration-owned QBrainAi.McpAgent.IntegrationTests, so this unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4).</remarks>
public sealed class McpAgentOptionsSectionTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-005: QBrainAi:McpAgent wins over McpServer:McpAgent, and an explicit delegate wins over both.
    /// </summary>
    [Fact]
    public void AddQBrainAiMcpAgent_CanonicalSectionAndDelegateWin()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["QBrainAi:McpAgent:ApiKey"] = "canonical-token",
            ["McpServer:McpAgent:ApiKey"] = "legacy-token",
            ["Mcp:McpAgent:ApiKey"] = "mcp-token",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddQBrainAiMcpAgent(options => options.ApiKey = "delegate-token");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<McpAgentOptions>>().Value;

        Assert.Equal("delegate-token", options.ApiKey);
    }
}
