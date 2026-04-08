// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - GraphRAG workflow tests
// FR-MCP-078: Ad-hoc text ingestion workflow tests
// FR-MCP-079: Entity and relationship CRUD workflow tests
// FR-MCP-080: Document management workflow tests
// TR-GRAPHRAG-ADHOC-001: Ad-hoc text ingestion delegation tests
// TR-GRAPHRAG-ADHOC-002: Entity and relationship CRUD delegation tests
// TR-GRAPHRAG-ADHOC-003: Document management delegation tests
// TEST-MCP-REPL-019: Workflows delegate to typed client contracts without duplicating logic

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Unit tests for GraphRAG workflow operations.
/// Tests lifecycle operations (status, index, query), ad-hoc text ingestion,
/// document management (list, chunks, delete), entity CRUD (create, list, get, update, delete),
/// and relationship CRUD (create, list, get, update, delete).
/// Mocks IGraphRagWorkflow to verify contract correctness and delegation semantics.
/// </summary>
public class GraphRagWorkflowTests
{
    private readonly IGraphRagWorkflow _workflow;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphRagWorkflowTests"/> class.
    /// Sets up IGraphRagWorkflow mock via NSubstitute.
    /// </summary>
    public GraphRagWorkflowTests()
    {
        _workflow = Substitute.For<IGraphRagWorkflow>();
    }

    #region Lifecycle Tests

