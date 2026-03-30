using System;
using System.Collections.Generic;

namespace McpServer.Repl.Core;

/// <summary>
/// Concrete implementation of <see cref="IDialogItem"/> for turn processing dialog.
/// </summary>
public sealed class DialogItem : IDialogItem
{
    /// <summary>
    /// Initializes a new <see cref="DialogItem"/> with the specified values.
    /// </summary>
    public DialogItem(DateTimeOffset timestamp, string role, string content, string category)
    {
        Timestamp = timestamp;
        Role = role;
        Content = content;
        Category = category;
    }

    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; }

    /// <inheritdoc />
    public string Role { get; }

    /// <inheritdoc />
    public string Content { get; }

    /// <inheritdoc />
    public string Category { get; }
}

/// <summary>
/// Concrete implementation of <see cref="ISessionAction"/> for turn actions.
/// </summary>
public sealed class SessionAction : ISessionAction
{
    /// <summary>
    /// Initializes a new <see cref="SessionAction"/> with the specified values.
    /// </summary>
    public SessionAction(int order, string description, string type, string status, string filePath)
    {
        Order = order;
        Description = description;
        Type = type;
        Status = status;
        FilePath = filePath ?? string.Empty;
    }

    /// <inheritdoc />
    public int Order { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public string Type { get; }

    /// <inheritdoc />
    public string Status { get; }

    /// <inheritdoc />
    public string FilePath { get; }
}

/// <summary>
/// Concrete implementation of <see cref="ISessionLogState"/> for testing and mocking scenarios.
/// </summary>
public sealed class SessionLogStateSnapshot : ISessionLogState
{
    /// <summary>
    /// Initializes a new <see cref="SessionLogStateSnapshot"/> with the specified values.
    /// </summary>
    public SessionLogStateSnapshot(
        string agent,
        string sessionId,
        string title,
        string model,
        DateTimeOffset started,
        DateTimeOffset lastUpdated,
        string status,
        string? currentTurnRequestId,
        string? currentTurnStatus,
        int turnCount)
    {
        Agent = agent;
        SessionId = sessionId;
        Title = title;
        Model = model;
        Started = started;
        LastUpdated = lastUpdated;
        Status = status;
        CurrentTurnRequestId = currentTurnRequestId;
        CurrentTurnStatus = currentTurnStatus;
        TurnCount = turnCount;
    }

    /// <inheritdoc />
    public string Agent { get; }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string Model { get; }

    /// <inheritdoc />
    public DateTimeOffset Started { get; }

    /// <inheritdoc />
    public DateTimeOffset LastUpdated { get; }

    /// <inheritdoc />
    public string Status { get; }

    /// <inheritdoc />
    public string? CurrentTurnRequestId { get; }

    /// <inheritdoc />
    public string? CurrentTurnStatus { get; }

    /// <inheritdoc />
    public int TurnCount { get; }
}

/// <summary>
/// Concrete implementation of <see cref="ISessionLogSummary"/> for query results.
/// </summary>
public sealed class SessionLogSummarySnapshot : ISessionLogSummary
{
    /// <summary>
    /// Initializes a new <see cref="SessionLogSummarySnapshot"/> with the specified values.
    /// </summary>
    public SessionLogSummarySnapshot(
        string agent,
        string sessionId,
        string title,
        string model,
        DateTimeOffset started,
        DateTimeOffset lastUpdated,
        string status,
        int turnCount,
        IReadOnlyList<string> tags,
        int filesModifiedCount)
    {
        Agent = agent;
        SessionId = sessionId;
        Title = title;
        Model = model;
        Started = started;
        LastUpdated = lastUpdated;
        Status = status;
        TurnCount = turnCount;
        Tags = tags ?? Array.Empty<string>();
        FilesModifiedCount = filesModifiedCount;
    }

    /// <inheritdoc />
    public string Agent { get; }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string Model { get; }

    /// <inheritdoc />
    public DateTimeOffset Started { get; }

    /// <inheritdoc />
    public DateTimeOffset LastUpdated { get; }

    /// <inheritdoc />
    public string Status { get; }

    /// <inheritdoc />
    public int TurnCount { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Tags { get; }

    /// <inheritdoc />
    public int FilesModifiedCount { get; }
}
