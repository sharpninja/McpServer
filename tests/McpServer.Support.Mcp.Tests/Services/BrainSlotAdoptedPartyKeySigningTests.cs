using System.Security.Cryptography;
using System.Text;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-SEC-006: Proves a brain slot whose party id was renamed by the Creativity/Logic migration can actually
/// SIGN, not merely report ready, after <see cref="BrainSlotPartyKeyReconciler"/> provisions its party.
/// </summary>
/// <remarks>
/// <para>
/// History worth keeping, because it is the reason these tests exist. The reconciler originally copied the legacy
/// party's PUBLIC signing PEM onto the renamed party. That is the one input guaranteed to produce an unusable
/// party: <c>KeyServerPartyKeyEntity</c> has no private-key column, so <c>GetPartyKeyAsync</c> can only ever return
/// the public half, and <see cref="InMemoryKeyServerService"/> retains a private handle only for a pair it
/// generated itself. Registering with a public PEM installed an EMPTY private-key set, so readiness went green
/// while every turn transaction was rejected with <see cref="TransactionFailureReason.UnknownKey"/>, after the
/// model provider had already been called and billed. Worse, persisting that row suppressed the coordinator's own
/// self-heal, which mints a working pair when no key row exists.
/// </para>
/// <para>
/// The corrected reconciler registers the renamed party with NO key material, so the keyserver mints a real pair
/// and keeps the private half. Verification of historical manifests is unaffected: a manifest carries its own
/// <c>PublisherPartyId</c> and verification resolves the public key from that legacy party, which the reconciler
/// deliberately leaves intact. Fixtures wire a real <see cref="InMemoryKeyServerService"/>, a real
/// <see cref="TurnTransactionCoordinator"/>, and a real <see cref="InMemorySubscriberCommitService"/> to an
/// in-memory <see cref="McpDbContext"/>, so no signature or key material is faked.
/// </para>
/// </remarks>
public sealed class BrainSlotAdoptedPartyKeySigningTests
{
    private const string CreativityPartyId = "brain-slot:creativity";
    private const string LegacyCreativityPartyId = "brain-slot:left-hemisphere";
    private const string SubscriberPartyId = "subscriber-1";

    /// <summary>
    /// TR-MCP-SEC-006: A renamed brain-slot party reports ready AND signs. The turn transaction that
    /// <see cref="BrainSlotInvocationService"/> publishes under <c>brain-slot:creativity</c> commits, proving the
    /// reconciler provisions a party holding a usable private key rather than a public-only descriptor.
    /// </summary>
    [Fact]
    public async Task RenamedPartyAfterReconciliation_ReportsReadyAndSignsTurnTransaction()
    {
        using var fixture = SigningFixture.Create();
        await fixture.RegisterLegacyPartyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        await fixture.RegisterSubscriberPartyAsync().ConfigureAwait(true);
        await fixture.AddRenamedSlotAsync("creativity", BrainSlotRoles.Creativity, CreativityPartyId).ConfigureAwait(true);

        var status = await fixture.Registry.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(status.RoleReadiness[BrainSlotRoles.Creativity]);
        Assert.NotNull(await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true));

        var result = await fixture.Coordinator.ExecuteAsync(
                BrainSlotTransactionRequest(CreativityPartyId),
                _ => Task.FromResult(new TurnMutationResult { Success = true }),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.True(result.MutationApplied);
        Assert.NotNull(result.DiffgramId);
    }

