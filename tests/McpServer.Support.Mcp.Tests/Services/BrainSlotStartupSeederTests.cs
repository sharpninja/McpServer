using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-QBSEED-001: Unit tests for <see cref="BrainSlotStartupSeeder"/> (FR-MCP-QBSEED-001,
/// TR-MCP-QBSEED-002). QuadBrain brain-slot definitions are GLOBAL: the seeder provisions a single global set,
/// stamped with the global workspace ("") and visible in every workspace context. Each test builds a real DI
/// container with an in-memory <see cref="McpDbContext"/>, the real <see cref="BrainSlotRegistryService"/>, and
/// the in-memory key server; only the credential resolver is stubbed.
/// </summary>
public sealed class BrainSlotStartupSeederTests
{
    private const string SomeWorkspace = @"F:\GitHub\McpServer";

    /// <summary>With execution enabled and four roles configured, the seeder makes the quad ready.</summary>
    [Fact]
    public async Task SeedAsync_WhenExecutionEnabledAndFourSlotsConfigured_SeedsQuadReady()
    {
        var slots = BrainSlotRoles.All.Select(Seed).ToList();
        await using var provider = BuildProvider(executionEnabled: true, slots);

        var applied = await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(4, applied);
        var status = await StatusAsync(provider, workspaceOverride: null).ConfigureAwait(true);
        Assert.True(status.QuadReady);
        Assert.All(BrainSlotRoles.All, role => Assert.True(status.RoleReadiness[role]));
    }

    /// <summary>FR-MCP-QUAD-001: global slots are visible under any workspace context and stamped with the global id.</summary>
    [Fact]
    public async Task SeedAsync_SlotsAreGlobal_VisibleUnderAnyWorkspaceAndStampedEmpty()
    {
        var slots = BrainSlotRoles.All.Select(Seed).ToList();
        await using var provider = BuildProvider(executionEnabled: true, slots);

        await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        // Visible when scoped to an unrelated workspace (global, not workspace-scoped).
        Assert.True((await StatusAsync(provider, workspaceOverride: SomeWorkspace).ConfigureAwait(true)).QuadReady);

        // Every persisted row carries the global workspace id ("").
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var rows = await db.BrainSlotDefinitions.IgnoreQueryFilters().AsNoTracking().ToListAsync().ConfigureAwait(true);
        Assert.Equal(4, rows.Count);
        Assert.All(rows, r => Assert.Equal(string.Empty, r.WorkspaceId));
    }

    /// <summary>Running the seeder twice is idempotent: still exactly four enabled slots, no exception.</summary>
    [Fact]
    public async Task SeedAsync_RunTwice_IsIdempotent()
    {
        var slots = BrainSlotRoles.All.Select(Seed).ToList();
        await using var provider = BuildProvider(executionEnabled: true, slots);

        await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);
        await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        var all = await ListAsync(provider).ConfigureAwait(true);
        Assert.Equal(4, all.Count(slot => slot.Enabled));
        Assert.True((await StatusAsync(provider, workspaceOverride: null).ConfigureAwait(true)).QuadReady);
    }

    /// <summary>When execution is disabled the seeder provisions nothing.</summary>
    [Fact]
    public async Task SeedAsync_WhenExecutionDisabled_SeedsNothing()
    {
        var slots = BrainSlotRoles.All.Select(Seed).ToList();
        await using var provider = BuildProvider(executionEnabled: false, slots);

        var applied = await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(0, applied);
        Assert.Empty(await ListAsync(provider).ConfigureAwait(true));
    }

    /// <summary>When no slots are configured the seeder provisions nothing.</summary>
    [Fact]
    public async Task SeedAsync_WhenNoSlotsConfigured_SeedsNothing()
    {
        await using var provider = BuildProvider(executionEnabled: true, []);

        var applied = await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(0, applied);
        Assert.Empty(await ListAsync(provider).ConfigureAwait(true));
    }

    /// <summary>One invalid slot is skipped and logged; the remaining valid slots are still provisioned.</summary>
    [Fact]
    public async Task SeedAsync_WhenOneSlotInvalid_SeedsRemainingAndDoesNotThrow()
    {
        var slots = BrainSlotRoles.All.Select(Seed).ToList();
        slots[1].ModelId = string.Empty; // invalid: modelId is required.
        await using var provider = BuildProvider(executionEnabled: true, slots);

        await CreateSeeder(provider).SeedAsync(CancellationToken.None).ConfigureAwait(true);

        var all = await ListAsync(provider).ConfigureAwait(true);
        Assert.Equal(3, all.Count);
    }

    private static BrainSlotSeedDefinition Seed(string role)
        => new()
        {
            SlotId = role.ToLowerInvariant() + "-main",
            Role = role,
            ProviderKind = "OpenAI",
            ModelId = "gpt-test",
            CredentialReference = "env:BRAIN_SLOT_TEST_KEY",
            Enabled = true,
            TimeoutSeconds = 30,
            MaxOutputTokens = 1024,
        };

    private static BrainSlotStartupSeeder CreateSeeder(IServiceProvider provider)
        => new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IOptionsMonitor<BrainSlotOptions>>(),
            NullLogger<BrainSlotStartupSeeder>.Instance);

    private static async Task<BrainSlotStatusResponse> StatusAsync(IServiceProvider provider, string? workspaceOverride)
    {
        using var scope = provider.CreateScope();
        if (workspaceOverride is not null)
            scope.ServiceProvider.GetRequiredService<McpDbContext>().OverrideWorkspaceId(workspaceOverride);
        return await scope.ServiceProvider.GetRequiredService<IBrainSlotRegistryService>()
            .GetStatusAsync().ConfigureAwait(true);
    }

    private static async Task<IReadOnlyList<BrainSlotDto>> ListAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IBrainSlotRegistryService>()
            .ListAsync().ConfigureAwait(true);
    }

    private static ServiceProvider BuildProvider(bool executionEnabled, List<BrainSlotSeedDefinition> slots)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var dbName = "brain-slot-seeder-" + Guid.NewGuid().ToString("N");
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddSingleton<InMemoryKeyServerService>(_ => new InMemoryKeyServerService(
            new StaticOptionsMonitor<KeyServerOptions>(new KeyServerOptions()),
            new TransactionManifestCanonicalizer()));
        services.AddSingleton<IKeyServerPartyRegistry>(sp => sp.GetRequiredService<InMemoryKeyServerService>());
        services.AddSingleton<IBrainSlotCredentialResolver, StubCredentialResolver>();
        services.Configure<BrainSlotOptions>(options =>
        {
            options.ExecutionEnabled = executionEnabled;
            options.Slots = slots;
        });
        services.AddScoped<IBrainSlotRegistryService, BrainSlotRegistryService>();
        return services.BuildServiceProvider();
    }

    private sealed class StubCredentialResolver : IBrainSlotCredentialResolver
    {
        public Task<string?> ResolveAsync(string credentialReference, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("resolved-secret");

        public bool IsSupportedReference(string credentialReference)
            => !string.IsNullOrWhiteSpace(credentialReference);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
        where T : class
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
