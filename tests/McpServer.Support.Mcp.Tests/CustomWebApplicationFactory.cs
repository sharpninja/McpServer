using System.Collections.Generic;
using McpServer.Support.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests;

/// <summary>TR-PLANNED-013: Web application factory for MCP API integration tests.</summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
{
    private readonly Action<IServiceCollection>? _configureServices;

    /// <summary>Initializes a new instance with no service overrides.</summary>
    public CustomWebApplicationFactory() : this(null) { }

    /// <summary>Initializes a new instance with optional service overrides.</summary>
    /// <param name="configureServices">Optional callback to register additional or replacement services.</param>
    internal CustomWebApplicationFactory(Action<IServiceCollection>? configureServices)
    {
        _configureServices = configureServices;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Mcp:DataSource", ":memory:" }
            });
        });

        if (_configureServices is not null)
            builder.ConfigureServices(_configureServices);
    }
}
