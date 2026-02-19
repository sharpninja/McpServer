using System.Collections.Generic;
using McpServer.Support.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Tests;

/// <summary>TR-PLANNED-013: Web application factory for MCP API integration tests.</summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
{
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
    }
}
