using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Queries;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-USECASE-002 / TR-MCP-USECASE-003: Controller unit tests for thin CQRS dispatch and HTTP mapping.
/// Uses NSubstitute for <see cref="IDispatcher"/> and a fixed <see cref="WorkspaceContext"/>.
/// </summary>
public sealed class UseCasesControllerTests
{
    private const string Workspace = @"C:\ws\usecase-tests";

    /// <summary>
    /// TEST-MCP-USECASE-002: POST creates via CreateUseCaseCommand and returns 201 with location.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenSuccess_ReturnsCreated()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var detail = new UseCaseDetailDto { UseCaseId = 42, Title = "Login", WorkspaceId = Workspace };
        dispatcher.SendAsync(Arg.Any<CreateUseCaseCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseDetailDto>.Success(detail));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateAsync(
            new CreateUseCaseRequest { Title = "Login" },
            CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal("/mcpserver/usecases/42", created.Location);
        Assert.Same(detail, created.Value);
        await dispatcher.Received(1).SendAsync(
            Arg.Is<CreateUseCaseCommand>(c => MatchesCreate(c, "Login")),
            Arg.Any<CancellationToken>());
    }

    private static bool MatchesCreate(CreateUseCaseCommand? command, string title)
        => command is not null
           && command.WorkspacePath == Workspace
           && command.Request is not null
           && command.Request.Title == title;

    /// <summary>
    /// TEST-MCP-USECASE-002: GET list dispatches ListUseCasesQuery.
    /// </summary>
    [Fact]
    public async Task ListAsync_WhenSuccess_ReturnsOk()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        IReadOnlyList<UseCaseSummaryDto> items =
        [
            new UseCaseSummaryDto { UseCaseId = 1, Title = "A" }
        ];
        dispatcher.QueryAsync(Arg.Any<ListUseCasesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<UseCaseSummaryDto>>.Success(items));

        var controller = CreateController(dispatcher);
        var action = await controller.ListAsync("A", CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(items, ok.Value);
        await dispatcher.Received(1).QueryAsync(
            Arg.Is<ListUseCasesQuery>(q => MatchesList(q, "A")),
            Arg.Any<CancellationToken>());
    }

    private static bool MatchesList(ListUseCasesQuery? query, string titleFilter)
        => query is not null && query.TitleFilter == titleFilter && query.WorkspacePath == Workspace;

    /// <summary>
    /// TEST-MCP-USECASE-002: NotFound Result maps to HTTP 404.
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenNotFound_Returns404()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.QueryAsync(Arg.Any<GetUseCaseQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseDetailDto>.Failure(UseCaseResultCodes.NotFoundMsg("Use case 9 not found.")));

        var controller = CreateController(dispatcher);
        var action = await controller.GetAsync(9, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(action.Result);
    }

    /// <summary>
    /// TEST-MCP-USECASE-002: Validation Result maps to HTTP 400.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenValidationFails_Returns400()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<CreateUseCaseCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseDetailDto>.Failure(UseCaseResultCodes.ValidationMsg("title is required.")));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateAsync(
            new CreateUseCaseRequest { Title = "x" },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    /// <summary>
    /// TEST-MCP-USECASE-002: Conflict Result maps to HTTP 409 for link endpoint.
    /// </summary>
    [Fact]
    public async Task LinkFrAsync_WhenConflict_Returns409()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<LinkUseCaseToFrCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseFrLinkDto>.Failure(UseCaseResultCodes.ConflictMsg("already exists")));

        var controller = CreateController(dispatcher);
        var action = await controller.LinkFrAsync(
            1,
            new LinkUseCaseToFrRequest { FrId = "FR-MCP-001" },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<ConflictObjectResult>(action.Result);
    }

    /// <summary>
    /// TEST-MCP-USECASE-002: DELETE returns 204 on success.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccess_ReturnsNoContent()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.SendAsync(Arg.Any<DeleteUseCaseCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        var controller = CreateController(dispatcher);
        var action = await controller.DeleteAsync(7, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<NoContentResult>(action);
    }

    /// <summary>
    /// TEST-MCP-USECASE-002: Diagram query dispatches GetUseCaseDiagramQuery.
    /// </summary>
    [Fact]
    public async Task DiagramAsync_WhenSuccess_ReturnsOk()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var diagram = new UseCaseDiagramDto
        {
            UseCaseId = 3,
            Format = "mermaid",
            Content = "sequenceDiagram",
        };
        dispatcher.QueryAsync(Arg.Any<GetUseCaseDiagramQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseDiagramDto>.Success(diagram));

        var controller = CreateController(dispatcher);
        var action = await controller.DiagramAsync(3, "mermaid", kind: "sequence", cancellationToken: CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(diagram, ok.Value);
        await dispatcher.Received(1).QueryAsync(
            Arg.Is<GetUseCaseDiagramQuery>(q => MatchesDiagram(q, 3, "mermaid")),
            Arg.Any<CancellationToken>());
    }

    private static bool MatchesDiagram(GetUseCaseDiagramQuery? query, long useCaseId, string format)
        => query is not null && query.UseCaseId == useCaseId && query.Format == format;

    /// <summary>
    /// TEST-MCP-USECASE-002: Coverage endpoint dispatches GetUseCaseFrCoverageQuery.
    /// </summary>
    [Fact]
    public async Task CoverageAsync_WhenSuccess_ReturnsOk()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var coverage = new UseCaseFrCoverageDto
        {
            UseCasesWithoutRealizesLink = [],
            FunctionalRequirementsWithoutRealizesUseCase = ["FR-MCP-001"],
        };
        dispatcher.QueryAsync(Arg.Any<GetUseCaseFrCoverageQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseFrCoverageDto>.Success(coverage));

        var controller = CreateController(dispatcher);
        var action = await controller.CoverageAsync(CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(coverage, ok.Value);
    }

    /// <summary>
    /// TEST-MCP-USECASE-002: from-fr endpoint dispatches CreateUseCaseFromFrCommand.
    /// </summary>
    [Fact]
    public async Task CreateFromFrAsync_WhenSuccess_ReturnsCreated()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var detail = new UseCaseDetailDto { UseCaseId = 11, Title = "From FR" };
        dispatcher.SendAsync(Arg.Any<CreateUseCaseFromFrCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<UseCaseDetailDto>.Success(detail));

        var controller = CreateController(dispatcher);
        var action = await controller.CreateFromFrAsync(
            "FR-MCP-USECASE-001",
            CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(action.Result);
        Assert.Equal("/mcpserver/usecases/11", created.Location);
        await dispatcher.Received(1).SendAsync(
            Arg.Is<CreateUseCaseFromFrCommand>(c => MatchesFromFr(c, "FR-MCP-USECASE-001")),
            Arg.Any<CancellationToken>());
    }

    private static bool MatchesFromFr(CreateUseCaseFromFrCommand? command, string frId)
        => command is not null && command.FrId == frId;

    /// <summary>
    /// TEST-MCP-USECASE-002: Null create body short-circuits to 400 without dispatch.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WhenBodyNull_ReturnsBadRequestWithoutDispatch()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        var controller = CreateController(dispatcher);

        var action = await controller.CreateAsync(null, CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(action.Result);
        await dispatcher.DidNotReceive().SendAsync(Arg.Any<ICommand<UseCaseDetailDto>>(), Arg.Any<CancellationToken>());
    }

    private static UseCasesController CreateController(IDispatcher dispatcher)
    {
        var workspace = new WorkspaceContext { WorkspacePath = Workspace };
        return new UseCasesController(dispatcher, workspace);
    }
}
