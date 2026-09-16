using System.Globalization;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBOPENAI-001: Maps OpenAI-compatible chat-completion requests onto QuadBrain orchestration so any
/// OpenAI-compatible client (including QBAgent) can use QuadBrain as a drop-in model.
/// </summary>
public interface IQuadBrainOpenAiChatService
{
    /// <summary>Completes an OpenAI-compatible chat request by running QuadBrain orchestration.</summary>
    /// <param name="request">The OpenAI-compatible request.</param>
    /// <param name="sessionId">FR-MCP-QUAD-SESSION-001: the session this QuadBrain instance is attached to (from the <c>X-Session-Id</c> header). Multiple instances run concurrently, each bound to its own session.</param>
    /// <param name="turnId">Optional turn id (from the <c>X-Turn-Id</c> header) used to correlate session-log writes and turn transactions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An OpenAI-compatible chat-completion response.</returns>
    Task<OpenAiChatCompletionResponse> CompleteAsync(
        OpenAiChatCompletionRequest request,
        string? sessionId = null,
        string? turnId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>FR-MCP-QBOPENAI-001: Default <see cref="IQuadBrainOpenAiChatService"/> backed by QuadBrain orchestration.</summary>
public sealed class QuadBrainOpenAiChatService : IQuadBrainOpenAiChatService
{
    private const string DefaultSourceType = "QBAgent";

    /// <summary>Max QuadBrain rounds in one HTTP completion (internals + deferred-intent continues).</summary>
    internal const int MaxProcessingRounds = 8;

    internal const string ContinueDirective =
        "Continue now. Return ONLY JSON with intent (final, continue, tool_calls, or reject). Example: {\"intent\":\"tool_calls\",\"tool_calls\":[{\"name\":\"read_file\",\"arguments\":{\"path\":\"AGENTS-README-FIRST.yaml\"}}]} or {\"intent\":\"final\",\"content\":\"...\"}. Do not answer in prose without that envelope.";

    private readonly IQuadBrainOrchestrationService _orchestration;
    private readonly QuadBrainToolInterceptor _interceptor;
    private readonly IBrainInteractionSessionLogger? _interactionLogger;
    private readonly ISessionLogService? _sessionLog;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainOpenAiChatService"/> class.</summary>
    /// <param name="orchestration">The QuadBrain orchestration service.</param>
    /// <param name="classifier">Internal/external tool classifier (defaults to the <c>mcp_</c>-prefix classifier).</param>
    /// <param name="internalToolExecutor">Server-side internal tool executor (defaults to a no-op).</param>
    /// <param name="interactionLogger">FR-MCP-QBEXEC-001 (AC-5): optional session-log writer used to record internal-tool failures.</param>
    /// <param name="sessionLog">Optional session-log service used to complete the correlated turn with the final assistant response.</param>
    public QuadBrainOpenAiChatService(
        IQuadBrainOrchestrationService orchestration,
        IQuadBrainToolClassifier? classifier = null,
        IQuadBrainInternalToolExecutor? internalToolExecutor = null,
        IBrainInteractionSessionLogger? interactionLogger = null,
        ISessionLogService? sessionLog = null)
    {
        _orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
        _interceptor = new QuadBrainToolInterceptor(
            classifier ?? new QuadBrainToolClassifier(),
            internalToolExecutor ?? NoopInternalToolExecutor.Instance);
        _interactionLogger = interactionLogger;
        _sessionLog = sessionLog;
    }

    /// <inheritdoc />
    public async Task<OpenAiChatCompletionResponse> CompleteAsync(
        OpenAiChatCompletionRequest request,
        string? sessionId = null,
        string? turnId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Messages is not { Count: > 0 })
            throw new ArgumentException("At least one message is required.", nameof(request));

        var toolChoice = ParseToolChoice(request.ToolChoice, request.Tools);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["openai.surface"] = "chat.completions",
            ["openai.model"] = string.IsNullOrWhiteSpace(request.Model) ? "quadbrain" : request.Model!.Trim(),
        };
        if (request.Tools is { Count: > 0 })
            metadata["openai.tools"] = string.Join(",", request.Tools.Select(static t => t.Function.Name));
        metadata["openai.tool_choice"] = toolChoice.MetadataValue;

        // FR-MCP-QUAD-SESSION-001: attach this QuadBrain run to its session (and optional turn) so orchestration,
        // inter-brain logging, internal-tool-failure logging, and turn transactions are correlated to the session.
        if (!string.IsNullOrWhiteSpace(sessionId))
            metadata["sessionId"] = sessionId.Trim();
        if (!string.IsNullOrWhiteSpace(turnId))
            metadata["turnId"] = turnId.Trim();

        var promptTools = toolChoice.Kind == ToolChoiceKind.None ? null : request.Tools;
        var seedInput = BuildPrompt(request.Messages, promptTools, toolChoice);
        var workingMessages = request.Messages.ToList();
        var orchestrationInput = seedInput;
        QuadBrainOrchestrationResponse? orchestration = null;
        string? lastInternalSignature = null;
        string? lastDeferred = null;
        OpenAiChatResponseMessage message = new() { Role = "assistant" };
        var finishReason = "stop";

        for (var round = 0; round < MaxProcessingRounds; round++)
        {
            orchestrationInput = BuildPrompt(workingMessages, promptTools, toolChoice);
            orchestration = await _orchestration.ExecuteFullOrchestrationAsync(
                new QuadBrainOrchestrationRequest
                {
                    Input = orchestrationInput,
                    TurnId = string.IsNullOrWhiteSpace(turnId) ? null : turnId.Trim(),
                    Metadata = metadata,
                    Progress = request.Progress,
                },
                cancellationToken).ConfigureAwait(false);

            if (IsFailedOrchestration(orchestration)
                && !TryCollectToolCalls(orchestration, out _))
            {
                message.Content = TryRoleFinalContent(orchestration) ?? BuildNoDecisionMessage(orchestration);
                finishReason = "stop";
                break;
            }

            if (toolChoice.Kind != ToolChoiceKind.None
                && TryCollectToolCalls(orchestration, out var toolCalls))
            {
                var interception = await _interceptor.InterceptAsync(toolCalls, turnId: null, cancellationToken)
                    .ConfigureAwait(false);
                ValidateToolChoiceResult(toolChoice, interception.RemainingToolCalls, interception.Failed, orchestration.Output);
                await LogInternalToolFailuresAsync(interception.Failed, metadata, cancellationToken).ConfigureAwait(false);
                var failureNote = BuildFailureNote(interception.Failed);
                var internalContent = BuildInternalSuccessContent(interception.Executed, failureNote);

                if (interception.RemainingToolCalls.Count > 0)
                {
                    message.ToolCalls = [.. interception.RemainingToolCalls];
                    if (internalContent.Length > 0)
                        message.Content = internalContent;
                    finishReason = "tool_calls";
                    break;
                }

                if (interception.Executed.Count > 0)
                {
                    var signature = string.Join(",", interception.Executed.Select(static item => item.ToolCall.Function.Name));
                    if (string.Equals(signature, lastInternalSignature, StringComparison.Ordinal))
                    {
                        message.Content = internalContent;
                        finishReason = "stop";
                        break;
                    }

                    lastInternalSignature = signature;
                    workingMessages.Add(new OpenAiChatMessage { Role = "assistant", Content = orchestration.Output });
                    workingMessages.Add(new OpenAiChatMessage { Role = "tool", Content = internalContent });
                    continue;
                }

                message.Content = internalContent;
                finishReason = "stop";
                break;
            }

            var output = string.IsNullOrWhiteSpace(orchestration.Output)
                ? BuildNoDecisionMessage(orchestration)
                : orchestration.Output;
            ValidateToolChoiceResult(toolChoice, [], [], orchestration.Output);
            var envelope = QuadBrainArbiterEnvelope.TryParse(output);

            if (toolChoice.Kind != ToolChoiceKind.None && QuadBrainArbiterEnvelope.RequiresAnotherRound(envelope))
            {
                if (string.Equals(output.Trim(), lastDeferred, StringComparison.Ordinal))
                {
                    message.Content = QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false);
                    if (string.IsNullOrWhiteSpace(message.Content))
                        message.Content = output;
                    finishReason = "stop";
                    break;
                }

                lastDeferred = output.Trim();
                workingMessages.Add(new OpenAiChatMessage { Role = "assistant", Content = output });
                workingMessages.Add(new OpenAiChatMessage { Role = "user", Content = ContinueDirective });
                continue;
            }

            message.Content = envelope is { IsFinal: true }
                ? envelope.Content ?? string.Empty
                : QuadBrainArbiterEnvelope.FormatForDisplay(output, showIntent: false);
            if (string.IsNullOrWhiteSpace(message.Content))
                message.Content = output;
            finishReason = "stop";
            break;
        }

