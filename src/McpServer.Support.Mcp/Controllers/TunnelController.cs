using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Tunnel lifecycle endpoints: start, stop, restart, and status.
/// Only available when a tunnel provider is registered.
/// </summary>
[ApiController]
[Route("mcp/tunnel")]
public sealed class TunnelController : ControllerBase
{
    private readonly ITunnelProvider? _tunnelProvider;

    /// <summary>Initializes a new instance of the <see cref="TunnelController"/> class.</summary>
    /// <param name="tunnelProvider">Optional tunnel provider (null when no provider is configured).</param>
    public TunnelController(ITunnelProvider? tunnelProvider = null)
    {
        _tunnelProvider = tunnelProvider;
    }

    /// <summary>Get the current tunnel status.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel status including running state, public URL, and any error.</returns>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatusAsync(CancellationToken ct)
    {
        if (_tunnelProvider is null)
            return Ok(new { provider = (string?)null, isRunning = false, error = "No tunnel provider configured." });

        var status = await _tunnelProvider.GetStatusAsync(ct).ConfigureAwait(false);
        return Ok(new
        {
            provider = _tunnelProvider.ProviderName,
            status.IsRunning,
            status.PublicUrl,
            status.Error,
        });
    }

    /// <summary>Start the tunnel. No-op if already running.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel status after start attempt.</returns>
    [HttpPost("start")]
    public async Task<ActionResult<object>> StartAsync(CancellationToken ct)
    {
        if (_tunnelProvider is null)
            return BadRequest(new { error = "No tunnel provider configured." });

        var pre = await _tunnelProvider.GetStatusAsync(ct).ConfigureAwait(false);
        if (pre.IsRunning)
            return Ok(new { provider = _tunnelProvider.ProviderName, pre.IsRunning, pre.PublicUrl, message = "Tunnel already running." });

        await _tunnelProvider.StartAsync(ct).ConfigureAwait(false);
        var post = await _tunnelProvider.GetStatusAsync(ct).ConfigureAwait(false);
        return Ok(new
        {
            provider = _tunnelProvider.ProviderName,
            post.IsRunning,
            post.PublicUrl,
            post.Error,
        });
    }

    /// <summary>Stop the tunnel. No-op if not running.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel status after stop attempt.</returns>
    [HttpPost("stop")]
    public async Task<ActionResult<object>> StopAsync(CancellationToken ct)
    {
        if (_tunnelProvider is null)
            return BadRequest(new { error = "No tunnel provider configured." });

        await _tunnelProvider.StopAsync(ct).ConfigureAwait(false);
        var status = await _tunnelProvider.GetStatusAsync(ct).ConfigureAwait(false);
        return Ok(new
        {
            provider = _tunnelProvider.ProviderName,
            status.IsRunning,
            status.PublicUrl,
            status.Error,
        });
    }

    /// <summary>Restart the tunnel (stop then start).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tunnel status after restart.</returns>
    [HttpPost("restart")]
    public async Task<ActionResult<object>> RestartAsync(CancellationToken ct)
    {
        if (_tunnelProvider is null)
            return BadRequest(new { error = "No tunnel provider configured." });

        await _tunnelProvider.StopAsync(ct).ConfigureAwait(false);
        await _tunnelProvider.StartAsync(ct).ConfigureAwait(false);
        var status = await _tunnelProvider.GetStatusAsync(ct).ConfigureAwait(false);
        return Ok(new
        {
            provider = _tunnelProvider.ProviderName,
            status.IsRunning,
            status.PublicUrl,
            status.Error,
        });
    }
}
