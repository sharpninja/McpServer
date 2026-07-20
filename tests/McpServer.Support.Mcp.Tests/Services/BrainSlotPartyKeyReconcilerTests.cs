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
/// TR-MCP-SEC-006: Tests that brain-slot readiness adopts the signing key held by a legacy party id after the
/// Creativity/Logic role rename moved <c>BrainSlotDefinitionEntity.PartyId</c> off the legacy hemisphere ids.
/// Fixtures use an in-memory <see cref="McpDbContext"/> for slot rows and a real
/// <see cref="InMemoryKeyServerService"/> for the trusted-party key store, so no key material is faked.
/// </summary>
public sealed class BrainSlotPartyKeyReconcilerTests
{
    private const string CreativityPartyId = "brain-slot:creativity";
    private const string LegacyCreativityPartyId = "brain-slot:left-hemisphere";
    private const string LogicPartyId = "brain-slot:logic";
    private const string LegacyLogicPartyId = "brain-slot:right-hemisphere";

    /// <summary>
    /// TR-MCP-SEC-006: A Creativity slot whose party id was renamed to <c>brain-slot:creativity</c> is provisioned
    /// with its OWN freshly minted signing key, becomes ready, and leaves the legacy key intact. The new key must
    /// NOT be the legacy public PEM: copying that produced a party that passed readiness and could never sign,
    /// because the key store holds no private half to copy.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WhenCreativityPartyKeyIsMissing_MintsOwnKeyAndPreservesLegacy()
    {
        using var fixture = ReconcilerFixture.Create();
        var legacyKey = await fixture.RegisterLegacyPartyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        await fixture.AddRenamedSlotAsync("creativity", BrainSlotRoles.Creativity, CreativityPartyId).ConfigureAwait(true);

        var status = await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(status.RoleReadiness[BrainSlotRoles.Creativity]);
        var minted = await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true);
        Assert.NotNull(minted);
        Assert.NotEqual(legacyKey.PublicKeyPem, minted!.PublicKeyPem);
        Assert.False(string.IsNullOrWhiteSpace(minted.PublicKeyPem));
        Assert.Equal("signing", minted.Purpose);
        Assert.Equal("active", minted.Status);

