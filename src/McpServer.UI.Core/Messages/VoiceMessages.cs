using System.Collections.Generic;
using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Command to create a voice session.</summary>
public sealed record CreateVoiceSessionCommand : ICommand<VoiceSessionInfo>
{
    /// <summary>Preferred language tag.</summary>
    public string? Language { get; init; }

    /// <summary>Client device identifier.</summary>
    public string? DeviceId { get; init; }

    /// <summary>Client display name.</summary>
    public string? ClientName { get; init; }

    /// <summary>Workspace path override.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Optional agent name for routing.</summary>
    public string? AgentName { get; init; }

    /// <summary>Optional agent path override.</summary>
    public string? AgentPath { get; init; }

    /// <summary>Optional model override.</summary>
    public string? AgentModel { get; init; }

    /// <summary>Optional seed prompt.</summary>
    public string? AgentSeed { get; init; }

    /// <summary>Optional attach prompt.</summary>
    public string? AgentPrompt { get; init; }

    /// <summary>Optional environment-variable map.</summary>
    public Dictionary<string, string>? AgentParameters { get; init; }

    /// <summary>Whether this is a one-shot session.</summary>
    public bool OneShotSession { get; init; }
}

/// <summary>Command to submit a voice turn.</summary>
public sealed record SubmitVoiceTurnCommand : ICommand<VoiceTurnInfo>
{
    /// <summary>Voice session ID.</summary>
    public required string SessionId { get; init; }

    /// <summary>User transcript text.</summary>
    public required string UserTranscriptText { get; init; }

    /// <summary>Optional transcript language.</summary>
    public string? Language { get; init; }

    /// <summary>Optional client timestamp.</summary>
    public string? ClientTimestampUtc { get; init; }
}

/// <summary>Command to interrupt an active voice turn.</summary>
public sealed record InterruptVoiceCommand(string SessionId) : ICommand<VoiceInterruptInfo>;

/// <summary>Query to retrieve voice session status.</summary>
public sealed record GetVoiceStatusQuery(string SessionId) : IQuery<VoiceSessionStatusInfo?>;

/// <summary>Query to retrieve voice session transcript.</summary>
public sealed record GetVoiceTranscriptQuery(string SessionId) : IQuery<VoiceTranscriptInfo?>;

/// <summary>Command to delete a voice session.</summary>
public sealed record DeleteVoiceSessionCommand(string SessionId) : ICommand<bool>;

/// <summary>Created voice session summary.</summary>
public sealed record VoiceSessionInfo(
    string SessionId,
    string Status,
    string Language,
    string? ModelRequested,
    string? ModelResolved);

/// <summary>Voice turn result.</summary>
public sealed record VoiceTurnInfo(
    string SessionId,
    string TurnId,
    string Status,
    string? AssistantDisplayText,
    string? AssistantSpeakText,
    IReadOnlyList<VoiceToolCallInfo> ToolCalls,
    string? Error,
    int LatencyMs,
    string? ModelRequested,
    string? ModelResolved);

/// <summary>Voice tool call record.</summary>
public sealed record VoiceToolCallInfo(
    string TurnId,
    string ToolName,
    int Step,
    string ArgumentsJson,
    string Status,
    bool IsMutation,
    string? ResultSummary,
    string? Error);

/// <summary>Voice interrupt result.</summary>
public sealed record VoiceInterruptInfo(string SessionId, bool Interrupted, string Status);

/// <summary>Voice session status snapshot.</summary>
public sealed record VoiceSessionStatusInfo(
    string SessionId,
    string Status,
    string Language,
    string CreatedUtc,
    string LastUpdatedUtc,
    bool IsTurnActive,
    string? LastError,
    string? LastTurnId,
    int TurnCounter,
    int TranscriptCount);

/// <summary>Voice transcript collection.</summary>
public sealed record VoiceTranscriptInfo(string SessionId, IReadOnlyList<VoiceTranscriptEntryInfo> Items);

/// <summary>Voice transcript entry.</summary>
public sealed record VoiceTranscriptEntryInfo(
    string TimestampUtc,
    string? TurnId,
    string Role,
    string Category,
    string Text);
