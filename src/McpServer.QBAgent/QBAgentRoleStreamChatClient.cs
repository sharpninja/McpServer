using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using McpServer.McpAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent;

/// <summary>
/// FR-MCP-QBPROGRESS-001: OpenAI-compatible client that POSTs <c>stream=true</c> and prints QuadBrain
/// role events as they arrive, then returns the Arbiter assistant message.
/// </summary>
internal sealed class QBAgentRoleStreamChatClient : IChatClient
{
    internal const string RoleEventName = "quadbrain.role";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly McpAgentOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly Action<string>? _roleProgress;
    private readonly bool _showIntent;

    public QBAgentRoleStreamChatClient(
        McpAgentOptions options,
        HttpClient http,
        bool ownsHttp,
        Action<string>? roleProgress,
        bool showIntent = false)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _ownsHttp = ownsHttp;
        _roleProgress = roleProgress;
        _showIntent = showIntent;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var content = new StringBuilder();
        var toolCalls = new List<AIContent>();
        await foreach (var update in GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            foreach (var part in update.Contents)
            {
                if (part is TextContent text && !string.IsNullOrEmpty(text.Text))
                    content.Append(text.Text);
                else if (part is FunctionCallContent call)
                    toolCalls.Add(call);
            }
        }

        ChatMessage message = toolCalls.Count > 0
            ? new ChatMessage(ChatRole.Assistant, toolCalls)
            : new ChatMessage(ChatRole.Assistant, content.ToString());
        return new ChatResponse([message]);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(messages, options);
        using var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var update in ReadSseAsync(stream, _roleProgress, cancellationToken, _showIntent).ConfigureAwait(false))
            yield return update;
    }

    /// <summary>Parses QuadBrain/OpenAI SSE. Role events go to <paramref name="roleProgress"/> immediately.</summary>
    internal static async IAsyncEnumerable<ChatResponseUpdate> ReadSseAsync(
        Stream stream,
        Action<string>? roleProgress,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default,
        bool showIntent = false)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        string? eventName = null;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (line.Length == 0)
            {
                eventName = null;
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line["event:".Length..].Trim();
                continue;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var data = line["data:".Length..].Trim();
            if (data.Length == 0)
                continue;
            if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                yield break;

            if (string.Equals(eventName, RoleEventName, StringComparison.Ordinal))
            {
                ReportRole(data, roleProgress, showIntent);
                continue;
            }

            if (TryReadAssistantUpdate(data, out var update) && update is not null)
                yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    private HttpRequestMessage BuildRequest(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = QBAgentChatClientFactory.ModelId,
            ["stream"] = true,
            ["messages"] = MapMessages(messages).ToArray(),
        };
        var tools = MapTools(options);
        if (tools is not null)
            payload["tools"] = tools;

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, BuildCompletionsUri())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey ?? string.Empty);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }

    private Uri BuildCompletionsUri()
    {
        var endpoint = QBAgentChatClientFactory.BuildEndpoint(_options.BaseUrl!);
        var trimmed = endpoint.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return new Uri($"{trimmed}/chat/completions");
    }

    /// <summary>Maps Agent Framework messages, including tool results, to OpenAI chat messages.</summary>
    internal static IReadOnlyList<object> MapMessages(IEnumerable<ChatMessage> messages)
    {
        var mapped = new List<object>();
        foreach (var message in messages)
        {
            var results = message.Contents.OfType<FunctionResultContent>().ToArray();
            if (results.Length > 0)
            {
                foreach (var result in results)
                {
                    mapped.Add(new
                    {
                        role = "tool",
                        tool_call_id = result.CallId,
                        content = FormatToolResult(result),
                    });
                }

                continue;
            }

            var calls = message.Contents.OfType<FunctionCallContent>().ToArray();
            if (calls.Length > 0)
            {
                mapped.Add(new
                {
                    role = "assistant",
                    content = string.IsNullOrWhiteSpace(message.Text) ? null : message.Text,
                    tool_calls = calls.Select(static call => new
                    {
                        id = call.CallId,
                        type = "function",
                        function = new
                        {
                            name = call.Name,
                            arguments = call.Arguments is null
                                ? "{}"
                                : JsonSerializer.Serialize(call.Arguments, JsonOptions),
                        },
                    }).ToArray(),
                });
                continue;
            }

            mapped.Add(new
            {
                role = message.Role.Value,
                content = message.Text,
            });
        }

        return mapped;
    }

    private static string FormatToolResult(FunctionResultContent result)
    {
        if (result.Result is null)
            return string.Empty;
        if (result.Result is string text)
            return text;
        try
        {
            return JsonSerializer.Serialize(result.Result, JsonOptions);
        }
        catch (NotSupportedException)
        {
            return result.Result.ToString() ?? string.Empty;
        }
    }

    private static object[]? MapTools(ChatOptions? options)
    {
        if (options?.Tools is not { Count: > 0 })
            return null;

        var tools = new List<object>();
        foreach (var tool in options.Tools)
        {
            if (tool is not AIFunction function)
                continue;
            tools.Add(new
            {
                type = "function",
                function = new
                {
                    name = function.Name,
                    description = function.Description,
                    parameters = function.JsonSchema,
                },
            });
        }

        return tools.Count == 0 ? null : [.. tools];
    }

    private static void ReportRole(string data, Action<string>? roleProgress, bool showIntent = false)
    {
        if (roleProgress is null)
            return;
        try
        {
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var role = root.TryGetProperty("role", out var roleEl) ? roleEl.GetString() : null;
            var phase = root.TryGetProperty("phase", out var phaseEl) ? phaseEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(phase))
                return;
            if (string.Equals(phase, "started", StringComparison.OrdinalIgnoreCase))
            {
                roleProgress($"{role} started");
                return;
            }

            var output = root.TryGetProperty("output", out var outputEl) ? outputEl.GetString() : null;
            var display = QBAgentIntentDisplay.FormatForDisplay(output, showIntent);
            if (string.IsNullOrWhiteSpace(display))
                display = showIntent ? output : string.Empty;
            roleProgress(string.IsNullOrWhiteSpace(display) ? $"{role} completed" : $"{role} completed:{Environment.NewLine}{display}");
        }
        catch (JsonException)
        {
        }
    }

    private static bool TryReadAssistantUpdate(string data, out ChatResponseUpdate? update)
    {
        update = null;
        try
        {
            using var document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                return false;
            var choice = choices[0];
            if (!choice.TryGetProperty("delta", out var delta))
                return false;

            if (delta.TryGetProperty("tool_calls", out var toolCalls)
                && toolCalls.ValueKind == JsonValueKind.Array
                && toolCalls.GetArrayLength() > 0)
            {
                var call = toolCalls[0];
                var id = call.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "call_0" : "call_0";
                var name = string.Empty;
                string? args = null;
                if (call.TryGetProperty("function", out var fn))
                {
                    name = fn.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                    args = fn.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() : null;
                }

                IDictionary<string, object?>? parsed = null;
                if (!string.IsNullOrWhiteSpace(args))
                {
                    try
                    {
                        parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(args, JsonOptions);
                    }
                    catch (JsonException)
                    {
                    }
                }

                update = new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(id, name, parsed)]);
                return true;
            }

            if (delta.TryGetProperty("content", out var contentEl)
                && contentEl.ValueKind == JsonValueKind.String
                && contentEl.GetString() is { Length: > 0 } text)
            {
                update = new ChatResponseUpdate(ChatRole.Assistant, text);
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