        var preserved = await fixture.GetSigningKeyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        Assert.NotNull(preserved);
        Assert.Equal(legacyKey.PublicKeyPem, preserved!.PublicKeyPem);
        Assert.Equal("active", preserved.Status);
    }

    /// <summary>
    /// TR-MCP-SEC-006: The Logic slot is provisioned the same way from <c>brain-slot:right-hemisphere</c>, with its
    /// own key rather than the legacy public PEM.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WhenLogicPartyKeyIsMissing_MintsOwnKeyAndPreservesLegacy()
    {
        using var fixture = ReconcilerFixture.Create();
        var legacyKey = await fixture.RegisterLegacyPartyAsync(LegacyLogicPartyId).ConfigureAwait(true);
        await fixture.AddRenamedSlotAsync("logic", BrainSlotRoles.Logic, LogicPartyId).ConfigureAwait(true);

        var status = await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(status.RoleReadiness[BrainSlotRoles.Logic]);
        var minted = await fixture.GetSigningKeyAsync(LogicPartyId).ConfigureAwait(true);
        Assert.NotNull(minted);
        Assert.NotEqual(legacyKey.PublicKeyPem, minted!.PublicKeyPem);
        Assert.False(string.IsNullOrWhiteSpace(minted.PublicKeyPem));
    }

    /// <summary>
    /// TR-MCP-SEC-006: Reconciliation is idempotent; repeated readiness checks keep the same minted key and never
    /// rotate it, so an installation does not churn signing identities on every status call.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_RunTwice_KeepsTheSameMintedSigningKey()
    {
        using var fixture = ReconcilerFixture.Create();
        var legacyKey = await fixture.RegisterLegacyPartyAsync(LegacyCreativityPartyId).ConfigureAwait(true);
        await fixture.AddRenamedSlotAsync("creativity", BrainSlotRoles.Creativity, CreativityPartyId).ConfigureAwait(true);

        await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var first = await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true);
        await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(legacyKey.PublicKeyPem, first!.PublicKeyPem);
        Assert.Equal(first.PublicKeyPem, second!.PublicKeyPem);
        Assert.Equal(first.CreatedAtUtc, second.CreatedAtUtc);
    }

    /// <summary>
    /// TR-MCP-SEC-006: With no legacy party present the readiness check must stay red and must not mint fresh
    /// key material for the renamed party.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WhenNoLegacyPartyExists_NeverGeneratesKeyMaterial()
    {
        using var fixture = ReconcilerFixture.Create();
        await fixture.AddRenamedSlotAsync("creativity", BrainSlotRoles.Creativity, CreativityPartyId).ConfigureAwait(true);

        var status = await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(status.RoleReadiness[BrainSlotRoles.Creativity]);
        Assert.Contains(
            status.ValidationErrors,
            error => error.Contains("trusted party signing key is missing or disabled.", StringComparison.Ordinal));
        Assert.Null(await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true));
    }

    /// <summary>
    /// TR-MCP-SEC-006: When the renamed party already owns a signing key the reconciliation is a no-op and does
    /// not overwrite that key with the legacy material.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_WhenPartyAlreadyHasSigningKey_LeavesItUnchanged()
    {
        using var fixture = ReconcilerFixture.Create();
        await fixture.Service.UpsertAsync(
                "creativity",
                SlotRequest(BrainSlotRoles.Creativity, CreativityPartyId),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var current = await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true);
        var legacyKey = await fixture.RegisterLegacyPartyAsync(LegacyCreativityPartyId).ConfigureAwait(true);

        await fixture.Service.GetStatusAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var after = await fixture.GetSigningKeyAsync(CreativityPartyId).ConfigureAwait(true);
        Assert.NotNull(current);
        Assert.NotNull(after);
        Assert.Equal(current!.PublicKeyPem, after!.PublicKeyPem);
        Assert.NotEqual(legacyKey.PublicKeyPem, after.PublicKeyPem);
    }

    private static UpsertBrainSlotRequest SlotRequest(string role, string partyId)
        => new()
        {
            Role = role,
            ProviderKind = "OpenAI",
            ModelId = "gpt-test",
            CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
            PartyId = partyId,
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
        };

    private static IOptionsMonitor<T> Monitor<T>(T value)
        where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string?>()).Returns(value);
        return monitor;
    }

    /// <summary>Fixture wiring an in-memory slot store to a real in-memory keyserver.</summary>
    private sealed class ReconcilerFixture : IDisposable
    {
        private ReconcilerFixture(McpDbContext db, InMemoryKeyServerService keyServer, BrainSlotRegistryService service)
        {
            Db = db;
            KeyServer = keyServer;
            Service = service;
        }

        public McpDbContext Db { get; }

        public InMemoryKeyServerService KeyServer { get; }

        public BrainSlotRegistryService Service { get; }

        public static ReconcilerFixture Create()
        {
            var workspace = new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" };
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
                .UseInMemoryDatabase("brain-slot-reconcile-" + Guid.NewGuid().ToString("N"))
                .Options;
            var db = new McpDbContext(dbOptions, workspace);
            var keyServer = new InMemoryKeyServerService(
                Monitor(new KeyServerOptions()),
                new TransactionManifestCanonicalizer());
            var service = new BrainSlotRegistryService(
                db,
                keyServer,
                new BrainSlotCredentialResolver(new ConfigurationBuilder().Build()),
                Monitor(new BrainSlotOptions { DefaultTimeoutSeconds = 30, MaxTimeoutSeconds = 300 }),
                NullLogger<BrainSlotRegistryService>.Instance);
            return new ReconcilerFixture(db, keyServer, service);
        }

        /// <summary>Registers a legacy hemisphere party and returns its generated signing key descriptor.</summary>
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

        /// <summary>Inserts a slot row whose party id was rewritten by the rename migration, bypassing upsert.</summary>
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
        public Task<PartyKeyDescriptor?> GetSigningKeyAsync(string partyId)
            => KeyServer.GetPartyKeyAsync(partyId, partyId + ":signing:1", TestContext.Current.CancellationToken);

        public void Dispose()
        {
            Db.Dispose();
            KeyServer.Dispose();
        }
    }
}
