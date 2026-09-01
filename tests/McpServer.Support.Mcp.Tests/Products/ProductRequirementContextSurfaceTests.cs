using System.Reflection;
using System.Text.Json;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Products;

/// <summary>
/// TEST-MCP-PRODUCT-006 / FR-MCP-PRODUCT-005 / TR-MCP-PRODUCT-API-001:
/// Drive shipped REST pack/search and STDIO context_pack/context_search, plus the
/// requirements_effective MCP tool name.
/// </summary>
public sealed class ProductRequirementContextSurfaceTests : IDisposable
{
    private const string Owner = @"F:\GitHub\ctx-surface-owner";
    private const string Sibling = @"F:\GitHub\ctx-surface-sibling";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly CallContext _ctx = new();

    /// <summary>Seeds a product, sibling FR body, and a sibling .cs chunk.</summary>
    public ProductRequirementContextSurfaceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new McpDbContext(_options);
        db.Database.EnsureCreated();
        SeedWorkspace(db, Owner, "owner");
        SeedWorkspace(db, Sibling, "sibling");
        SeedLayer(db, Owner, "layer-1", 1);
        SeedLayer(db, Sibling, "layer-1", 1);
        SeedRequirement(db, Sibling, "FR-CTX-001", "Sibling context FR", "SIBLING-FR-BODY-UNIQUE");
        SeedRequirement(db, Owner, "FR-CTX-OWNER", "Owner FR", "OWNER-FR-BODY");
        db.Documents.Add(new ContextDocumentEntity
        {
            Id = "sib-cs",
            WorkspaceId = Sibling,
            SourceKey = @"F:\GitHub\ctx-surface-sibling\src\Secret.cs",
            SourceType = "repo",
            ContentHash = "hash-secret",
        });
        db.Chunks.Add(new ContextChunkEntity
        {
            Id = "sib-cs-0",
            DocumentId = "sib-cs",
            WorkspaceId = Sibling,
            Content = "class Secret { }",
            ChunkIndex = 0,
        });
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>REST search with sourceType=product-requirements returns sibling FR and origin, not Secret.cs.</summary>
    [Fact]
    public async Task RestSearch_ProductRequirements_IncludesSiblingFrNotCs()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        db.OverrideWorkspaceId(Owner);
        var controller = CreateController(db);

        var action = await controller.SearchAsync(
            new ContextSearchRequest { Query = "SIBLING", SourceType = "product-requirements", Limit = 20 },
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("SIBLING-FR-BODY-UNIQUE", json, StringComparison.Ordinal);
        Assert.Contains(Sibling.Replace("\\", "\\\\"), json, StringComparison.Ordinal);
        Assert.DoesNotContain("class Secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret.cs", json, StringComparison.Ordinal);
    }

    /// <summary>REST pack includes sibling FR body plus origin and excludes sibling source files.</summary>
    [Fact]
    public async Task RestPack_IncludesSiblingFrNotCs()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        db.OverrideWorkspaceId(Owner);
        var controller = CreateController(db);

        var action = await controller.GetPackAsync(
            new ContextPackRequest { Query = "SIBLING", Limit = 20 },
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var pack = Assert.IsType<ContextPack>(ok.Value);
        Assert.Contains(pack.Chunks, c => c.Content.Contains("SIBLING-FR-BODY-UNIQUE", StringComparison.Ordinal)
            && c.Content.Contains(Sibling, StringComparison.Ordinal));
        Assert.DoesNotContain(pack.Chunks, c =>
            c.Content.Contains("class Secret", StringComparison.Ordinal)
            || c.Content.Contains("Secret.cs", StringComparison.Ordinal));
    }

    /// <summary>STDIO context_search with sourceType=product-requirements includes sibling FR, not .cs.</summary>
    [Fact]
    public async Task McpContextSearch_ProductRequirements_IncludesSiblingFrNotCs()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var tools = CreateTools(db);

        var json = await tools.ContextSearch(
            "SIBLING",
            Owner,
            limit: 20,
            sourceType: "product-requirements",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("SIBLING-FR-BODY-UNIQUE", json, StringComparison.Ordinal);
        Assert.Contains("originWorkspaceId=", json, StringComparison.Ordinal);
        Assert.Contains("ctx-surface-sibling", json, StringComparison.Ordinal);
        Assert.DoesNotContain("class Secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret.cs", json, StringComparison.Ordinal);
    }

    /// <summary>STDIO context_pack includes sibling FR body plus origin and excludes sibling .cs.</summary>
    [Fact]
    public async Task McpContextPack_IncludesSiblingFrNotCs()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var tools = CreateTools(db);

        var json = await tools.ContextPack(
            "SIBLING",
            Owner,
            limit: 20,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("SIBLING-FR-BODY-UNIQUE", json, StringComparison.Ordinal);
        Assert.Contains("originWorkspaceId=", json, StringComparison.Ordinal);
        Assert.Contains("ctx-surface-sibling", json, StringComparison.Ordinal);
        Assert.DoesNotContain("class Secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret.cs", json, StringComparison.Ordinal);
    }

    /// <summary>Plan verification step 4: MCP exposes requirements_effective with productScope.</summary>
    [Fact]
    public void McpRequirementsEffectiveTool_IsDeclared()
    {
        var names = typeof(FwhMcpTools)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(m => m.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToArray();
        Assert.Contains("requirements_effective", names);
    }

    /// <summary>requirements_effective dispatches the share query with the caller productScope.</summary>
    [Fact]
    public async Task McpRequirementsEffective_DispatchesShareQueryWithProductScope()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        GetProductEffectiveRequirementsQuery? captured = null;
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.QueryAsync<EffectiveRequirementsResult>(
                Arg.Any<GetProductEffectiveRequirementsQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                captured = ci.Arg<GetProductEffectiveRequirementsQuery>()
                    ?? throw new InvalidOperationException("Missing GetProductEffectiveRequirementsQuery.");
                return new GetProductEffectiveRequirementsQueryHandler(db).HandleAsync(
                    captured,
                    new CallContext { CancellationToken = ci.Arg<CancellationToken>() });
            });
        var tools = CreateTools(db, dispatcher);

