using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// FR-MCP-078/079/080, TR-GRAPHRAG-ADHOC-001/002/003: Validates HTTP status codes and response
/// shapes for the GraphRAG ad-hoc management endpoints in <see cref="GraphRagController"/>.
/// Uses NSubstitute mocks for <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagControllerAdHocTests
{
    private readonly IGraphRagService _service = Substitute.For<IGraphRagService>();
    private readonly GraphRagController _controller;

    /// <summary>Initializes the controller with a mocked service.</summary>
    public GraphRagControllerAdHocTests()
    {
        _controller = new GraphRagController(_service);
    }

    // ── IngestText ──

    /// <summary>FR-MCP-078: POST documents/ingest returns 200 on success.</summary>
    [Fact]
    public async Task IngestText_ValidRequest_Returns200()
    {
        var request = new GraphRagIngestTextRequest { Content = "hello world" };
        var expected = new GraphRagIngestTextResponse { DocumentId = "doc-1", SourceType = "adhoc-text", SourceKey = "test" };
        _service.IngestTextAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.IngestTextAsync(request, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-078: POST documents/ingest returns 400 when content is empty.</summary>
    [Fact]
    public async Task IngestText_EmptyContent_Returns400()
    {
        var request = new GraphRagIngestTextRequest { Content = "" };

        var result = await _controller.IngestTextAsync(request, CancellationToken.None).ConfigureAwait(true);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, bad.StatusCode);
    }

    /// <summary>FR-MCP-078: POST documents/ingest returns 400 when request is null.</summary>
    [Fact]
    public async Task IngestText_NullRequest_Returns400()
    {
        var result = await _controller.IngestTextAsync(null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── ListDocuments ──

    /// <summary>FR-MCP-080: GET documents returns 200.</summary>
    [Fact]
    public async Task ListDocuments_Returns200()
    {
        var expected = new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 };
        _service.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.ListDocumentsAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ── GetDocumentChunks ──

    /// <summary>FR-MCP-080: GET documents/{id}/chunks returns 200 when found.</summary>
    [Fact]
    public async Task GetDocumentChunks_Found_Returns200()
    {
        var expected = new GraphRagDocumentChunksResponse { DocumentId = "doc-1", Chunks = [], TotalChunks = 0 };
        _service.GetDocumentChunksAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.GetDocumentChunksAsync("doc-1", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-080: GET documents/{id}/chunks returns 404 when not found.</summary>
    [Fact]
    public async Task GetDocumentChunks_NotFound_Returns404()
    {
        _service.GetDocumentChunksAsync("doc-99", Arg.Any<CancellationToken>()).Returns((GraphRagDocumentChunksResponse?)null);

        var result = await _controller.GetDocumentChunksAsync("doc-99", CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFound.StatusCode);
    }

    // ── DeleteDocument ──

    /// <summary>FR-MCP-080: DELETE documents/{id} returns 200 on success.</summary>
    [Fact]
    public async Task DeleteDocument_Success_Returns200()
    {
        var expected = new GraphRagDocumentDeleteResponse { DocumentId = "doc-1", ChunksRemoved = 3, Success = true };
        _service.DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.DeleteDocumentAsync("doc-1", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-080: DELETE documents/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task DeleteDocument_NotFound_Returns404()
    {
        var expected = new GraphRagDocumentDeleteResponse { DocumentId = "doc-99", ChunksRemoved = 0, Success = false };
        _service.DeleteDocumentAsync("doc-99", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.DeleteDocumentAsync("doc-99", CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFound.StatusCode);
    }

    // ── CreateEntity ──

    /// <summary>FR-MCP-079: POST entities returns 201.</summary>
    [Fact]
    public async Task CreateEntity_ValidRequest_Returns201()
    {
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _service.CreateEntityAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.CreateEntityAsync(request, CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
    }

    /// <summary>FR-MCP-079: POST entities returns 400 when name is missing.</summary>
    [Fact]
    public async Task CreateEntity_MissingName_Returns400()
    {
        var request = new GraphEntityRequest { Name = "", EntityType = "person" };

        var result = await _controller.CreateEntityAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── ListEntities ──

    /// <summary>FR-MCP-079: GET entities returns 200.</summary>
    [Fact]
    public async Task ListEntities_Returns200()
    {
        var expected = new GraphEntityListResponse { Entities = [], TotalCount = 0 };
        _service.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.ListEntitiesAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ── GetEntity ──

    /// <summary>FR-MCP-079: GET entities/{id} returns 200 when found.</summary>
    [Fact]
    public async Task GetEntity_Found_Returns200()
    {
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" };
        _service.GetEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.GetEntityAsync("ge-1", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-079: GET entities/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task GetEntity_NotFound_Returns404()
    {
        _service.GetEntityAsync("ge-99", Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var result = await _controller.GetEntityAsync("ge-99", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── UpdateEntity ──

    /// <summary>FR-MCP-079: PUT entities/{id} returns 200 when found.</summary>
    [Fact]
    public async Task UpdateEntity_Found_Returns200()
    {
        var request = new GraphEntityRequest { Name = "Bob", EntityType = "person" };
        var expected = new GraphEntityResponse { Id = "ge-1", Name = "Bob", EntityType = "person" };
        _service.UpdateEntityAsync("ge-1", request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.UpdateEntityAsync("ge-1", request, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-079: PUT entities/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task UpdateEntity_NotFound_Returns404()
    {
        var request = new GraphEntityRequest { Name = "Bob", EntityType = "person" };
        _service.UpdateEntityAsync("ge-99", request, Arg.Any<CancellationToken>()).Returns((GraphEntityResponse?)null);

        var result = await _controller.UpdateEntityAsync("ge-99", request, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── DeleteEntity ──

    /// <summary>FR-MCP-079: DELETE entities/{id} returns 204 when found.</summary>
    [Fact]
    public async Task DeleteEntity_Found_Returns204()
    {
        _service.DeleteEntityAsync("ge-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteEntityAsync("ge-1", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>FR-MCP-079: DELETE entities/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task DeleteEntity_NotFound_Returns404()
    {
        _service.DeleteEntityAsync("ge-99", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteEntityAsync("ge-99", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── CreateRelationship ──

    /// <summary>FR-MCP-079: POST relationships returns 201.</summary>
    [Fact]
    public async Task CreateRelationship_ValidRequest_Returns201()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _service.CreateRelationshipAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.CreateRelationshipAsync(request, CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
    }

    /// <summary>FR-MCP-079: POST relationships returns 400 when required fields missing.</summary>
    [Fact]
    public async Task CreateRelationship_MissingFields_Returns400()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "", TargetEntityId = "ge-2", RelationshipType = "knows" };

        var result = await _controller.CreateRelationshipAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ── ListRelationships ──

    /// <summary>FR-MCP-079: GET relationships returns 200.</summary>
    [Fact]
    public async Task ListRelationships_Returns200()
    {
        var expected = new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 };
        _service.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.ListRelationshipsAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    // ── GetRelationship ──

    /// <summary>FR-MCP-079: GET relationships/{id} returns 200 when found.</summary>
    [Fact]
    public async Task GetRelationship_Found_Returns200()
    {
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "knows" };
        _service.GetRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.GetRelationshipAsync("gr-1", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-079: GET relationships/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task GetRelationship_NotFound_Returns404()
    {
        _service.GetRelationshipAsync("gr-99", Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var result = await _controller.GetRelationshipAsync("gr-99", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── UpdateRelationship ──

    /// <summary>FR-MCP-079: PUT relationships/{id} returns 200 when found.</summary>
    [Fact]
    public async Task UpdateRelationship_Found_Returns200()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        var expected = new GraphRelationshipResponse { Id = "gr-1", SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        _service.UpdateRelationshipAsync("gr-1", request, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _controller.UpdateRelationshipAsync("gr-1", request, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, ok.StatusCode);
    }

    /// <summary>FR-MCP-079: PUT relationships/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task UpdateRelationship_NotFound_Returns404()
    {
        var request = new GraphRelationshipRequest { SourceEntityId = "ge-1", TargetEntityId = "ge-2", RelationshipType = "works-with" };
        _service.UpdateRelationshipAsync("gr-99", request, Arg.Any<CancellationToken>()).Returns((GraphRelationshipResponse?)null);

        var result = await _controller.UpdateRelationshipAsync("gr-99", request, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ── DeleteRelationship ──

    /// <summary>FR-MCP-079: DELETE relationships/{id} returns 204 when found.</summary>
    [Fact]
    public async Task DeleteRelationship_Found_Returns204()
    {
        _service.DeleteRelationshipAsync("gr-1", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _controller.DeleteRelationshipAsync("gr-1", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NoContentResult>(result);
    }

    /// <summary>FR-MCP-079: DELETE relationships/{id} returns 404 when not found.</summary>
    [Fact]
    public async Task DeleteRelationship_NotFound_Returns404()
    {
        _service.DeleteRelationshipAsync("gr-99", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _controller.DeleteRelationshipAsync("gr-99", CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
