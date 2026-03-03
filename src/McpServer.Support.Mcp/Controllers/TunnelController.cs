using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Tunnel lifecycle endpoints: list strategies, enable/disable, start, stop, restart, and status.
/// Uses <see cref="TunnelRegistry"/> to manage multiple tunnel providers.
/// </summary>
[ApiController]
[Route("mcpserver/tunnel")]
public sealed class TunnelController : ControllerBase
{
    private readonly TunnelRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="TunnelController"/> class.</summary>
    /// <param name="registry">Tunnel registry managing all providers.</param>
    public TunnelController(TunnelRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>List all registered tunnel providers with their current state.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Array of tunnel provider info objects.</returns>
    [HttpGet("list")]
    public async Task<ActionResult<IReadOnlyList<TunnelInfo>>> ListAsync(CancellationToken ct)
    {
        var list = await _registry.ListAsync(ct).ConfigureAwait(false);
        return Ok(list);
    }

    /// <summary>Get the status of a specific tunnel provider.</summary>
    /// <param name="name">Provider name (e.g. <c>ngrok</c>, <c>cloudflare</c>, <c>frp</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel provider info or 404 if not found.</returns>
    [HttpGet("{name}/status")]
    public async Task<ActionResult<TunnelInfo>> GetStatusAsync(string name, CancellationToken ct)
    {
        var info = await _registry.GetAsync(name, ct).ConfigureAwait(false);
        return info is null ? NotFound(new { error = $"Tunnel provider '{name}' not found." }) : Ok(info);
    }

    /// <summary>Enable a tunnel provider (does not start it).</summary>
    /// <param name="name">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated tunnel info.</returns>
    [HttpPost("{name}/enable")]
    public async Task<ActionResult<TunnelInfo>> EnableAsync(string name, CancellationToken ct)
    {
        if (!_registry.Enable(name))
            return NotFound(new { error = $"Tunnel provider '{name}' not found." });

        var info = await _registry.GetAsync(name, ct).ConfigureAwait(false);
        return Ok(info);
    }

    /// <summary>Disable a tunnel provider. Stops it if running.</summary>
    /// <param name="name">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated tunnel info.</returns>
    [HttpPost("{name}/disable")]
    public async Task<ActionResult<TunnelInfo>> DisableAsync(string name, CancellationToken ct)
    {
        if (!await _registry.DisableAsync(name, ct).ConfigureAwait(false))
            return NotFound(new { error = $"Tunnel provider '{name}' not found." });

        var info = await _registry.GetAsync(name, ct).ConfigureAwait(false);
        return Ok(info);
    }

    /// <summary>Start a tunnel provider. Must be enabled first.</summary>
    /// <param name="name">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after start attempt.</returns>
    [HttpPost("{name}/start")]
    public async Task<ActionResult<TunnelInfo>> StartAsync(string name, CancellationToken ct)
    {
        var info = await _registry.StartAsync(name, ct).ConfigureAwait(false);
        return info is null ? NotFound(new { error = $"Tunnel provider '{name}' not found." }) : Ok(info);
    }

    /// <summary>Stop a tunnel provider.</summary>
    /// <param name="name">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after stop.</returns>
    [HttpPost("{name}/stop")]
    public async Task<ActionResult<TunnelInfo>> StopAsync(string name, CancellationToken ct)
    {
        var info = await _registry.StopAsync(name, ct).ConfigureAwait(false);
        return info is null ? NotFound(new { error = $"Tunnel provider '{name}' not found." }) : Ok(info);
    }

    /// <summary>Restart a tunnel provider (stop then start). Must be enabled.</summary>
    /// <param name="name">Provider name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel info after restart.</returns>
    [HttpPost("{name}/restart")]
    public async Task<ActionResult<TunnelInfo>> RestartAsync(string name, CancellationToken ct)
    {
        var info = await _registry.RestartAsync(name, ct).ConfigureAwait(false);
        return info is null ? NotFound(new { error = $"Tunnel provider '{name}' not found." }) : Ok(info);
    }
}
