using McpServer.Cqrs;
using McpServer.GraphRag.Commands;
using McpServer.GraphRag.Queries;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003: Validates CQRS command and query handlers
/// for GraphRAG ad-hoc management delegate to <see cref="IGraphRagService"/> correctly and wrap
/// results in <see cref="Result{T}"/> success/failure.
/// </summary>
public sealed class GraphRagCqrsTests
{
    private readonly IGraphRagService _service = Substitute.For<IGraphRagService>();
    private readonly CallContext _ctx = new();

    // ── IngestText Command ──

    /// <summary>FR-MCP-078: IngestText handler returns Success on normal path.</summary>
    [Fact]
    public async Task IngestTextHandler_Success_ReturnsResult()
    {
        var request = new GraphRagIngestTextRequest { Content = "hello" };
        var expected = new GraphRagIngestTextResponse { DocumentId = "doc-1", SourceType = "adhoc-text", SourceKey = "test" };
        _service.IngestTextAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagIngestTextCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagIngestTextCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("doc-1", result.Value!.DocumentId);
    }

    /// <summary>FR-MCP-078: IngestText handler returns Failure on exception.</summary>
    [Fact]
    public async Task IngestTextHandler_Exception_ReturnsFailure()
    {
        var request = new GraphRagIngestTextRequest { Content = "hello" };
        _service.IngestTextAsync(request, Arg.Any<CancellationToken>()).Throws(new InvalidOperationException("boom"));

        var handler = new GraphRagIngestTextCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagIngestTextCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
        Assert.Contains("boom", result.Error, StringComparison.Ordinal);
    }

    // ── DeleteDocument Command ──

