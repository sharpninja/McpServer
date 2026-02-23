using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Client;

/// <summary>DI extension methods for registering <see cref="McpServerClient"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>The named HttpClient identifier used by the MCP Server client.</summary>
    public const string HttpClientName = "McpServerClient";

    /// <summary>
    /// Registers <see cref="McpServerClient"/> in the DI container with a named
    /// <see cref="System.Net.Http.HttpClient"/> via <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="McpServerClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServerClient(
        this IServiceCollection services,
        Action<McpServerClientOptions> configure)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        services.Configure(configure);

        services.AddHttpClient(HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<McpServerClientOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = options.Timeout;
        });

        services.AddTransient(sp =>
        {
            var factory = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>();
            var http = factory.CreateClient(HttpClientName);
            var options = sp.GetRequiredService<IOptions<McpServerClientOptions>>().Value;
            return new McpServerClient(http, options);
        });

        return services;
    }
}
