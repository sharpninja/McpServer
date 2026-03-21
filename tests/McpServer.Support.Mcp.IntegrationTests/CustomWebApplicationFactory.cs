using System.Collections.Generic;
using System.IO;
using McpServer.Support.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>TR-PLANNED-013: Web application factory for MCP API integration tests.</summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
{
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

    /// <summary>Initializes a new instance with no service overrides.</summary>
    public CustomWebApplicationFactory() : this(null, null) { }

    /// <summary>Initializes a new instance with optional service overrides.</summary>
    /// <param name="configureServices">Optional callback to register additional or replacement services.</param>
    /// <param name="configurationOverrides">Optional configuration values injected before startup binding.</param>
    internal CustomWebApplicationFactory(
        Action<IServiceCollection>? configureServices,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _configureServices = configureServices;
        _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseContentRoot(ResolveContentRoot());
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mcp:DataSource", ":memory:" }
            });

            if (_configurationOverrides.Count > 0)
                config.AddInMemoryCollection(_configurationOverrides);
        });

        if (_configureServices is not null)
            builder.ConfigureServices(_configureServices);
    }

    internal static string ResolveContentRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "McpServer.sln");
            if (File.Exists(solutionPath))
                return Path.Combine(current.FullName, "src", "McpServer.Support.Mcp");

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the solution root for McpServer.Support.Mcp integration tests.");
    }
}
