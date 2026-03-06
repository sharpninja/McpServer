using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op chat service used when host does not provide implementation.
/// </summary>
public sealed class NoOpChatWindowService : IChatWindowService
{
    /// <inheritdoc />
    public Task<ChatFileOpenResult> OpenAgentConfigAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatFileOpenResult(false, null, "Not configured"));

    /// <inheritdoc />
    public Task<ChatFileOpenResult> OpenPromptTemplatesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatFileOpenResult(false, null, "Not configured"));

    /// <inheritdoc />
    public Task<IReadOnlyList<PromptTemplate>> LoadPromptsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PromptTemplate>>(Array.Empty<PromptTemplate>());

    /// <inheritdoc />
    public Task<string> PopulatePromptAsync(PromptTemplate? prompt, CancellationToken cancellationToken = default)
        => Task.FromResult(prompt?.Template ?? string.Empty);

    /// <inheritdoc />
    public Task<ChatPreparedPromptResult> SubmitPromptAsync(PromptTemplate? prompt, CancellationToken cancellationToken = default)
    {
        var text = prompt?.Template ?? string.Empty;
        return Task.FromResult(new ChatPreparedPromptResult(!string.IsNullOrWhiteSpace(text), text));
    }

    /// <inheritdoc />
    public Task<ChatLoadModelsResult> LoadModelsAsync(string? initialPreferredModel, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatLoadModelsResult(false, Array.Empty<string>(), null));

    /// <inheritdoc />
    public Task<ChatSendMessageResult> SendMessageAsync(
        ChatSendRequest request,
        IProgress<string>? contentProgress = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatSendMessageResult(false, string.Empty, false, "Not configured"));
}
