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
        services.TryAddSingleton<ITransactionManifestCanonicalizer, TransactionManifestCanonicalizer>();
        services.TryAddSingleton<ITransactionDiffgramProtector, TransactionDiffgramProtector>();
        services.AddSingleton<ISubscriberCommitService, InMemorySubscriberCommitService>();
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
        services.TryAddSingleton<ITransactionManifestCanonicalizer, TransactionManifestCanonicalizer>();
        services.TryAddSingleton<ITransactionDiffgramProtector, TransactionDiffgramProtector>();
        services.AddHttpClient<IKeyServerManifestService, HttpKeyServerManifestService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<SubscriberOptions>>().Value;
            client.BaseAddress = new Uri(options.KeyServerBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.CommitTimeoutSeconds));
        });
        services.AddSingleton<ISubscriberCommitService, InMemorySubscriberCommitService>();
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
}
