using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using OpenAIChatClient = OpenAI.Chat.ChatClient;
using OpenAIClientOptions = OpenAI.OpenAIClientOptions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QUAD-002: Creates OpenAI/OpenAI-compatible brain-slot chat clients.
/// </summary>
public sealed class BrainSlotChatClientFactory : IBrainSlotChatClientFactory
{
    /// <inheritdoc />
    public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);
        BrainSlotValidation.NormalizeProviderKind(slot.ProviderKind);

        var options = new OpenAIClientOptions
        {
            NetworkTimeout = TimeSpan.FromSeconds(slot.TimeoutSeconds <= 0 ? 30 : slot.TimeoutSeconds),
        };
        if (!string.IsNullOrWhiteSpace(slot.Endpoint))
            options.Endpoint = new Uri(slot.Endpoint);

        var chatClient = new OpenAIChatClient(
            slot.ModelId,
            new ApiKeyCredential(credential),
            options);
        return new ExtensionsAiBrainSlotChatClient(chatClient.AsIChatClient());
    }

    private sealed class ExtensionsAiBrainSlotChatClient : IBrainSlotChatClient
    {
        private readonly IChatClient _client;

        public ExtensionsAiBrainSlotChatClient(IChatClient client)
        {
            _client = client;
        }

        public async Task<string> CompleteAsync(BrainSlotDefinitionEntity slot, string input, CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(slot.SystemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, slot.SystemPrompt));
            messages.Add(new ChatMessage(ChatRole.User, input));

            var options = new ChatOptions();
            if (slot.MaxOutputTokens > 0)
                options.MaxOutputTokens = slot.MaxOutputTokens;

            var response = await _client.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            return response.Text ?? string.Empty;
        }
    }
}
