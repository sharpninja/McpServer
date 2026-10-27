using System;
using System.Net.Http;

namespace QBrainAi.Client;

/// <summary>
/// Factory for creating <see cref="QBrainAiClient"/> instances without dependency injection.
///
/// <para>Use <see cref="Create(QBrainAiClientOptions)"/> when you want the factory to own
/// the <see cref="HttpClient"/> lifetime, or <see cref="Create(HttpClient, QBrainAiClientOptions)"/>
/// when you already have a managed <see cref="HttpClient"/> (e.g. from <c>IHttpClientFactory</c>).</para>
///
/// <para><strong>API key:</strong> The key can be supplied via
/// <see cref="QBrainAiClientOptions.ApiKey"/> at creation time, or set later via
/// <see cref="QBrainAiClient.ApiKey"/>. Authentication is validated at call time, not at
/// construction.</para>
/// </summary>
/// <example>
/// <code>
/// var client = QBrainAiClientFactory.Create(new QBrainAiClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147"),
///     ApiKey = "my-workspace-token"
/// });
/// </code>
/// </example>
/// <seealso cref="QBrainAiClient"/>
/// <seealso cref="ServiceCollectionExtensions"/>
public static class QBrainAiClientFactory
{
    /// <summary>
    /// Creates a new <see cref="QBrainAiClient"/> with a factory-managed <see cref="HttpClient"/>.
    /// The <see cref="HttpClient.Timeout"/> is set from <see cref="QBrainAiClientOptions.Timeout"/>.
    /// </summary>
    /// <param name="options">
    /// Configuration supplying <see cref="QBrainAiClientOptions.BaseUrl"/> and optional
    /// <see cref="QBrainAiClientOptions.ApiKey"/> seed value.
    /// </param>
    /// <returns>A fully-initialized <see cref="QBrainAiClient"/> facade.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static QBrainAiClient Create(QBrainAiClientOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        var http = new HttpClient
        {
            BaseAddress = options.BaseUrl,
            Timeout = options.Timeout,
        };

        return new QBrainAiClient(http, options);
    }

    /// <summary>
    /// Creates a new <see cref="QBrainAiClient"/> using an existing <see cref="HttpClient"/>.
    /// The caller retains ownership and is responsible for disposing <paramref name="http"/>.
    /// </summary>
    /// <param name="http">Pre-configured <see cref="HttpClient"/> instance.</param>
    /// <param name="options">
    /// Configuration supplying <see cref="QBrainAiClientOptions.BaseUrl"/> and optional
    /// <see cref="QBrainAiClientOptions.ApiKey"/> seed value.
    /// </param>
    /// <returns>A fully-initialized <see cref="QBrainAiClient"/> facade.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="http"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static QBrainAiClient Create(HttpClient http, QBrainAiClientOptions options)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));
        return new QBrainAiClient(http, options);
    }
}
