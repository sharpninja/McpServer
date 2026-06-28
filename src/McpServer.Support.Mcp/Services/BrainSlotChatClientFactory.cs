using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);
        var providerKind = BrainSlotValidation.NormalizeProviderKind(slot.ProviderKind);

        if (string.Equals(providerKind, "OpenAICompatible", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(slot.Endpoint))
        {
            return new OpenAiCompatibleBrainSlotChatClient(credential);
        }

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

    /// <summary>
    /// TEST-MCP-QBOLLAMA-001: Extracts assistant text from OpenAI-compatible providers, including Ollama thinking
    /// fields that may return an empty <c>content</c> and place generated text in <c>reasoning</c> instead.
    /// </summary>
    /// <param name="responseJson">The OpenAI-compatible chat completion response JSON.</param>
    /// <returns>The first non-empty assistant text from content, reasoning, or reasoning_content.</returns>
    internal static string ExtractOpenAiCompatibleMessageText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var message = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");
        return FirstNonEmpty(
            GetString(message, "content"),
            GetString(message, "reasoning"),
            GetString(message, "reasoning_content"));
    }

    /// <summary>Builds the OpenAI-compatible request JSON for provider dispatch and focused tests.</summary>
    internal static string BuildOpenAiCompatibleRequestJson(BrainSlotDefinitionEntity slot, string input, double? temperature)
        => JsonSerializer.Serialize(BuildOpenAiCompatibleRequest(slot, input, temperature), JsonOptions);

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static object BuildOpenAiCompatibleRequest(BrainSlotDefinitionEntity slot, string input, double? temperature)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(slot.SystemPrompt))
            messages.Add(new { role = "system", content = slot.SystemPrompt });
        messages.Add(new { role = "user", content = input });

        var request = new Dictionary<string, object?>
        {
            ["model"] = slot.ModelId,
            ["messages"] = messages,
            ["stream"] = false,
        };
        if (slot.MaxOutputTokens > 0)
            request["max_tokens"] = slot.MaxOutputTokens;
        if (temperature.HasValue)
            request["temperature"] = temperature.Value;
        return request;
    }

    private sealed class OpenAiCompatibleBrainSlotChatClient(string credential) : IBrainSlotChatClient
    {
        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(slot);
            if (string.IsNullOrWhiteSpace(slot.Endpoint))
                throw new InvalidOperationException("OpenAICompatible brain slots require an endpoint.");

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(slot.TimeoutSeconds <= 0 ? 30 : slot.TimeoutSeconds),
            };

            var baseEndpoint = slot.Endpoint.EndsWith("/", StringComparison.Ordinal) ? slot.Endpoint : slot.Endpoint + "/";
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseEndpoint), "chat/completions"))
            {
                Content = new StringContent(
                    BuildOpenAiCompatibleRequestJson(slot, input, temperature),
                    Encoding.UTF8,
                    "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI-compatible provider failed with status {(int)response.StatusCode}: {responseText}");

            return ExtractOpenAiCompatibleMessageText(responseText);
        }

    }

    private sealed class ExtensionsAiBrainSlotChatClient : IBrainSlotChatClient
    {
        private readonly IChatClient _client;

        public ExtensionsAiBrainSlotChatClient(IChatClient client)
        {
            _client = client;
        }

        public async Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrWhiteSpace(slot.SystemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, slot.SystemPrompt));
            messages.Add(new ChatMessage(ChatRole.User, input));

            var options = new ChatOptions();
            if (slot.MaxOutputTokens > 0)
                options.MaxOutputTokens = slot.MaxOutputTokens;
            if (temperature.HasValue)
                options.Temperature = (float)temperature.Value;

            var response = await _client.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            return response.Text ?? string.Empty;
        }
    }
}
