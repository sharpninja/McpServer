using System.Collections.Generic;
using McpServer.Support.Mcp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.SpecFlow.Tests.Support;

/// <summary>Web application factory for SpecFlow integration tests.</summary>
public sealed class McpSpecFlowWebApplicationFactory : WebApplicationFactory<McpApiEntryPoint>
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
