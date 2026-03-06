using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Bridges <see cref="McpSessionLogService"/> to UI.Core's <see cref="ISessionLogApiClient"/>
/// so that <c>SessionLogListViewModel</c> and <c>SessionLogDetailViewModel</c> can operate
/// against the app-owned service.
/// </summary>
internal sealed class UiCoreSessionLogApiClientAdapter : ISessionLogApiClient
{
    private readonly McpSessionLogService _service;
    private readonly ILogger<UiCoreSessionLogApiClientAdapter> _logger;

    public UiCoreSessionLogApiClientAdapter(
        McpSessionLogService service,
        ILogger<UiCoreSessionLogApiClientAdapter>? logger = null)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? NullLogger<UiCoreSessionLogApiClientAdapter>.Instance;
    }

    public async Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        try
        {
            var all = await _service.GetAllSessionsAsync(cancellationToken);

            // Apply optional filters
            IEnumerable<Models.Json.UnifiedSessionLog> filtered = all;
            if (!string.IsNullOrWhiteSpace(query.Text))
            {
                var text = query.Text;
                filtered = filtered.Where(s =>
                    (s.Title?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.SessionId?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            if (!string.IsNullOrWhiteSpace(query.Agent))
                filtered = filtered.Where(s => string.Equals(s.SourceType, query.Agent, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.Model))
                filtered = filtered.Where(s => string.Equals(s.Model, query.Model, StringComparison.OrdinalIgnoreCase));

            var list = filtered.ToList();
            var total = list.Count;

            // Apply pagination
            var offset = Math.Max(0, query.Offset);
            var limit = query.Limit > 0 ? query.Limit : 20;
            var page = list.Skip(offset).Take(limit).ToList();

            var summaries = page.Select(UiCoreMessageMapper.ToSessionLogSummary).ToList();
            return new ListSessionLogsResult(summaries, total, limit, offset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SessionLog list failed");
            return new ListSessionLogsResult([], 0, query.Limit, query.Offset);
        }
    }

    public async Task<SessionLogDetail?> GetSessionLogAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var all = await _service.GetAllSessionsAsync(cancellationToken);
            var match = all.FirstOrDefault(s => string.Equals(s.SessionId, sessionId, StringComparison.OrdinalIgnoreCase));
            return match is null ? null : UiCoreMessageMapper.ToSessionLogDetail(match);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SessionLog get failed for {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<SessionLogSubmitOutcome> SubmitSessionLogAsync(SubmitSessionLogCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var dto = UiCoreMessageMapper.ToUnifiedSessionLogDto(command.SessionLog);
            var result = await _service.SubmitAsync(dto, cancellationToken);
            return new SessionLogSubmitOutcome(result.Id, result.SourceType, result.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SessionLog submit failed");
            return new SessionLogSubmitOutcome(0, command.SessionLog.SourceType, command.SessionLog.SessionId);
        }
    }

    public async Task<SessionLogDialogAppendOutcome> AppendSessionLogDialogAsync(AppendSessionLogDialogCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            var items = command.Items
                .Select(UiCoreMessageMapper.ToProcessingDialogItemDto)
                .ToList();
            var result = await _service.AppendDialogAsync(
                command.Agent, command.SessionId, command.RequestId, items, cancellationToken);
            return new SessionLogDialogAppendOutcome(result.Agent, result.SessionId, result.RequestId, result.TotalDialogCount);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SessionLog dialog append failed");
            return new SessionLogDialogAppendOutcome(command.Agent, command.SessionId, command.RequestId, 0);
        }
    }
}

