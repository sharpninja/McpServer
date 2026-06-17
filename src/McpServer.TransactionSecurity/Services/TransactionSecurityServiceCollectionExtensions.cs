using System.Net.Http;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.Services;

/// <summary>Registers transaction-security keyserver, subscriber, and coordinator services.</summary>
public static class TransactionSecurityServiceCollectionExtensions
{
    /// <summary>Registers the in-memory keyserver services used by the keyserver host and Support.Mcp compatibility host.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration source.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddTransactionKeyServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<KeyServerOptions>(configuration.GetSection(KeyServerOptions.SectionName));
        services.PostConfigure<KeyServerOptions>(HydrateKeyServerKeyMaterial);
        services.TryAddSingleton<ITransactionManifestCanonicalizer, TransactionManifestCanonicalizer>();
        services.TryAddSingleton<ITransactionDiffgramProtector, TransactionDiffgramProtector>();
        services.AddSingleton<InMemoryKeyServerService>();
        services.AddSingleton<IKeyServerPartyRegistry>(sp => sp.GetRequiredService<InMemoryKeyServerService>());
        services.AddSingleton<IKeyServerManifestService>(sp => sp.GetRequiredService<InMemoryKeyServerService>());
        return services;
    }

    /// <summary>Registers an in-memory subscriber that verifies manifests through in-process keyserver services.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration source.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInProcessTransactionSubscriber(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SubscriberOptions>(configuration.GetSection(SubscriberOptions.SectionName));
        services.PostConfigure<SubscriberOptions>(HydrateSubscriberKeyMaterial);
        services.TryAddSingleton<ITransactionManifestCanonicalizer, TransactionManifestCanonicalizer>();
        services.TryAddSingleton<ITransactionDiffgramProtector, TransactionDiffgramProtector>();
        services.AddSubscriberMessageLog();
        services.AddSingleton<ISubscriberCommitService, InMemorySubscriberCommitService>();
        services.AddTransactionPubSub();
        return services;
    }

    /// <summary>Registers a subscriber that verifies manifests through a separate keyserver over HTTP.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration source.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHttpTransactionSubscriber(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<SubscriberOptions>(configuration.GetSection(SubscriberOptions.SectionName));
        services.PostConfigure<SubscriberOptions>(HydrateSubscriberKeyMaterial);
        services.TryAddSingleton<ITransactionManifestCanonicalizer, TransactionManifestCanonicalizer>();
        services.TryAddSingleton<ITransactionDiffgramProtector, TransactionDiffgramProtector>();
        services.AddHttpClient<IKeyServerManifestService, HttpKeyServerManifestService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SubscriberOptions>>().Value;
            client.BaseAddress = new Uri(options.KeyServerBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.CommitTimeoutSeconds));
        });
        services.AddSubscriberMessageLog();
        services.AddSingleton<ISubscriberCommitService, InMemorySubscriberCommitService>();
        services.AddTransactionPubSub();
        return services;
    }

    /// <summary>Registers the first-slice in-process transaction-security stack used by Support.Mcp.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Configuration source.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInProcessTransactionSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddTransactionKeyServer(configuration);
        services.AddInProcessTransactionSubscriber(configuration);
        services.Configure<TurnTransactionOptions>(configuration.GetSection(TurnTransactionOptions.SectionName));
        services.AddSingleton<ITransactionDegradedModePolicy, TransactionDegradedModePolicy>();
        services.AddSingleton<ITransactionAuditWriter, InMemoryTransactionAuditWriter>();
        services.AddSingleton<IDiffgramBuilder, JsonDiffgramBuilder>();
        services.AddSingleton<ITurnTransactionCoordinator, TurnTransactionCoordinator>();
        return services;
    }

    private static IServiceCollection AddSubscriberMessageLog(this IServiceCollection services)
    {
        // FR-MCP-SUBLOG-001: high-performance received-message sink. No-op unless Mcp:Subscriber:Parseable is enabled.
        services.AddHttpClient("subscriber-parseable");
        services.TryAddSingleton<ISubscriberMessageLog>(sp =>
        {
            var parseable = sp.GetRequiredService<IOptions<SubscriberOptions>>().Value.Parseable;
            if (!parseable.Enabled || string.IsNullOrWhiteSpace(parseable.Url))
                return new NoopSubscriberMessageLog();

            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("subscriber-parseable");
            return new ParseableSubscriberMessageLog(httpClient, parseable);
        });
        return services;
    }

    private static IServiceCollection AddTransactionPubSub(this IServiceCollection services)
    {
        services.TryAddSingleton<DirectSubscriberTransactionPubSub>();
        services.TryAddSingleton<ITransactionPubSubBrokerClient, ProcessTopicTransactionPubSubBrokerClient>();
        services.TryAddSingleton<ITransactionPubSubBrokerStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<TurnTransactionOptions>>().CurrentValue;
            return string.IsNullOrWhiteSpace(options.PubSubDatabasePath)
                ? new InMemoryTransactionPubSubBrokerStore()
                : new SqliteTransactionSecurityStateStore(options.PubSubDatabasePath);
        });
        services.AddHttpClient<HttpSubscriberTransactionPubSub>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<TurnTransactionOptions>>().CurrentValue;
            client.BaseAddress = new Uri(options.SubscriberBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.CommitTimeoutSeconds));
        });
        services.TryAddSingleton<ITransactionPubSub>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsMonitor<TurnTransactionOptions>>().CurrentValue;
            var inner = CreatePubSubTransport(sp, options);
            return options.DurablePubSubEnabled
                ? new DurableTransactionPubSub(
                    inner,
                    sp.GetRequiredService<ITransactionPubSubBrokerStore>(),
                    TimeSpan.FromSeconds(Math.Max(0, options.PubSubInProgressClaimLeaseSeconds)),
                    GetDurableTopicName(options),
                    GetDurableSubscriberId(options))
                : inner;
        });
        services.TryAddSingleton<ITransactionPubSubReplayService>(sp =>
        {
            var pubSub = sp.GetRequiredService<ITransactionPubSub>();
            return pubSub as ITransactionPubSubReplayService ?? new NoopTransactionPubSubReplayService();
        });
        return services;
    }

    private static ITransactionPubSub CreatePubSubTransport(IServiceProvider services, TurnTransactionOptions options)
        => options.PubSubTransport switch
        {
            TransactionPubSubTransport.Http => CreateHttpPubSubTransport(services, options),
            TransactionPubSubTransport.ExternalBroker => new ExternalBrokerTransactionPubSub(
                services.GetRequiredService<ITransactionPubSubBrokerClient>(),
                options.PubSubTopics,
                options.PubSubSubscribers),
            _ => services.GetRequiredService<DirectSubscriberTransactionPubSub>(),
        };

    private static ITransactionPubSub CreateHttpPubSubTransport(IServiceProvider services, TurnTransactionOptions options)
    {
        var configuredSubscribers = GetHttpSubscriberTargets(options);
        if (configuredSubscribers.Count <= 1)
            return services.GetRequiredService<HttpSubscriberTransactionPubSub>();

        var factory = services.GetRequiredService<IHttpClientFactory>();
        var subscribers = configuredSubscribers
            .Select(target =>
            {
                var client = factory.CreateClient();
                client.BaseAddress = new Uri(target.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.CommitTimeoutSeconds));
                return new FanOutTransactionPubSub.FanOutSubscriber(
                    target.SubscriberId,
                    target.Required,
                    new HttpSubscriberTransactionPubSub(client));
            })
            .ToArray();
        return new FanOutTransactionPubSub(subscribers);
    }

    private static IReadOnlyList<HttpSubscriberTarget> GetHttpSubscriberTargets(TurnTransactionOptions options)
    {
        if (options.PubSubSubscribers.Count > 0)
        {
            return options.PubSubSubscribers
                .Select((subscriber, index) => new HttpSubscriberTarget(
                    string.IsNullOrWhiteSpace(subscriber.SubscriberId) ? $"subscriber-{index + 1}" : subscriber.SubscriberId.Trim(),
                    string.IsNullOrWhiteSpace(subscriber.BaseUrl) ? options.SubscriberBaseUrl : subscriber.BaseUrl.Trim(),
                    subscriber.Required))
                .ToArray();
        }

        var urls = options.SubscriberBaseUrls
            .Append(options.SubscriberBaseUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (urls.Length == 0)
            urls = ["http://localhost:7168"];

        return urls
            .Select((url, index) => new HttpSubscriberTarget($"subscriber-{index + 1}", url, true))
            .ToArray();
    }

    private static string? GetDurableTopicName(TurnTransactionOptions options)
        => options.PubSubTransport == TransactionPubSubTransport.ExternalBroker
            ? options.PubSubTopics.CommitTopic
            : null;

    private static string? GetDurableSubscriberId(TurnTransactionOptions options)
        => options.PubSubSubscribers.Count == 1
            ? options.PubSubSubscribers[0].SubscriberId
            : null;

    private sealed record HttpSubscriberTarget(string SubscriberId, string BaseUrl, bool Required);

    /// <summary>
    /// Registers parties declared in <see cref="KeyServerOptions.ProvisionedParties"/>.
    /// </summary>
    /// <param name="services">Service provider containing keyserver services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes after all configured parties have been registered.</returns>
    public static async Task ProvisionConfiguredTransactionKeysAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.GetService<IOptions<KeyServerOptions>>()?.Value;
        if (options?.ProvisionedParties.Count is not > 0)
            return;

        var registry = services.GetRequiredService<IKeyServerPartyRegistry>();
        foreach (var party in options.ProvisionedParties)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await registry.RegisterPartyAsync(
                    new PartyRegistrationRequest
                    {
                        PartyId = party.PartyId,
                        Role = party.Role,
                        ActiveSigningKeyId = party.ActiveSigningKeyId,
                        ActiveEncryptionKeyId = party.ActiveEncryptionKeyId,
                        SigningPublicKeyPem = party.SigningPublicKeyPem,
                        SigningPrivateKeyPem = party.SigningPrivateKeyPem,
                        EncryptionPublicKeyPem = party.EncryptionPublicKeyPem,
                        Status = party.Status,
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static void HydrateKeyServerKeyMaterial(KeyServerOptions options)
    {
        foreach (var party in options.ProvisionedParties)
        {
            party.SigningPublicKeyPem = ReadPemIfConfigured(
                party.SigningPublicKeyPem,
                party.SigningPublicKeyPemFile,
                "keyserver provisioned signing public key");
            party.SigningPrivateKeyPem = ReadPemIfConfigured(
                party.SigningPrivateKeyPem,
                party.SigningPrivateKeyPemFile,
                "keyserver provisioned signing private key");
            party.EncryptionPublicKeyPem = ReadPemIfConfigured(
                party.EncryptionPublicKeyPem,
                party.EncryptionPublicKeyPemFile,
                "keyserver provisioned encryption public key");
        }
    }

    private static void HydrateSubscriberKeyMaterial(SubscriberOptions options)
    {
        options.EncryptionPrivateKeyPem = ReadPemIfConfigured(
            options.EncryptionPrivateKeyPem,
            options.EncryptionPrivateKeyPemFile,
            "subscriber encryption private key");

        foreach (var key in options.EncryptionKeys)
        {
            key.PrivateKeyPem = ReadPemIfConfigured(
                key.PrivateKeyPem,
                key.PrivateKeyPemFile,
                "subscriber encryption key-ring private key") ?? string.Empty;
        }
    }

    private static string? ReadPemIfConfigured(string? inlinePem, string? filePath, string description)
    {
        if (!string.IsNullOrWhiteSpace(inlinePem) || string.IsNullOrWhiteSpace(filePath))
            return inlinePem;

        var resolvedPath = filePath.Trim();
        if (!File.Exists(resolvedPath))
            throw new FileNotFoundException($"Configured {description} PEM file was not found.", resolvedPath);

        return File.ReadAllText(resolvedPath).Trim();
    }
}
