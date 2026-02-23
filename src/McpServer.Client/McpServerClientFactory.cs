using System;
using System.Net.Http;

namespace McpServer.Client;

/// <summary>Factory for creating <see cref="McpServerClient"/> instances without DI.</summary>
public static class McpServerClientFactory
{
    /// <summary>
    /// Creates a new <see cref="McpServerClient"/> with the specified options.
    /// The caller is responsible for the lifetime of the underlying <see cref="HttpClient"/>.
    /// </summary>
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

    /// <summary>Creates a new <see cref="McpServerClient"/> using an existing <see cref="HttpClient"/>.</summary>
    public static McpServerClient Create(HttpClient http, McpServerClientOptions options)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return new McpServerClient(http, options);
    }
}
