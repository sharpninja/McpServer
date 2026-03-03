using System.Text.Json;
using McpServer.Support.Mcp.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// FR-MCP-031: Server-Sent Events stream for REST clients to receive change notifications.
/// </summary>
[Route("mcpserver/events")]
[ApiController]
public sealed class EventStreamController : ControllerBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Opens an SSE stream of workspace change events.
    /// Optionally filter by category (e.g. <c>?category=todo</c>).
    /// </summary>
    [HttpGet]
    [Produces("text/event-stream")]
    public async Task StreamEventsAsync(
        [FromQuery] string? category,
        [FromServices] IChangeEventBus eventBus,
        [FromServices] ILogger<EventStreamController> logger,
        CancellationToken ct)
    {
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.ContentType = "text/event-stream";

        // Immediately notify the client that the connection is established.
        var connected = new ChangeEvent
        {
            Category = ChangeEventCategories.Connection,
            Action = ChangeEventActions.Connected,
            EntityId = category,
        };
        var connectedData = JsonSerializer.Serialize(connected, s_jsonOptions);
        await Response.WriteAsync($"event: {connected.Category}\ndata: {connectedData}\n\n", ct).ConfigureAwait(false);
        await Response.Body.FlushAsync(ct).ConfigureAwait(false);

        try
        {
            await foreach (var evt in eventBus.SubscribeAsync(ct).ConfigureAwait(false))
            {
                if (category is not null && !string.Equals(evt.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;

                var data = JsonSerializer.Serialize(evt, s_jsonOptions);
                await Response.WriteAsync($"event: {evt.Category}\ndata: {data}\n\n", ct).ConfigureAwait(false);
                await Response.Body.FlushAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — not an error.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Event stream error");

            // Best-effort: try to send a failure event before the stream closes.
            try
            {
                var failed = new ChangeEvent
                {
                    Category = ChangeEventCategories.Connection,
                    Action = ChangeEventActions.ConnectionFailed,
                    EntityId = ex.Message,
                };
                var failedData = JsonSerializer.Serialize(failed, s_jsonOptions);
                await Response.WriteAsync($"event: {failed.Category}\ndata: {failedData}\n\n", CancellationToken.None).ConfigureAwait(false);
                await Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Stream may already be closed; nothing more we can do.
            }
        }
    }
}
