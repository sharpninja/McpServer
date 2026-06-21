using System.Security.Cryptography;
using System.Text;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace McpServer.Acid.IntegrationTests;

/// <summary>How an ACID transaction participant is provided to the harness.</summary>
internal enum AcidParticipantMode
{
    /// <summary>In-process test double / in-memory implementation (no external host).</summary>
    Mock,

    /// <summary>A real instance: an isolated host spun up for the test and disposed afterwards.</summary>
    Running,
}

/// <summary>
/// Selected modes for the coordinator's two collaborators. The coordinator itself is the system under test
/// and is always the real <see cref="TurnTransactionCoordinator"/>; the MCP Server's role (the caller that
/// invokes the coordinator) is mocked by the harness, which drives <c>ExecuteAsync</c> directly.
/// </summary>
/// <param name="KeyServer">Third-party key server mode.</param>
/// <param name="Subscriber">Subscriber mode.</param>
internal sealed record AcidParticipants(
    AcidParticipantMode KeyServer,
    AcidParticipantMode Subscriber)
{
    /// <summary>Baseline: both collaborators mocked in-process.</summary>
    public static AcidParticipants AllMock { get; } =
        new(AcidParticipantMode.Mock, AcidParticipantMode.Mock);

    /// <summary>Both collaborators backed by real spun-up instances.</summary>
    public static AcidParticipants AllRunning { get; } =
        new(AcidParticipantMode.Running, AcidParticipantMode.Running);
}

/// <summary>
/// FR-MCP-118..128 / TEST-MCP-ACID-001: Pluggable harness that exercises the complete ACID turn-transaction
/// lifecycle. The <see cref="TurnTransactionCoordinator"/> is the system under test and is always real; the
/// harness plays the MCP Server caller by driving the coordinator directly. The coordinator's two collaborators -
/// the third-party key server and the subscriber - are each provided as a mock (in-process implementation) or a
/// real running instance (an isolated host spun up for the test and torn down afterwards).
/// </summary>
internal sealed class AcidTransactionHarness : IDisposable
{
    /// <summary>Publisher party identifier used across the harness.</summary>
    public const string PublisherPartyId = "publisher-1";

    /// <summary>Subscriber party identifier used across the harness.</summary>
    public const string SubscriberPartyId = "subscriber-1";

    private readonly List<IDisposable> _disposables;
    private readonly IKeyServerManifestService _keyManifest;
    private readonly IKeyServerPartyRegistry _keyRegistry;
    private readonly ISubscriberCommitService _subscriber;

    private AcidTransactionHarness(
        AcidParticipants participants,
        IKeyServerManifestService keyManifest,
        IKeyServerPartyRegistry keyRegistry,
        ISubscriberCommitService subscriber,
        ITransactionPubSub pubSub,
        TurnTransactionOptions options,
        List<IDisposable> disposables)
    {
        Participants = participants;
        _keyManifest = keyManifest;
        _keyRegistry = keyRegistry;
        _subscriber = subscriber;
        _disposables = disposables;
        Options = options;
        Audit = new InMemoryTransactionAuditWriter();
        var monitor = new FixedOptionsMonitor<TurnTransactionOptions>(options);
        Coordinator = new TurnTransactionCoordinator(
            monitor,
            keyRegistry,
            keyManifest,
            pubSub,
            new JsonDiffgramBuilder(),
            new TransactionDegradedModePolicy(monitor),
            Audit);
    }

    /// <summary>Selected participant modes.</summary>
    public AcidParticipants Participants { get; }

    /// <summary>Effective turn-transaction options.</summary>
    public TurnTransactionOptions Options { get; }

    /// <summary>The MCP Server transaction coordinator under test.</summary>
    public TurnTransactionCoordinator Coordinator { get; }

    /// <summary>Captured transaction audit trail.</summary>
    public InMemoryTransactionAuditWriter Audit { get; }

    /// <summary>The active subscriber commit service (mock in-memory, or the running host service).</summary>
    public ISubscriberCommitService Subscriber => _subscriber;

    /// <summary>The active key server manifest service (mock in-memory, or HTTP-backed for a running host).</summary>
    public IKeyServerManifestService KeyServer => _keyManifest;

