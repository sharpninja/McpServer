using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
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

/// <summary>Tests for the durable brain-slot registry. TEST-MCP-175 and TEST-MCP-176.</summary>
public sealed class BrainSlotRegistryServiceTests
{
    /// <summary>Enabling a second slot for the same role requires replaceExisting=true.</summary>
    [Fact]
    public async Task UpsertAsync_WhenEnabledRoleAlreadyExists_RequiresReplaceExisting()
    {
        using var fixture = RegistryFixture.Create();
        await fixture.Service.UpsertAsync("left-a", SlotRequest(
                BrainSlotRoles.LeftHemisphere,
                enabled: true,
                partyId: "brain-slot:left-hemisphere-a"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var ex = await Assert.ThrowsAsync<BrainSlotConflictException>(() =>
            fixture.Service.UpsertAsync("left-b", SlotRequest(BrainSlotRoles.LeftHemisphere, enabled: true), cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("replaceExisting=true", ex.Message, StringComparison.Ordinal);
        Assert.Single(fixture.Db.BrainSlotDefinitions.Where(slot => slot.Enabled && slot.Role == BrainSlotRoles.LeftHemisphere));
    }

    /// <summary>replaceExisting=true disables the previous slot, disables its party, and audits the replacement.</summary>
    [Fact]
    public async Task UpsertAsync_WithReplaceExisting_DisablesOldSlotAndParty()
    {
        using var fixture = RegistryFixture.Create();
        await fixture.Service.UpsertAsync("left-a", SlotRequest(
                BrainSlotRoles.LeftHemisphere,
                enabled: true,
                partyId: "brain-slot:left-hemisphere-a"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        await fixture.Service.UpsertAsync("left-b", SlotRequest(
                BrainSlotRoles.LeftHemisphere,
                enabled: true,
                replaceExisting: true,
                partyId: "brain-slot:left-hemisphere-b"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var rows = await fixture.Db.BrainSlotDefinitions
            .IgnoreQueryFilters()
            .Where(slot => slot.Role == BrainSlotRoles.LeftHemisphere)
            .OrderBy(slot => slot.SlotId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(2, rows.Count);
        Assert.False(rows[0].Enabled);
        Assert.True(rows[1].Enabled);
        Assert.Contains(fixture.Db.DataAuditLogs, log => log.Action == "replace" && log.EntityKey == "left-a");

        var disabledKey = await fixture.KeyServer.GetPartyKeyAsync(
            "brain-slot:left-hemisphere-a",
            "brain-slot:left-hemisphere-a:signing:1", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(disabledKey);
        Assert.Equal("disabled", disabledKey!.Status);
    }

    /// <summary>Upsert restores a soft-deleted slot row instead of leaving a tombstone collision.</summary>
    [Fact]
    public async Task UpsertAsync_AfterSoftDelete_RestoresExistingRow()
    {
        using var fixture = RegistryFixture.Create();
        await fixture.Service.UpsertAsync("curiosity-main", SlotRequest(BrainSlotRoles.CuriosityEngine, enabled: false, modelId: "old-model"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await fixture.Service.DeleteAsync("curiosity-main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var restored = await fixture.Service.UpsertAsync(
            "curiosity-main",
            SlotRequest(BrainSlotRoles.CuriosityEngine, enabled: true, modelId: "new-model"), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal("new-model", restored.ModelId);
        Assert.NotNull(await fixture.Service.GetAsync("curiosity-main", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));
        var allRows = await fixture.Db.BrainSlotDefinitions
            .IgnoreQueryFilters()
            .Where(slot => slot.SlotId == "curiosity-main")
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Single(allRows);
        Assert.False((bool)fixture.Db.Entry(allRows[0]).Property("IsDeleted").CurrentValue!);
    }

    /// <summary>A workspace is quad-ready only when all four roles are enabled and have active party keys.</summary>
    [Fact]
    public async Task GetStatusAsync_WhenFourEnabledSlotsHaveActiveMappings_ReturnsQuadReady()
    {
        using var fixture = RegistryFixture.Create();
        foreach (var role in BrainSlotRoles.All)
        {
            await fixture.Service.UpsertAsync(role.ToLowerInvariant(), SlotRequest(role, enabled: true), cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }

        var status = await fixture.Service.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(status.QuadReady);
        Assert.Empty(status.MissingRoles);
        Assert.Empty(status.DisabledRoles);
        Assert.All(BrainSlotRoles.All, role => Assert.True(status.RoleReadiness[role]));
    }

    /// <summary>OpenAI-compatible custom endpoints require an explicit allowlist entry.</summary>
    [Fact]
    public async Task UpsertAsync_WhenEndpointHostIsNotAllowlisted_RejectsSlot()
    {
        using var fixture = RegistryFixture.Create();
        var request = SlotRequest(BrainSlotRoles.RightHemisphere, enabled: false);
        request.ProviderKind = "OpenAICompatible";
        request.Endpoint = "https://models.example.test/v1";

        var ex = await Assert.ThrowsAsync<BrainSlotValidationException>(() =>
            fixture.Service.UpsertAsync("right-custom", request, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Equal(BrainSlotReasonCodes.EndpointNotAllowed, ex.Reason);
    }

    private static UpsertBrainSlotRequest SlotRequest(
        string role,
        bool enabled,
        string modelId = "gpt-test",
        bool replaceExisting = false,
        string? partyId = null)
        => new()
        {
            Role = role,
            ProviderKind = "OpenAI",
            ModelId = modelId,
            CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
            PartyId = partyId ?? string.Empty,
            Enabled = enabled,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
            ReplaceExisting = replaceExisting,
        };

    private static IOptionsMonitor<T> Monitor<T>(T value) where T : class
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        monitor.Get(Arg.Any<string?>()).Returns(value);
        return monitor;
    }

    private sealed class RegistryFixture : IDisposable
    {
        private RegistryFixture(
            McpDbContext db,
            InMemoryKeyServerService keyServer,
            BrainSlotRegistryService service)
        {
            Db = db;
            KeyServer = keyServer;
            Service = service;
        }

        public McpDbContext Db { get; }

        public InMemoryKeyServerService KeyServer { get; }

        public BrainSlotRegistryService Service { get; }

        public static RegistryFixture Create()
        {
            var workspace = new WorkspaceContext { WorkspacePath = @"F:\GitHub\McpServer" };
            var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
                .UseInMemoryDatabase("brain-slot-registry-" + Guid.NewGuid().ToString("N"))
                .Options;
            var db = new McpDbContext(dbOptions, workspace);
            var keyServer = new InMemoryKeyServerService(
                Monitor(new KeyServerOptions()),
                new TransactionManifestCanonicalizer());
            var resolver = new BrainSlotCredentialResolver(new ConfigurationBuilder().Build());
            var service = new BrainSlotRegistryService(
                db,
                keyServer,
                resolver,
                Monitor(new BrainSlotOptions { DefaultTimeoutSeconds = 30, MaxTimeoutSeconds = 300 }),
                NullLogger<BrainSlotRegistryService>.Instance);
            return new RegistryFixture(db, keyServer, service);
        }

        public void Dispose()
        {
            Db.Dispose();
            KeyServer.Dispose();
        }
    }
}
