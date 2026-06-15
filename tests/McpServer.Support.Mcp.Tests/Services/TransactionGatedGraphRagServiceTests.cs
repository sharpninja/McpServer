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
/// TEST-MCP-161: Verifies GraphRAG mutations fail closed while required turn transactions are active.
/// </summary>
public sealed class TransactionGatedGraphRagServiceTests
{
    /// <summary>GraphRAG indexing fails before calling the inner service while required transactions are active.</summary>
    [Fact]
    public async Task IndexAsync_WhenRequiredTransactionsActive_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.IndexAsync(new GraphRagIndexRequest { Force = true }, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("GraphRAG mutations are not transaction compensated", exception.Message, StringComparison.Ordinal);
        await inner.DidNotReceive()
            .IndexAsync(Arg.Any<GraphRagIndexRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG text ingestion fails before calling the inner service while required transactions are active.</summary>
    [Fact]
    public async Task IngestTextAsync_WhenRequiredTransactionsActive_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.IngestTextAsync(new GraphRagIngestTextRequest { Content = "hello" }, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("GraphRAG mutations are not transaction compensated", exception.Message, StringComparison.Ordinal);
        await inner.DidNotReceive()
            .IngestTextAsync(Arg.Any<GraphRagIngestTextRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG document deletion fails before calling the inner service while required transactions are active.</summary>
    [Fact]
    public async Task DeleteDocumentAsync_WhenRequiredTransactionsActive_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteDocumentAsync("doc-1", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("GraphRAG mutations are not transaction compensated", exception.Message, StringComparison.Ordinal);
        await inner.DidNotReceive()
            .DeleteDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG entity CRUD fails before calling the inner service while required transactions are active.</summary>
    [Fact]
    public async Task EntityMutations_WhenRequiredTransactionsActive_FailClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };

        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.CreateEntityAsync(request, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.UpdateEntityAsync("ge-1", request, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteEntityAsync("ge-1", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        await inner.DidNotReceive()
            .CreateEntityAsync(Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.DidNotReceive()
            .UpdateEntityAsync(Arg.Any<string>(), Arg.Any<GraphEntityRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.DidNotReceive()
            .DeleteEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG relationship CRUD fails before calling the inner service while required transactions are active.</summary>
    [Fact]
    public async Task RelationshipMutations_WhenRequiredTransactionsActive_FailClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var sut = CreateSut(inner, RequiredCoordinator(), RequiredOptions());
        var request = new GraphRelationshipRequest
        {
            SourceEntityId = "ge-1",
            TargetEntityId = "ge-2",
            RelationshipType = "knows",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.CreateRelationshipAsync(request, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.UpdateRelationshipAsync("gr-1", request, CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.DeleteRelationshipAsync("gr-1", CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        await inner.DidNotReceive()
            .CreateRelationshipAsync(Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.DidNotReceive()
            .UpdateRelationshipAsync(Arg.Any<string>(), Arg.Any<GraphRelationshipRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await inner.DidNotReceive()
            .DeleteRelationshipAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG mutations fail closed when the coordinator is degraded.</summary>
    [Fact]
    public async Task InitializeAsync_WhenCoordinatorDegraded_FailsClosedWithoutCallingInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        var coordinator = Substitute.For<ITurnTransactionCoordinator>();
        coordinator.GetStatus().Returns(new TurnTransactionStatusResponse
        {
            Enabled = true,
            Degraded = true,
            Message = "transaction gate unavailable",
        });
        var sut = CreateSut(inner, coordinator, RequiredOptions());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await sut.InitializeAsync(CancellationToken.None).ConfigureAwait(true))
            .ConfigureAwait(true);

        Assert.Contains("transaction gate unavailable", exception.Message, StringComparison.Ordinal);
        await inner.DidNotReceive()
            .InitializeAsync(Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>GraphRAG read methods continue to delegate while required transactions are active.</summary>
    [Fact]
    public async Task ReadMethods_WhenRequiredTransactionsActive_DelegateToInner()
    {
        var inner = Substitute.For<IGraphRagService>();
        inner.GetStatusAsync(Arg.Any<CancellationToken>()).Returns(new GraphRagStatusResponse { Enabled = true });
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

        Assert.True((await sut.GetStatusAsync(CancellationToken.None).ConfigureAwait(true)).Enabled);
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
