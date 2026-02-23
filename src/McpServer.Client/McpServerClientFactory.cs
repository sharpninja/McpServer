using System;
using System.Net.Http;

namespace McpServer.Client;

/// <summary>
/// Factory for creating <see cref="McpServerClient"/> instances without dependency injection.
///
/// <para>Use <see cref="Create(McpServerClientOptions)"/> when you want the factory to own
/// the <see cref="HttpClient"/> lifetime, or <see cref="Create(HttpClient, McpServerClientOptions)"/>
/// when you already have a managed <see cref="HttpClient"/> (e.g. from <c>IHttpClientFactory</c>).</para>
///
/// <para><strong>API key:</strong> The key can be supplied via
/// <see cref="McpServerClientOptions.ApiKey"/> at creation time, or set later via
/// <see cref="McpServerClient.ApiKey"/>. Authentication is validated at call time, not at
/// construction.</para>
/// </summary>
/// <example>
/// <code>
/// var client = McpServerClientFactory.Create(new McpServerClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147"),
///     ApiKey = "my-workspace-token"
/// });
/// </code>
/// </example>
/// <seealso cref="McpServerClient"/>
/// <seealso cref="ServiceCollectionExtensions"/>
public static class McpServerClientFactory
{
    /// <summary>
    /// Creates a new <see cref="McpServerClient"/> with a factory-managed <see cref="HttpClient"/>.
    /// The <see cref="HttpClient.Timeout"/> is set from <see cref="McpServerClientOptions.Timeout"/>.
    /// </summary>
    /// <param name="options">
    /// Configuration supplying <see cref="McpServerClientOptions.BaseUrl"/> and optional
    /// <see cref="McpServerClientOptions.ApiKey"/> seed value.
    /// </param>
    /// <returns>A fully-initialized <see cref="McpServerClient"/> facade.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static McpServerClient Create(McpServerClientOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        var http = new HttpClient
        {
            BaseAddress = options.BaseUrl,
            Timeout = options.Timeout,
        };

        return new McpServerClient(http, options);
    }

    /// <summary>
    /// Creates a new <see cref="McpServerClient"/> using an existing <see cref="HttpClient"/>.
    /// The caller retains ownership and is responsible for disposing <paramref name="http"/>.
    /// </summary>
    /// <param name="http">Pre-configured <see cref="HttpClient"/> instance.</param>
    /// <param name="options">
    /// Configuration supplying <see cref="McpServerClientOptions.BaseUrl"/> and optional
    /// <see cref="McpServerClientOptions.ApiKey"/> seed value.
    /// </param>
    /// <returns>A fully-initialized <see cref="McpServerClient"/> facade.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="http"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static McpServerClient Create(HttpClient http, McpServerClientOptions options)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return new McpServerClient(http, options);
    }
}
