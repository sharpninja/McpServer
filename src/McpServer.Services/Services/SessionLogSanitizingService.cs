using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Decorates <see cref="ISessionLogService"/> so read results are sanitized while writes remain unchanged.
/// </summary>
public sealed class SessionLogSanitizingService : ISessionLogService
{
    private readonly ISessionLogService inner;
    private readonly ISessionLogSanitizer sanitizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLogSanitizingService"/> class.
    /// </summary>
    /// <param name="inner">The inner session-log service.</param>
    /// <param name="sanitizer">The sanitizer used for read projections.</param>
    public SessionLogSanitizingService(ISessionLogService inner, ISessionLogSanitizer sanitizer)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    /// <inheritdoc />
    public Task<long> SubmitAsync(
        UnifiedSessionLogDto dto,
        string? sourceFilePath = null,
        string? contentHash = null,
        CancellationToken cancellationToken = default)
    {
        return inner.SubmitAsync(dto, sourceFilePath, contentHash, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> IsUnchangedAsync(
        string sourceType,
        string sessionId,
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        return inner.IsUnchangedAsync(sourceType, sessionId, contentHash, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AppendProcessingDialogAsync(
        string sourceType,
        string sessionId,
        string requestId,
        IReadOnlyList<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
    {
        return inner.AppendProcessingDialogAsync(sourceType, sessionId, requestId, items, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SessionLogQueryResult> QueryAsync(
        SessionLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return sanitizer.SanitizeQueryResult(result);
    }

    /// <inheritdoc />
    public async Task<UnifiedSessionLogDto?> GetAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetAsync(sourceType, sessionId, cancellationToken).ConfigureAwait(false);
        return sanitizer.SanitizeSessionLog(result);
    }

    /// <inheritdoc />
    public Task<long> UpsertTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
    {
        return inner.UpsertTurnAsync(sourceType, sessionId, turn, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> ReplaceTurnAsync(
        string sourceType,
        string sessionId,
        UnifiedRequestEntryDto turn,
        CancellationToken cancellationToken = default)
    {
        return inner.ReplaceTurnAsync(sourceType, sessionId, turn, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> SetSessionTitleAsync(
        string sourceType,
        string sessionId,
        string title,
        CancellationToken cancellationToken = default)
    {
        return inner.SetSessionTitleAsync(sourceType, sessionId, title, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> SetTurnTitleAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string title,
        CancellationToken cancellationToken = default)
    {
        return inner.SetTurnTitleAsync(sourceType, sessionId, requestId, title, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ReplaceTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        UnifiedRequestEntryDto payload,
        CancellationToken cancellationToken = default)
    {
        return inner.ReplaceTurnSectionAsync(sourceType, sessionId, requestId, section, payload, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ClearTurnSectionAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        CancellationToken cancellationToken = default)
    {
        return inner.ClearTurnSectionAsync(sourceType, sessionId, requestId, section, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteTurnItemAsync(
        string sourceType,
        string sessionId,
        string requestId,
        string section,
        string itemKey,
        CancellationToken cancellationToken = default)
    {
        return inner.DeleteTurnItemAsync(sourceType, sessionId, requestId, section, itemKey, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteTurnAsync(
        string sourceType,
        string sessionId,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        return inner.DeleteTurnAsync(sourceType, sessionId, requestId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(
        string sourceType,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        return inner.DeleteSessionAsync(sourceType, sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> OpenSessionAsync(
        string sourceType,
        string sessionId,
        string? title = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        return inner.OpenSessionAsync(sourceType, sessionId, title, model, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> RepairWorkspaceStampsAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        return inner.RepairWorkspaceStampsAsync(dryRun, cancellationToken);
    }
}
