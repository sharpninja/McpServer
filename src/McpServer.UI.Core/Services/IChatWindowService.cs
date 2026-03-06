using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Service abstraction for chat window operations.
/// </summary>
public interface IChatWindowService
{
    /// <summary>Opens agent config file in default editor.</summary>
    Task<ChatFileOpenResult> OpenAgentConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>Opens prompt template file in default editor.</summary>
    Task<ChatFileOpenResult> OpenPromptTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads prompt templates.</summary>
    Task<IReadOnlyList<PromptTemplate>> LoadPromptsAsync(CancellationToken cancellationToken = default);

    /// <summary>Expands a prompt template into editor text.</summary>
    Task<string> PopulatePromptAsync(PromptTemplate? prompt, CancellationToken cancellationToken = default);

    /// <summary>Prepares a prompt and indicates whether it should be sent immediately.</summary>
    Task<ChatPreparedPromptResult> SubmitPromptAsync(PromptTemplate? prompt, CancellationToken cancellationToken = default);

    /// <summary>Loads available chat models.</summary>
    Task<ChatLoadModelsResult> LoadModelsAsync(string? initialPreferredModel, CancellationToken cancellationToken = default);

    /// <summary>Sends a chat request and streams incremental content through progress callback.</summary>
    Task<ChatSendMessageResult> SendMessageAsync(
        ChatSendRequest request,
        IProgress<string>? contentProgress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of trying to open a file in default editor.
/// </summary>
public sealed record ChatFileOpenResult(bool Opened, string? FilePath = null, string? ErrorMessage = null);

/// <summary>
/// Request payload for sending a chat message.
/// </summary>
public sealed record ChatSendRequest(string UserMessage, string ContextSummary, string? Model);

/// <summary>
/// Prompt pre-processing result.
/// </summary>
public sealed record ChatPreparedPromptResult(bool ShouldSend, string PromptText);

/// <summary>
/// Chat model discovery result.
/// </summary>
public sealed record ChatLoadModelsResult(bool IsReachable, IReadOnlyList<string> Models, string? SelectedModel);

/// <summary>
/// Chat send result.
/// </summary>
public sealed record ChatSendMessageResult(bool Success, string ReplyText, bool WasCancelled, string? ErrorMessage = null);
