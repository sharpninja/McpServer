using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Products;

/// <summary>
/// Shared in-memory SQLite fixture for Phase 1 product handler tests.
/// Seeds registered workspaces used by the named acceptance cases.
/// </summary>
internal sealed class ProductHandlerTestContext : IDisposable
{
    /// <summary>Owner workspace used by most cases.</summary>
    public const string Owner = @"F:\GitHub\McpServer";

    /// <summary>Registered member workspace.</summary>
    public const string Member = @"F:\GitHub\mcpserver-grok-plugin";

    /// <summary>Registered non-owner used for 403 cases.</summary>
    public const string Other = @"F:\GitHub\other-workspace";

    /// <summary>Registered outsider used for 404 isolation.</summary>
    public const string Outsider = @"F:\GitHub\outsider";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Shared call context.</summary>
    public CallContext CallContext { get; } = new();

    /// <summary>Opens an isolated SQLite database and seeds workspaces.</summary>
    public ProductHandlerTestContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDb();
        db.Database.EnsureCreated();
        SeedWorkspace(db, Owner, "McpServer");
        SeedWorkspace(db, Member, "Grok plugin");
        SeedWorkspace(db, Other, "Other");
        SeedWorkspace(db, Outsider, "Outsider");
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Creates a context sharing the fixture connection.</summary>
    public McpDbContext CreateDb() => new(_options);

    /// <summary>Creates the default PROD-MCPSERVER product as the owner.</summary>
    public async Task<ProductDto> CreateDefaultProductAsync(McpDbContext db)
    {
        var result = await new CreateProductCommandHandler(db)
            .HandleAsync(
                new CreateProductCommand(Owner, new CreateProductRequest
                {
                    Key = "PROD-MCPSERVER",
                    Name = "McpServer",
                }),
                CallContext)
            .ConfigureAwait(true);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error);
        return result.Value!;
    }

    /// <summary>Adds the registered member workspace to PROD-MCPSERVER.</summary>
    public async Task AddDefaultMemberAsync(McpDbContext db)
    {
        var result = await new AddProductMemberCommandHandler(db)
            .HandleAsync(new AddProductMemberCommand(Owner, "PROD-MCPSERVER", Member), CallContext)
            .ConfigureAwait(true);
        if (!result.IsSuccess)
            throw new InvalidOperationException(result.Error);
    }

    /// <summary>Create handler bound to <paramref name="db"/>.</summary>
    public static CreateProductCommandHandler Create(McpDbContext db) => new(db);

    /// <summary>Update handler bound to <paramref name="db"/>.</summary>
    public static UpdateProductCommandHandler Update(McpDbContext db) => new(db);

    /// <summary>Delete handler bound to <paramref name="db"/>.</summary>
    public static DeleteProductCommandHandler Delete(McpDbContext db) => new(db);

    /// <summary>Add-member handler bound to <paramref name="db"/>.</summary>
    public static AddProductMemberCommandHandler AddMember(McpDbContext db) => new(db);

    /// <summary>Remove-member handler bound to <paramref name="db"/>.</summary>
    public static RemoveProductMemberCommandHandler RemoveMember(McpDbContext db) => new(db);

    /// <summary>Get handler bound to <paramref name="db"/>.</summary>
    public static GetProductQueryHandler Get(McpDbContext db) => new(db);

    /// <summary>List handler bound to <paramref name="db"/>.</summary>
    public static ListProductsQueryHandler List(McpDbContext db) => new(db);

    /// <summary>List-members handler bound to <paramref name="db"/>.</summary>
    public static ListProductMembersQueryHandler ListMembers(McpDbContext db) => new(db);

    private static void SeedWorkspace(McpDbContext db, string workspaceId, string name)
    {
        if (db.Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == workspaceId))
            return;

        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = name,
            IsEnabled = true,
        });
    }
}
