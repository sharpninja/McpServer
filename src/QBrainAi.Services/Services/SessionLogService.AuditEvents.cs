// SPDX-License-Identifier: Apache-2.0
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>FR-QB-003/005: Immutable event receipts and atomic projections in the existing session-log lifecycle.</summary>
public sealed partial class SessionLogService
{
    private const long MaximumAuditOrder = 9007199254740991L;
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<SessionAuditEventReceipt> AppendAuditEventAsync(
        SessionAuditEventRequest auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        SyncDbWorkspaceFromContext();
        if (!TryValidateAuditEvent(auditEvent, out var order, out var body))
            return RejectedAuditEvent(auditEvent, order, "invalid_audit_event");
        using (body)
        {
            var workspace = ResolveWorkspaceId();
            if (string.IsNullOrEmpty(workspace)
                || !string.Equals(workspace, WorkspaceIdentity.CanonicalizeWorkspaceId(auditEvent.WorkspacePath), StringComparison.Ordinal))
                return RejectedAuditEvent(auditEvent, order, "payload_conflict");
            var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(auditEvent.PayloadJson)));
            if (!string.Equals(digest, auditEvent.PayloadHash, StringComparison.Ordinal))
                return RejectedAuditEvent(auditEvent, order, "payload_conflict");

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false)
                : null;
            try
            {
                var stored = await _db.SessionAuditEvents.AsNoTracking()
                    .SingleOrDefaultAsync(row => row.EventId == auditEvent.EventId, cancellationToken).ConfigureAwait(false);
                if (stored is not null)
                    return SameAuditEvent(stored, auditEvent, order)
                        ? AuditReceipt(stored, stored.Accepted ? "unchanged" : "rejected")
                        : RejectedAuditEvent(auditEvent, order, "payload_conflict");

                var turn = await FindAuditTurnAsync(auditEvent.AgentIdentity, auditEvent.McpSessionId,
                    auditEvent.RequestId, cancellationToken).ConfigureAwait(false);
                if (turn is null)
                    return RejectedAuditEvent(auditEvent, order, "request_unknown");
                if (await _db.SessionAuditEvents.AnyAsync(row => row.SessionLogTurnId == turn.Id
                    && row.PositionKey == order, cancellationToken).ConfigureAwait(false))
                    return RejectedAuditEvent(auditEvent, order, "payload_conflict");

                var row = new SessionAuditEventEntity
                {
                    WorkspaceId = workspace, SessionLogTurnId = turn.Id, CapturedWorkspacePath = auditEvent.WorkspacePath,
                    AgentIdentity = auditEvent.AgentIdentity, McpSessionId = auditEvent.McpSessionId,
                    RequestId = auditEvent.RequestId, EventId = auditEvent.EventId, CapturedOrder = order,
                    Kind = auditEvent.Kind, PayloadJson = auditEvent.PayloadJson, PayloadHash = auditEvent.PayloadHash,
                    Accepted = !IsTerminalTurnStatus(turn.Status),
                };
                if (!row.Accepted)
                {
                    row.Reason = "skipped_terminal";
                    row.StoredStatus = turn.Status;
                }
                else
                {
                    row.PositionKey = order;
                    AddAuditProjection(turn, auditEvent, order, body!.RootElement);
                }
                _db.SessionAuditEvents.Add(row);
                await SaveChangesBudgetedAsync(cancellationToken, _submitCommandBudget).ConfigureAwait(false);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return AuditReceipt(row, row.Accepted ? "persisted" : "rejected");
            }
            catch (Exception error) when (error is DbUpdateException or DbException)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _db.ChangeTracker.Clear();
                // Unique constraints arbitrate independent contexts. Only an independently read
                // winner can turn a racing write into unchanged; non-constraint storage failures escape.
                var winner = await _db.SessionAuditEvents.AsNoTracking()
                    .SingleOrDefaultAsync(row => row.EventId == auditEvent.EventId, cancellationToken).ConfigureAwait(false);
                if (winner is not null)
                    return SameAuditEvent(winner, auditEvent, order)
                        ? AuditReceipt(winner, winner.Accepted ? "unchanged" : "rejected")
                        : RejectedAuditEvent(auditEvent, order, "payload_conflict");
                var collision = await _db.SessionAuditEvents.AsNoTracking().AnyAsync(row =>
                    row.AgentIdentity == auditEvent.AgentIdentity && row.McpSessionId == auditEvent.McpSessionId
                    && row.RequestId == auditEvent.RequestId && row.PositionKey == order, cancellationToken).ConfigureAwait(false);
                if (collision)
                    return RejectedAuditEvent(auditEvent, order, "payload_conflict");
                // The global UUID index can reject a tenant-invisible owner. Classify the
                // constraint failure without reading that owner's receipt or bypassing filters.
                if (HandoffDbExceptions.IsUniqueViolation(error))
                    return RejectedAuditEvent(auditEvent, order, "payload_conflict");
                throw;
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                _db.ChangeTracker.Clear();
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<SessionAuditEventReceipt?> ReadAuditEventAsync(
        string sourceType, string sessionId, string requestId, string eventId, CancellationToken cancellationToken = default)
    {
        SyncDbWorkspaceFromContext();
        var stored = await _db.SessionAuditEvents.AsNoTracking().SingleOrDefaultAsync(row =>
            row.AgentIdentity == sourceType && row.McpSessionId == sessionId && row.RequestId == requestId
            && row.EventId == eventId, cancellationToken).ConfigureAwait(false);
        return stored is null ? null : AuditReceipt(stored, stored.Accepted ? "persisted" : "rejected");
    }

    /// <inheritdoc />
    public async Task<SessionAuditCompletionResult> CheckAuditCompletionAsync(
        string sourceType, string sessionId, string requestId, CancellationToken cancellationToken = default)
    {
        SyncDbWorkspaceFromContext();
        var rejected = await _db.SessionAuditEvents.CountAsync(row => row.AgentIdentity == sourceType
            && row.McpSessionId == sessionId && row.RequestId == requestId && !row.Accepted,
            cancellationToken).ConfigureAwait(false);
        return new SessionAuditCompletionResult { Allowed = rejected == 0, RejectedCount = rejected };
    }

    private Task<SessionLogTurnEntity?> FindAuditTurnAsync(
        string agent, string session, string request, CancellationToken cancellationToken)
    {
        var workspace = ResolveWorkspaceId();
        return _db.SessionLogTurns.Include(turn => turn.SessionLog)
            .Include(turn => turn.Actions).Include(turn => turn.ProcessingDialog).Include(turn => turn.StringListItems)
            .AsSplitQuery().SingleOrDefaultAsync(turn => turn.SessionLog != null
                && turn.SessionLog.WorkspaceId == workspace && turn.SessionLog.SourceType == agent
                && turn.SessionLog.SessionId == session && turn.RequestId == request, cancellationToken);
    }

    private static bool TryValidateAuditEvent(
        SessionAuditEventRequest input, out long order, out JsonDocument? document)
    {
        order = 0;
        document = null;
        if (string.IsNullOrWhiteSpace(input.WorkspacePath) || string.IsNullOrWhiteSpace(input.AgentIdentity)
            || string.IsNullOrWhiteSpace(input.McpSessionId) || string.IsNullOrWhiteSpace(input.RequestId)
            || !Guid.TryParseExact(input.EventId, "D", out var id) || id == Guid.Empty
            || input.Kind is not ("action" or "decision" or "dialog" or "metadata")
            || input.Order.ValueKind != JsonValueKind.Number || !input.Order.TryGetDecimal(out var number)
            || number < 1 || number > MaximumAuditOrder || decimal.Truncate(number) != number
            || string.IsNullOrWhiteSpace(input.PayloadJson) || string.IsNullOrWhiteSpace(input.PayloadHash))
            return false;
        order = (long)number;
        try
        {
            document = JsonDocument.Parse(input.PayloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return DisposeInvalidAuditDocument(ref document);
            if (input.Kind == "decision")
            {
                if (!TryAuditText(document.RootElement, "conclusion", out _)
                    || !TryAuditText(document.RootElement, "timestamp", out var timestamp)
                    || !DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                    return DisposeInvalidAuditDocument(ref document);
            }
            else if (input.Kind == "action")
            {
                var action = JsonSerializer.Deserialize<UnifiedActionDto>(input.PayloadJson, AuditJsonOptions);
                if (action is null)
                    return DisposeInvalidAuditDocument(ref document);
                ValidateToolExecutionActions([action]);
            }
            else if (input.Kind == "dialog")
            {
                if (!TryAuditText(document.RootElement, "content", out _)
                    || !TryAuditText(document.RootElement, "role", out _)
                    || !TryAuditText(document.RootElement, "timestamp", out var timestamp)
                    || !DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                    return DisposeInvalidAuditDocument(ref document);
            }
            return true;
        }
        catch (Exception error) when (error is JsonException or ArgumentException)
        {
            return DisposeInvalidAuditDocument(ref document);
        }
    }

    private static bool DisposeInvalidAuditDocument(ref JsonDocument? document)
    {
        document?.Dispose();
        document = null;
        return false;
    }

    private static bool TryAuditText(JsonElement body, string name, out string value)
    {
        value = string.Empty;
        if (!body.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String)
            return false;
        value = item.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool SameAuditEvent(SessionAuditEventEntity stored, SessionAuditEventRequest input, long order) =>
        stored.CapturedWorkspacePath == input.WorkspacePath && stored.AgentIdentity == input.AgentIdentity
        && stored.McpSessionId == input.McpSessionId && stored.RequestId == input.RequestId
        && stored.EventId == input.EventId && stored.CapturedOrder == order && stored.Kind == input.Kind
        && stored.PayloadHash == input.PayloadHash && stored.PayloadJson == input.PayloadJson;

    private static SessionAuditEventReceipt AuditReceipt(SessionAuditEventEntity stored, string code) => new()
    {
        WorkspacePath = stored.CapturedWorkspacePath, AgentIdentity = stored.AgentIdentity, McpSessionId = stored.McpSessionId,
        RequestId = stored.RequestId, EventId = stored.EventId, Order = stored.CapturedOrder, PayloadHash = stored.PayloadHash,
        Code = code, Revision = stored.Accepted ? stored.Id : null, Reason = stored.Reason, StoredStatus = stored.StoredStatus,
    };

    private static SessionAuditEventReceipt RejectedAuditEvent(SessionAuditEventRequest input, long order, string reason) => new()
    {
        WorkspacePath = input.WorkspacePath, AgentIdentity = input.AgentIdentity, McpSessionId = input.McpSessionId,
        RequestId = input.RequestId, EventId = input.EventId, Order = order, PayloadHash = input.PayloadHash,
        Code = "rejected", Reason = reason,
    };

    private static void AddAuditProjection(SessionLogTurnEntity turn, SessionAuditEventRequest input, long order, JsonElement body)
    {
        if (input.Kind == "action")
        {
            var action = JsonSerializer.Deserialize<UnifiedActionDto>(input.PayloadJson, AuditJsonOptions)!;
            turn.Actions.Add(new SessionLogActionEntity
            {
                WorkspaceId = turn.WorkspaceId, SessionLogTurn = turn, Order = action.Order,
                Description = action.Description, Type = action.Type, Status = action.Status, FilePath = action.FilePath,
                AuditEventId = input.EventId, AuditEventOrder = order,
            });
        }
        else if (input.Kind == "dialog")
        {
            var item = JsonSerializer.Deserialize<ProcessingDialogItemDto>(input.PayloadJson, AuditJsonOptions)!;
            turn.ProcessingDialog.Add(new SessionLogProcessingDialogEntity
            {
                WorkspaceId = turn.WorkspaceId, SessionLogTurn = turn,
                Timestamp = DateTimeOffset.Parse(item.Timestamp!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                OriginalTimestamp = item.Timestamp, Role = item.Role!, Content = item.Content!, Category = item.Category,
                AuditEventId = input.EventId, AuditEventOrder = order,
            });
        }
        else if (input.Kind == "decision")
        {
            var conclusion = body.GetProperty("conclusion").GetString()!;
            var timestamp = body.GetProperty("timestamp").GetString()!;
            turn.StringListItems.Add(new SessionLogTurnStringListEntity
            {
                WorkspaceId = turn.WorkspaceId, SessionLogTurn = turn, ListType = "DesignDecision", Value = conclusion,
                AuditEventId = input.EventId, AuditEventOrder = order,
            });
            turn.ProcessingDialog.Add(new SessionLogProcessingDialogEntity
            {
                WorkspaceId = turn.WorkspaceId, SessionLogTurn = turn, Role = "model", Category = "decision",
                Content = conclusion, OriginalTimestamp = timestamp,
                Timestamp = DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                AuditEventId = input.EventId, AuditEventOrder = order,
            });
            turn.Actions.Add(new SessionLogActionEntity
            {
                WorkspaceId = turn.WorkspaceId, SessionLogTurn = turn, Order = order,
                Description = conclusion, Type = "design_decision", Status = "completed",
                AuditEventId = input.EventId, AuditEventOrder = order,
            });
        }
        // Ordinals remain contiguous projections. The immutable event row retains captured
        // positions, while legacy rows keep their existing ordinals and relative order.
        var dialogOrdinal = turn.ProcessingDialog.Where(item => item.AuditEventId is null)
            .Select(item => item.Ordinal).DefaultIfEmpty(-1).Max();
        foreach (var item in turn.ProcessingDialog.Where(item => item.AuditEventId is not null)
            .OrderBy(item => item.AuditEventOrder).ThenBy(item => item.AuditEventId, StringComparer.Ordinal))
            item.Ordinal = checked(++dialogOrdinal);
        foreach (var group in turn.StringListItems.Where(item => item.AuditEventId is not null).GroupBy(item => item.ListType))
        {
            var ordinal = turn.StringListItems.Where(item => item.AuditEventId is null && item.ListType == group.Key)
                .Select(item => item.Ordinal).DefaultIfEmpty(-1).Max();
            foreach (var item in group.OrderBy(item => item.AuditEventOrder).ThenBy(item => item.AuditEventId, StringComparer.Ordinal))
                item.Ordinal = checked(++ordinal);
        }
    }
}
