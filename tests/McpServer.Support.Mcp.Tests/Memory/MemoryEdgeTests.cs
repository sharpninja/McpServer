using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TEST-MCP-MEMORY-012 / TR-MCP-MEMORY-MODEL-002 / FR-MCP-MEMORY-012:
/// Unique (From, To, EdgeType) at the DB layer, plus S3 create-edge acceptance.
/// </summary>
public sealed class MemoryEdgeTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-03: Unique (FromMemoryId, ToMemoryId, EdgeType) is enforced at the DB layer.</summary>
    [Fact]
    public async Task UniqueFromToType_Enforced()
    {
        Assert.True(_harness.HasUniqueEdgeIndex());
        var entity = _harness.FindMappedEntity(typeof(MemoryEdgeEntity));
        Assert.NotNull(entity);

        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        db.Set<MemoryEdgeEntity>().Add(new MemoryEdgeEntity
        {
            FromMemoryId = "MEMORY-A-001",
            ToMemoryId = "MEMORY-B-001",
            EdgeType = "explicit",
            Weight = 1,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.Set<MemoryEdgeEntity>().Add(new MemoryEdgeEntity
        {
            FromMemoryId = "MEMORY-A-001",
            ToMemoryId = "MEMORY-B-001",
            EdgeType = "explicit",
            Weight = 0.5,
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-12: Duplicate explicit edge (same From, To, EdgeType) is 409 (documented).</summary>
    [Fact]
    public async Task DuplicateExplicit_StableBehavior()
    {
        var from = await SeedAsync("duplicate-edge from").ConfigureAwait(true);
        var to = await SeedAsync("duplicate-edge to").ConfigureAwait(true);
        var request = new MemoryCreateEdgeRequest
        {
            FromMemoryId = from.Id,
            ToMemoryId = to.Id,
            EdgeType = "explicit",
            Weight = 0.4,
        };

        var first = await _harness.CreateEdgeAsync(request, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var second = await _harness.CreateEdgeAsync(
            request with { Weight = 0.9 },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(first.StatusCode is 200 or 201, first.Error);
        Assert.Equal(from.Id, first.Edge?.FromMemoryId);
        Assert.Equal(to.Id, first.Edge?.ToMemoryId);
        Assert.Equal("explicit", first.Edge?.EdgeType, ignoreCase: true);
        Assert.Equal(MemoryExploreLimits.DuplicateExplicitStatusCode, second.StatusCode);
        Assert.Equal(409, second.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Conflict, second.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-14: Edge weight outside [0,1] is rejected on create.</summary>
    [Fact]
    public async Task WeightOutOfRange_Returns400()
    {
        var from = await SeedAsync("weight-range from").ConfigureAwait(true);
        var to = await SeedAsync("weight-range to").ConfigureAwait(true);

        var low = await _harness.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = from.Id,
                ToMemoryId = to.Id,
                EdgeType = "explicit",
                Weight = MemoryExploreLimits.MinWeight - 0.1,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var high = await _harness.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = from.Id,
                ToMemoryId = to.Id,
                EdgeType = "explicit",
                Weight = MemoryExploreLimits.MaxWeight + 0.1,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, low.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, low.FailureKind);
        Assert.Equal(400, high.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, high.FailureKind);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-15: Invalid EdgeType is rejected on create.</summary>
    [Fact]
    public async Task InvalidEdgeType_Returns400()
    {
        var from = await SeedAsync("invalid-type from").ConfigureAwait(true);
        var to = await SeedAsync("invalid-type to").ConfigureAwait(true);

        var created = await _harness.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = from.Id,
                ToMemoryId = to.Id,
                EdgeType = "not-an-edge-type",
                Weight = 0.5,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(400, created.StatusCode);
        Assert.Equal(MemoryMutationFailureKind.Validation, created.FailureKind);
        Assert.DoesNotContain("not-an-edge-type", MemoryExploreLimits.AllowedEdgeTypes);
    }

    /// <summary>AC-FR-MCP-MEMORY-012-20: Creating an edge to a foreign-workspace memory id fails closed.</summary>
    [Fact]
    public async Task ForeignTarget_FailsClosed()
    {
        var local = await SeedAsync("foreign-target local").ConfigureAwait(true);
        var foreign = await SeedAsync("foreign-target other", workspacePath: _harness.WorkspaceB)
            .ConfigureAwait(true);

        var created = await _harness.CreateEdgeAsync(
            new MemoryCreateEdgeRequest
            {
                FromMemoryId = local.Id,
                ToMemoryId = foreign.Id,
                EdgeType = "explicit",
                Weight = 0.5,
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(
            created.StatusCode is 400 or 403 or 404,
            created.Error ?? $"Foreign target must fail closed, not {created.StatusCode}.");
        Assert.True(
            created.FailureKind is MemoryMutationFailureKind.Validation or MemoryMutationFailureKind.NotFound,
            $"Foreign target FailureKind was {created.FailureKind}.");
    }

    private async Task<MemoryItem> SeedAsync(string content, string? workspacePath = null)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "fact",
            Scope = MemoryScope.Workspace,
            Text = content,
            Content = content,
            Type = "fact",
        }, workspacePath: workspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        Assert.NotNull(added.Memory);
        return added.Memory!;
    }
}
