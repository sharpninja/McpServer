using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-121: Status endpoints for the turn transaction coordinator.
/// </summary>
[ApiController]
[Route("mcpserver/turntransactions")]
public sealed class TurnTransactionsController : ControllerBase
{
    private readonly ITurnTransactionCoordinator _coordinator;

    /// <summary>Initializes a new instance of the <see cref="TurnTransactionsController"/> class.</summary>
    /// <param name="coordinator">Turn transaction coordinator.</param>
    public TurnTransactionsController(ITurnTransactionCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    /// <summary>Gets the current turn transaction coordinator status.</summary>
    /// <returns>Coordinator status.</returns>
    [HttpGet("status")]
    public ActionResult<TurnTransactionStatusResponse> GetStatus()
        => Ok(_coordinator.GetStatus());
}
