using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-PRODUCT-003 / TR-MCP-PRODUCT-API-001: Controller dispatches CQRS only.
/// Phase 3 red: stub returns 501 until green.
/// </summary>
public sealed class ProductsControllerTests
{
    private const string Workspace = @"F:\GitHub\McpServer";

    /// <summary>POST create dispatches CreateProductCommand and returns 201.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuccess_ReturnsCreated()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var dto = new ProductDto
        {
            Key = "PROD-MCPSERVER",
            Name = "McpServer",
            OwnerWorkspaceId = Workspace,
            MemberWorkspaceIds = [Workspace],
        };
        dispatcher.SendAsync(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDto>.Success(dto));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateAsync(
            new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "McpServer" },
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal("/mcpserver/products/PROD-MCPSERVER", created.Location);
        Assert.Same(dto, created.Value);
        await dispatcher.Received(1).SendAsync(
            Arg.Is<CreateProductCommand>(c => MatchesCreate(c)),
            Arg.Any<CancellationToken>());
    }

    private static bool MatchesCreate(CreateProductCommand? command)
        => command is not null
           && command.WorkspacePath == Workspace
           && command.Request is { Key: "PROD-MCPSERVER" };

    /// <summary>Invalid key Result maps to HTTP 400.</summary>
    [Fact]
    public async Task CreateAsync_WhenInvalidKey_ReturnsBadRequest()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDto>.Failure("400: invalid key"));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateAsync(
            new CreateProductRequest { Key = "mcpserver", Name = "Bad" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    /// <summary>Duplicate key Result maps to HTTP 409.</summary>
    [Fact]
    public async Task CreateAsync_WhenConflict_ReturnsConflict()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDto>.Failure("409: exists"));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateAsync(
            new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "McpServer" },
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(action.Result);
    }

    /// <summary>Outsider get maps to HTTP 404.</summary>
    [Fact]
    public async Task GetAsync_WhenNotFound_ReturnsNotFound()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.QueryAsync(Arg.Any<GetProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDto>.Failure("404: not found"));

        var controller = CreateController(dispatcher);
        var action = await controller.GetAsync("PROD-MCPSERVER", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(action.Result);
    }

    /// <summary>Non-owner mutate maps to HTTP 403.</summary>
    [Fact]
    public async Task DeleteAsync_WhenForbidden_ReturnsForbid()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<DeleteProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ProductDto>.Failure("403: owner only"));

        var controller = CreateController(dispatcher);
        var action = await controller.DeleteAsync("PROD-MCPSERVER", CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    /// <summary>Effective productScope is accepted on the requirements controller in Phase 3.</summary>
    [Fact]
    public async Task RequirementsEffective_AcceptsProductScopeQuery()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.QueryAsync(Arg.Any<GetProductEffectiveRequirementsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<McpServer.Support.Mcp.Requirements.Models.EffectiveRequirementsResult>.Failure("not wired"));

        var controller = CreateController(dispatcher);
        _ = controller;
        Assert.Contains("productScope", typeof(RequirementsController)
            .GetMethod(nameof(RequirementsController.GetEffectiveRequirementsAsync))!
            .GetParameters()
            .Select(p => p.Name));
    }

    private static ProductsController CreateController(IDispatcher dispatcher)
    {
        return new ProductsController(dispatcher, new WorkspaceContext { WorkspacePath = Workspace });
    }
}
