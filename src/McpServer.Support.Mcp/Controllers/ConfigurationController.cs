using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-068 / TR-MCP-CFG-006: Admin-only configuration inspection and patch endpoints backed by
/// the effective <see cref="IConfiguration"/> view and persisted <c>appsettings.yaml</c> updates.
/// </summary>
[ApiController]
[Route("mcpserver/configuration")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly AppSettingsFileService _appSettingsFileService;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationController"/> class.</summary>
    /// <param name="appSettingsFileService">The configuration file helper.</param>
    public ConfigurationController(AppSettingsFileService appSettingsFileService)
    {
        _appSettingsFileService = appSettingsFileService;
    }

    /// <summary>
    /// Returns the current effective configuration as flattened key-value pairs.
    /// </summary>
    [HttpGet]
    public ActionResult<Dictionary<string, string>> GetConfigurationValues()
    {
        return Ok(_appSettingsFileService.GetConfigurationValues());
    }

    /// <summary>
    /// Applies flattened key-value updates to <c>appsettings.yaml</c>, reloads the active configuration,
    /// and returns the updated effective configuration view.
    /// </summary>
    /// <param name="values">Flattened configuration values to patch.</param>
    /// <param name="ct">The cancellation token.</param>
    [HttpPatch]
    public async Task<ActionResult<Dictionary<string, string>>> PatchConfigurationValuesAsync(
        [FromBody] Dictionary<string, string?>? values,
        CancellationToken ct)
    {
        if (values is null || values.Count == 0)
            return BadRequest(new { error = "Request body must contain at least one configuration value." });

        try
        {
            var updated = await _appSettingsFileService.PatchYamlConfigurationAsync(values, ct).ConfigureAwait(false);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
