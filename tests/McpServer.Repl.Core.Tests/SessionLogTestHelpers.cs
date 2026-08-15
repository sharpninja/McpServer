using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Fake in-memory implementation of ISessionLogState for testing turn lifecycle.
/// Tracks session and turn state with validation rules.
/// </summary>
internal sealed class FakeSessionLogState : ISessionLogState
{
    private readonly HashSet<string> _completedRequestIds = new();
    private string? _currentTurnRequestId;
    private string? _lastCompletedStatus;

    public string Agent { get; private set; } = string.Empty;
    public string SessionId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public DateTimeOffset Started { get; private set; }
    public DateTimeOffset LastUpdated { get; private set; }
    public string Status { get; private set; } = "in_progress";
    public string? CurrentTurnRequestId => _currentTurnRequestId;
    public string? CurrentTurnStatus { get; private set; }
    public int TurnCount { get; private set; }

    public void OpenSession(string agent, string sessionId, string title, string model)
    {
        Agent = agent;
        SessionId = sessionId;
        Title = title;
        Model = model;
        Started = DateTimeOffset.UtcNow;
        LastUpdated = Started;
        Status = "in_progress";
    }

    public void BeginTurn(string requestId)
    {
        if (string.IsNullOrEmpty(SessionId))
        {
            throw new InvalidOperationException("No session is active");
        }

        if (_completedRequestIds.Contains(requestId))
        {
            throw new InvalidOperationException($"Turn with request ID {requestId} already exists");
        }

        if (_currentTurnRequestId != null)
        {
            throw new InvalidOperationException("A turn is already in progress");
        }

        _currentTurnRequestId = requestId;
        CurrentTurnStatus = "in_progress";
        LastUpdated = DateTimeOffset.UtcNow;
    }

    public void UpdateTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        if (_lastCompletedStatus != null)
        {
            throw new InvalidOperationException($"Turn is immutable (status: {_lastCompletedStatus})");
        }

        LastUpdated = DateTimeOffset.UtcNow;
    }

    public void CompleteTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        _completedRequestIds.Add(_currentTurnRequestId);
        _lastCompletedStatus = "completed";
        _currentTurnRequestId = null;
        CurrentTurnStatus = null;
        TurnCount++;
        LastUpdated = DateTimeOffset.UtcNow;
        _lastCompletedStatus = null;
    }

    public void FailTurn()
    {
        if (_currentTurnRequestId == null)
        {
            throw new InvalidOperationException("No active turn");
        }

        _completedRequestIds.Add(_currentTurnRequestId);
        _lastCompletedStatus = "failed";
        _currentTurnRequestId = null;
        CurrentTurnStatus = null;
        TurnCount++;
        LastUpdated = DateTimeOffset.UtcNow;
        _lastCompletedStatus = null;
    }
}

/// <summary>
/// Stub implementation of ISessionLogClientAdapter for testing workflow routing.
/// Returns predefined responses without making actual HTTP calls.
/// </summary>
internal sealed class StubSessionLogClient : ISessionLogClientAdapter
{
    private int _dialogCount = 0;
    private readonly List<UnifiedSessionLogDto> _sessions = new();

    public UnifiedSessionLogDto? LastSubmitted => _sessions.Count == 0 ? null : _sessions[^1];

    public Task<SessionLogSubmitResult> SubmitAsync(
        UnifiedSessionLogDto sessionLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        _sessions.Add(sessionLog);

        var result = new SessionLogSubmitResult
        {
            Id = _sessions.Count,
            SourceType = sessionLog.SourceType,
            SessionId = sessionLog.SessionId
        };

        return Task.FromResult(result);
    }

    public Task<SessionLogQueryResult> QueryAsync(
        string? agent = null,
        string? model = null,
        string? text = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var items = new List<UnifiedSessionLogDto>();

        foreach (var session in _sessions)
        {
            if (agent != null && session.SourceType != agent)
                continue;
            if (model != null && session.Model != model)
                continue;
            if (text != null && session.Title?.Contains(text) != true)
                continue;

            items.Add(session);
        }

        // If no sessions were submitted but agent is "Copilot", return a default test session
        if (items.Count == 0 && agent == "Copilot")
        {
            items.Add(new UnifiedSessionLogDto
            {
                SourceType = "Copilot",
                SessionId = "Copilot-20260304T113901Z-test",
                Title = "Test Session",
                Model = "claude-sonnet-4"
            });
        }

        var result = new SessionLogQueryResult
        {
            Items = items,
            TotalCount = items.Count,
            Limit = limit,
            Offset = offset
        };

        return Task.FromResult(result);
    }

    public Task<DialogAppendResult> AppendDialogAsync(
        string agent,
        string sessionId,
        string requestId,
        List<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);
        ArgumentNullException.ThrowIfNull(items);

        _dialogCount += items.Count;

        var result = new DialogAppendResult
        {
            Agent = agent,
            SessionId = sessionId,
            RequestId = requestId,
            TotalDialogCount = _dialogCount
        };

        return Task.FromResult(result);
    }
}
