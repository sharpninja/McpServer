using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-140: DB-FK-001 behavioral tests for normalized TODO requirement
/// links and compatibility JSON projections.
/// </summary>
public sealed class EfTodoServiceRequirementLinkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly string _workspacePath;
    private readonly EfTodoService _sut;

    /// <summary>Builds an isolated SQLite-backed EF TODO service.</summary>
    public EfTodoServiceRequirementLinkTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"todo-link-{Guid.NewGuid():N}");
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var services = new ServiceCollection();
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = _workspacePath });
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_connection));
        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
        }

        _sut = new EfTodoService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions()),
            Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions { Provider = TodoStorageOptions.DatabaseProvider }),
            Substitute.For<IWriteAuditLog>(),
            NullLogger<EfTodoService>.Instance,
            Substitute.For<IChangeEventBus>());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// TEST-MCP-140: Creating a TODO with requirement JSON projection values
    /// creates the durable TODO anchor, placeholder requirements, and normalized
    /// link rows.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithRequirementProjection_CreatesTodoRecordRequirementsAndLinks()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "TODO-LINK-001",
            Title = "Link requirements",
            Section = "Backlog",
            Priority = "high",
            FunctionalRequirements = ["FR-LINK-001"],
            TechnicalRequirements = ["TR-LINK-001"],
        }).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.True(await db.TodoRecords.AnyAsync(row => row.TodoId == "TODO-LINK-001").ConfigureAwait(true));
        Assert.True(await db.Requirements.AnyAsync(row => row.Kind == "fr" && row.Id == "FR-LINK-001").ConfigureAwait(true));
        Assert.True(await db.Requirements.AnyAsync(row => row.Kind == "tr" && row.Id == "TR-LINK-001").ConfigureAwait(true));

        var links = await db.TodoRequirementLinks
            .OrderBy(row => row.RequirementKind)
            .Select(row => row.RequirementKind + ":" + row.RequirementId)
            .ToListAsync()
            .ConfigureAwait(true);
        Assert.Equal(["fr:FR-LINK-001", "tr:TR-LINK-001"], links);
    }

    /// <summary>
    /// TEST-MCP-140: Requirement projections may contain legacy display text; durable
    /// link rows use only the canonical bounded requirement identifier.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithRequirementProjectionDisplayText_CreatesCanonicalRequirementLinks()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "TODO-LINK-003",
            Title = "Link legacy requirement labels",
            Section = "Backlog",
            Priority = "high",
            FunctionalRequirements = ["FR-REQ-006: Client must throw typed exceptions for HTTP error responses"],
            TechnicalRequirements = ["TR-MCP-002: RequirementsDocumentService parses all four files"],
        }).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var links = await db.TodoRequirementLinks
            .OrderBy(row => row.RequirementKind)
            .Select(row => row.RequirementKind + ":" + row.RequirementId)
            .ToListAsync()
            .ConfigureAwait(true);

        Assert.Equal(["fr:FR-REQ-006", "tr:TR-MCP-002"], links);
        Assert.DoesNotContain(links, link => link.Contains(' ', StringComparison.Ordinal));
    }

    /// <summary>
    /// TEST-MCP-140: Updating requirement projections keeps JSON fields and
    /// normalized link rows synchronized, including stale-link soft deletion.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_RequirementProjection_KeepsLinksAndJsonProjectionInSync()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "TODO-LINK-002",
            Title = "Link requirements",
            Section = "Backlog",
            Priority = "high",
            FunctionalRequirements = ["FR-LINK-OLD-001"],
            TechnicalRequirements = ["TR-OLD-001"],
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var updated = await _sut.UpdateAsync("TODO-LINK-002", new TodoUpdateRequest
        {
            FunctionalRequirements = ["FR-LINK-NEW-001"],
            TechnicalRequirements = ["TR-NEW-001"],
        }).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        Assert.Equal(["FR-LINK-NEW-001"], updated.Item!.FunctionalRequirements);
        Assert.Equal(["TR-NEW-001"], updated.Item!.TechnicalRequirements);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var activeLinks = await db.TodoRequirementLinks
            .OrderBy(row => row.RequirementKind)
            .Select(row => row.RequirementKind + ":" + row.RequirementId)
            .ToListAsync()
            .ConfigureAwait(true);
        Assert.Equal(["fr:FR-LINK-NEW-001", "tr:TR-NEW-001"], activeLinks);

        var oldLinks = await db.TodoRequirementLinks
            .IgnoreQueryFilters()
            .Where(row => row.RequirementId == "FR-LINK-OLD-001" || row.RequirementId == "TR-OLD-001")
            .ToListAsync()
            .ConfigureAwait(true);
        Assert.Equal(2, oldLinks.Count);
        Assert.All(oldLinks, row => Assert.True((bool)db.Entry(row).Property("IsDeleted").CurrentValue!));
    }
}
