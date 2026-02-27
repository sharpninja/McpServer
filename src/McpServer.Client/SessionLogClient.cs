using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for session log endpoints (<c>/mcp/sessionlog</c>). Supports submitting (upserting)
/// session logs, querying historical logs with filters, and appending processing dialog items
/// to existing log entries.
/// </summary>
/// <seealso cref="McpServerClient.SessionLog"/>
public sealed class SessionLogClient : McpClientBase
{
    /// <inheritdoc />
    public SessionLogClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal SessionLogClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Submit (upsert) a session log entry. Creates or updates based on session ID.</summary>
    public async Task<SessionLogSubmitResult> SubmitAsync(UnifiedSessionLogDto sessionLog, CancellationToken cancellationToken = default)
    {
        return await PostAsync<SessionLogSubmitResult>("mcp/sessionlog", sessionLog, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Query session logs with optional filters.</summary>
    public async Task<SessionLogQueryResult> QueryAsync(
        string? agent = null, string? model = null, string? text = null,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (agent is not null) parts.Add($"agent={Uri.EscapeDataString(agent)}");
        if (model is not null) parts.Add($"model={Uri.EscapeDataString(model)}");
        if (text is not null) parts.Add($"text={Uri.EscapeDataString(text)}");
        if (from.HasValue) parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue) parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");
        if (limit != 100) parts.Add($"limit={limit}");
        if (offset != 0) parts.Add($"offset={offset}");
        var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return await GetAsync<SessionLogQueryResult>($"mcp/sessionlog{qs}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Append processing dialog items to a session log entry.</summary>
    public async Task<DialogAppendResult> AppendDialogAsync(
        string agent, string sessionId, string requestId,
        List<ProcessingDialogItemDto> items, CancellationToken cancellationToken = default)
    {
        var path = $"mcp/sessionlog/{Uri.EscapeDataString(agent)}/{Uri.EscapeDataString(sessionId)}/{Uri.EscapeDataString(requestId)}/dialog";
        return await PostAsync<DialogAppendResult>(path, items, cancellationToken).ConfigureAwait(false);
    }
}