        var response = CreateResponse(
            request,
            message,
            finishReason,
            orchestrationInput,
            orchestration?.TransactionId);
        await CompleteSessionTurnAsync(sessionId, turnId, response, cancellationToken).ConfigureAwait(false);
        return response;
    }

    private static OpenAiChatCompletionResponse CreateResponse(
        OpenAiChatCompletionRequest request,
        OpenAiChatResponseMessage message,
        string finishReason,
        string orchestrationInput,
        string? transactionId = null)
    {
        var promptTokens = EstimateTokens(orchestrationInput);
        var completionTokens = EstimateTokens(message.Content) + EstimateTokens(SerializeToolCalls(message.ToolCalls));

        return new OpenAiChatCompletionResponse
        {
            Id = $"chatcmpl-{(string.IsNullOrWhiteSpace(transactionId) ? Guid.NewGuid().ToString("N") : transactionId)}",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? "quadbrain" : request.Model!.Trim(),
            Choices =
            [
                new OpenAiChatChoice
                {
                    Index = 0,
                    Message = message,
                    FinishReason = finishReason,
                },
            ],
            Usage = new OpenAiUsage
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
            },
        };
    }

    private async Task CompleteSessionTurnAsync(
        string? sessionId,
        string? turnId,
        OpenAiChatCompletionResponse response,
        CancellationToken cancellationToken)
    {
        if (_sessionLog is null || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(turnId))
            return;

        var choice = response.Choices.FirstOrDefault();
        var assistantResponse = choice?.Message.Content;
        if (string.IsNullOrWhiteSpace(assistantResponse))
            assistantResponse = SerializeToolCalls(choice?.Message.ToolCalls);

        await _sessionLog.UpsertTurnAsync(
            DefaultSourceType,
            sessionId.Trim(),
            new UnifiedRequestEntryDto
            {
                RequestId = turnId.Trim(),
                Status = "completed",
                Response = assistantResponse ?? string.Empty,
                Model = response.Model,
                TokenCount = response.Usage.TotalTokens,
                PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                TodoId = SessionLogTurnContextValidator.NoneSentinel,
                Actions =
                [
                    new UnifiedActionDto
                    {
                        Order = 1,
                        Type = "quadbrain_response",
                        Status = "completed",
                        Description = "QuadBrain OpenAI-compatible response returned to caller.",
                    },
                ],
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Best-effort token estimate (~4 characters per token); 0 for empty text.</summary>
    private static int EstimateTokens(string? text)
        => string.IsNullOrEmpty(text) ? 0 : (text.Length + 3) / 4;

    private static string BuildNoDecisionMessage(QuadBrainOrchestrationResponse orchestration)
    {
        var reason = string.IsNullOrWhiteSpace(orchestration.Reason)
            ? (string.IsNullOrWhiteSpace(orchestration.Status) ? "empty" : orchestration.Status.Trim())
            : orchestration.Reason.Trim();
        return $"QuadBrain returned no decision ({reason}).";
    }

    private static string? SerializeToolCalls(List<OpenAiToolCall>? toolCalls)
        => toolCalls is { Count: > 0 }
            ? string.Concat(toolCalls.Select(call => call.Function.Name + call.Function.Arguments))
            : null;

    private static ToolChoiceDirective ParseToolChoice(
        JsonElement? toolChoice,
        IReadOnlyList<OpenAiToolDefinition>? tools)
    {
        if (toolChoice is null || toolChoice.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return ToolChoiceDirective.Auto;

        var element = toolChoice.Value;
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
                return ToolChoiceDirective.Auto;
            if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                return ToolChoiceDirective.None;
            if (string.Equals(value, "required", StringComparison.OrdinalIgnoreCase))
            {
                if (tools is not { Count: > 0 })
                    throw new ArgumentException("tool_choice 'required' requires at least one tool.", nameof(toolChoice));
                return ToolChoiceDirective.Required;
            }

            throw new ArgumentException("tool_choice must be 'auto', 'none', 'required', or a function object.", nameof(toolChoice));
        }

        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty("type", out var type)
            || type.ValueKind != JsonValueKind.String
            || !string.Equals(type.GetString(), "function", StringComparison.OrdinalIgnoreCase)
            || !element.TryGetProperty("function", out var function)
            || function.ValueKind != JsonValueKind.Object
            || !function.TryGetProperty("name", out var name)
            || name.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(name.GetString()))
        {
            throw new ArgumentException("tool_choice function object must include type 'function' and function.name.", nameof(toolChoice));
        }

        var toolName = name.GetString()!.Trim();
        if (tools is not { Count: > 0 }
            || !tools.Any(tool => string.Equals(tool.Function.Name, toolName, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                $"tool_choice function '{toolName}' does not match any declared tool.",
                nameof(toolChoice));
        }

        return ToolChoiceDirective.Specific(toolName);
    }

    private static void ValidateToolChoiceResult(
        ToolChoiceDirective toolChoice,
        IReadOnlyList<OpenAiToolCall> remainingToolCalls,
        IReadOnlyList<ExecutedInternalTool> failedInternalToolCalls,
        string? rawOutput)
    {
        if (toolChoice.Kind == ToolChoiceKind.Auto || toolChoice.Kind == ToolChoiceKind.None)
            return;

        if (remainingToolCalls.Count == 0)
        {
            throw new InvalidOperationException(
                $"tool_choice '{toolChoice.MetadataValue}' was requested, but QuadBrain did not return an external tool call. Output: {rawOutput ?? string.Empty}");
        }

        if (toolChoice.Kind != ToolChoiceKind.Specific)
            return;

        foreach (var call in remainingToolCalls)
        {
            if (!string.Equals(call.Function.Name, toolChoice.ToolName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"tool_choice requested tool '{toolChoice.ToolName}' but QuadBrain returned tool '{call.Function.Name}'.");
            }
        }

        foreach (var failed in failedInternalToolCalls)
        {
            throw new InvalidOperationException(
                $"tool_choice requested tool '{toolChoice.ToolName}' but QuadBrain returned internal tool '{failed.ToolCall.Function.Name}'.");
        }
    }

    internal static bool IsFailedOrchestration(QuadBrainOrchestrationResponse orchestration)
    {
        if (string.Equals(orchestration.Reason, BrainSlotReasonCodes.ProviderFailed, StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.Equals(orchestration.Status, "committed", StringComparison.OrdinalIgnoreCase)
               && string.IsNullOrWhiteSpace(orchestration.Output);
    }

    internal static string? TryRoleFinalContent(QuadBrainOrchestrationResponse orchestration)
    {
        foreach (var role in new[] { BrainSlotRoles.Logic, BrainSlotRoles.Creativity, BrainSlotRoles.CuriosityEngine })
        {
            var result = orchestration.RoleResults.FirstOrDefault(item =>
                string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase));
            var envelope = QuadBrainArbiterEnvelope.TryParse(result?.Output);
            if (envelope is { IsFinal: true } && !string.IsNullOrWhiteSpace(envelope.Content))
                return envelope.Content;
        }

        return null;
    }

    private static bool TryCollectToolCalls(QuadBrainOrchestrationResponse orchestration, out List<OpenAiToolCall> toolCalls)
    {
        var envelope = QuadBrainArbiterEnvelope.TryParse(orchestration.Output);
        if (envelope is { HasToolCalls: true })
        {
            toolCalls = [.. envelope.ToolCalls];
            return true;
        }

        if (TryParseToolCalls(orchestration.Output, out toolCalls))
            return true;

        if (envelope is { IsFinal: true })
            return false;

        foreach (var role in new[] { BrainSlotRoles.Logic, BrainSlotRoles.Creativity, BrainSlotRoles.CuriosityEngine })
        {
            var result = orchestration.RoleResults.FirstOrDefault(item =>
                string.Equals(item.Role, role, StringComparison.OrdinalIgnoreCase));
            var roleEnvelope = QuadBrainArbiterEnvelope.TryParse(result?.Output);
            if (roleEnvelope is { HasToolCalls: true })
            {
                toolCalls = [.. roleEnvelope.ToolCalls];
                return true;
            }
        }

        toolCalls = [];
        return false;
    }

    /// <summary>
    /// Detects QuadBrain's tool-call convention: an Arbiter output that is a JSON object with a
    /// <c>tool_calls</c> array of <c>{ "name": ..., "arguments": { ... } }</c> entries. When present these are
    /// converted to OpenAI assistant tool calls so the Agent Framework loop can execute them.
    /// </summary>
    private static bool TryParseToolCalls(string? output, out List<OpenAiToolCall> toolCalls)
    {
        toolCalls = [];
        if (string.IsNullOrWhiteSpace(output))
            return false;

        var json = ExtractToolCallsJson(output);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("tool_calls", out var calls) ||
                calls.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var index = 0;
            foreach (var call in calls.EnumerateArray())
            {
                if (call.ValueKind != JsonValueKind.Object ||
                    !call.TryGetProperty("name", out var name) ||
                    name.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var arguments = call.TryGetProperty("arguments", out var args)
                    ? args.GetRawText()
                    : "{}";
                toolCalls.Add(new OpenAiToolCall
                {
                    Id = $"call_{index}",
                    Function = new OpenAiFunctionCall
                    {
                        Name = name.GetString() ?? string.Empty,
                        Arguments = arguments,
                    },
                });
                index++;
            }

            return toolCalls.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Accepts a raw JSON object, a markdown-fenced JSON block, or prose that embeds a
    /// <c>tool_calls</c> object. Arbiter often narrates and then emits JSON.
    /// </summary>
    private static string? ExtractToolCallsJson(string output)
    {
        var trimmed = output.Trim();
        if (trimmed.Length == 0)
            return null;

        var fenced = ExtractFencedJson(trimmed);
        if (!string.IsNullOrWhiteSpace(fenced))
            trimmed = fenced;

        if (trimmed[0] == '{')
        {
            var slice = SliceBalancedObject(trimmed, 0);
            if (!string.IsNullOrWhiteSpace(slice))
                return slice;
        }

        var marker = trimmed.IndexOf("\"tool_calls\"", StringComparison.Ordinal);
        if (marker < 0)
            return null;
        var open = trimmed.LastIndexOf('{', marker);
        return open < 0 ? null : SliceBalancedObject(trimmed, open);
    }

    private static string? ExtractFencedJson(string text)
    {
        var fence = text.IndexOf("```", StringComparison.Ordinal);
        if (fence < 0)
            return null;
        var start = text.IndexOf('\n', fence);
        if (start < 0)
            return null;
        var end = text.IndexOf("```", start + 1, StringComparison.Ordinal);
        if (end < 0)
            return null;
        var inner = text[(start + 1)..end].Trim();
        return inner.Length > 0 ? inner : null;
    }

    private static string? SliceBalancedObject(string text, int openIndex)
    {
        if (openIndex < 0 || openIndex >= text.Length || text[openIndex] != '{')
            return null;

        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = openIndex; i < text.Length; i++)
        {
            var ch = text[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (ch == '\\')
                    escape = true;
                else if (ch == '"')
                    inString = false;
                continue;
            }

            if (ch == '"')
            {
                inString = true;
                continue;
            }

            if (ch == '{')
                depth++;
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                    return text[openIndex..(i + 1)];
            }
        }

        return null;
    }

    /// <summary>
    /// Folds the chat transcript into a single prompt for orchestration. The full role-tagged transcript is
    /// preserved so QuadBrain sees system context and prior turns, not just the last user message.
    /// </summary>
    /// <summary>
    /// FR-MCP-QBEXEC-001 (AC-5): Records each failed MCP-internal tool to the session log. Best-effort - a no-op
    /// when no logger is configured or no session/turn context is carried in the orchestration metadata.
    /// </summary>
    private async Task LogInternalToolFailuresAsync(
        IReadOnlyList<ExecutedInternalTool> failed,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken)
    {
        if (_interactionLogger is null || failed.Count == 0)
            return;

        var sourceType = MetadataValue(metadata, "sourceType") ?? "QBAgent";
        var sessionId = MetadataValue(metadata, "sessionId");
        var turnId = MetadataValue(metadata, "turnId");
        foreach (var failure in failed)
        {
            await _interactionLogger.LogInternalToolFailureAsync(
                    sourceType, sessionId, turnId, failure.ToolCall.Function.Name, failure.Outcome.Error, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string? MetadataValue(IReadOnlyDictionary<string, string> metadata, string key)
        => metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    /// <summary>Builds a human-readable note for internal tool calls that could not be completed server-side.</summary>
    private static string BuildFailureNote(IReadOnlyList<ExecutedInternalTool> failed)
    {
        if (failed.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var failure in failed)
        {
            builder.Append("Note: the MCP tool '")
                   .Append(failure.ToolCall.Function.Name)
                   .Append("' could not be completed server-side")
                   .AppendLine(string.IsNullOrWhiteSpace(failure.Outcome.Error) ? "." : $": {failure.Outcome.Error}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildInternalSuccessContent(
        IReadOnlyList<ExecutedInternalTool> executed,
        string failureNote)
    {
        var builder = new StringBuilder();
        if (failureNote.Length > 0)
            builder.AppendLine(failureNote);

        foreach (var item in executed)
        {
            if (!string.IsNullOrWhiteSpace(item.Outcome.ResultJson))
                builder.AppendLine(item.Outcome.ResultJson);
            else
                builder.Append(item.ToolCall.Function.Name).AppendLine(" completed.");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildPrompt(
        IReadOnlyList<OpenAiChatMessage> messages,
        IReadOnlyList<OpenAiToolDefinition>? tools,
        ToolChoiceDirective toolChoice)
    {
        var builder = new StringBuilder();
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content) && message.ToolCalls is not { Count: > 0 })
                continue;
            var role = string.IsNullOrWhiteSpace(message.Role) ? "user" : message.Role.Trim();
            builder.Append(role.ToLower(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(message.ToolCallId))
                builder.Append(" [").Append(message.ToolCallId.Trim()).Append(']');
            builder.Append(": ");
            if (!string.IsNullOrWhiteSpace(message.Content))
                builder.AppendLine(message.Content.Trim());
            else
                builder.AppendLine(SerializeToolCalls(message.ToolCalls));
        }

        if (tools is { Count: > 0 })
        {
            builder.AppendLine()
                   .AppendLine("Available tools. To call one, respond with ONLY a JSON object of the form")
                   .AppendLine("{\"tool_calls\":[{\"name\":\"<tool>\",\"arguments\":{...}}]}; otherwise answer normally.");
            foreach (var tool in tools)
            {
                builder.Append("- ").Append(tool.Function.Name);
                if (!string.IsNullOrWhiteSpace(tool.Function.Description))
                    builder.Append(": ").Append(tool.Function.Description);
                builder.AppendLine();
            }

            if (tools.Any(static t => string.Equals(t.Function.Name, "mcp_todo_query", StringComparison.Ordinal)))
            {
                builder.AppendLine("If the user asks to list or query TODOs, you MUST call mcp_todo_query with done=false for open items. Respond with ONLY the tool_calls JSON. Do not describe the plan in prose.");
            }

            if (toolChoice.Kind == ToolChoiceKind.Required)
            {
                builder.AppendLine("Tool choice directive: you must call at least one available tool.");
            }
            else if (toolChoice.Kind == ToolChoiceKind.Specific)
            {
                builder.Append("Tool choice directive: you must call the tool '")
                       .Append(toolChoice.ToolName)
                       .AppendLine("' and no other tool.");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private enum ToolChoiceKind
    {
        Auto,
        None,
        Required,
        Specific,
    }

    private sealed record ToolChoiceDirective(ToolChoiceKind Kind, string? ToolName)
    {
        public static ToolChoiceDirective Auto { get; } = new(ToolChoiceKind.Auto, null);

        public static ToolChoiceDirective None { get; } = new(ToolChoiceKind.None, null);

        public static ToolChoiceDirective Required { get; } = new(ToolChoiceKind.Required, null);

        public static ToolChoiceDirective Specific(string toolName) => new(ToolChoiceKind.Specific, toolName);

        public string MetadataValue => Kind == ToolChoiceKind.Specific ? ToolName! : Kind.ToString().ToLowerInvariant();
    }
}
