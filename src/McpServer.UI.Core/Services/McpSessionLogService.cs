using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.UI.Core.Models.Json;
using ClientModels = McpServer.Client.Models;

namespace McpServer.UI.Core.Services;

public sealed class McpSessionLogService
{
    private const int PageSize = 1000;
    private readonly McpServerClient _client;

    /// <summary>Creates a session-log service using a pre-authenticated, shared MCP client.</summary>
    public McpSessionLogService(McpServerClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<IReadOnlyList<UnifiedSessionLog>> GetAllSessionsAsync(CancellationToken cancellationToken)
    {
        var sessions = new List<UnifiedSessionLog>();
        var offset = 0;
        var total = int.MaxValue;

        while (offset < total)
        {
            var page = await _client.SessionLog.QueryAsync(
                limit: PageSize,
                offset: offset,
                cancellationToken: cancellationToken).ConfigureAwait(true);

            if (page.Items == null || page.Items.Count == 0)
                break;

            total = page.TotalCount;
            foreach (var item in page.Items)
                sessions.Add(Map(item));

            offset += page.Items.Count;
            if (page.Items.Count < PageSize)
                break;
        }

        return sessions;
    }

    private static UnifiedSessionLog Map(ClientModels.UnifiedSessionLogDto dto)
    {
        var log = new UnifiedSessionLog
        {
            SourceType = dto.SourceType ?? "",
            SessionId = dto.SessionId ?? "",
            Title = dto.Title ?? "",
            Model = dto.Model ?? "",
            Started = ParseDateTime(dto.Started),
            LastUpdated = ParseDateTime(dto.LastUpdated),
            Status = dto.Status ?? "",
            EntryCount = dto.EntryCount,
            TotalTokens = dto.TotalTokens ?? 0,
            CursorSessionLabel = dto.CursorSessionLabel,
            Workspace = dto.Workspace == null
                ? null
                : new WorkspaceInfo
                {
                    Project = dto.Workspace.Project ?? "",
                    TargetFramework = dto.Workspace.TargetFramework ?? "",
                    Repository = dto.Workspace.Repository ?? "",
                    Branch = dto.Workspace.Branch ?? ""
                },
            CopilotStatistics = dto.CopilotStatistics == null
                ? null
                : new StatisticsInfo
                {
                    AverageSuccessScore = dto.CopilotStatistics.AverageSuccessScore,
                    TotalNetTokens = dto.CopilotStatistics.TotalNetTokens,
                    TotalNetPremiumRequests = dto.CopilotStatistics.TotalNetPremiumRequests,
                    CompletedCount = dto.CopilotStatistics.CompletedCount,
                    InProgressCount = dto.CopilotStatistics.InProgressCount
                }
        };

        if (dto.Entries != null)
        {
            foreach (var entry in dto.Entries)
                log.Entries.Add(MapEntry(entry, log.SourceType));
        }

        if (log.EntryCount <= 0)
            log.EntryCount = log.Entries.Count;

        return log;
    }

    private static UnifiedRequestEntry MapEntry(ClientModels.UnifiedRequestEntryDto dto, string sourceType)
    {
        var entry = new UnifiedRequestEntry
        {
            RequestId = dto.RequestId ?? "",
            Timestamp = ParseDateTime(dto.Timestamp),
            Model = dto.Model ?? "",
            ModelProvider = dto.ModelProvider ?? "",
            Agent = sourceType ?? "",
            QueryText = dto.QueryText ?? "",
            QueryTitle = dto.QueryTitle ?? "",
            Response = dto.Response ?? "",
            Interpretation = dto.Interpretation ?? "",
            Status = dto.Status ?? "",
            ContextList = dto.ContextList ?? new List<string>(),
            RawContext = dto.RawContext,
            TokenCount = dto.TokenCount ?? 0,
            IsPremium = dto.IsPremium ?? false,
            Score = dto.Score,
            FailureNote = dto.FailureNote ?? "",
            OriginalEntry = dto.OriginalEntry ?? dto
        };

        if (dto.Tags != null && dto.Tags.Count > 0)
            entry.Tags.AddRange(dto.Tags);

        if (dto.Actions != null)
        {
            foreach (var action in dto.Actions)
            {
                entry.Actions.Add(new UnifiedAction
                {
                    Order = action.Order,
                    Description = action.Description ?? "",
                    Type = action.Type ?? "",
                    Status = action.Status ?? "",
                    FilePath = action.FilePath ?? ""
                });
            }
        }

        if (dto.ProcessingDialog != null)
        {
            foreach (var dialog in dto.ProcessingDialog)
            {
                entry.ProcessingDialog.Add(new UnifiedProcessingDialogItem
                {
                    Timestamp = dialog.Timestamp ?? "",
                    Role = dialog.Role ?? "",
                    Content = dialog.Content ?? "",
                    Category = dialog.Category ?? ""
                });
            }
        }

        return entry;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTimeOffset.TryParse(value, out var dto))
            return dto.UtcDateTime;
        if (DateTime.TryParse(value, out var dt))
            return dt;
        return null;
    }

    /// <summary>Submits (upserts) a session log via the MCP server.</summary>
    public async Task<ClientModels.SessionLogSubmitResult> SubmitAsync(
        ClientModels.UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default)
    {
        return await _client.SessionLog.SubmitAsync(sessionLog, cancellationToken);
    }

    /// <summary>Appends processing dialog items to a specific request entry.</summary>
    public async Task<ClientModels.DialogAppendResult> AppendDialogAsync(
        string agent, string sessionId, string requestId,
        List<ClientModels.ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
    {
        return await _client.SessionLog.AppendDialogAsync(agent, sessionId, requestId, items, cancellationToken);
    }
}

