using System.Globalization;
using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>Persists normalized transcript sessions through the existing session-log service.</summary>
public sealed class TranscriptSessionLogPersister : ITranscriptSessionPersister
{
    private readonly ISessionLogService _sessionLogService;

    /// <summary>Initializes a transcript session-log persister.</summary>
    /// <param name="sessionLogService">Primary session-log service.</param>
    public TranscriptSessionLogPersister(ISessionLogService sessionLogService)
    {
        _sessionLogService = sessionLogService ?? throw new ArgumentNullException(nameof(sessionLogService));
    }

    /// <inheritdoc />
    public async Task<string> PersistAsync(
        TranscriptIngestionRequest request,
        TranscriptSession session,
        TranscriptSessionReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(receipt);

        var dto = MapToSessionLogDto(request, session);
        var id = await _sessionLogService.SubmitAsync(dto, receipt.YamlArtifactPath, receipt.SourceHash, cancellationToken).ConfigureAwait(false);
        return "sessionLogId:" + id.ToString(CultureInfo.InvariantCulture);
    }

    private static UnifiedSessionLogDto MapToSessionLogDto(TranscriptIngestionRequest request, TranscriptSession session)
    {
        var events = session.Events.OrderBy(item => item.Order).ToArray();
        var firstTimestamp = events.Select(item => item.TimestampUtc).FirstOrDefault(item => item is not null);
        var lastTimestamp = events.Select(item => item.TimestampUtc).LastOrDefault(item => item is not null);
        var workspacePath = string.IsNullOrWhiteSpace(session.WorkspacePath) ? request.WorkspacePath : session.WorkspacePath;

        return new UnifiedSessionLogDto
        {
            SourceType = session.SourceKind.ToString(),
            SessionId = session.SessionId,
            Title = "Imported " + session.SourceKind + " transcript" + (string.IsNullOrWhiteSpace(session.NativeSessionId) ? string.Empty : " " + session.NativeSessionId),
            Model = session.Model,
            Started = FormatTimestamp(firstTimestamp),
            LastUpdated = FormatTimestamp(lastTimestamp ?? firstTimestamp),
            Status = "completed",
            TurnCount = events.Length,
            Workspace = string.IsNullOrWhiteSpace(workspacePath)
                ? null
                : new WorkspaceInfoDto
                {
                    Project = Path.GetFileName(Path.TrimEndingDirectorySeparator(workspacePath!)),
                    Repository = workspacePath,
                },
            Turns = events.Select(item => MapEvent(session.SourceKind, item)).ToList(),
        };
    }

    private static UnifiedRequestEntryDto MapEvent(TranscriptSourceKind sourceKind, TranscriptEvent item)
    {
        var text = JoinText(item.Content);
        var timestamp = FormatTimestamp(item.TimestampUtc);
        var role = item.Role.Trim();
        var entry = new UnifiedRequestEntryDto
        {
            RequestId = item.Id,
            Timestamp = timestamp,
            QueryTitle = CreateTitle(text, item.NativeType),
            Status = "completed",
            Tags = ["transcript-import", sourceKind.ToString(), "native:" + item.NativeType],
            ContextList = item.Metadata.Select(pair => pair.Key + "=" + pair.Value).ToList(),
            OriginalEntry = new Dictionary<string, object?>
            {
                ["id"] = item.Id,
                ["order"] = item.Order,
                ["role"] = item.Role,
                ["nativeType"] = item.NativeType,
                ["content"] = item.Content.Select(block => new Dictionary<string, object?>
                {
                    ["type"] = block.Type,
                    ["text"] = block.Text,
                }).ToArray(),
                ["metadata"] = item.Metadata,
            },
            ProcessingDialog = [new ProcessingDialogItemDto
            {
                Timestamp = timestamp,
                Role = role,
                Category = "transcript_event",
                Content = string.IsNullOrWhiteSpace(text) ? item.NativeType : text,
            }],
        };

        if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            entry.Response = text;
        else if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
            entry.Interpretation = text;
        else
            entry.QueryText = text;

        return entry;
    }

    private static string JoinText(IReadOnlyList<TranscriptContentBlock> blocks)
        => string.Join("\n", blocks.Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));

    private static string CreateTitle(string text, string nativeType)
    {
        var title = string.IsNullOrWhiteSpace(text)
            ? nativeType
            : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n')[0];
        return title.Length <= 80 ? title : title[..80];
    }

    private static string? FormatTimestamp(DateTimeOffset? timestamp)
        => timestamp?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}