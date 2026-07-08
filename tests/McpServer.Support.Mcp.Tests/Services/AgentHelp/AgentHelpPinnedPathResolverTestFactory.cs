using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

/// <summary>
/// TEST-MCP-HELP-009: Factory for constructing pinned path resolvers in unit tests.
/// </summary>
internal static class AgentHelpPinnedPathResolverTestFactory
{
    /// <summary>Creates a resolver with optional in-memory configuration overrides.</summary>
    public static AgentHelpPinnedPathResolver Create(IReadOnlyDictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var hostEnvironment = new TestHostEnvironment();
        return new AgentHelpPinnedPathResolver(configuration, hostEnvironment, services);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "McpServer.Support.Mcp.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}