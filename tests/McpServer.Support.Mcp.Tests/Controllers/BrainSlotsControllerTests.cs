using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Tests for brain-slot REST controller contracts. TEST-MCP-176.</summary>
public sealed class BrainSlotsControllerTests
{
    /// <summary>GET status returns the registry readiness projection.</summary>
    [Fact]
    public async Task GetStatusAsync_ReturnsOkPayload()
    {
        var registry = Substitute.For<IBrainSlotRegistryService>();
        registry.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BrainSlotStatusResponse
            {
                QuadReady = false,
                MissingRoles = [BrainSlotRoles.CuriosityEngine],
            });
        var controller = new BrainSlotsController(registry, Substitute.For<IBrainSlotInvocationService>());

        var action = await controller.GetStatusAsync(CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var status = Assert.IsType<BrainSlotStatusResponse>(ok.Value);
        Assert.False(status.QuadReady);
        Assert.Contains(BrainSlotRoles.CuriosityEngine, status.MissingRoles);
    }

    /// <summary>PUT maps validation exceptions to BadRequest with the structured reason.</summary>
    [Fact]
    public async Task UpsertAsync_WhenValidationFails_ReturnsBadRequest()
    {
        var registry = Substitute.For<IBrainSlotRegistryService>();
        registry.UpsertAsync(Arg.Any<string>(), Arg.Any<UpsertBrainSlotRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BrainSlotDto>(
                new BrainSlotValidationException("bad endpoint", BrainSlotReasonCodes.EndpointNotAllowed)));
        var controller = new BrainSlotsController(registry, Substitute.For<IBrainSlotInvocationService>());

        var action = await controller.UpsertAsync(
            "slot-1",
            new UpsertBrainSlotRequest { Role = BrainSlotRoles.LeftHemisphere },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    /// <summary>PUT maps enabled-role conflicts to Conflict.</summary>
    [Fact]
    public async Task UpsertAsync_WhenEnabledRoleConflicts_ReturnsConflict()
    {
        var registry = Substitute.For<IBrainSlotRegistryService>();
        registry.UpsertAsync(Arg.Any<string>(), Arg.Any<UpsertBrainSlotRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BrainSlotDto>(
                new BrainSlotConflictException("replaceExisting required")));
        var controller = new BrainSlotsController(registry, Substitute.For<IBrainSlotInvocationService>());

        var action = await controller.UpsertAsync(
            "slot-1",
            new UpsertBrainSlotRequest { Role = BrainSlotRoles.LeftHemisphere },
            CancellationToken.None).ConfigureAwait(true);

        Assert.IsType<ConflictObjectResult>(action.Result);
    }

    /// <summary>POST invoke forwards the slot id and invocation request to the invocation service.</summary>
    [Fact]
    public async Task InvokeAsync_ForwardsRequest()
    {
        var invocation = Substitute.For<IBrainSlotInvocationService>();
        invocation.InvokeAsync(Arg.Any<string>(), Arg.Any<BrainSlotInvokeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new BrainSlotInvokeResponse
            {
                Status = "committed",
                Reason = BrainSlotReasonCodes.None,
                SlotId = "curiosity-main",
                Role = BrainSlotRoles.CuriosityEngine,
                Output = "committed output",
            });
        var controller = new BrainSlotsController(Substitute.For<IBrainSlotRegistryService>(), invocation);

        var action = await controller.InvokeAsync(
            "curiosity-main",
            new BrainSlotInvokeRequest { Input = "find gaps", AdmitToGraphRag = true },
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<BrainSlotInvokeResponse>(ok.Value);
        Assert.Equal("committed output", response.Output);
        await invocation.Received(1).InvokeAsync(
            "curiosity-main",
            Arg.Is<BrainSlotInvokeRequest>(request => request != null && request.AdmitToGraphRag),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }
}
