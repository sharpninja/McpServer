using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QBrainAi.McpAgent.IntegrationTests;

/// <summary>
/// TR-MCP-QBRAIN-005: the legacy McpServer:McpAgent section binds through the options validator, which
/// requires a workspace path that is fully qualified on the running host. A fully qualified path differs
/// by host, so this is an integration test on every host (TR-MCP-USECASE-018 AC-006: no unit test depends
/// on the platform).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-QBRAIN-005; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved unchanged from QBrainAi.McpAgent.Tests (McpAgentOptionsSectionTests.cs, where it shared a file with the unit class McpAgentOptionsSectionTests) into the integration-owned QBrainAi.McpAgent.IntegrationTests, so the unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4; operator 2026-11-06: "Per-owner projects (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
public sealed class McpAgentOptionsLegacySectionHostTests
{
    /// <summary>
    /// TR-MCP-QBRAIN-005: McpServer:McpAgent still binds when the canonical section is absent.
    /// An in-memory configuration supplies the legacy section only, with a host-native workspace path.
    /// </summary>
    [Fact]
    public void AddQBrainAiMcpAgent_BindsLegacyMcpServerSection()
    {
        var workspacePath = OperatingSystem.IsWindows() ? @"Q:\fixture\McpServer" : "/var/lib/McpServer";
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
}
