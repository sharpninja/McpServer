using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using McpServer.AgentFramework;
using McpServer.AgentFramework.AgentFramework;
using McpServer.AgentFramework.SessionLog;
using McpServer.AgentFramework.Todo;
using McpServer.Client;
using McpServer.Common.Copilot;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

internal sealed class HostedAgentFrameworkExecutionStrategy(
    ICopilotClient copilotClient,
    WorkspaceTokenService workspaceTokenService,
    ServerRuntimeInfo serverRuntimeInfo,
    IServiceProvider serviceProvider,
    ILogger<HostedAgentFrameworkExecutionStrategy> logger)
    : IAgentExecutionStrategy
{
    public string Name => AgentExecutionStrategyNames.HostedAgentFramework;

    public ValueTask<IAgentExecutionSession> CreateSessionAsync(
        AgentExecutionSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var workspacePath = string.IsNullOrWhiteSpace(request.WorkspacePath)
            ? request.Options.WorkingDirectory ?? Environment.CurrentDirectory
            : request.WorkspacePath;
        var apiKey = workspaceTokenService.GetToken(workspacePath) ?? workspaceTokenService.GenerateToken(workspacePath);
        var baseUrl = new Uri($"http://127.0.0.1:{serverRuntimeInfo.ListenPort}");
        var hostedOptions = CreateHostedAgentOptions(request, workspacePath, baseUrl, apiKey);
        var httpClient = new HttpClient
        {
            Timeout = request.Options.Timeout > TimeSpan.Zero && request.Options.Timeout != Timeout.InfiniteTimeSpan
                ? request.Options.Timeout
                : TimeSpan.FromSeconds(300),
        };
        var client = new McpServerClient(
            httpClient,
            new McpServerClientOptions
            {
                ApiKey = apiKey,
                BaseUrl = baseUrl,
                Timeout = httpClient.Timeout,
                WorkspacePath = workspacePath,
            });
        var optionsMonitor = Microsoft.Extensions.Options.Options.Create(hostedOptions);
        var identifiers = new McpSessionIdentifierFactory(optionsMonitor, TimeProvider.System);
        var sessionLog = new SessionLogWorkflow(client, identifiers, TimeProvider.System);
        var todo = new TodoWorkflow(client);
        var hostedAgent = new McpHostedAgent(
            client,
            identifiers,
            new ChatClientAgentOptions
            {
                Description = hostedOptions.Description,
                Id = hostedOptions.AgentId,
                Name = hostedOptions.AgentName,
            },
            optionsMonitor,
            sessionLog,
            todo,
            serviceProvider);

        return ValueTask.FromResult<IAgentExecutionSession>(
            new HostedAgentExecutionSession(
                request,
                httpClient,
                hostedAgent,
                copilotClient,
                logger));
    }

    private static McpAgentFrameworkOptions CreateHostedAgentOptions(
        AgentExecutionSessionRequest request,
        string workspacePath,
        Uri baseUrl,
        string apiKey)
    {
        var agentName = BuildHostedAgentName(request.AgentName);
        return new McpAgentFrameworkOptions
        {
            AgentId = BuildHostedAgentId(agentName),
            AgentName = agentName,
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            Description = $"Hosted Agent Framework execution strategy for {agentName}.",
            RequireAuthentication = true,
            SourceType = McpHostedAgentDefaults.DefaultSourceType,
            Timeout = request.Options.Timeout > TimeSpan.Zero && request.Options.Timeout != Timeout.InfiniteTimeSpan
                ? request.Options.Timeout
                : TimeSpan.FromSeconds(300),
            WorkspacePath = workspacePath,
        };
    }

    private static string BuildHostedAgentId(string agentName)
    {
        var builder = new StringBuilder(agentName.Length);
        foreach (var ch in agentName)
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToLowerInvariant(ch));
            else if (builder.Length == 0 || builder[^1] != '-')
                builder.Append('-');
        }

        var normalized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(normalized)
            ? "mcpserver-hosted-agent"
            : $"mcpserver-{normalized}-hosted-agent";
    }

    private static string BuildHostedAgentName(string? agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            return McpHostedAgentDefaults.DefaultAgentName;

        var trimmed = agentName.Trim();
        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private sealed class HostedAgentExecutionSession : IAgentExecutionSession
    {
        private readonly HostedAgentStdioChatClient _baseChatClient;
        private readonly ChatOptions _chatOptions;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly AgentExecutionSessionRequest _request;
        private bool _disposed;
        private readonly IChatClient _wrappedChatClient;

        public HostedAgentExecutionSession(
            AgentExecutionSessionRequest request,
            HttpClient httpClient,
            IMcpHostedAgent hostedAgent,
            ICopilotClient copilotClient,
            ILogger logger)
        {
            _request = request;
            _httpClient = httpClient;
            _logger = logger;
            _baseChatClient = new HostedAgentStdioChatClient(copilotClient, request.Options, logger);
            var runOptions = hostedAgent.CreateRunOptions();
            _wrappedChatClient = runOptions.ChatClientFactory?.Invoke(_baseChatClient) ?? _baseChatClient;
            _chatOptions = runOptions.ChatOptions?.Clone() ?? new ChatOptions();
        }

        public bool IsAlive => !_disposed && _baseChatClient.IsAlive;

        public int? ProcessId => _baseChatClient.ProcessId;

        public Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default) =>
            ExecuteAsync(_request.InitialPrompt, cancellationToken);

        public IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default) =>
            StreamAsync(_request.InitialPrompt, cancellationToken);

        public Task<CopilotResult> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            ExecuteAsync(prompt, cancellationToken);

        public IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default) =>
            StreamAsync(prompt, cancellationToken);

        public Task SendEscapeAsync(CancellationToken cancellationToken = default) =>
            _baseChatClient.SendEscapeAsync(cancellationToken);

        public Task EndAsync(TimeSpan timeout) => _baseChatClient.EndAsync(timeout);

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            switch (_wrappedChatClient)
            {
                case IAsyncDisposable asyncDisposable when !ReferenceEquals(asyncDisposable, _baseChatClient):
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable when !ReferenceEquals(disposable, _baseChatClient):
                    disposable.Dispose();
                    break;
            }

            await _baseChatClient.DisposeAsync().ConfigureAwait(false);
            _httpClient.Dispose();
        }

        private async Task<CopilotResult> ExecuteAsync(string prompt, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _wrappedChatClient.GetResponseAsync(
                        [new ChatMessage(ChatRole.User, prompt)],
                        _chatOptions.Clone(),
                        cancellationToken)
                    .ConfigureAwait(false);
                var body = ReadResponseText(response);
                return new CopilotResult
                {
                    Body = body,
                    ContentType = CopilotContentType.Text,
                    State = CopilotResultState.Success,
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Hosted Agent Framework execution failed for workspace {WorkspacePath}", _request.WorkspacePath);
                return new CopilotResult
                {
                    Body = string.Empty,
                    State = CopilotResultState.Error,
                    Stderr = ex.Message,
                };
            }
        }

        private async IAsyncEnumerable<string> StreamAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ChatResponse? response = null;
            string? errorText = null;
            try
            {
                response = await _wrappedChatClient.GetResponseAsync(
                        [new ChatMessage(ChatRole.User, prompt)],
                        _chatOptions.Clone(),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Hosted Agent Framework streaming execution failed for workspace {WorkspacePath}", _request.WorkspacePath);
                errorText = ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(errorText))
            {
                yield return $"error: {errorText}";
                yield break;
            }

            if (response is null)
                yield break;

            using var reader = new StringReader(ReadResponseText(response));
            while (true)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    yield break;

                if (!string.IsNullOrWhiteSpace(line))
                    yield return line;
            }
        }

        private static string ReadResponseText(ChatResponse response)
        {
            if (!string.IsNullOrWhiteSpace(response.Text))
                return response.Text.Trim();

            return response.Messages
                .Select(static message => message.Text)
                .FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text))
                ?.Trim()
                ?? string.Empty;
        }
    }

    private sealed class HostedAgentStdioChatClient(
        ICopilotClient copilotClient,
        CopilotClientOptions options,
        ILogger logger)
        : IChatClient, IAsyncDisposable
    {
        private const string ReadySentinel = "Esc to cancel";
        private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

        private readonly SemaphoreSlim _gate = new(1, 1);
        private CopilotInteractiveSession? _session;
        private bool _disposed;

        public int? ProcessId => _session?.ProcessId;

        public bool IsAlive => !_disposed && (_session is null || _session.IsAlive);

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? chatOptions = null,
            CancellationToken cancellationToken = default)
        {
            var prompt = BuildPrompt(messages, chatOptions, _session is null);
            var body = await SendPromptAsync(prompt, cancellationToken).ConfigureAwait(false);
            return ConvertToChatResponse(body, chatOptions);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? chatOptions = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(messages, chatOptions, cancellationToken).ConfigureAwait(false);
            foreach (var update in response.ToChatResponseUpdates())
                yield return update;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public async Task SendEscapeAsync(CancellationToken cancellationToken = default)
        {
            if (_session is null || !_session.IsAlive)
                return;

            await _session.SendEscapeAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task EndAsync(TimeSpan timeout)
        {
            if (_session is null)
                return;

            await _session.EndAsync(timeout).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_session is not null)
                await _session.DisposeAsync().ConfigureAwait(false);

            _gate.Dispose();
        }

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        private async Task<string> SendPromptAsync(string prompt, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_session is null || !_session.IsAlive)
                {
                    logger.LogDebug("Creating stdio-backed hosted-agent session in {WorkingDirectory}", options.WorkingDirectory);
                    _session = copilotClient.CreateInteractiveSession(prompt, CloneOptions(options));
                    return await ReadUntilSentinelAsync(_session.StandardOutput, cancellationToken).ConfigureAwait(false);
                }

                if (_session.StandardInput is null)
                    throw new InvalidOperationException("Interactive session does not expose a writable standard input stream.");

                await _session.StandardInput.WriteLineAsync(prompt.AsMemory(), cancellationToken).ConfigureAwait(false);
                await _session.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
                return await ReadUntilSentinelAsync(_session.StandardOutput, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static CopilotClientOptions CloneOptions(CopilotClientOptions source)
        {
            var clone = new CopilotClientOptions
            {
                AgentPath = source.AgentPath,
                GitHubToken = source.GitHubToken,
                Model = source.Model,
                RunAs = source.RunAs,
                Silent = source.Silent,
                Timeout = source.Timeout,
                WorkingDirectory = source.WorkingDirectory,
            };

            foreach (var pair in source.EnvironmentVariables)
                clone.EnvironmentVariables[pair.Key] = pair.Value;

            return clone;
        }

        private static async Task<string> ReadUntilSentinelAsync(StreamReader reader, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (line is null)
                    break;

                if (line.Contains(ReadySentinel, StringComparison.Ordinal))
                    break;

                builder.AppendLine(LineSanitizer.Sanitize(line));
            }

            return builder.ToString().Trim();
        }

        private static ChatResponse ConvertToChatResponse(string body, ChatOptions? chatOptions)
        {
            if (TryParseEnvelope(body, out var envelope))
            {
                switch (envelope.Type)
                {
                    case "tool_call" when !string.IsNullOrWhiteSpace(envelope.ToolName):
                    {
                        IList<AIContent> contents =
                        [
                            new FunctionCallContent(
                                Guid.NewGuid().ToString("N"),
                                envelope.ToolName!,
                                ConvertArguments(envelope.Arguments))
                        ];
                        var message = new ChatMessage(ChatRole.Assistant, contents);
                        return new ChatResponse(message)
                        {
                            FinishReason = ChatFinishReason.ToolCalls,
                            ModelId = chatOptions?.ModelId,
                        };
                    }

                    case "error_response":
                        return CreateTextResponse(envelope.UserMessage ?? "The request failed.", chatOptions?.ModelId);

                    case "final_response":
                        return CreateTextResponse(
                            envelope.DisplayText
                            ?? envelope.SpeakText
                            ?? string.Empty,
                            chatOptions?.ModelId);
                }
            }

            return CreateTextResponse(body, chatOptions?.ModelId);
        }

        private static ChatResponse CreateTextResponse(string text, string? modelId)
        {
            IList<AIContent> contents =
            [
                new TextContent(text)
            ];

            var message = new ChatMessage(ChatRole.Assistant, contents);
            return new ChatResponse(message)
            {
                FinishReason = ChatFinishReason.Stop,
                ModelId = modelId,
            };
        }

        private static string BuildPrompt(IEnumerable<ChatMessage> messages, ChatOptions? chatOptions, bool includeToolInstructions)
        {
            var messageList = messages?.ToList() ?? [];
            var builder = new StringBuilder();

            if (includeToolInstructions)
            {
                if (!string.IsNullOrWhiteSpace(chatOptions?.Instructions))
                {
                    builder.AppendLine(chatOptions.Instructions.Trim());
                    builder.AppendLine();
                }

                foreach (var systemMessage in messageList.Where(static message => message.Role == ChatRole.System))
                {
                    var systemText = ExtractMessageText(systemMessage);
                    if (!string.IsNullOrWhiteSpace(systemText))
                    {
                        builder.AppendLine(systemText);
                        builder.AppendLine();
                    }
                }

                AppendToolInstructions(builder, chatOptions?.Tools);
            }
            else if (chatOptions?.Tools is { Count: > 0 })
            {
                builder.AppendLine("Continue using the established JSON response schema and available tool names.");
                builder.AppendLine("Return ONLY JSON with either a tool_call, final_response, or error_response object.");
                builder.AppendLine();
            }

            var latestMessage = messageList.LastOrDefault();
            if (latestMessage is not null)
                AppendMessage(builder, latestMessage);

            return builder.ToString().Trim();
        }

        private static void AppendToolInstructions(StringBuilder builder, IList<AITool>? tools)
        {
            if (tools is not { Count: > 0 })
                return;

            builder.AppendLine("You can either answer directly or request one tool call.");
            builder.AppendLine("Return exactly one JSON object matching one of these schemas:");
            builder.AppendLine("{\"type\":\"tool_call\",\"toolName\":\"...\",\"arguments\":{...},\"reasoningSummary\":\"...\"}");
            builder.AppendLine("{\"type\":\"final_response\",\"displayText\":\"...\",\"speakText\":\"...\",\"reasoningSummary\":\"...\"}");
            builder.AppendLine("{\"type\":\"error_response\",\"userMessage\":\"...\",\"speakText\":\"...\"}");
            builder.AppendLine("Return ONLY JSON. No markdown or code fences.");
            builder.AppendLine();
            builder.AppendLine("Available tools:");
            foreach (var tool in tools)
            {
                builder.Append("- ");
                builder.Append(tool.Name);
                if (!string.IsNullOrWhiteSpace(tool.Description))
                {
                    builder.Append(": ");
                    builder.Append(tool.Description.Trim());
                }

                builder.AppendLine();
            }

            builder.AppendLine();
        }

        private static void AppendMessage(StringBuilder builder, ChatMessage message)
        {
            var functionResults = message.Contents.OfType<FunctionResultContent>().ToArray();
            if (functionResults.Length > 0)
            {
                foreach (var result in functionResults)
                {
                    builder.AppendLine("Tool result:");
                    builder.AppendLine(SerializeToolResult(result.Result));
                }

                builder.AppendLine("Continue and return ONLY JSON.");
                return;
            }

            var text = ExtractMessageText(message);
            if (!string.IsNullOrWhiteSpace(text))
                builder.AppendLine(text);
        }

        private static string ExtractMessageText(ChatMessage message)
        {
            if (!string.IsNullOrWhiteSpace(message.Text))
                return message.Text!;

            return string.Join(
                Environment.NewLine,
                message.Contents
                    .OfType<TextContent>()
                    .Select(static content => content.Text)
                    .Where(static text => !string.IsNullOrWhiteSpace(text)));
        }

        private static string SerializeToolResult(object? result) =>
            result switch
            {
                null => "null",
                string text => text,
                _ => JsonSerializer.Serialize(result, s_jsonOptions),
            };

        private static bool TryParseEnvelope(string body, out ResponseEnvelope envelope)
        {
            envelope = null!;
            var json = TryExtractJsonObject(body, out var extracted)
                ? extracted
                : body?.Trim();
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return false;

                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                    return false;

                var type = typeElement.GetString()?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(type))
                    return false;

                envelope = new ResponseEnvelope
                {
                    Type = type!,
                    ToolName = root.TryGetProperty("toolName", out var toolNameElement) && toolNameElement.ValueKind == JsonValueKind.String
                        ? toolNameElement.GetString()
                        : null,
                    Arguments = root.TryGetProperty("arguments", out var argsElement) && argsElement.ValueKind == JsonValueKind.Object
                        ? argsElement.Clone()
                        : null,
                    DisplayText = root.TryGetProperty("displayText", out var displayTextElement) && displayTextElement.ValueKind == JsonValueKind.String
                        ? displayTextElement.GetString()
                        : null,
                    SpeakText = root.TryGetProperty("speakText", out var speakTextElement) && speakTextElement.ValueKind == JsonValueKind.String
                        ? speakTextElement.GetString()
                        : null,
                    UserMessage = root.TryGetProperty("userMessage", out var userMessageElement) && userMessageElement.ValueKind == JsonValueKind.String
                        ? userMessageElement.GetString()
                        : null,
                };
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryExtractJsonObject(string? text, out string json)
        {
            json = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace < 0 || lastBrace <= firstBrace)
                return false;

            json = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
            return true;
        }

        private static Dictionary<string, object?> ConvertArguments(JsonElement? arguments)
        {
            if (arguments is null || arguments.Value.ValueKind != JsonValueKind.Object)
                return [];

            return arguments.Value.EnumerateObject()
                .ToDictionary(
                    static property => property.Name,
                    static property => ConvertJsonValue(property.Value),
                    StringComparer.Ordinal);
        }

        private static object? ConvertJsonValue(JsonElement value) =>
            value.ValueKind switch
            {
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                JsonValueKind.Number when value.TryGetInt64(out var intValue) => intValue,
                JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Array => value.EnumerateArray().Select(ConvertJsonValue).ToArray(),
                JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                    static property => property.Name,
                    static property => ConvertJsonValue(property.Value),
                    StringComparer.Ordinal),
                _ => value.GetRawText(),
            };

        private sealed record ResponseEnvelope
        {
            public required string Type { get; init; }

            public string? DisplayText { get; init; }

            public JsonElement? Arguments { get; init; }

            public string? SpeakText { get; init; }

            public string? ToolName { get; init; }

            public string? UserMessage { get; init; }
        }

    }
}
