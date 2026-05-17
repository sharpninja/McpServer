using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003: Validates that the GraphRAG ad-hoc
/// MCP STDIO tools in <see cref="FwhMcpTools"/> delegate to <see cref="IGraphRagService"/>
/// and return non-null JSON.
/// </summary>
public sealed class GraphRagMcpToolTests : IDisposable
{
    private readonly IGraphRagService _graphRagService = Substitute.For<IGraphRagService>();
    private readonly FwhMcpTools _tools;
    private readonly McpDbContext _db;

    /// <summary>Initializes FwhMcpTools with real sealed-class instances backed by mocked interfaces.</summary>
    public GraphRagMcpToolTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"McpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();

        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = "." };
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        // Construct sealed classes with real instances and mocked collaborators
        var chunker = new Chunker();
        var repoIngestor = new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance);
        var sessionLogIngestor = new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, Substitute.For<ISessionLogService>(), NullLogger<SessionLogIngestor>.Instance);
        var externalDocsIngestor = new ExternalDocsIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<ExternalDocsIngestor>.Instance);
        var gitHubIngestor = new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance);
        var issueIngestor = new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance);
        var websiteIngestor = Substitute.For<IWebsiteIngestor>();

        var coordinator = new IngestionCoordinator(
            _db, repoIngestor, sessionLogIngestor, externalDocsIngestor,
            gitHubIngestor, issueIngestor, websiteIngestor,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null, // IChangeEventBus is optional
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);

        var todoService = Substitute.For<ITodoService>();
        var todoServiceFactory = Substitute.For<ITodoServiceFactory>();
        var todoServiceResolver = new TodoServiceResolver(todoService, ingestionOptions, todoServiceFactory);
        var workspaceAccessor = new WorkspaceServiceAccessor(todoServiceResolver, httpContextAccessor, ingestionOptions);
        var desktopLaunchOptions = MsOptions.Options.Create(new DesktopLaunchOptions());
        var processRunner = Substitute.For<IProcessRunner>();
        var configuration = Substitute.For<IConfiguration>();
        var desktopLaunchService = new DesktopLaunchService(configuration, desktopLaunchOptions, processRunner, NullLogger<DesktopLaunchService>.Instance);
        var todoCreationService = new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance);
        var todoUpdateService = new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance);

        _tools = new FwhMcpTools(
            _db,
            Substitute.For<IRepoFileService>(),
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            _graphRagService,
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            todoCreationService,
            todoUpdateService,
            Substitute.For<ITodoExecutionService>(),
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance);
    }

    /// <summary>Disposes DB context.</summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    // ── IngestText ──

    /// <summary>FR-MCP-078: graphrag_ingest_text calls IngestTextAsync and returns JSON.</summary>
    [Fact]
    public async Task GraphRagIngestText_CallsService_ReturnsJson()
    {
        var expected = new GraphRagIngestTextResponse { DocumentId = "doc-1", SourceType = "adhoc-text", SourceKey = "test" };
        _graphRagService.IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagIngestText("hello", ".").ConfigureAwait(true);

        Assert.NotNull(json);
        Assert.Contains("doc-1", json, StringComparison.Ordinal);
        await _graphRagService.Received(1).IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>FR-MCP-078: graphrag_ingest_text returns error when content empty.</summary>
    [Fact]
    public async Task GraphRagIngestText_EmptyContent_ReturnsError()
    {
        var json = await _tools.GraphRagIngestText("", ".").ConfigureAwait(true);

        Assert.Contains("error", json, StringComparison.Ordinal);
    }

    // ── ListDocuments ──

    /// <summary>FR-MCP-080: graphrag_list_documents calls ListDocumentsAsync and returns JSON.</summary>
    [Fact]
    public async Task GraphRagListDocuments_CallsService_ReturnsJson()
    {
        var expected = new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 };
        _graphRagService.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagListDocuments(".").ConfigureAwait(true);

        Assert.NotNull(json);
        await _graphRagService.Received(1).ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    // ── GetDocumentChunks ──

    /// <summary>FR-MCP-080: graphrag_get_document_chunks returns JSON when found.</summary>
    [Fact]
    public async Task GraphRagGetDocumentChunks_Found_ReturnsJson()
    {
        var expected = new GraphRagDocumentChunksResponse { DocumentId = "doc-1", Chunks = [], TotalChunks = 0 };
        _graphRagService.GetDocumentChunksAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagGetDocumentChunks("doc-1", ".").ConfigureAwait(true);

        Assert.Contains("doc-1", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-080: graphrag_get_document_chunks returns error when not found.</summary>
    [Fact]
    public async Task GraphRagGetDocumentChunks_NotFound_ReturnsError()
    {
        _graphRagService.GetDocumentChunksAsync("doc-99", Arg.Any<CancellationToken>()).Returns((GraphRagDocumentChunksResponse?)null);

        var json = await _tools.GraphRagGetDocumentChunks("doc-99", ".").ConfigureAwait(true);

        Assert.Contains("not found", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteDocument ──

    /// <summary>FR-MCP-080: graphrag_delete_document calls DeleteDocumentAsync.</summary>
    [Fact]
    public async Task GraphRagDeleteDocument_CallsService_ReturnsJson()
    {
        var expected = new GraphRagDocumentDeleteResponse { DocumentId = "doc-1", ChunksRemoved = 2, Success = true };
        _graphRagService.DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagDeleteDocument("doc-1", ".").ConfigureAwait(true);

        Assert.NotNull(json);
        await _graphRagService.Received(1).DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    // ── CreateEntity ──

    /// <summary>FR-MCP-079: graphrag_create_entity calls CreateEntityAsync.</summary>
    [Fact]
    public async Task GraphRagCreateEntity_CallsService_ReturnsJson()
    {
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _graphRagService.CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagCreateEntity("Alice", "person", ".").ConfigureAwait(true);

        Assert.Contains("ge-1", json, StringComparison.Ordinal);
    }

    // ── ListEntities ──

    /// <summary>FR-MCP-079: graphrag_list_entities calls ListEntitiesAsync.</summary>
    [Fact]
    public async Task GraphRagListEntities_CallsService_ReturnsJson()
    {
        var expected = new GraphEntityListResponse { Entities = [], TotalCount = 0 };
        _graphRagService.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagListEntities(".").ConfigureAwait(true);

        Assert.NotNull(json);
    }

    // ── GetEntity ──

    /// <summary>FR-MCP-079: graphrag_get_entity returns JSON when found.</summary>
    [Fact]
    public async Task GraphRagGetEntity_Found_ReturnsJson()
    {
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _graphRagService.GetEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagGetEntity("ge-1", ".").ConfigureAwait(true);

        Assert.Contains("ge-1", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-079: graphrag_get_entity returns error when not found.</summary>
    [Fact]
    public async Task GraphRagGetEntity_NotFound_ReturnsError()
    {
        _graphRagService.GetEntityAsync("ge-99", Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var json = await _tools.GraphRagGetEntity("ge-99", ".").ConfigureAwait(true);

        Assert.Contains("not found", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── UpdateEntity ──

    /// <summary>FR-MCP-079: graphrag_update_entity calls UpdateEntityAsync.</summary>
    [Fact]
    public async Task GraphRagUpdateEntity_Found_ReturnsJson()
    {
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Bob", EntityType = "person" };
        _graphRagService.UpdateEntityAsync("ge-1", Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagUpdateEntity("ge-1", "Bob", "person", ".").ConfigureAwait(true);

        Assert.Contains("Bob", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-079: graphrag_update_entity returns error when not found.</summary>
    [Fact]
    public async Task GraphRagUpdateEntity_NotFound_ReturnsError()
    {
        _graphRagService.UpdateEntityAsync("ge-99", Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var json = await _tools.GraphRagUpdateEntity("ge-99", "Bob", "person", ".").ConfigureAwait(true);

        Assert.Contains("not found", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteEntity ──

    /// <summary>FR-MCP-079: graphrag_delete_entity calls DeleteEntityAsync.</summary>
    [Fact]
    public async Task GraphRagDeleteEntity_CallsService_ReturnsJson()
    {
        _graphRagService.DeleteEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(true);

        var json = await _tools.GraphRagDeleteEntity("ge-1", ".").ConfigureAwait(true);

        Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── CreateRelationship ──

    /// <summary>FR-MCP-079: graphrag_create_relationship calls CreateRelationshipAsync.</summary>
    [Fact]
    public async Task GraphRagCreateRelationship_CallsService_ReturnsJson()
    {
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _graphRagService.CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagCreateRelationship("ge-1", "ge-2", "knows", ".").ConfigureAwait(true);

        Assert.Contains("gr-1", json, StringComparison.Ordinal);
    }

    // ── ListRelationships ──

    /// <summary>FR-MCP-079: graphrag_list_relationships calls ListRelationshipsAsync.</summary>
    [Fact]
    public async Task GraphRagListRelationships_CallsService_ReturnsJson()
    {
        var expected = new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 };
        _graphRagService.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagListRelationships(".").ConfigureAwait(true);

        Assert.NotNull(json);
    }

    // ── GetRelationship ──

    /// <summary>FR-MCP-079: graphrag_get_relationship returns JSON when found.</summary>
    [Fact]
    public async Task GraphRagGetRelationship_Found_ReturnsJson()
    {
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _graphRagService.GetRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagGetRelationship("gr-1", ".").ConfigureAwait(true);

        Assert.Contains("gr-1", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-079: graphrag_get_relationship returns error when not found.</summary>
    [Fact]
    public async Task GraphRagGetRelationship_NotFound_ReturnsError()
    {
        _graphRagService.GetRelationshipAsync("gr-99", Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var json = await _tools.GraphRagGetRelationship("gr-99", ".").ConfigureAwait(true);

        Assert.Contains("not found", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── UpdateRelationship ──

    /// <summary>FR-MCP-079: graphrag_update_relationship calls UpdateRelationshipAsync.</summary>
    [Fact]
    public async Task GraphRagUpdateRelationship_Found_ReturnsJson()
    {
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        _graphRagService.UpdateRelationshipAsync("gr-1", Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var json = await _tools.GraphRagUpdateRelationship("gr-1", "ge-1", "ge-2", "works-with", ".").ConfigureAwait(true);

        Assert.Contains("works-with", json, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-079: graphrag_update_relationship returns error when not found.</summary>
    [Fact]
    public async Task GraphRagUpdateRelationship_NotFound_ReturnsError()
    {
        _graphRagService.UpdateRelationshipAsync("gr-99", Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var json = await _tools.GraphRagUpdateRelationship("gr-99", "ge-1", "ge-2", "works-with", ".").ConfigureAwait(true);

        Assert.Contains("not found", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteRelationship ──

    /// <summary>FR-MCP-079: graphrag_delete_relationship calls DeleteRelationshipAsync.</summary>
    [Fact]
    public async Task GraphRagDeleteRelationship_CallsService_ReturnsJson()
    {
        _graphRagService.DeleteRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(true);

        var json = await _tools.GraphRagDeleteRelationship("gr-1", ".").ConfigureAwait(true);

        Assert.Contains("true", json, StringComparison.OrdinalIgnoreCase);
    }
}
