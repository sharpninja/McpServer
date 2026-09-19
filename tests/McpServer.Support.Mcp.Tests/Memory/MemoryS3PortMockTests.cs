using McpServer.Support.Mcp.Services;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-012 / FR-MCP-MEMORY-012:
/// Mocks-first green proof that S3 explore/edge/Hebbian port contracts are assertable.
/// </summary>
public sealed class MemoryS3PortMockTests
{
    /// <summary>AC-FR-MCP-MEMORY-012-01: mock explore returns an explicit neighbor with weight and EdgeType.</summary>
    [Fact]
    public async Task PortMock_ExplicitEdge_ReturnedWithWeight()
    {
        var port = Substitute.For<IMemoryS3Port>();
        port.ExploreAsync(Arg.Any<MemoryExploreRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryExploreResult(200, [
                new MemoryExploreNeighbor { Id = "MEMORY-S3-B", Weight = 0.8, EdgeType = "explicit", Depth = 1 },
            ], SeedId: "MEMORY-S3-A"));

        var result = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-A" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(200, result.StatusCode);
        var hit = Assert.Single(result.Items!);
        Assert.Equal("MEMORY-S3-B", hit.Id);
        Assert.Equal(0.8, hit.Weight);
        Assert.Equal("explicit", hit.EdgeType, ignoreCase: true);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-02: mock explore with Hebbian off omits co-retrieved-only edges.</summary>
    [Fact]
    public async Task PortMock_HebbianOff_NoCoRetrievedOnlyEdges()
    {
        Assert.False(MemoryExploreLimits.DefaultHebbianEnabled);
        var port = Substitute.For<IMemoryS3Port>();
        port.ExploreAsync(Arg.Any<MemoryExploreRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryExploreResult(
                200,
                [new MemoryExploreNeighbor { Id = "MEMORY-S3-EXPLICIT", Weight = 0.9, EdgeType = "explicit", Depth = 1 }],
                HebbianApplied: false));

        var result = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-A", HebbianEnabled = null },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.HebbianApplied);
        Assert.DoesNotContain(result.Items!, item => item.EdgeType.Equals("co-retrieved", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Items!, item => item.Id == "MEMORY-S3-EXPLICIT");
    }

    /// <summary>AC-FR-MCP-MEMORY-012-09 / AC-FR-MCP-MEMORY-012-10: mock explore rejects non-positive and above-max depth with 400.</summary>
    [Fact]
    public async Task PortMock_DepthBounds_Return400()
    {
        var port = Substitute.For<IMemoryS3Port>();
        port.ExploreAsync(Arg.Any<MemoryExploreRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryExploreResult(400, FailureKind: MemoryMutationFailureKind.Validation));

        var zero = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-A", Depth = 0 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var above = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-A", Depth = MemoryExploreLimits.MaxDepth + 1 },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, zero.StatusCode);
        Assert.Equal(400, above.StatusCode);
        Assert.Equal(MemoryExploreLimits.DepthAboveMaxStatusCode, above.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-11: mock create-edge rejects a self-loop with 400.</summary>
    [Fact]
    public async Task PortMock_SelfLoop_Rejected()
    {
        var port = Substitute.For<IMemoryS3Port>();
        port.CreateEdgeAsync(Arg.Any<MemoryCreateEdgeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryCreateEdgeResult(
                MemoryExploreLimits.SelfLoopStatusCode,
                FailureKind: MemoryMutationFailureKind.Validation));

        var result = await port.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = "MEMORY-S3-A",
                ToMemoryId = "MEMORY-S3-A",
                EdgeType = "explicit",
                Weight = 1,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, result.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-18: mock explore is directed and does not imply a reverse edge.</summary>
    [Fact]
    public async Task PortMock_Directed_NoImpliedReverse()
    {
        var port = Substitute.For<IMemoryS3Port>();
        port.ExploreAsync(Arg.Is<MemoryExploreRequest>(request => request != null && request.SeedId == "MEMORY-S3-A"), Arg.Any<CancellationToken>())
            .Returns(new MemoryExploreResult(200, [
                new MemoryExploreNeighbor { Id = "MEMORY-S3-B", Weight = 0.75, EdgeType = "explicit", Depth = 1 },
            ]));
        port.ExploreAsync(Arg.Is<MemoryExploreRequest>(request => request != null && request.SeedId == "MEMORY-S3-B"), Arg.Any<CancellationToken>())
            .Returns(new MemoryExploreResult(200, []));

        var forward = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-A" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        var reverse = await port.ExploreAsync(
            new MemoryExploreRequest { SeedId = "MEMORY-S3-B" },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains(forward.Items!, item => item.Id == "MEMORY-S3-B");
        Assert.DoesNotContain(reverse.Items!, item => item.Id == "MEMORY-S3-A");
    }

    /// <summary>AC-FR-MCP-MEMORY-012-20: mock create-edge fails closed for a foreign target.</summary>
    [Fact]
    public async Task PortMock_ForeignTarget_FailsClosed()
    {
        var port = Substitute.For<IMemoryS3Port>();
        port.CreateEdgeAsync(Arg.Any<MemoryCreateEdgeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryCreateEdgeResult(404, FailureKind: MemoryMutationFailureKind.NotFound));

        var result = await port.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = "MEMORY-S3-LOCAL",
                ToMemoryId = "MEMORY-S3-FOREIGN",
                EdgeType = "explicit",
                Weight = 0.5,
            },
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.StatusCode is 400 or 403 or 404);
        Assert.True(result.FailureKind is MemoryMutationFailureKind.Validation or MemoryMutationFailureKind.NotFound);
    }
}
