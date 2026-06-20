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
    private readonly IQuadBrainOrchestrationService _orchestration;
    private readonly QuadBrainToolInterceptor _interceptor;
    private readonly IBrainInteractionSessionLogger? _interactionLogger;

    /// <summary>Initializes a new instance of the <see cref="QuadBrainOpenAiChatService"/> class.</summary>
    /// <param name="orchestration">The QuadBrain orchestration service.</param>
    /// <param name="classifier">Internal/external tool classifier (defaults to the <c>mcp_</c>-prefix classifier).</param>
    /// <param name="internalToolExecutor">Server-side internal tool executor (defaults to a no-op).</param>
    /// <param name="interactionLogger">FR-MCP-QBEXEC-001 (AC-5): optional session-log writer used to record internal-tool failures.</param>
    public QuadBrainOpenAiChatService(
        IQuadBrainOrchestrationService orchestration,
        IQuadBrainToolClassifier? classifier = null,
        IQuadBrainInternalToolExecutor? internalToolExecutor = null,
        IBrainInteractionSessionLogger? interactionLogger = null)
    {
        _orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
        _interceptor = new QuadBrainToolInterceptor(
            classifier ?? new QuadBrainToolClassifier(),
            internalToolExecutor ?? NoopInternalToolExecutor.Instance);
        _interactionLogger = interactionLogger;
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

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["openai.surface"] = "chat.completions",
            ["openai.model"] = string.IsNullOrWhiteSpace(request.Model) ? "quadbrain" : request.Model!.Trim(),
        };
        if (request.Tools is { Count: > 0 })
            metadata["openai.tools"] = string.Join(",", request.Tools.Select(static t => t.Function.Name));

        // FR-MCP-QUAD-SESSION-001: attach this QuadBrain run to its session (and optional turn) so orchestration,
        // inter-brain logging, internal-tool-failure logging, and turn transactions are correlated to the session.
        if (!string.IsNullOrWhiteSpace(sessionId))
            metadata["sessionId"] = sessionId.Trim();
        if (!string.IsNullOrWhiteSpace(turnId))
            metadata["turnId"] = turnId.Trim();

        var orchestrationInput = BuildPrompt(request.Messages, request.Tools);
        var orchestration = await _orchestration.ExecuteFullOrchestrationAsync(
            new QuadBrainOrchestrationRequest
            {
                Input = orchestrationInput,
                TurnId = string.IsNullOrWhiteSpace(turnId) ? null : turnId.Trim(),
                Metadata = metadata,
            },
            cancellationToken).ConfigureAwait(false);

        var message = new OpenAiChatResponseMessage { Role = "assistant" };
        string finishReason;
        if (TryParseToolCalls(orchestration.Output, out var toolCalls))
        {
            // FR-MCP-QBEXEC-001: execute MCP-internal tools server-side and strip them; only external (and any
            // unhandled internal) calls are emitted to the agent.
            var interception = await _interceptor.InterceptAsync(toolCalls, turnId: null, cancellationToken).ConfigureAwait(false);

            // FR-MCP-QBEXEC-001 (AC-5): internal tool failures are NOT emitted to the agent as tool commands; they
            // are surfaced as a note AND recorded to the session log (best-effort) so the failure is durably captured.
            await LogInternalToolFailuresAsync(interception.Failed, metadata, cancellationToken).ConfigureAwait(false);
            var failureNote = BuildFailureNote(interception.Failed);
            if (interception.RemainingToolCalls.Count > 0)
            {
                message.ToolCalls = [.. interception.RemainingToolCalls];
                if (failureNote.Length > 0)
                    message.Content = failureNote;
                finishReason = "tool_calls";
            }
            else
            {
                // No external tool commands; surface any failure note (empty when all ran server-side).
                message.Content = failureNote;
                finishReason = "stop";
            }
        }
        else
        {
            message.Content = orchestration.Output ?? string.Empty;
            finishReason = "stop";
        }

        // FR-MCP-QBOPENAI-001 (G-019): QuadBrain orchestration does not surface real provider token counts, so
        // usage is a documented best-effort estimate (~4 characters per token) over the folded prompt and the
        // assistant content/tool-call output so OpenAI clients receive a non-zero usage block.
        var promptTokens = EstimateTokens(orchestrationInput);
        var completionTokens = EstimateTokens(message.Content) + EstimateTokens(SerializeToolCalls(message.ToolCalls));

        return new OpenAiChatCompletionResponse
        {
            Id = $"chatcmpl-{(string.IsNullOrWhiteSpace(orchestration.TransactionId) ? Guid.NewGuid().ToString("N") : orchestration.TransactionId)}",
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

    /// <summary>Best-effort token estimate (~4 characters per token); 0 for empty text.</summary>
    private static int EstimateTokens(string? text)
        => string.IsNullOrEmpty(text) ? 0 : (text.Length + 3) / 4;

    private static string? SerializeToolCalls(List<OpenAiToolCall>? toolCalls)
        => toolCalls is { Count: > 0 }
            ? string.Concat(toolCalls.Select(call => call.Function.Name + call.Function.Arguments))
            : null;

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

        var trimmed = output.Trim();
        if (trimmed.Length == 0 || trimmed[0] != '{')
            return false;

        try
        {
            using var document = JsonDocument.Parse(trimmed);
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

    private static string BuildPrompt(
        IReadOnlyList<OpenAiChatMessage> messages,
        IReadOnlyList<OpenAiToolDefinition>? tools)
    {
        var builder = new StringBuilder();
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
                continue;
            var role = string.IsNullOrWhiteSpace(message.Role) ? "user" : message.Role.Trim();
            builder.Append(role.ToLower(CultureInfo.InvariantCulture))
                   .Append(": ")
                   .AppendLine(message.Content.Trim());
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
        }

        return builder.ToString().TrimEnd();
    }
}