        var json = await tools.RequirementsEffective(
            Owner,
            layerKey: "layer-1",
            productScope: "local",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("local", captured!.ProductScope);
        Assert.Contains("FR-CTX-OWNER", json, StringComparison.Ordinal);
        Assert.DoesNotContain("FR-CTX-001", json, StringComparison.Ordinal);
    }

    private async Task CreateProductAsync(McpDbContext db)
    {
        var created = await new CreateProductCommandHandler(db).HandleAsync(
            new CreateProductCommand(Owner, new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "Shared" }),
            _ctx);
        Assert.True(created.IsSuccess, created.Error);
        var added = await new AddProductMemberCommandHandler(db).HandleAsync(
            new AddProductMemberCommand(Owner, "PROD-MCPSERVER", Sibling),
            _ctx);
        Assert.True(added.IsSuccess, added.Error);
    }

    private ContextController CreateController(McpDbContext db)
    {
        var dispatcher = CreateDispatcher(db);
        var search = Substitute.For<IContextSearchService>();
        search.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchResult([], []));
        var graph = Substitute.For<IGraphRagService>();
        return new ContextController(
            db,
            search,
            graph,
            CreateCoordinator(db),
            MsOptions.Options.Create(new GraphRagOptions { Enabled = false, EnhanceContextSearch = false }),
            dispatcher: dispatcher);
    }

    private FwhMcpTools CreateTools(McpDbContext db, IDispatcher? dispatcher = null)
    {
        dispatcher ??= CreateDispatcher(db);
        var search = Substitute.For<IContextSearchService>();
        search.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchResult([], []));
        var graph = Substitute.For<IGraphRagService>();
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = Owner };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var todoService = Substitute.For<ITodoService>();
        var todoServiceFactory = Substitute.For<ITodoServiceFactory>();
        var todoServiceResolver = new TodoServiceResolver(todoService, ingestionOptions, todoServiceFactory);
        var workspaceAccessor = new WorkspaceServiceAccessor(todoServiceResolver, httpContextAccessor, ingestionOptions);
        var processRunner = Substitute.For<IProcessRunner>();
        var configuration = Substitute.For<IConfiguration>();
        var desktopLaunchService = new DesktopLaunchService(
            configuration,
            MsOptions.Options.Create(new DesktopLaunchOptions()),
            processRunner,
            NullLogger<DesktopLaunchService>.Instance);
        return new FwhMcpTools(
            db,
            Substitute.For<IRepoFileService>(),
            CreateCoordinator(db),
            Substitute.For<ISyncStatusStore>(),
            search,
            graph,
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            Substitute.For<IMemoryService>(),
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance),
            new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance),
            Substitute.For<ITodoExecutionService>(),
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance,
            agentHelpService: Substitute.For<IAgentHelpConversationService>(),
            dispatcher: dispatcher);
    }

    private IngestionCoordinator CreateCoordinator(McpDbContext db)
    {
        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = Owner };
        var chunker = new Chunker();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        return new IngestionCoordinator(
            db,
            new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance),
            new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, Substitute.For<ISessionLogService>(), NullLogger<SessionLogIngestor>.Instance),
            new ExternalDocsIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<ExternalDocsIngestor>.Instance),
            new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance),
            new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance),
            Substitute.For<IWebsiteIngestor>(),
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);
    }

    private static IDispatcher CreateDispatcher(McpDbContext db)
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.QueryAsync<IReadOnlyList<ProductRequirementChunkDto>>(
                Arg.Any<GetProductRequirementContextQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var query = ci.Arg<GetProductRequirementContextQuery>()
                    ?? throw new InvalidOperationException("Missing GetProductRequirementContextQuery.");
                return new GetProductRequirementContextQueryHandler(db).HandleAsync(
                    query,
                    new CallContext { CancellationToken = ci.Arg<CancellationToken>() });
            });
        dispatcher.QueryAsync<EffectiveRequirementsResult>(
                Arg.Any<GetProductEffectiveRequirementsQuery>(),
                Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var query = ci.Arg<GetProductEffectiveRequirementsQuery>()
                    ?? throw new InvalidOperationException("Missing GetProductEffectiveRequirementsQuery.");
                return new GetProductEffectiveRequirementsQueryHandler(db).HandleAsync(
                    query,
                    new CallContext { CancellationToken = ci.Arg<CancellationToken>() });
            });
        return dispatcher;
    }

    private static void SeedWorkspace(McpDbContext db, string id, string name)
    {
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = id,
            WorkspacePath = id,
            Name = name,
            IsEnabled = true,
            CurrentRequirementLayerKey = "layer-1",
        });
    }

    private static void SeedLayer(McpDbContext db, string workspaceId, string key, int order)
    {
        db.RequirementScopeLayers.Add(new RequirementScopeLayerEntity
        {
            WorkspaceId = workspaceId,
            Key = key,
            Order = order,
            Name = key,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static void SeedRequirement(McpDbContext db, string workspaceId, string id, string title, string body)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = workspaceId,
            Kind = "fr",
            Id = id,
            Title = title,
            Body = body,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}
