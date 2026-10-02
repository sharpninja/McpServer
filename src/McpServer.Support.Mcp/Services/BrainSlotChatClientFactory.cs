using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using OpenAIChatClient = OpenAI.Chat.ChatClient;
using OpenAIClientOptions = OpenAI.OpenAIClientOptions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QUAD-002: Creates OpenAI, OpenAI-compatible, or Cli brain-slot chat clients.
/// </summary>
public sealed class BrainSlotChatClientFactory : IBrainSlotChatClientFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProcessSpawner? _processSpawner;
    private readonly IProcessEnvironmentService? _processEnvironment;
    private readonly CliBrainSlotSessionStore? _sessionStore;
    private readonly IOptionsMonitor<BrainSlotOptions>? _brainSlotOptions;
    private readonly ILogger<BrainSlotChatClientFactory> _cliLogger;

    /// <summary>Creates a factory that only serves HTTP OpenAI-compatible slots.</summary>
    public BrainSlotChatClientFactory()
        : this(null, null, null, null, null)
    {
    }

    /// <summary>Creates a factory that can also serve persistent Cli brain slots.</summary>
    public BrainSlotChatClientFactory(
        IProcessSpawner? processSpawner,
        IProcessEnvironmentService? processEnvironment,
        CliBrainSlotSessionStore? sessionStore,
        IOptionsMonitor<BrainSlotOptions>? brainSlotOptions,
        ILogger<BrainSlotChatClientFactory>? cliLogger)
    {
        _processSpawner = processSpawner;
        _processEnvironment = processEnvironment;
        _sessionStore = sessionStore;
        _brainSlotOptions = brainSlotOptions;
        _cliLogger = cliLogger ?? NullLogger<BrainSlotChatClientFactory>.Instance;
    }

    /// <inheritdoc />
    public IBrainSlotChatClient Create(BrainSlotDefinitionEntity slot, string credential)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);
        var providerKind = BrainSlotValidation.NormalizeProviderKind(slot.ProviderKind);

        if (string.Equals(providerKind, "Cli", StringComparison.OrdinalIgnoreCase))
        {
            if (_processSpawner is null || _processEnvironment is null || _sessionStore is null || _brainSlotOptions is null)
                throw new InvalidOperationException("Cli brain slots require process spawning services.");

            return new CliBrainSlotChatClient(
                _processSpawner,
                _processEnvironment,
                _sessionStore,
                _brainSlotOptions,
                _cliLogger);
        }

        if (string.Equals(providerKind, "OpenAICompatible", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(slot.Endpoint))
        {
            return new OpenAiCompatibleBrainSlotChatClient(credential);
        }

        return new ExtensionsAiBrainSlotChatClient(CreateOpenAiIChatClient(slot, credential));
    }

    private static IChatClient CreateOpenAiIChatClient(BrainSlotDefinitionEntity slot, string credential)
    {
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
        return chatClient.AsIChatClient();
    }

    /// <inheritdoc />
    public IBrainSlotCompletionStrategy CreateStrategy(BrainSlotDefinitionEntity slot, string credential)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);
        var providerKind = BrainSlotValidation.NormalizeProviderKind(slot.ProviderKind);
        if (string.Equals(providerKind, "Cli", StringComparison.OrdinalIgnoreCase))
            return new CliCompletionStrategy(Create(slot, credential));
        if (string.Equals(providerKind, "OpenAICompatible", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(slot.Endpoint))
            return new OpenAiCompatibleCompletionStrategy(credential);
        return new OpenAiCompletionStrategy(CreateOpenAiIChatClient(slot, credential));
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

    /// <summary>FR-MCP-LLMSTRATEGY-001: Flatten role prompt plus shared turn context into OpenAI-compatible JSON.</summary>
    internal static string BuildOpenAiCompatibleRequestJson(
        BrainSlotDefinitionEntity slot,
        string input,
        BrainSlotTurnContext context,
        double? temperature)
        => JsonSerializer.Serialize(BuildOpenAiCompatibleRequest(slot, input, context, temperature), JsonOptions);

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static object BuildOpenAiCompatibleRequest(BrainSlotDefinitionEntity slot, string input, double? temperature)
        => BuildOpenAiCompatibleRequest(slot, input, context: null, temperature);

    private static object BuildOpenAiCompatibleRequest(
        BrainSlotDefinitionEntity slot,
        string input,
        BrainSlotTurnContext? context,
        double? temperature)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(slot.SystemPrompt))
            messages.Add(new { role = "system", content = slot.SystemPrompt });
        messages.Add(new { role = "user", content = input });
        if (context is not null)
        {
            messages.Add(new { role = "user", content = context.OriginalInput });
            var identifiers = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                identifiers.Append("sessionId=").Append(context.SessionId).AppendLine();
            if (!string.IsNullOrWhiteSpace(context.TurnId))
                identifiers.Append("turnId=").Append(context.TurnId).AppendLine();
            if (!string.IsNullOrWhiteSpace(context.TransactionId))
                identifiers.Append("transactionId=").Append(context.TransactionId).AppendLine();
            foreach (var role in BrainSlotRoles.All)
            {
                if (context.CommittedRoleEvidence.TryGetValue(role, out var value) && !string.IsNullOrWhiteSpace(value))
                    identifiers.Append("evidence.").Append(role).Append('=').Append(value).AppendLine();
            }

            var extra = identifiers.ToString().TrimEnd();
            if (extra.Length > 0)
                messages.Add(new { role = "user", content = extra });
        }

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

    internal static async Task<string> SendOpenAiCompatibleAsync(
        BrainSlotDefinitionEntity slot,
        string credential,
        string requestJson,
        CancellationToken cancellationToken)
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
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI-compatible provider failed with status {(int)response.StatusCode}: {responseText}");

        return ExtractOpenAiCompatibleMessageText(responseText);
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

            return await SendOpenAiCompatibleAsync(
                    slot,
                    credential,
                    BuildOpenAiCompatibleRequestJson(slot, input, temperature),
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private sealed class OpenAiCompatibleCompletionStrategy(string credential) : IBrainSlotCompletionStrategy
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            return SendOpenAiCompatibleAsync(
                slot,
                credential,
                BuildOpenAiCompatibleRequestJson(slot, input, context, temperature),
                cancellationToken);
        }
    }

    private sealed class OpenAiCompletionStrategy(IChatClient client) : IBrainSlotCompletionStrategy
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            return SendOpenAiAsync(client, BuildOpenAiMessages(slot, input, context), BuildOpenAiOptions(slot, temperature), cancellationToken);
        }
    }

    private sealed class CliCompletionStrategy(IBrainSlotChatClient inner) : IBrainSlotCompletionStrategy
    {
        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            BrainSlotTurnContext context,
            double? temperature,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            return inner.CompleteAsync(slot, FlattenPrompt(input, context), temperature, cancellationToken);
        }
    }

    private static string FlattenPrompt(string input, BrainSlotTurnContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine(input);
        builder.AppendLine(context.OriginalInput);
        if (!string.IsNullOrWhiteSpace(context.SessionId))
            builder.Append("sessionId=").AppendLine(context.SessionId);
        if (!string.IsNullOrWhiteSpace(context.TurnId))
            builder.Append("turnId=").AppendLine(context.TurnId);
        if (!string.IsNullOrWhiteSpace(context.TransactionId))
            builder.Append("transactionId=").AppendLine(context.TransactionId);
        foreach (var role in BrainSlotRoles.All)
        {
            if (context.CommittedRoleEvidence.TryGetValue(role, out var value) && !string.IsNullOrWhiteSpace(value))
                builder.Append("evidence.").Append(role).Append('=').AppendLine(value);
        }

        return builder.ToString().TrimEnd();
    }

    private sealed class ExtensionsAiBrainSlotChatClient : IBrainSlotChatClient
    {
        private readonly IChatClient _client;

        public ExtensionsAiBrainSlotChatClient(IChatClient client)
        {
            _client = client;
        }

        public Task<string> CompleteAsync(
            BrainSlotDefinitionEntity slot,
            string input,
            double? temperature,
            CancellationToken cancellationToken = default)
            => SendOpenAiAsync(_client, BuildOpenAiMessages(slot, input, context: null), BuildOpenAiOptions(slot, temperature), cancellationToken);
    }

    internal static IReadOnlyList<ChatMessage> BuildOpenAiMessages(
        BrainSlotDefinitionEntity slot,
        string input,
        BrainSlotTurnContext? context)
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(slot.SystemPrompt))
            messages.Add(new ChatMessage(ChatRole.System, slot.SystemPrompt));
        messages.Add(new ChatMessage(ChatRole.User, input));
        if (context is not null)
        {
            messages.Add(new ChatMessage(ChatRole.User, context.OriginalInput));
            var identifiers = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                identifiers.Append("sessionId=").Append(context.SessionId).AppendLine();
            if (!string.IsNullOrWhiteSpace(context.TurnId))
                identifiers.Append("turnId=").Append(context.TurnId).AppendLine();
            if (!string.IsNullOrWhiteSpace(context.TransactionId))
                identifiers.Append("transactionId=").Append(context.TransactionId).AppendLine();
            foreach (var role in BrainSlotRoles.All)
            {
                if (context.CommittedRoleEvidence.TryGetValue(role, out var value) && !string.IsNullOrWhiteSpace(value))
                    identifiers.Append("evidence.").Append(role).Append('=').AppendLine(value);
            }

            var extra = identifiers.ToString().TrimEnd();
            if (extra.Length > 0)
                messages.Add(new ChatMessage(ChatRole.User, extra));
        }

        return messages;
    }

    internal static ChatOptions BuildOpenAiOptions(BrainSlotDefinitionEntity slot, double? temperature)
    {
        var options = new ChatOptions();
        if (slot.MaxOutputTokens > 0)
            options.MaxOutputTokens = slot.MaxOutputTokens;
        if (temperature.HasValue)
            options.Temperature = (float)temperature.Value;
        return options;
    }

    internal static async Task<string> SendOpenAiAsync(
        IChatClient client,
        IReadOnlyList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken cancellationToken)
    {
        var response = await client.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }
}