    /// <summary>
    /// Builds a harness for the given participant modes. The optional <paramref name="subscriberOverride"/> swaps
    /// the subscriber implementation (used to inject failure doubles in mock mode).
    /// </summary>
    public static AcidTransactionHarness Create(
        AcidParticipants participants,
        ISubscriberCommitService? subscriberOverride = null,
        bool enabled = true,
        bool degradedModeEnabled = true)
    {
        var disposables = new List<IDisposable>();
        var canonicalizer = new TransactionManifestCanonicalizer();

        // --- Key server participant ---
        IKeyServerManifestService keyManifest;
        IKeyServerPartyRegistry keyRegistry;

        if (participants.KeyServer == AcidParticipantMode.Mock)
        {
            var inMemoryKeyServer = new InMemoryKeyServerService(
                new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
                canonicalizer);
            disposables.Add(inMemoryKeyServer);
            keyManifest = inMemoryKeyServer;
            keyRegistry = inMemoryKeyServer;
        }
        else
        {
            var keyFactory = new WebApplicationFactory<KeyServerEntryPoint>();
            disposables.Add(keyFactory);
            var keyHttp = keyFactory.CreateClient();
            disposables.Add(keyHttp);
            keyManifest = new HttpKeyServerManifestService(keyHttp);
            keyRegistry = keyFactory.Services.GetRequiredService<IKeyServerPartyRegistry>();
        }

        // --- Subscriber participant + pub-sub transport ---
        ISubscriberCommitService subscriber;
        ITransactionPubSub pubSub;
        var transport = TransactionPubSubTransport.Direct;

        if (participants.Subscriber == AcidParticipantMode.Mock)
        {
            subscriber = subscriberOverride ?? new InMemorySubscriberCommitService(
                keyManifest,
                canonicalizer,
                new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions { PartyId = SubscriberPartyId }),
                new TransactionDiffgramProtector());
            if (subscriber is IDisposable disposableSubscriber)
                disposables.Add(disposableSubscriber);
            pubSub = new DirectSubscriberTransactionPubSub(subscriber);
        }
        else
        {
            transport = TransactionPubSubTransport.Http;
            var keyManifestForHost = keyManifest;
            var subscriberFactory = new WebApplicationFactory<SubscriberEntryPoint>()
                .WithWebHostBuilder(builder =>
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IKeyServerManifestService>();
                        services.AddSingleton(keyManifestForHost);
                    }));
            disposables.Add(subscriberFactory);
            var subscriberHttp = subscriberFactory.CreateClient();
            disposables.Add(subscriberHttp);
            subscriber = subscriberFactory.Services.GetRequiredService<ISubscriberCommitService>();
            pubSub = new HttpSubscriberTransactionPubSub(subscriberHttp);
        }

        var options = new TurnTransactionOptions
        {
            Enabled = enabled,
            RequiredForMutations = true,
            DegradedModeEnabled = degradedModeEnabled,
            PubSubTransport = transport,
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
        };

        return new AcidTransactionHarness(
            participants,
            keyManifest,
            keyRegistry,
            subscriber,
            pubSub,
            options,
            disposables);
    }

    /// <summary>Registers the standard publisher and subscriber parties with the active key server.</summary>
    public async Task RegisterPartiesAsync()
    {
        await _keyRegistry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" })
            .ConfigureAwait(true);
        await _keyRegistry.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = SubscriberPartyId, Role = "subscriber" })
            .ConfigureAwait(true);
    }

    /// <summary>Signs a manifest through the active key server (mock or running).</summary>
    public async Task<TransactionManifestDto> SignManifestAsync(string transactionId, long sequence, string nonce)
    {
        var response = await _keyManifest.SignManifestAsync(CreateSignRequest(transactionId, sequence, nonce))
            .ConfigureAwait(true);
        Assert.True(response.Success, $"manifest sign failed: {response.Reason}");
        Assert.NotNull(response.Manifest);
        return response.Manifest!;
    }

    /// <summary>Builds a sign request for the standard publisher/subscriber pair.</summary>
    public static TransactionManifestSignRequest CreateSignRequest(string transactionId, long sequence, string nonce)
        => new()
        {
            TransactionId = transactionId,
            TurnId = "turn-acid",
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        };

    /// <summary>Builds a commit request whose payload matches the signed manifest hashes.</summary>
    public static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    /// <summary>Lowercase hex SHA-256 of a UTF-8 string.</summary>
    public static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    /// <inheritdoc />
    public void Dispose()
    {
        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                _disposables[i].Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }
        }
    }
}

/// <summary>Fixed <see cref="IOptionsMonitor{TOptions}"/> that returns a constant value.</summary>
/// <typeparam name="TOptions">Options type.</typeparam>
internal sealed class FixedOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
    where TOptions : class
{
    /// <inheritdoc />
    public TOptions CurrentValue { get; } = currentValue;

    /// <inheritdoc />
    public TOptions Get(string? name) => CurrentValue;

    /// <inheritdoc />
    public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
}
