using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-TRIM-001: existing assembly-qualified process API names remain resolvable.</summary>
public sealed class ProcessRunnerTypeForwardingTests
{
    /// <summary>Checks every relocated public type against the old assembly without running external processes.</summary>
    [Fact]
    public void ServicesAssembly_ForwardsAllPublicProcessTypes()
    {
        var assembly = typeof(SessionLogService).Assembly;
        var types = new[]
        {
            typeof(IProcessRunner), typeof(ProcessRunner), typeof(ProcessRunnerOptions),
            typeof(ProcessRunRequest), typeof(ProcessRunResult),
        };
        foreach (var type in types)
        {
            Assert.Contains(type, assembly.GetForwardedTypes());
            Assert.Same(type, Type.GetType($"{type.FullName}, {assembly.FullName}", throwOnError: true));
        }
    }
}