    /// <summary>
    /// TR-MCP-SEC-006: The legacy party and its key survive the rename, so a manifest signed under the legacy
    /// publisher id before the migration still verifies afterwards. This is why the reconciler copies nothing and
    /// deletes nothing: historical verification resolves the public key from the manifest's own PublisherPartyId.
    /// </summary>
    [Fact]
    public async Task LegacyPartyKey_StillVerifiesHistoricalManifestsAfterTheRename()
    {
        using var fixture = SigningFixture.Create();
        await fixture.RegisterLegacyPartyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        await fixture.RegisterSubscriberPartyAsync().ConfigureAwait(true);
        await fixture.AddRenamedSlotAsync("creativity", BrainSlotRoles.Creativity, CreativityPartyId).ConfigureAwait(true);
        await fixture.Registry.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var legacyKey = await fixture.GetSigningKeyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        Assert.NotNull(legacyKey);
        Assert.Equal("active", legacyKey!.Status, ignoreCase: true);

        var signed = await fixture.KeyServer.SignManifestAsync(
                new TransactionManifestSignRequest
                {
                    TransactionId = "sec-006-legacy-signature",
                    PublisherPartyId = LegacyCreativityPartyId,
                    SubscriberPartyId = SubscriberPartyId,
                    Sequence = 1,
                    Nonce = "sec-006-legacy-signature:1",
                    DiffgramSha256 = "plain",
                    EncryptedBodySha256 = "encrypted",
                },
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(signed.Success);
        Assert.NotNull(signed.Manifest?.Signature);
        var payload = Encoding.UTF8.GetBytes(fixture.Canonicalizer.CanonicalizeUnsigned(signed.Manifest!));
        var signature = Convert.FromBase64String(signed.Manifest!.Signature!.Value);
        using var legacyPublicKey = ECDsa.Create();
        legacyPublicKey.ImportFromPem(legacyKey.PublicKeyPem);

        Assert.True(legacyPublicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256));
    }

    /// <summary>
    /// TR-MCP-SEC-006 control case: with NO key row for the renamed party the coordinator's own
    /// <c>EnsureDefaultPartiesAsync</c> mints a fresh signing pair, signs, and the subscriber commits after
    /// verifying the signature. This proves the harness can reach "committed" and that the rejection in
    /// <see cref="AdoptedLegacySigningKey_ReportsReadyButCannotSignTurnTransaction"/> is caused by the adopted
    /// public-only row suppressing that self-heal.
    /// </summary>
    [Fact]
    public async Task PartyWithNoAdoptedKeyRow_SelfHealsAndCommitsTurnTransaction()
    {
        using var fixture = SigningFixture.Create();
        Assert.Null(await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true));

        var result = await fixture.Coordinator.ExecuteAsync(
                BrainSlotTransactionRequest(CreativityPartyId),
                _ => Task.FromResult(new TurnMutationResult { Success = true }),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("committed", result.Status);
        Assert.Equal(TransactionFailureReason.None, result.Reason);
        Assert.True(result.MutationApplied);
    }

    private static TurnTransactionRequest BrainSlotTransactionRequest(string publisherPartyId)
        => new()
        {
            TransactionId = $"brain-slot-{Guid.NewGuid():N}",
            TurnId = "turn-sec-006",
            OperationName = "brain-slot.invoke",
            OperationBodyJson = "{\"slotId\":\"creativity\"}",
            PublisherPartyId = publisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Mutating = true,
        };

