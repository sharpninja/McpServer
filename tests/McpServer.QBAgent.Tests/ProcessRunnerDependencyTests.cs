using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.QBAgent.Tests;

/// <summary>TEST-MCP-TRIM-001: the agent uses shared process execution without server storage dependencies.</summary>
public sealed class ProcessRunnerDependencyTests
{
    /// <summary>Supplies existing public process API types whose namespace and signatures must remain stable.</summary>
    public static TheoryData<Type> ProcessTypes =>
    [
        typeof(IProcessRunner), typeof(ProcessRunner), typeof(ProcessRunnerOptions),
        typeof(ProcessRunRequest), typeof(ProcessRunResult),
    ];

    /// <summary>Checks shared assembly ownership without launching a process or invoking an agent.</summary>
    [Theory]
    [MemberData(nameof(ProcessTypes))]
    public void PublicProcessType_IsOwnedBySharedAgentCli(Type processType)
    {
        Assert.Equal("McpServer.Common.AgentCli", processType.Assembly.GetName().Name);
        Assert.Equal("McpServer.Support.Mcp.Services", processType.Namespace);
    }

    /// <summary>Inspects the built agent dependency manifest for transitive server and EF dependencies.</summary>
    [Fact]
    public void AgentDependencyManifest_ExcludesServerStorage()
    {
        using var dependencies = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "McpServer.QBAgent.deps.json")));
        var libraries = dependencies.RootElement.GetProperty("libraries")
            .EnumerateObject().Select(item => item.Name.Split('/')[0]).ToArray();
        Assert.DoesNotContain("McpServer.Services", libraries);
        Assert.DoesNotContain("McpServer.Storage", libraries);
        Assert.DoesNotContain(libraries, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
