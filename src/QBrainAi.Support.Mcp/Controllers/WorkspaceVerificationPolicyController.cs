// SPDX-License-Identifier: Apache-2.0
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QBrainAi.Client.Models;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Controllers;

/// <summary>TR-QB-VERIFY-001: read-only, authenticated workspace verification policy endpoint.</summary>
[ApiController]
[Route("qbrainai/workspace/verification-policy")]
[Route("mcpserver/workspace/verification-policy")]
public sealed class WorkspaceVerificationPolicyController(
    IConfiguration configuration, WorkspaceContext workspace, WorkspaceTokenService tokens) : ControllerBase
{
    /// <summary>Returns a signed policy for the resolved workspace and a fresh caller challenge.</summary>
    [HttpGet]
    public ActionResult<WorkspaceVerificationPolicyEnvelope> Get([FromQuery] string challenge)
    {
        if (HttpContext.Items.TryGetValue(Middleware.WorkspaceAuthMiddleware.IsDefaultKeyItem, out var isDefault) && isDefault is true)
            return StatusCode(403, new { error = "workspace_verification_policy_forbidden" });
        try
        {
            return Ok(WorkspaceVerificationPolicyReader.Read(configuration, workspace, tokens, challenge));
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { error = "workspace_verification_policy_forbidden" });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "invalid_policy_challenge" });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(503, new { error = "workspace_verification_policy_unavailable" });
        }
    }
}