    private static IOptionsMonitor<T> Monitor<T>(T value)
        where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string?>()).Returns(value);
        return monitor;
    }

    /// <summary>Fixture wiring the brain-slot registry, the keyserver, and the turn transaction coordinator.</summary>
    private sealed class SigningFixture : IDisposable
    {
        private readonly InMemorySubscriberCommitService _subscriber;

        private SigningFixture(
            McpDbContext db,
            InMemoryKeyServerService keyServer,
            InMemorySubscriberCommitService subscriber,
            BrainSlotRegistryService registry,
            TurnTransactionCoordinator coordinator,
            TransactionManifestCanonicalizer canonicalizer)
        {
            Db = db;
            KeyServer = keyServer;
            _subscriber = subscriber;
            Registry = registry;
            Coordinator = coordinator;
            Canonicalizer = canonicalizer;
        }

        /// <summary>Gets the in-memory slot store.</summary>
        public McpDbContext Db { get; }

        /// <summary>Gets the real keyserver holding the trusted-party key material.</summary>
        public InMemoryKeyServerService KeyServer { get; }

        /// <summary>Gets the brain-slot registry whose readiness check triggers reconciliation.</summary>
        public BrainSlotRegistryService Registry { get; }

        /// <summary>Gets the coordinator that signs and commits brain-slot turn transactions.</summary>
        public TurnTransactionCoordinator Coordinator { get; }

        /// <summary>Gets the canonicalizer used to rebuild the signed manifest payload.</summary>
        public TransactionManifestCanonicalizer Canonicalizer { get; }

        /// <summary>Builds a fixture with a fresh in-memory database and keyserver.</summary>
        /// <returns>The constructed fixture.</returns>
        public static SigningFixture Create()
        {
            var workspace = new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" };
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
                .UseInMemoryDatabase("brain-slot-sign-" + Guid.NewGuid().ToString("N"))
                .Options;
            var db = new McpDbContext(dbOptions, workspace);
            var canonicalizer = new TransactionManifestCanonicalizer();
            var keyServer = new InMemoryKeyServerService(Monitor(new KeyServerOptions()), canonicalizer);
            var registry = new BrainSlotRegistryService(
                db,
                keyServer,
                new BrainSlotCredentialResolver(new ConfigurationBuilder().Build()),
                Monitor(new BrainSlotOptions { DefaultTimeoutSeconds = 30, MaxTimeoutSeconds = 300 }),
                NullLogger<BrainSlotRegistryService>.Instance);
            var transactionOptions = new TurnTransactionOptions
            {
                Enabled = true,
                RequiredForMutations = true,
                DegradedModeEnabled = false,
                SubscriberPartyId = SubscriberPartyId,
            };
            var subscriber = new InMemorySubscriberCommitService(
                keyServer,
                canonicalizer,
                Monitor(new SubscriberOptions { PartyId = SubscriberPartyId }));
            var coordinator = new TurnTransactionCoordinator(
                Monitor(transactionOptions),
                keyServer,
                keyServer,
                new DirectSubscriberTransactionPubSub(subscriber),
                new JsonDiffgramBuilder(),
                new TransactionDegradedModePolicy(Monitor(transactionOptions)),
                new InMemoryTransactionAuditWriter());
            return new SigningFixture(db, keyServer, subscriber, registry, coordinator, canonicalizer);
        }

        /// <summary>Registers a legacy hemisphere party and returns its generated signing key descriptor.</summary>
        /// <param name="legacyPartyId">Legacy hemisphere party identifier.</param>
        /// <returns>The generated signing key descriptor.</returns>
        public async Task<PartyKeyDescriptor> RegisterLegacyPartyAsync(string legacyPartyId)
        {
            await KeyServer.RegisterPartyAsync(
                    new PartyRegistrationRequest
                    {
                        PartyId = legacyPartyId,
                        Role = legacyPartyId,
                        Status = "active",
                    },
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
            var key = await GetSigningKeyAsync(legacyPartyId).ConfigureAwait(false);
            Assert.NotNull(key);
            return key!;
        }

        /// <summary>Registers the subscriber party so direct manifest signing has an encryption counterparty.</summary>
        /// <returns>A task that completes when the subscriber party is registered.</returns>
        public async Task RegisterSubscriberPartyAsync()
            => await KeyServer.RegisterPartyAsync(
                    new PartyRegistrationRequest
                    {
                        PartyId = SubscriberPartyId,
                        Role = "subscriber",
                        Status = "active",
                    },
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(false);

        /// <summary>Inserts a slot row whose party id was rewritten by the rename migration, bypassing upsert.</summary>
        /// <param name="slotId">Slot identifier.</param>
        /// <param name="role">Brain-slot role.</param>
        /// <param name="partyId">Renamed party identifier.</param>
        /// <returns>A task that completes when the row is persisted.</returns>
        public async Task AddRenamedSlotAsync(string slotId, string role, string partyId)
        {
            var now = DateTimeOffset.UtcNow;
            Db.BrainSlotDefinitions.Add(new BrainSlotDefinitionEntity
            {
                SlotId = slotId,
                Role = role,
                ProviderKind = "OpenAI",
                ModelId = "gpt-test",
                CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
                PartyId = partyId,
                Enabled = true,
                TimeoutSeconds = 30,
                MaxOutputTokens = 1024,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await Db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        /// <summary>Reads the conventional signing key descriptor for a party, if any.</summary>
        /// <param name="partyId">Party identifier.</param>
        /// <returns>The signing key descriptor, or <see langword="null"/>.</returns>
        public Task<PartyKeyDescriptor?> GetSigningKeyAsync(string partyId)
            => KeyServer.GetPartyKeyAsync(partyId, partyId + ":signing:1", TestContext.Current.CancellationToken);

        /// <inheritdoc />
        public void Dispose()
        {
            _subscriber.Dispose();
            Db.Dispose();
            KeyServer.Dispose();
        }
    }
}
