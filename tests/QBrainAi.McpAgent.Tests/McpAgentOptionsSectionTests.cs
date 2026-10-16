using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QBrainAi.McpAgent.Tests;

/// <summary>
/// TR-MCP-QBRAIN-005: McpAgent configuration section aliases.
/// </summary>
public sealed class McpAgentOptionsSectionTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-005: McpServer:McpAgent still binds when the canonical section is absent.
    /// An in-memory configuration supplies the legacy section only.
    /// </summary>
    [Fact]
    public void AddQBrainAiMcpAgent_BindsLegacyMcpServerSection()
    {
        var workspacePath = OperatingSystem.IsWindows() ? @"E:\github\McpServer" : "/var/lib/McpServer";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["McpServer:McpAgent:ApiKey"] = "legacy-token",
            ["McpServer:McpAgent:WorkspacePath"] = workspacePath,
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddQBrainAiMcpAgent();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<McpAgentOptions>>().Value;

        Assert.Equal("legacy-token", options.ApiKey);
        Assert.Equal(workspacePath, options.WorkspacePath);
    }

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