    /// <summary>FR-MCP-080: DeleteDocument handler returns Success.</summary>
    [Fact]
    public async Task DeleteDocumentHandler_Success_ReturnsResult()
    {
        var expected = new GraphRagDocumentDeleteResponse { DocumentId = "doc-1", ChunksRemoved = 3, Success = true };
        _service.DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagDeleteDocumentCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagDeleteDocumentCommand("ws", "doc-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("doc-1", result.Value!.DocumentId);
    }

    /// <summary>FR-MCP-080: DeleteDocument handler returns Failure on exception.</summary>
    [Fact]
    public async Task DeleteDocumentHandler_Exception_ReturnsFailure()
    {
        _service.DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var handler = new GraphRagDeleteDocumentCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagDeleteDocumentCommand("ws", "doc-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── CreateEntity Command ──

    /// <summary>FR-MCP-079: CreateEntity handler returns Success.</summary>
    [Fact]
    public async Task CreateEntityHandler_Success_ReturnsResult()
    {
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _service.CreateEntityAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagCreateEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagCreateEntityCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("ge-1", result.Value!.Id);
    }

    /// <summary>FR-MCP-079: CreateEntity handler returns Failure on exception.</summary>
    [Fact]
    public async Task CreateEntityHandler_Exception_ReturnsFailure()
    {
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };
        _service.CreateEntityAsync(request, Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var handler = new GraphRagCreateEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagCreateEntityCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── UpdateEntity Command ──

    /// <summary>FR-MCP-079: UpdateEntity handler returns Success when entity found.</summary>
    [Fact]
    public async Task UpdateEntityHandler_Success_ReturnsResult()
    {
        var request = new GraphEntityRequest { Name = "Bob", EntityType = "person" };
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Bob", EntityType = "person" };
        _service.UpdateEntityAsync("ge-1", request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagUpdateEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagUpdateEntityCommand("ws", "ge-1", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bob", result.Value!.Name);
    }

    /// <summary>FR-MCP-079: UpdateEntity handler returns Failure when entity not found.</summary>
    [Fact]
    public async Task UpdateEntityHandler_NotFound_ReturnsFailure()
    {
        var request = new GraphEntityRequest { Name = "Bob", EntityType = "person" };
        _service.UpdateEntityAsync("ge-99", request, Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var handler = new GraphRagUpdateEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagUpdateEntityCommand("ws", "ge-99", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteEntity Command ──

    /// <summary>FR-MCP-079: DeleteEntity handler returns Success with true when deleted.</summary>
    [Fact]
    public async Task DeleteEntityHandler_Success_ReturnsTrue()
    {
        _service.DeleteEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new GraphRagDeleteEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagDeleteEntityCommand("ws", "ge-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    /// <summary>FR-MCP-079: DeleteEntity handler returns Success with false when not found.</summary>
    [Fact]
    public async Task DeleteEntityHandler_NotFound_ReturnsFalse()
    {
        _service.DeleteEntityAsync("ge-99", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new GraphRagDeleteEntityCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagDeleteEntityCommand("ws", "ge-99"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    // ── CreateRelationship Command ──

    /// <summary>FR-MCP-079: CreateRelationship handler returns Success.</summary>
    [Fact]
    public async Task CreateRelationshipHandler_Success_ReturnsResult()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _service.CreateRelationshipAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagCreateRelationshipCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagCreateRelationshipCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("gr-1", result.Value!.Id);
    }

    /// <summary>FR-MCP-079: CreateRelationship handler returns Failure on exception.</summary>
    [Fact]
    public async Task CreateRelationshipHandler_Exception_ReturnsFailure()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _service.CreateRelationshipAsync(request, Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var handler = new GraphRagCreateRelationshipCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagCreateRelationshipCommand("ws", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── UpdateRelationship Command ──

    /// <summary>FR-MCP-079: UpdateRelationship handler returns Success when found.</summary>
    [Fact]
    public async Task UpdateRelationshipHandler_Success_ReturnsResult()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        _service.UpdateRelationshipAsync("gr-1", request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagUpdateRelationshipCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagUpdateRelationshipCommand("ws", "gr-1", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("works-with", result.Value!.RelationshipType);
    }

    /// <summary>FR-MCP-079: UpdateRelationship handler returns Failure when not found.</summary>
    [Fact]
    public async Task UpdateRelationshipHandler_NotFound_ReturnsFailure()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        _service.UpdateRelationshipAsync("gr-99", request, Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var handler = new GraphRagUpdateRelationshipCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagUpdateRelationshipCommand("ws", "gr-99", request), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── DeleteRelationship Command ──

    /// <summary>FR-MCP-079: DeleteRelationship handler returns Success with true.</summary>
    [Fact]
    public async Task DeleteRelationshipHandler_Success_ReturnsTrue()
    {
        _service.DeleteRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(true);

        var handler = new GraphRagDeleteRelationshipCommandHandler(_service);
        var result = await handler.HandleAsync(new GraphRagDeleteRelationshipCommand("ws", "gr-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    // ── ListDocuments Query ──

    /// <summary>FR-MCP-080: ListDocuments handler returns Success.</summary>
    [Fact]
    public async Task ListDocumentsHandler_Success_ReturnsResult()
    {
        var expected = new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 };
        _service.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagListDocumentsQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagListDocumentsQuery("ws", 0, 50, null), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalCount);
    }

    /// <summary>FR-MCP-080: ListDocuments handler returns Failure on exception.</summary>
    [Fact]
    public async Task ListDocumentsHandler_Exception_ReturnsFailure()
    {
        _service.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var handler = new GraphRagListDocumentsQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagListDocumentsQuery("ws", 0, 50, null), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── GetDocumentChunks Query ──

    /// <summary>FR-MCP-080: GetDocumentChunks handler returns Success when document exists.</summary>
    [Fact]
    public async Task GetDocumentChunksHandler_Success_ReturnsResult()
    {
        var expected = new GraphRagDocumentChunksResponse { DocumentId = "doc-1", Chunks = [], TotalChunks = 0 };
        _service.GetDocumentChunksAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagGetDocumentChunksQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetDocumentChunksQuery("ws", "doc-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("doc-1", result.Value!.DocumentId);
    }

    /// <summary>FR-MCP-080: GetDocumentChunks handler returns Failure when document not found.</summary>
    [Fact]
    public async Task GetDocumentChunksHandler_NotFound_ReturnsFailure()
    {
        _service.GetDocumentChunksAsync("doc-99", Arg.Any<CancellationToken>()).Returns((GraphRagDocumentChunksResponse?)null);

        var handler = new GraphRagGetDocumentChunksQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetDocumentChunksQuery("ws", "doc-99"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── GetEntity Query ──

    /// <summary>FR-MCP-079: GetEntity handler returns Success when entity exists.</summary>
    [Fact]
    public async Task GetEntityHandler_Success_ReturnsResult()
    {
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _service.GetEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagGetEntityQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetEntityQuery("ws", "ge-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("ge-1", result.Value!.Id);
    }

    /// <summary>FR-MCP-079: GetEntity handler returns Failure when entity not found.</summary>
    [Fact]
    public async Task GetEntityHandler_NotFound_ReturnsFailure()
    {
        _service.GetEntityAsync("ge-99", Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var handler = new GraphRagGetEntityQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetEntityQuery("ws", "ge-99"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── ListEntities Query ──

    /// <summary>FR-MCP-079: ListEntities handler returns Success.</summary>
    [Fact]
    public async Task ListEntitiesHandler_Success_ReturnsResult()
    {
        var expected = new GraphEntityListResponse { Entities = [], TotalCount = 0 };
        _service.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagListEntitiesQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagListEntitiesQuery("ws", 0, 50, null), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    // ── GetRelationship Query ──

    /// <summary>FR-MCP-079: GetRelationship handler returns Success when found.</summary>
    [Fact]
    public async Task GetRelationshipHandler_Success_ReturnsResult()
    {
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _service.GetRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagGetRelationshipQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetRelationshipQuery("ws", "gr-1"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
        Assert.Equal("gr-1", result.Value!.Id);
    }

    /// <summary>FR-MCP-079: GetRelationship handler returns Failure when not found.</summary>
    [Fact]
    public async Task GetRelationshipHandler_NotFound_ReturnsFailure()
    {
        _service.GetRelationshipAsync("gr-99", Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var handler = new GraphRagGetRelationshipQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagGetRelationshipQuery("ws", "gr-99"), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }

    // ── ListRelationships Query ──

    /// <summary>FR-MCP-079: ListRelationships handler returns Success.</summary>
    [Fact]
    public async Task ListRelationshipsHandler_Success_ReturnsResult()
    {
        var expected = new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 };
        _service.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = new GraphRagListRelationshipsQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagListRelationshipsQuery("ws", 0, 50, null, null), _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    /// <summary>FR-MCP-079: ListRelationships handler returns Failure on exception.</summary>
    [Fact]
    public async Task ListRelationshipsHandler_Exception_ReturnsFailure()
    {
        _service.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>()).Throws(new Exception("fail"));

        var handler = new GraphRagListRelationshipsQueryHandler(_service);
        var result = await handler.HandleAsync(new GraphRagListRelationshipsQuery("ws", 0, 50, null, null), _ctx).ConfigureAwait(true);

        Assert.True(result.IsFailure);
    }
}
