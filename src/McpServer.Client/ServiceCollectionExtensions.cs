using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Client;

/// <summary>
/// Extension methods for registering <see cref="McpServerClient"/> and its dependencies in
/// a Microsoft.Extensions.DependencyInjection <see cref="IServiceCollection"/>.
///
/// <para>A named <see cref="System.Net.Http.HttpClient"/> (see <see cref="HttpClientName"/>)
/// is configured via <c>IHttpClientFactory</c> so that socket exhaustion and DNS recycling
/// are handled automatically.</para>
/// </summary>
/// <example>
/// <code>
/// services.AddMcpServerClient(opts =>
/// {
///     opts.BaseUrl = new Uri("http://localhost:7147");
///     opts.ApiKey  = configuration["Mcp:ApiKey"];
/// });
/// </code>
/// </example>
/// <seealso cref="McpServerClient"/>
/// <seealso cref="McpServerClientOptions"/>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The named <see cref="System.Net.Http.HttpClient"/> identifier registered by
    /// <see cref="AddMcpServerClient"/>. Use this constant when you need to configure
    /// the underlying HTTP client via <c>IHttpClientFactory</c> policies (e.g. retries, timeouts).
    /// </summary>
    public const string HttpClientName = "McpServerClient";

    /// <summary>
    /// Registers <see cref="McpServerClient"/> as a transient service backed by a named
    /// <see cref="System.Net.Http.HttpClient"/> via <c>IHttpClientFactory</c>.
    /// Each resolution creates a new <see cref="McpServerClient"/> instance with the
    /// latest <see cref="McpServerClientOptions"/> values.
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <param name="configure">
    /// Action to configure <see cref="McpServerClientOptions"/>. Called once at registration
    /// time; the resulting options are snapshot-bound via <c>IOptions&lt;T&gt;</c>.
    /// </param>
    /// <returns>The <paramref name="services"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
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
