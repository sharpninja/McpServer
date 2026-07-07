using McpServer.Support.Mcp.GraphRag;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FederatedGraphRagService"/>. Validates merge semantics
/// for list/query operations, pass-through for write operations and when federation
/// is disabled, and graceful degradation on remote failure.
/// FR-MCP-084, TEST-MCP-FED-003.
/// </summary>
public sealed class FederatedGraphRagServiceTests
{
    private readonly IGraphRagService _inner = Substitute.For<IGraphRagService>();
    private readonly IGraphRagFederationClient _client = Substitute.For<IGraphRagFederationClient>();

    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled };
        if (defaultTarget is not null)
        {
            opts.DefaultTarget = defaultTarget;
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://remote:7147" });
        }
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private FederatedGraphRagService CreateSut(FederationRegistry? registry = null)
    {
        registry ??= CreateRegistry();
        return new FederatedGraphRagService(
            _inner,
            registry,
            _client,
            NullLogger<FederatedGraphRagService>.Instance);
    }

    // --- ListEntitiesAsync ---

    /// <summary>Merges local and remote entities with local winning on ID collision.</summary>
    [Fact]
    public async Task ListEntitiesAsync_BothReturn_MergesLocalWins()
    {
        var localEntity = MakeEntity("E-001", "Local Entity");
        var remoteEntity1 = MakeEntity("E-001", "Remote Entity"); // collision
        var remoteEntity2 = MakeEntity("E-002", "Remote Only");

        _inner.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [localEntity], TotalCount = 1 });
        _client.QueryEntitiesAsync(Arg.Any<FederationTarget>(), 0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [remoteEntity1, remoteEntity2], TotalCount = 2 });

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.ListEntitiesAsync(0, 50, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Entities.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Entities, e => e.Id == "E-001" && e.Name == "Local Entity");
        Assert.Contains(result.Entities, e => e.Id == "E-002" && e.Name == "Remote Only");
    }

    /// <summary>When remote call throws, returns local-only entities gracefully.</summary>
    [Fact]
    public async Task ListEntitiesAsync_RemoteFails_ReturnsLocalOnly()
    {
        _inner.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphEntityListResponse { Entities = [MakeEntity("E-001")], TotalCount = 1 });
        _client.QueryEntitiesAsync(Arg.Any<FederationTarget>(), 0, 50, null, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.ListEntitiesAsync(0, 50, ct: TestContext.Current.CancellationToken);

        Assert.Single(result.Entities);
        Assert.Equal("E-001", result.Entities[0].Id);
    }

    /// <summary>When federation is disabled, delegates directly to the inner service.</summary>
    [Fact]
    public async Task ListEntitiesAsync_FederationDisabled_DelegatesToLocal()
    {
        var expected = new GraphEntityListResponse { Entities = [MakeEntity("E-001")], TotalCount = 1 };
        _inner.ListEntitiesAsync(0, 50, null, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: false));
        var result = await sut.ListEntitiesAsync(0, 50, ct: TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
        await _client.DidNotReceiveWithAnyArgs().QueryEntitiesAsync(default!, default, default, default, ct: TestContext.Current.CancellationToken);
    }

    // --- ListRelationshipsAsync ---

    /// <summary>Merges local and remote relationships with local winning on ID collision.</summary>
    [Fact]
    public async Task ListRelationshipsAsync_BothReturn_MergesLocalWins()
    {
        var localRel = MakeRelationship("R-001", "Local Rel");
        var remoteRel1 = MakeRelationship("R-001", "Remote Rel"); // collision
        var remoteRel2 = MakeRelationship("R-002", "Remote Only");

        _inner.ListRelationshipsAsync(0, 50, null, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipListResponse { Relationships = [localRel], TotalCount = 1 });
        _client.QueryRelationshipsAsync(Arg.Any<FederationTarget>(), 0, 50, null, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRelationshipListResponse { Relationships = [remoteRel1, remoteRel2], TotalCount = 2 });

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.ListRelationshipsAsync(0, 50, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Relationships.Count);
        Assert.Contains(result.Relationships, r => r.Id == "R-001" && r.Description == "Local Rel");
        Assert.Contains(result.Relationships, r => r.Id == "R-002");
    }

    // --- ListDocumentsAsync ---

    /// <summary>Merges local and remote documents with local winning on ID collision.</summary>
    [Fact]
    public async Task ListDocumentsAsync_BothReturn_MergesLocalWins()
    {
        var localDoc = MakeDocument("D-001", "local-key");
        var remoteDoc1 = MakeDocument("D-001", "remote-key"); // collision
        var remoteDoc2 = MakeDocument("D-002", "remote-only");

        _inner.ListDocumentsAsync(0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentListResponse { Documents = [localDoc], TotalCount = 1 });
        _client.QueryDocumentsAsync(Arg.Any<FederationTarget>(), 0, 50, null, Arg.Any<CancellationToken>())
            .Returns(new GraphRagDocumentListResponse { Documents = [remoteDoc1, remoteDoc2], TotalCount = 2 });

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.ListDocumentsAsync(0, 50, ct: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Documents.Count);
        Assert.Contains(result.Documents, d => d.Id == "D-001" && d.SourceKey == "local-key");
        Assert.Contains(result.Documents, d => d.Id == "D-002");
    }

    // --- QueryAsync ---

    /// <summary>When both return, local answer takes priority and remote supplements citations.</summary>
    [Fact]
    public async Task QueryAsync_BothReturn_LocalAnswerPriority()
    {
        var localResponse = new GraphRagQueryResponse { Query = "test", Answer = "Local answer", Mode = "local" };
        var remoteResponse = new GraphRagQueryResponse { Query = "test", Answer = "Remote answer", Mode = "global" };

        _inner.QueryAsync(Arg.Any<GraphRagQueryRequest>(), Arg.Any<CancellationToken>()).Returns(localResponse);
        _client.QueryGraphRagAsync(Arg.Any<FederationTarget>(), Arg.Any<GraphRagQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(remoteResponse);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new GraphRagQueryRequest { Query = "test" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Local answer", result.Answer);
    }

    /// <summary>When remote query throws, returns local-only answer gracefully.</summary>
    [Fact]
    public async Task QueryAsync_RemoteFails_ReturnsLocalOnly()
    {
        var localResponse = new GraphRagQueryResponse { Query = "test", Answer = "Local answer" };
        _inner.QueryAsync(Arg.Any<GraphRagQueryRequest>(), Arg.Any<CancellationToken>()).Returns(localResponse);
        _client.QueryGraphRagAsync(Arg.Any<FederationTarget>(), Arg.Any<GraphRagQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Timeout"));

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.QueryAsync(new GraphRagQueryRequest { Query = "test" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Local answer", result.Answer);
    }

    // --- Write operations pass through ---

    /// <summary>CreateEntityAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task CreateEntityAsync_AlwaysDelegatesToLocal()
    {
        var request = new GraphEntityRequest { Name = "Entity", EntityType = "test" };
        var expected = MakeEntity("E-003");
        _inner.CreateEntityAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.CreateEntityAsync(request, ct: TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }

    /// <summary>DeleteEntityAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task DeleteEntityAsync_AlwaysDelegatesToLocal()
    {
        _inner.DeleteEntityAsync("E-001", Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.DeleteEntityAsync("E-001", ct: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    /// <summary>GetStatusAsync always delegates to the inner service.</summary>
    [Fact]
    public async Task GetStatusAsync_AlwaysDelegatesToLocal()
    {
        var expected = new GraphRagStatusResponse();
        _inner.GetStatusAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut(CreateRegistry(enabled: true, defaultTarget: "remote"));
        var result = await sut.GetStatusAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, result);
    }

    // --- Helpers ---

    private static GraphEntityResponse MakeEntity(string id, string name = "Entity") => new()
    {
        Id = id,
        Name = name,
        EntityType = "test",
    };

    private static GraphRelationshipResponse MakeRelationship(string id, string description = "Rel") => new()
    {
        Id = id,
        SourceEntityId = "src",
        TargetEntityId = "tgt",
        RelationshipType = "test",
        Description = description,
    };

    private static GraphRagDocumentSummary MakeDocument(string id, string sourceKey) => new()
    {
        Id = id,
        SourceKey = sourceKey,
        SourceType = "adhoc",
        ContentHash = "hash",
    };
}
