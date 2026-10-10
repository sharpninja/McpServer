using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-221 / FR-MCP-173: GraphRAG mutations skip coordinator/keyserver and
/// invoke the inner service even when turn transactions are required.
/// </summary>
public sealed class TransactionGatedGraphRagServiceTests
{
    /// <summary>GraphRAG indexing delegates to the inner service while required transactions are active.</summary>
    [Fact]
    public async Task IndexAsync_WhenRequiredTransactionsActive_DelegatesToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        inner.IndexAsync(Arg.Any<GraphRagIndexRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagStatusResponse());
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        await sut.IndexAsync(new GraphRagIndexRequest { Force = true }, CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .IndexAsync(Arg.Any<GraphRagIndexRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG text ingestion delegates to the inner service while required transactions are active.</summary>
    [Fact]
    public async Task IngestTextAsync_WhenRequiredTransactionsActive_DelegatesToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        await sut.IngestTextAsync(new GraphRagIngestTextRequest { Content = "hello" }, CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG document deletion delegates to the inner service while required transactions are active.</summary>
    [Fact]
    public async Task DeleteDocumentAsync_WhenRequiredTransactionsActive_DelegatesToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        await sut.DeleteDocumentAsync("doc-1", CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .DeleteDocumentAsync("doc-1", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG entity CRUD delegates to the inner service while required transactions are active.</summary>
    [Fact]
    public async Task EntityMutations_WhenRequiredTransactionsActive_DelegateToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };

        await sut.CreateEntityAsync(request, CancellationToken.None).ConfigureAwait(true);
        await sut.UpdateEntityAsync("ge-1", request, CancellationToken.None).ConfigureAwait(true);
        await sut.DeleteEntityAsync("ge-1", CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.Received(1)
            .UpdateEntityAsync("ge-1", Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.Received(1)
            .DeleteEntityAsync("ge-1", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG relationship CRUD delegates to the inner service while required transactions are active.</summary>
    [Fact]
    public async Task RelationshipMutations_WhenRequiredTransactionsActive_DelegateToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());
        var request = new GraphRelationshipRequest
        {
            SourceEntityId = "ge-1",
            TargetEntityId = "ge-2",
            RelationshipType = "knows",
        };

        await sut.CreateRelationshipAsync(request, CancellationToken.None).ConfigureAwait(true);
        await sut.UpdateRelationshipAsync("gr-1", request, CancellationToken.None).ConfigureAwait(true);
        await sut.DeleteRelationshipAsync("gr-1", CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.Received(1)
            .UpdateRelationshipAsync("gr-1", Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.Received(1)
            .DeleteRelationshipAsync("gr-1", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG initialize still delegates when the coordinator is degraded.</summary>
    [Fact]
    public async Task InitializeAsync_WhenCoordinatorDegraded_DelegatesToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        inner.InitializeAsync(Arg.Any<GraphRagStorageScope>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagStatusResponse());
        var coordinator = Substitute.For<ITurnTransactionCoordinator>();
        coordinator.GetStatus().Returns(new TurnTransactionStatusResponse
        {
            Enabled = true,
            Degraded = true,
            Message = "transaction gate unavailable",
        });
        var sut = CreateSut(inner, coordinator, RequiredOptions());

        await sut.InitializeAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);

        await inner.Received(1)
            .InitializeAsync(Arg.Any<GraphRagStorageScope>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG read methods continue to delegate while required transactions are active.</summary>
    [Fact]
    public async Task ReadMethods_WhenRequiredTransactionsActive_DelegateToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        inner.GetStatusAsync(Arg.Any<GraphRagStorageScope>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagStatusResponse { Enabled = true });
        inner.QueryAsync(Arg.Any<GraphRagQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagQueryResponse { Query = "hello", Answer = "world" });
        inner.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentListResponse { Documents = [], TotalCount = 0 });
        inner.GetDocumentChunksAsync("doc-1", Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentChunksResponse { DocumentId = "doc-1", Chunks = [], TotalChunks = 0 });
        inner.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [], TotalCount = 0 });
        inner.GetEntityAsync("ge-1", Arg.Any<CancellationToken>())
            .Returns(new GraphEntityResponse { Id = "ge-1", Name = "Alice", EntityType = "person" });
        inner.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipListResponse { Relationships = [], TotalCount = 0 });
        inner.GetRelationshipAsync("gr-1", Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipResponse
            {
                Id = "gr-1",
                SourceEntityId = "ge-1",
                TargetEntityId = "ge-2",
                RelationshipType = "knows",
            });
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        Assert.True((await sut.GetStatusAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true)).Enabled);
        Assert.Equal("world", (await sut.QueryAsync(new GraphRagQueryRequest { Query = "hello" }, CancellationToken.None).ConfigureAwait(true)).Answer);
        Assert.Empty((await sut.ListDocumentsAsync(ct: CancellationToken.None).ConfigureAwait(true)).Documents);
        Assert.Equal("doc-1", (await sut.GetDocumentChunksAsync("doc-1", CancellationToken.None).ConfigureAwait(true))!.DocumentId);
        Assert.Empty((await sut.ListEntitiesAsync(ct: CancellationToken.None).ConfigureAwait(true)).Entities);
        Assert.Equal("Alice", (await sut.GetEntityAsync("ge-1", CancellationToken.None).ConfigureAwait(true))!.Name);
        Assert.Empty((await sut.ListRelationshipsAsync(ct: CancellationToken.None).ConfigureAwait(true)).Relationships);
        Assert.Equal("gr-1", (await sut.GetRelationshipAsync("gr-1", CancellationToken.None).ConfigureAwait(true))!.Id);
    }

    /// <summary>GraphRAG mutations delegate when mutation transactions are not required.</summary>
    [Fact]
    public async Task IndexAsync_WhenTransactionsNotRequired_DelegatesToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        inner.IndexAsync(Arg.Any<GraphRagIndexRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GraphRagStatusResponse { Enabled = true, State = "indexed" });
        var sut = CreateSut(
            inner,
            RequiredCoordinator(),
            Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = false }));

        var result = await sut.IndexAsync(new GraphRagIndexRequest { Force = true }, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("indexed", result.State);
        await inner.Received(1)
            .IndexAsync(Arg.Any<GraphRagIndexRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    private static TransactionGatedGraphRagService CreateSut(
        IGraphRagService inner,
        ITurnTransactionCoordinator coordinator,
        IOptions<TurnTransactionOptions> options)
        => new(inner, coordinator, options);

    private static ITurnTransactionCoordinator RequiredCoordinator()
    {
        var coordinator = Substitute.For<ITurnTransactionCoordinator>();
        coordinator.GetStatus().Returns(new TurnTransactionStatusResponse
        {
            Enabled = true,
            Degraded = false,
            Message = "available",
        });
        return coordinator;
    }

    private static IOptions<TurnTransactionOptions> RequiredOptions()
        => Microsoft.Extensions.Options.Options.Create(new TurnTransactionOptions { Enabled = true, RequiredForMutations = true });
}