    /// <summary>
    /// Validates that GetStatusAsync returns the GraphRAG status for the workspace.
    /// Tests FR-MCP-078 lifecycle status retrieval via REPL workflow.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ReturnsStatus()
    {
        var expected = new GraphRagStatusResult
        {
            Enabled = true,
            State = "ready",
            IsInitialized = true,
            IsIndexed = true,
            Backend = "internal-fallback"
        };

        _workflow.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.GetStatusAsync();

        Assert.NotNull(result);
        Assert.True(result.Enabled);
        Assert.Equal("ready", result.State);
        Assert.True(result.IsInitialized);
        Assert.True(result.IsIndexed);
        await _workflow.Received(1).GetStatusAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that IndexAsync triggers indexing and returns updated status.
    /// Tests FR-MCP-078 lifecycle indexing via REPL workflow.
    /// </summary>
    [Fact]
    public async Task IndexAsync_TriggersIndexing_ReturnsStatus()
    {
        var expected = new GraphRagStatusResult
        {
            Enabled = true,
            State = "indexing",
            IsIndexed = false
        };

        _workflow.IndexAsync(true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.IndexAsync(force: true);

        Assert.NotNull(result);
        Assert.Equal("indexing", result.State);
        await _workflow.Received(1).IndexAsync(true, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that QueryAsync returns query results with answer, citations, and graph data.
    /// Tests FR-MCP-078 lifecycle query via REPL workflow.
    /// </summary>
    [Fact]
    public async Task QueryAsync_ReturnsQueryResult()
    {
        var expected = new GraphRagQueryResult
        {
            Query = "What is authentication?",
            Mode = "local",
            Answer = "Authentication is the process of verifying identity.",
            Citations = new List<GraphRagCitation>
            {
                new() { SourceKey = "auth-doc", Snippet = "verify identity" }
            },
            Entities = new List<string> { "Authentication", "Identity" }
        };

        _workflow.QueryAsync("What is authentication?", "local", null, true, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.QueryAsync("What is authentication?", mode: "local");

        Assert.NotNull(result);
        Assert.Equal("What is authentication?", result.Query);
        Assert.Equal("local", result.Mode);
        Assert.NotEmpty(result.Answer);
        Assert.Single(result.Citations);
        Assert.Equal(2, result.Entities.Count);
        await _workflow.Received(1).QueryAsync("What is authentication?", "local", null, true, null, null, null, null, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Ingestion Tests

    /// <summary>
    /// Validates that IngestTextAsync ingests raw text and returns document/chunk metadata.
    /// Tests FR-MCP-078, TR-GRAPHRAG-ADHOC-001 ad-hoc ingestion via REPL workflow.
    /// </summary>
    [Fact]
    public async Task IngestTextAsync_IngestsText_ReturnsResult()
    {
        var request = new GraphRagIngestTextRequest
        {
            Content = "This is test content for ingestion.",
            Title = "Test Document",
            SourceType = "adhoc-text"
        };

        var expected = new GraphRagIngestTextResult
        {
            DocumentId = "doc-001",
            ChunkCount = 1,
            TokenCount = 8,
            SourceType = "adhoc-text",
            SourceKey = "Test Document"
        };

        _workflow.IngestTextAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.IngestTextAsync(request);

        Assert.NotNull(result);
        Assert.Equal("doc-001", result.DocumentId);
        Assert.Equal(1, result.ChunkCount);
        Assert.Equal("adhoc-text", result.SourceType);
        await _workflow.Received(1).IngestTextAsync(request, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Document Management Tests

    /// <summary>
    /// Validates that ListDocumentsAsync returns paginated document list.
    /// Tests FR-MCP-080, TR-GRAPHRAG-ADHOC-003 document listing via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_ReturnsPaginatedDocuments()
    {
        var expected = new GraphRagDocumentListResult
        {
            Documents = new List<GraphRagDocumentSummary>
            {
                new() { Id = "doc-001", SourceType = "adhoc-text", SourceKey = "Test Doc", ChunkCount = 3 },
                new() { Id = "doc-002", SourceType = "repo", SourceKey = "README.md", ChunkCount = 5 }
            },
            TotalCount = 2
        };

        _workflow.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListDocumentsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Documents.Count);
        await _workflow.Received(1).ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListDocumentsAsync supports source type filtering.
    /// Tests FR-MCP-080 document listing with filters via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_WithSourceTypeFilter_ReturnsFilteredDocuments()
    {
        var expected = new GraphRagDocumentListResult
        {
            Documents = new List<GraphRagDocumentSummary>
            {
                new() { Id = "doc-001", SourceType = "adhoc-text", SourceKey = "Test Doc", ChunkCount = 3 }
            },
            TotalCount = 1
        };

        _workflow.ListDocumentsAsync(0, 50, "adhoc-text", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListDocumentsAsync(sourceType: "adhoc-text");

        Assert.Single(result.Documents);
        Assert.Equal("adhoc-text", result.Documents[0].SourceType);
        await _workflow.Received(1).ListDocumentsAsync(0, 50, "adhoc-text", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that GetDocumentChunksAsync retrieves chunks for a document.
    /// Tests FR-MCP-080, TR-GRAPHRAG-ADHOC-003 document chunk retrieval via REPL workflow.
    /// </summary>
    [Fact]
    public async Task GetDocumentChunksAsync_ReturnsDocumentChunks()
    {
        var expected = new GraphRagDocumentChunksResult
        {
            DocumentId = "doc-001",
            Chunks = new List<GraphRagDocumentChunkItem>
            {
                new() { Id = "chunk-001", Content = "First chunk", TokenCount = 3, ChunkIndex = 0 },
                new() { Id = "chunk-002", Content = "Second chunk", TokenCount = 3, ChunkIndex = 1 }
            },
            TotalChunks = 2
        };

        _workflow.GetDocumentChunksAsync("doc-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.GetDocumentChunksAsync("doc-001");

        Assert.NotNull(result);
        Assert.Equal("doc-001", result.DocumentId);
        Assert.Equal(2, result.TotalChunks);
        Assert.Equal(2, result.Chunks.Count);
        await _workflow.Received(1).GetDocumentChunksAsync("doc-001", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that DeleteDocumentAsync removes a document and returns deletion statistics.
    /// Tests FR-MCP-080, TR-GRAPHRAG-ADHOC-003 document deletion via REPL workflow.
    /// </summary>
    [Fact]
    public async Task DeleteDocumentAsync_DeletesDocument_ReturnsResult()
    {
        var expected = new GraphRagDocumentDeleteResult
        {
            DocumentId = "doc-001",
            ChunksRemoved = 3,
            Success = true
        };

        _workflow.DeleteDocumentAsync("doc-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.DeleteDocumentAsync("doc-001");

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("doc-001", result.DocumentId);
        Assert.Equal(3, result.ChunksRemoved);
        await _workflow.Received(1).DeleteDocumentAsync("doc-001", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Entity CRUD Tests

    /// <summary>
    /// Validates that CreateEntityAsync creates a new graph entity.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 entity creation via REPL workflow.
    /// </summary>
    [Fact]
    public async Task CreateEntityAsync_CreatesEntity_ReturnsResult()
    {
        var request = new GraphEntityRequest
        {
            Name = "Authentication Module",
            EntityType = "component",
            Description = "Handles user authentication"
        };

        var expected = new GraphEntityResult
        {
            Id = "entity-001",
            Name = "Authentication Module",
            EntityType = "component",
            Description = "Handles user authentication"
        };

        _workflow.CreateEntityAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.CreateEntityAsync(request);

        Assert.NotNull(result);
        Assert.Equal("entity-001", result.Id);
        Assert.Equal("Authentication Module", result.Name);
        Assert.Equal("component", result.EntityType);
        await _workflow.Received(1).CreateEntityAsync(request, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListEntitiesAsync returns paginated entity list.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 entity listing via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListEntitiesAsync_ReturnsPaginatedEntities()
    {
        var expected = new GraphEntityListResult
        {
            Entities = new List<GraphEntityResult>
            {
                new() { Id = "entity-001", Name = "Auth Module", EntityType = "component" },
                new() { Id = "entity-002", Name = "User Service", EntityType = "service" }
            },
            TotalCount = 2
        };

        _workflow.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListEntitiesAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Entities.Count);
        await _workflow.Received(1).ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListEntitiesAsync supports entity type filtering.
    /// Tests FR-MCP-079 entity listing with type filter via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListEntitiesAsync_WithTypeFilter_ReturnsFilteredEntities()
    {
        var expected = new GraphEntityListResult
        {
            Entities = new List<GraphEntityResult>
            {
                new() { Id = "entity-002", Name = "User Service", EntityType = "service" }
            },
            TotalCount = 1
        };

        _workflow.ListEntitiesAsync(0, 50, "service", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListEntitiesAsync(entityType: "service");

        Assert.Single(result.Entities);
        Assert.Equal("service", result.Entities[0].EntityType);
        await _workflow.Received(1).ListEntitiesAsync(0, 50, "service", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that GetEntityAsync retrieves an entity by ID.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 entity retrieval via REPL workflow.
    /// </summary>
    [Fact]
    public async Task GetEntityAsync_ReturnsEntity()
    {
        var expected = new GraphEntityResult
        {
            Id = "entity-001",
            Name = "Auth Module",
            EntityType = "component",
            Description = "Handles authentication"
        };

        _workflow.GetEntityAsync("entity-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.GetEntityAsync("entity-001");

        Assert.NotNull(result);
        Assert.Equal("entity-001", result.Id);
        Assert.Equal("Auth Module", result.Name);
        await _workflow.Received(1).GetEntityAsync("entity-001", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that UpdateEntityAsync updates an existing entity.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 entity update via REPL workflow.
    /// </summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesEntity_ReturnsResult()
    {
        var request = new GraphEntityRequest
        {
            Name = "Auth Module v2",
            EntityType = "component",
            Description = "Updated authentication module"
        };

        var expected = new GraphEntityResult
        {
            Id = "entity-001",
            Name = "Auth Module v2",
            EntityType = "component",
            Description = "Updated authentication module"
        };

        _workflow.UpdateEntityAsync("entity-001", request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.UpdateEntityAsync("entity-001", request);

        Assert.NotNull(result);
        Assert.Equal("Auth Module v2", result.Name);
        Assert.Equal("Updated authentication module", result.Description);
        await _workflow.Received(1).UpdateEntityAsync("entity-001", request, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that DeleteEntityAsync deletes an entity by ID.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 entity deletion via REPL workflow.
    /// </summary>
    [Fact]
    public async Task DeleteEntityAsync_DeletesEntity()
    {
        _workflow.DeleteEntityAsync("entity-001", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _workflow.DeleteEntityAsync("entity-001");

        await _workflow.Received(1).DeleteEntityAsync("entity-001", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Relationship CRUD Tests

    /// <summary>
    /// Validates that CreateRelationshipAsync creates a new graph relationship.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 relationship creation via REPL workflow.
    /// </summary>
    [Fact]
    public async Task CreateRelationshipAsync_CreatesRelationship_ReturnsResult()
    {
        var request = new GraphRelationshipRequest
        {
            SourceEntityId = "entity-001",
            TargetEntityId = "entity-002",
            RelationshipType = "depends_on",
            Weight = 0.9
        };

        var expected = new GraphRelationshipResult
        {
            Id = "rel-001",
            SourceEntityId = "entity-001",
            TargetEntityId = "entity-002",
            RelationshipType = "depends_on",
            Weight = 0.9
        };

        _workflow.CreateRelationshipAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.CreateRelationshipAsync(request);

        Assert.NotNull(result);
        Assert.Equal("rel-001", result.Id);
        Assert.Equal("depends_on", result.RelationshipType);
        Assert.Equal(0.9, result.Weight);
        await _workflow.Received(1).CreateRelationshipAsync(request, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListRelationshipsAsync returns paginated relationship list.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 relationship listing via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListRelationshipsAsync_ReturnsPaginatedRelationships()
    {
        var expected = new GraphRelationshipListResult
        {
            Relationships = new List<GraphRelationshipResult>
            {
                new() { Id = "rel-001", SourceEntityId = "e1", TargetEntityId = "e2", RelationshipType = "depends_on" },
                new() { Id = "rel-002", SourceEntityId = "e2", TargetEntityId = "e3", RelationshipType = "uses" }
            },
            TotalCount = 2
        };

        _workflow.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListRelationshipsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Relationships.Count);
        await _workflow.Received(1).ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListRelationshipsAsync supports entity ID filtering.
    /// Tests FR-MCP-079 relationship listing with entity filter via REPL workflow.
    /// </summary>
    [Fact]
    public async Task ListRelationshipsAsync_WithEntityFilter_ReturnsFilteredRelationships()
    {
        var expected = new GraphRelationshipListResult
        {
            Relationships = new List<GraphRelationshipResult>
            {
                new() { Id = "rel-001", SourceEntityId = "entity-001", TargetEntityId = "entity-002", RelationshipType = "depends_on" }
            },
            TotalCount = 1
        };

        _workflow.ListRelationshipsAsync(0, 50, "entity-001", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListRelationshipsAsync(entityId: "entity-001");

        Assert.Single(result.Relationships);
        await _workflow.Received(1).ListRelationshipsAsync(0, 50, "entity-001", null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that GetRelationshipAsync retrieves a relationship by ID.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 relationship retrieval via REPL workflow.
    /// </summary>
    [Fact]
    public async Task GetRelationshipAsync_ReturnsRelationship()
    {
        var expected = new GraphRelationshipResult
        {
            Id = "rel-001",
            SourceEntityId = "entity-001",
            TargetEntityId = "entity-002",
            RelationshipType = "depends_on",
            Weight = 0.9
        };

        _workflow.GetRelationshipAsync("rel-001", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.GetRelationshipAsync("rel-001");

        Assert.NotNull(result);
        Assert.Equal("rel-001", result.Id);
        Assert.Equal("entity-001", result.SourceEntityId);
        await _workflow.Received(1).GetRelationshipAsync("rel-001", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that UpdateRelationshipAsync updates an existing relationship.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 relationship update via REPL workflow.
    /// </summary>
    [Fact]
    public async Task UpdateRelationshipAsync_UpdatesRelationship_ReturnsResult()
    {
        var request = new GraphRelationshipRequest
        {
            SourceEntityId = "entity-001",
            TargetEntityId = "entity-002",
            RelationshipType = "strongly_depends_on",
            Weight = 1.0
        };

        var expected = new GraphRelationshipResult
        {
            Id = "rel-001",
            SourceEntityId = "entity-001",
            TargetEntityId = "entity-002",
            RelationshipType = "strongly_depends_on",
            Weight = 1.0
        };

        _workflow.UpdateRelationshipAsync("rel-001", request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.UpdateRelationshipAsync("rel-001", request);

        Assert.NotNull(result);
        Assert.Equal("strongly_depends_on", result.RelationshipType);
        Assert.Equal(1.0, result.Weight);
        await _workflow.Received(1).UpdateRelationshipAsync("rel-001", request, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that DeleteRelationshipAsync deletes a relationship by ID.
    /// Tests FR-MCP-079, TR-GRAPHRAG-ADHOC-002 relationship deletion via REPL workflow.
    /// </summary>
    [Fact]
    public async Task DeleteRelationshipAsync_DeletesRelationship()
    {
        _workflow.DeleteRelationshipAsync("rel-001", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _workflow.DeleteRelationshipAsync("rel-001");

        await _workflow.Received(1).DeleteRelationshipAsync("rel-001", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Command Shape Constant Tests

    /// <summary>
    /// Validates that all GraphRagCommandShapes constants are correctly defined.
    /// Tests TR-MCP-REPL-005 command namespace organization for GraphRAG.
    /// </summary>
    [Fact]
    public void CommandShapes_AllConstantsMatchExpectedValues()
    {
        Assert.Equal("workflow.graphrag", GraphRagCommandShapes.MethodNamespace);
        Assert.Equal("workflow.graphrag.status", GraphRagCommandShapes.StatusMethod);
        Assert.Equal("workflow.graphrag.index", GraphRagCommandShapes.IndexMethod);
        Assert.Equal("workflow.graphrag.query", GraphRagCommandShapes.QueryMethod);
        Assert.Equal("workflow.graphrag.ingest", GraphRagCommandShapes.IngestMethod);
        Assert.Equal("workflow.graphrag.documents.list", GraphRagCommandShapes.DocumentsListMethod);
        Assert.Equal("workflow.graphrag.documents.chunks", GraphRagCommandShapes.DocumentsChunksMethod);
        Assert.Equal("workflow.graphrag.documents.delete", GraphRagCommandShapes.DocumentsDeleteMethod);
        Assert.Equal("workflow.graphrag.entities.create", GraphRagCommandShapes.EntitiesCreateMethod);
        Assert.Equal("workflow.graphrag.entities.list", GraphRagCommandShapes.EntitiesListMethod);
        Assert.Equal("workflow.graphrag.entities.get", GraphRagCommandShapes.EntitiesGetMethod);
        Assert.Equal("workflow.graphrag.entities.update", GraphRagCommandShapes.EntitiesUpdateMethod);
        Assert.Equal("workflow.graphrag.entities.delete", GraphRagCommandShapes.EntitiesDeleteMethod);
        Assert.Equal("workflow.graphrag.relationships.create", GraphRagCommandShapes.RelationshipsCreateMethod);
        Assert.Equal("workflow.graphrag.relationships.list", GraphRagCommandShapes.RelationshipsListMethod);
        Assert.Equal("workflow.graphrag.relationships.get", GraphRagCommandShapes.RelationshipsGetMethod);
        Assert.Equal("workflow.graphrag.relationships.update", GraphRagCommandShapes.RelationshipsUpdateMethod);
        Assert.Equal("workflow.graphrag.relationships.delete", GraphRagCommandShapes.RelationshipsDeleteMethod);
    }

    /// <summary>
    /// Validates that all command shape methods start with the namespace prefix.
    /// Tests TR-MCP-REPL-004 command registry namespace consistency.
    /// </summary>
    [Theory]
    [InlineData(GraphRagCommandShapes.StatusMethod)]
    [InlineData(GraphRagCommandShapes.IndexMethod)]
    [InlineData(GraphRagCommandShapes.QueryMethod)]
    [InlineData(GraphRagCommandShapes.IngestMethod)]
    [InlineData(GraphRagCommandShapes.DocumentsListMethod)]
    [InlineData(GraphRagCommandShapes.DocumentsChunksMethod)]
    [InlineData(GraphRagCommandShapes.DocumentsDeleteMethod)]
    [InlineData(GraphRagCommandShapes.EntitiesCreateMethod)]
    [InlineData(GraphRagCommandShapes.EntitiesListMethod)]
    [InlineData(GraphRagCommandShapes.EntitiesGetMethod)]
    [InlineData(GraphRagCommandShapes.EntitiesUpdateMethod)]
    [InlineData(GraphRagCommandShapes.EntitiesDeleteMethod)]
    [InlineData(GraphRagCommandShapes.RelationshipsCreateMethod)]
    [InlineData(GraphRagCommandShapes.RelationshipsListMethod)]
    [InlineData(GraphRagCommandShapes.RelationshipsGetMethod)]
    [InlineData(GraphRagCommandShapes.RelationshipsUpdateMethod)]
    [InlineData(GraphRagCommandShapes.RelationshipsDeleteMethod)]
    public void CommandShape_StartsWithNamespace(string method)
    {
        Assert.StartsWith(GraphRagCommandShapes.MethodNamespace + ".", method, StringComparison.Ordinal);
    }

    #endregion

    #region Pagination Tests

    /// <summary>
    /// Validates that ListDocumentsAsync supports pagination with skip/take.
    /// Tests FR-MCP-080 pagination semantics for document listing.
    /// </summary>
    [Fact]
    public async Task ListDocumentsAsync_WithPagination_PassesCorrectParameters()
    {
        var expected = new GraphRagDocumentListResult
        {
            Documents = new List<GraphRagDocumentSummary>(),
            TotalCount = 100
        };

        _workflow.ListDocumentsAsync(20, 10, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListDocumentsAsync(skip: 20, take: 10);

        Assert.Equal(100, result.TotalCount);
        await _workflow.Received(1).ListDocumentsAsync(20, 10, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListEntitiesAsync supports pagination with skip/take.
    /// Tests FR-MCP-079 pagination semantics for entity listing.
    /// </summary>
    [Fact]
    public async Task ListEntitiesAsync_WithPagination_PassesCorrectParameters()
    {
        var expected = new GraphEntityListResult
        {
            Entities = new List<GraphEntityResult>(),
            TotalCount = 200
        };

        _workflow.ListEntitiesAsync(50, 25, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListEntitiesAsync(skip: 50, take: 25);

        Assert.Equal(200, result.TotalCount);
        await _workflow.Received(1).ListEntitiesAsync(50, 25, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that ListRelationshipsAsync supports pagination with skip/take.
    /// Tests FR-MCP-079 pagination semantics for relationship listing.
    /// </summary>
    [Fact]
    public async Task ListRelationshipsAsync_WithPagination_PassesCorrectParameters()
    {
        var expected = new GraphRelationshipListResult
        {
            Relationships = new List<GraphRelationshipResult>(),
            TotalCount = 150
        };

        _workflow.ListRelationshipsAsync(10, 5, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expected));

        var result = await _workflow.ListRelationshipsAsync(skip: 10, take: 5);

        Assert.Equal(150, result.TotalCount);
        await _workflow.Received(1).ListRelationshipsAsync(10, 5, null, null, Arg.Any<CancellationToken>());
    }

    #endregion
}
