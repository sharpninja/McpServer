using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-MEMORY-003 acceptance: REST endpoints expose memory CRUD and map
/// service mutation failures to HTTP results without changing the structured
/// memory payload.
/// </summary>
public sealed class MemoryControllerTests
{
    /// <summary>GET /mcpserver/memory returns the service-provided visible memory list.</summary>
    [Fact]
    public async Task ListAsync_ReturnsOkPayload()
    {
        var service = Substitute.For<IMemoryService>();
        service.ListAsync(Arg.Any<MemoryListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryQueryResult(
            [
                new MemoryItem
                {
                    Id = "MEMORY-OPERATOR-001",
                    Category = "OPERATOR",
                    Scope = MemoryScope.Global,
                    Text = "global memory",
                    Version = 1,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                }
            ],
            1));

        var controller = new MemoryController(service);
        var action = await controller.ListAsync("Global", "operator", "global", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var result = Assert.IsType<MemoryQueryResult>(ok.Value);
        Assert.Single(result.Items);
        Assert.Equal("MEMORY-OPERATOR-001", result.Items[0].Id);
        await service.Received(1).ListAsync(
            Arg.Is<MemoryListRequest>(request => request != null
                && request.Scope == MemoryScope.Global
                && request.Category == "operator"
                && request.Keyword == "global"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>GET /mcpserver/memory accepts Effective as the explicit default list scope.</summary>
    [Fact]
    public async Task ListAsync_WithEffectiveScope_ForwardsNullScope()
    {
        var service = Substitute.For<IMemoryService>();
        service.ListAsync(Arg.Any<MemoryListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryQueryResult([], 0));

        var controller = new MemoryController(service);
        var action = await controller.ListAsync("Effective", null, null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<OkObjectResult>(action.Result);
        await service.Received(1).ListAsync(
            Arg.Is<MemoryListRequest>(request => request != null && request.Scope == null),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>GET /mcpserver/memory rejects non-contract list scopes with BadRequest.</summary>
    [Fact]
    public async Task ListAsync_WithInvalidScope_ReturnsBadRequest()
    {
        var service = Substitute.For<IMemoryService>();
        var controller = new MemoryController(service);

        var action = await controller.ListAsync("1", null, null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        await service.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }

    /// <summary>POST /mcpserver/memory returns Created with the canonical memory id location.</summary>
    [Fact]
    public async Task AddAsync_WhenSuccessful_ReturnsCreated()
    {
        var service = Substitute.For<IMemoryService>();
        service.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(
                true,
                Memory: new MemoryItem
                {
                    Id = "MEMORY-OPERATOR-001",
                    Category = "OPERATOR",
                    Scope = MemoryScope.Global,
                    Text = "created",
                    Version = 1,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                }));

        var controller = new MemoryController(service);
        var action = await controller.AddAsync(new MemoryAddRequest
        {
            Category = "operator",
            Scope = MemoryScope.Global,
            Text = "created",
        }, CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal("/mcpserver/memory/MEMORY-OPERATOR-001", created.Location);
        var mutation = Assert.IsType<MemoryMutationResult>(created.Value);
        Assert.True(mutation.Success);
    }

    /// <summary>POST /mcpserver/memory uses the transaction-gated mutation service when it is registered.</summary>
    [Fact]
    public async Task AddAsync_WhenTransactionGateRegistered_UsesGatedAddService()
    {
        var service = Substitute.For<IMemoryService>();
        var gated = Substitute.For<ITransactionGatedMemoryService>();
        gated.AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(
                true,
                Memory: new MemoryItem
                {
                    Id = "MEMORY-OPERATOR-001",
                    Category = "OPERATOR",
                    Scope = MemoryScope.Global,
                    Text = "created",
                    Version = 1,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                }));

        var controller = new MemoryController(service, gated);
        var action = await controller.AddAsync(new MemoryAddRequest
        {
            Category = "operator",
            Scope = MemoryScope.Global,
            Text = "created",
        }, CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal("/mcpserver/memory/MEMORY-OPERATOR-001", created.Location);
        await gated.Received(1)
            .AddAsync(Arg.Any<MemoryAddRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, default).ConfigureAwait(true);
    }

    /// <summary>PUT /mcpserver/memory/{id} maps validation failures to BadRequest.</summary>
    [Fact]
    public async Task UpdateAsync_WhenValidationFails_ReturnsBadRequest()
    {
        var service = Substitute.For<IMemoryService>();
        service.UpdateAsync("MEMORY-OPERATOR-001", Arg.Any<MemoryUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(false, "invalid", FailureKind: MemoryMutationFailureKind.Validation));

        var controller = new MemoryController(service);
        var action = await controller.UpdateAsync(
            "MEMORY-OPERATOR-001",
            new MemoryUpdateRequest { Text = "" },
            CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var mutation = Assert.IsType<MemoryMutationResult>(badRequest.Value);
        Assert.Equal(MemoryMutationFailureKind.Validation, mutation.FailureKind);
    }

    /// <summary>DELETE /mcpserver/memory/{id} maps missing visible memory rows to NotFound.</summary>
    [Fact]
    public async Task RemoveAsync_WhenMissing_ReturnsNotFound()
    {
        var service = Substitute.For<IMemoryService>();
        service.RemoveAsync("MEMORY-MISSING-001", Arg.Any<CancellationToken>())
            .Returns(new MemoryMutationResult(false, "missing", FailureKind: MemoryMutationFailureKind.NotFound));

        var controller = new MemoryController(service);
        var action = await controller.RemoveAsync("MEMORY-MISSING-001", CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(action.Result);
        var mutation = Assert.IsType<MemoryMutationResult>(notFound.Value);
        Assert.Equal(MemoryMutationFailureKind.NotFound, mutation.FailureKind);
    }
}
