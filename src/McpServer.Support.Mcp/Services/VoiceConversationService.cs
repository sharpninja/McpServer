using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// In-memory voice conversation orchestration service backed by Copilot CLI and MCP todo services.
/// </summary>
public sealed partial class VoiceConversationService : IVoiceConversationService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ConcurrentDictionary<string, VoiceSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICopilotClient _copilotClient;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IOptionsMonitor<VoiceConversationOptions> _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<VoiceConversationService> _logger;

    /// <summary>
    /// Creates a new <see cref="VoiceConversationService"/>.
    /// </summary>
    public VoiceConversationService(
        ICopilotClient copilotClient,
        WorkspaceServiceAccessor workspaceAccessor,
        IOptionsMonitor<VoiceConversationOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<VoiceConversationService> logger)
    {
        _copilotClient = copilotClient ?? throw new ArgumentNullException(nameof(copilotClient));
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<VoiceSessionCreateResponse> CreateSessionAsync(VoiceSessionCreateRequest? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();

        var opts = _options.CurrentValue;
        var now = DateTimeOffset.UtcNow;
        var sessionId = $"voice-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToLowerInvariant();
        var language = NormalizeLanguage(request?.Language);
        var state = new VoiceSessionState(sessionId, language, request?.DeviceId, request?.ClientName, request?.WorkspacePath, now);
        _sessions[sessionId] = state;

        return Task.FromResult(new VoiceSessionCreateResponse
        {
            SessionId = sessionId,
            Status = "idle",
            Language = language,
            ModelRequested = opts.CopilotModel,
            ModelResolved = opts.CopilotModel
        });
    }

    /// <inheritdoc />
    public async Task<VoiceTurnResponse?> SubmitTurnAsync(string sessionId, VoiceTurnRequest request, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (request is null) throw new ArgumentNullException(nameof(request));

        var userText = (request.UserTranscriptText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(userText))
            throw new ArgumentException("UserTranscriptText is required.", nameof(request));

        if (!_sessions.TryGetValue(sessionId, out var state))
            return null;

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        CancellationTokenSource? linkedCts = null;
        string turnId;
        try
        {
            if (state.IsTurnActive)
                return BuildBusyTurnResponse(state, sessionId);

            state.IsTurnActive = true;
            state.Status = "thinking";
            state.LastError = null;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            state.TurnCounter++;
            turnId = $"turn-{state.TurnCounter.ToString("0000", CultureInfo.InvariantCulture)}";
            state.LastTurnId = turnId;

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            state.ActiveTurnCts = linkedCts;

            AddTranscriptEntryIfEnabled(state, new VoiceTranscriptEntryDto
            {
                TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                TurnId = turnId,
                Role = "user",
                Category = "transcript",
                Text = userText
            });
        }
        finally
        {
            state.Gate.Release();
        }

        var sw = Stopwatch.StartNew();
        VoiceTurnExecutionResult execution;
        try
        {
            execution = await ExecuteTurnAsync(state, turnId, userText, linkedCts!.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            execution = new VoiceTurnExecutionResult("interrupted", "Voice turn interrupted.", "Interrupted.", [], null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice turn failed: Session={SessionId}; Turn={TurnId}", sessionId, turnId);
            execution = new VoiceTurnExecutionResult("error", "The voice request failed.", "Sorry, the request failed.", [], ex.Message);
        }

        sw.Stop();

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (state.ActiveTurnCts == linkedCts)
                state.ActiveTurnCts = null;

            linkedCts?.Dispose();
            state.IsTurnActive = false;
            state.Status = execution.Status is "completed" or "interrupted" ? "idle" : "error";
            state.LastError = execution.Error;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;

            state.LastTurnToolCalls.Clear();
            foreach (var item in execution.ToolCalls)
                state.LastTurnToolCalls.Add(item);

            if (!string.IsNullOrWhiteSpace(execution.AssistantDisplayText))
            {
                AddTranscriptEntryIfEnabled(state, new VoiceTranscriptEntryDto
                {
                    TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                    TurnId = turnId,
                    Role = "assistant",
                    Category = execution.Status == "error" ? "error" : "transcript",
                    Text = execution.AssistantDisplayText!
                });
            }

            return new VoiceTurnResponse
            {
                SessionId = sessionId,
                TurnId = turnId,
                Status = execution.Status,
                AssistantDisplayText = execution.AssistantDisplayText,
                AssistantSpeakText = execution.AssistantSpeakText,
                ToolCalls = execution.ToolCalls,
                Error = execution.Error,
                LatencyMs = (int)Math.Clamp(sw.ElapsedMilliseconds, 0, int.MaxValue),
                ModelRequested = _options.CurrentValue.CopilotModel,
                ModelResolved = _options.CurrentValue.CopilotModel
            };
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<VoiceInterruptResponse?> InterruptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return Task.FromResult<VoiceInterruptResponse?>(null);

        var interrupted = false;
        lock (state.SyncRoot)
        {
            if (state.ActiveTurnCts is { IsCancellationRequested: false })
            {
                interrupted = true;
                state.Status = "interrupting";
                state.LastUpdatedUtc = DateTimeOffset.UtcNow;
                state.ActiveTurnCts.Cancel();
            }
        }

        return Task.FromResult<VoiceInterruptResponse?>(new VoiceInterruptResponse
        {
            SessionId = sessionId,
            Interrupted = interrupted,
            Status = interrupted ? "interrupting" : state.Status
        });
    }

    /// <inheritdoc />
    public Task<VoiceSessionStatusDto?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return Task.FromResult<VoiceSessionStatusDto?>(null);

        lock (state.SyncRoot)
        {
            return Task.FromResult<VoiceSessionStatusDto?>(new VoiceSessionStatusDto
            {
                SessionId = state.SessionId,
                Status = state.Status,
                Language = state.Language,
                CreatedUtc = state.CreatedUtc.ToString("O"),
                LastUpdatedUtc = state.LastUpdatedUtc.ToString("O"),
                IsTurnActive = state.IsTurnActive,
                LastError = state.LastError,
                LastTurnId = state.LastTurnId
            });
        }
    }

    /// <inheritdoc />
    public Task<VoiceTranscriptResponse?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return Task.FromResult<VoiceTranscriptResponse?>(null);

        lock (state.SyncRoot)
        {
            return Task.FromResult<VoiceTranscriptResponse?>(new VoiceTranscriptResponse
            {
                SessionId = sessionId,
                Items = state.Transcript.ToList()
            });
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryRemove(sessionId, out var state))
            return false;

        lock (state.SyncRoot)
        {
            try
            {
                state.ActiveTurnCts?.Cancel();
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning("{ExceptionDetail}", ex.ToString());
                // ignored
            }

            state.ActiveTurnCts?.Dispose();
        }

        // Gracefully end the interactive Copilot session
        if (state.InteractiveSession is not null)
        {
            if (state.InteractiveSession.IsAlive)
            {
                try
                {
                    await state.InteractiveSession.EndAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error ending interactive session: {SessionId}", sessionId);
                }
            }

            await state.InteractiveSession.DisposeAsync().ConfigureAwait(false);
        }

        state.Gate.Dispose();
        return true;
    }
}

public sealed partial class VoiceConversationService
{
    private async Task<VoiceTurnExecutionResult> ExecuteTurnAsync(
        VoiceSessionState state,
        string turnId,
        string userText,
        CancellationToken cancellationToken)
    {
        var opts = _options.CurrentValue;
        var toolRecords = new List<VoiceToolCallRecordDto>();
        var toolResultsForPrompt = new List<string>();
        var guardState = new VoiceTurnGuardState(opts.MaxWritesPerTurn, opts.MaxDeletesPerTurn);

        for (var step = 1; step <= Math.Max(1, opts.MaxToolSteps); step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CopilotResult copilotResult;

            if (state.InteractiveSession is { IsAlive: true })
            {
                // Interactive session is alive — send via stdin
                var stdinPrompt = step == 1
                    ? userText
                    : toolResultsForPrompt[^1];
                copilotResult = await state.InteractiveSession.SendAsync(stdinPrompt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // First turn or session died — launch interactive process with full system prompt
                var prompt = BuildCopilotPrompt(state, turnId, userText, toolResultsForPrompt, step);
                var copilotOpts = BuildCopilotOptions(opts, state.WorkspacePath);

                try
                {
                    state.InteractiveSession = _copilotClient.CreateInteractiveSession(prompt, copilotOpts);
                    copilotResult = await state.InteractiveSession.ReadInitialResponseAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    _logger.LogError(ex, "Failed to create interactive Copilot session: {SessionId}", state.SessionId);
                    return ErrorResult($"Failed to start Copilot: {ex.Message}", toolRecords);
                }
            }

            if (copilotResult.State != CopilotResultState.Success)
            {
                var err = BuildCopilotFailureMessage(copilotResult);
                _logger.LogWarning(
                    "Voice turn Copilot CLI failure: Session={SessionId}; Turn={TurnId}; Step={Step}; State={State}; ExitCode={ExitCode}",
                    state.SessionId,
                    turnId,
                    step,
                    copilotResult.State,
                    copilotResult.ExitCode);
                return ErrorResult(err, toolRecords);
            }

            if (!TryParseModelEnvelope(copilotResult.Body, out var envelope, out var parseError))
            {
                // Try JSON repair first
                CopilotResult repaired;
                if (state.InteractiveSession is { IsAlive: true })
                {
                    repaired = await state.InteractiveSession.SendAsync(
                        BuildJsonRepairPrompt(copilotResult.Body),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    repaired = await _copilotClient.InvokeAsync(
                        BuildJsonRepairPrompt(copilotResult.Body),
                        BuildCopilotOptions(opts),
                        cancellationToken).ConfigureAwait(false);
                }

                if (repaired.State != CopilotResultState.Success ||
                    !TryParseModelEnvelope(repaired.Body, out envelope, out parseError))
                {
                    // Fallback: treat raw text as a conversational final_response
                    var rawText = (copilotResult.Body ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(rawText))
                    {
                        _logger.LogDebug("Voice turn: treating non-JSON response as plain text final_response ({Length} chars)", rawText.Length);
                        envelope = new ModelEnvelope
                        {
                            Type = "final_response",
                            DisplayText = rawText,
                            SpeakText = rawText
                        };
                    }
                    else
                    {
                        return ErrorResult($"Model returned invalid JSON output: {parseError}", toolRecords);
                    }
                }
            }

            switch (envelope!.Type)
            {
                case "final_response":
                {
                    var display = (envelope.DisplayText ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(display))
                        display = "Done.";
                    var speak = string.IsNullOrWhiteSpace(envelope.SpeakText) ? display : envelope.SpeakText!.Trim();

                    return new VoiceTurnExecutionResult(
                        "completed",
                        display,
                        string.IsNullOrWhiteSpace(speak) ? "Done." : speak,
                        toolRecords.ToList(),
                        null);
                }
                case "error_response":
                {
                    var display = string.IsNullOrWhiteSpace(envelope.UserMessage)
                        ? "The request could not be completed."
                        : envelope.UserMessage!;
                    var speak = string.IsNullOrWhiteSpace(envelope.SpeakText) ? display : envelope.SpeakText!;

                    return new VoiceTurnExecutionResult(
                        "error",
                        display,
                        speak,
                        toolRecords.ToList(),
                        display);
                }
                case "tool_call":
                {
                    if (string.IsNullOrWhiteSpace(envelope.ToolName))
                        return ErrorResult("Model tool_call missing toolName.", toolRecords);

                    var toolOutcome = await ExecuteToolCallAsync(
                        state,
                        turnId,
                        step,
                        envelope.ToolName!,
                        envelope.Arguments,
                        guardState,
                        cancellationToken).ConfigureAwait(false);

                    toolRecords.Add(toolOutcome.Record);

                    if (_options.CurrentValue.LogToolCalls)
                    {
                        AddTranscriptEntryIfEnabled(state, new VoiceTranscriptEntryDto
                        {
                            TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                            TurnId = turnId,
                            Role = "tool",
                            Category = toolOutcome.Record.Status == "executed" ? "tool_result" : "tool_call",
                            Text = $"{toolOutcome.Record.ToolName} ({toolOutcome.Record.Status}): {toolOutcome.Record.ResultSummary ?? toolOutcome.Record.Error ?? string.Empty}"
                        });
                    }

                    var modelToolResultPayload = new
                    {
                        step,
                        toolName = toolOutcome.Record.ToolName,
                        status = toolOutcome.Record.Status,
                        isMutation = toolOutcome.Record.IsMutation,
                        error = toolOutcome.Record.Error,
                        summary = toolOutcome.Record.ResultSummary,
                        result = toolOutcome.ResultForModel
                    };
                    toolResultsForPrompt.Add(JsonSerializer.Serialize(modelToolResultPayload, s_jsonOptions));
                    continue;
                }
                default:
                    return ErrorResult($"Unsupported model response type '{envelope.Type}'.", toolRecords);
            }
        }

        return ErrorResult("Model exceeded maximum tool steps for a single turn.", toolRecords);
    }

    private async Task<ToolExecutionOutcome> ExecuteToolCallAsync(
        VoiceSessionState state,
        string turnId,
        int step,
        string toolName,
        JsonElement arguments,
        VoiceTurnGuardState guardState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (arguments.ValueKind != JsonValueKind.Object)
            return BlockedToolOutcome(turnId, step, toolName, arguments, false, "Tool arguments must be a JSON object.");

        var normalizedToolName = toolName.Trim().ToLowerInvariant();
        var isMutation = IsMutationTool(normalizedToolName);
        var argsJson = JsonSerializer.Serialize(arguments, s_jsonOptions);

        if (!guardState.TryRegister(normalizedToolName, argsJson, isMutation, out var guardError))
            return BlockedToolOutcome(turnId, step, normalizedToolName, arguments, isMutation, guardError ?? "Blocked by guardrail.");

        try
        {
            object resultPayload;
            string summary;

            switch (normalizedToolName)
            {
                case "todo_list":
                case "todo_search":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, ["keyword", "priority", "section", "id", "done", "limit"]);
                    var query = new TodoQueryRequest
                    {
                        Keyword = GetOptionalString(arguments, "keyword"),
                        Priority = GetOptionalString(arguments, "priority"),
                        Section = GetOptionalString(arguments, "section"),
                        Id = GetOptionalString(arguments, "id"),
                        Done = GetOptionalNullableBool(arguments, "done")
                    };

                    if (normalizedToolName == "todo_search" && string.IsNullOrWhiteSpace(query.Keyword))
                        throw new VoiceToolValidationException("todo_search requires a non-empty keyword.");

                    var result = await _workspaceAccessor.GetTodoService().QueryAsync(query, cancellationToken).ConfigureAwait(false);
                    var limit = Math.Clamp(GetOptionalInt(arguments, "limit") ?? 10, 1, 50);
                    var items = result.Items.Take(limit).Select(MapTodoSummary).ToList();
                    resultPayload = new
                    {
                        success = true,
                        totalCount = result.TotalCount,
                        returnedCount = items.Count,
                        items
                    };
                    summary = $"Returned {items.Count} todo item(s) (total {result.TotalCount}).";
                    break;
                }
                case "todo_get":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, ["id"]);
                    var id = RequireString(arguments, "id");
                    var item = await _workspaceAccessor.GetTodoService().GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                    if (item is null)
                    {
                        resultPayload = new { success = false, error = $"Todo '{id}' not found." };
                        summary = $"Todo '{id}' not found.";
                    }
                    else
                    {
                        resultPayload = new { success = true, item };
                        summary = $"Loaded todo {item.Id}.";
                    }

                    break;
                }
                case "todo_create":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, [
                        "id", "title", "section", "priority", "estimate", "description", "technicalDetails",
                        "implementationTasks", "dependsOn", "functionalRequirements", "technicalRequirements"
                    ]);

                    var request = new TodoCreateRequest
                    {
                        Id = RequireString(arguments, "id"),
                        Title = RequireString(arguments, "title"),
                        Section = RequireString(arguments, "section"),
                        Priority = RequireString(arguments, "priority"),
                        Estimate = GetOptionalString(arguments, "estimate"),
                        Description = GetOptionalStringList(arguments, "description"),
                        TechnicalDetails = GetOptionalStringList(arguments, "technicalDetails"),
                        ImplementationTasks = GetOptionalTaskList(arguments, "implementationTasks"),
                        DependsOn = GetOptionalStringList(arguments, "dependsOn"),
                        FunctionalRequirements = GetOptionalStringList(arguments, "functionalRequirements"),
                        TechnicalRequirements = GetOptionalStringList(arguments, "technicalRequirements"),
                    };

                    var result = await _workspaceAccessor.GetTodoService().CreateAsync(request, cancellationToken).ConfigureAwait(false);
                    resultPayload = new { success = result.Success, error = result.Error, item = result.Item };
                    summary = result.Success && result.Item is not null
                        ? $"Created todo {result.Item.Id}."
                        : $"Create failed: {result.Error}";
                    break;
                }
                case "todo_update":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, [
                        "id", "title", "priority", "section", "done", "estimate", "description", "technicalDetails",
                        "implementationTasks", "note", "completedDate", "doneSummary", "remaining",
                        "dependsOn", "functionalRequirements", "technicalRequirements"
                    ]);

                    var id = RequireString(arguments, "id");
                    var update = new TodoUpdateRequest
                    {
                        Title = GetOptionalString(arguments, "title"),
                        Priority = GetOptionalString(arguments, "priority"),
                        Section = GetOptionalString(arguments, "section"),
                        Done = GetOptionalNullableBool(arguments, "done"),
                        Estimate = GetOptionalString(arguments, "estimate"),
                        Description = GetOptionalStringList(arguments, "description"),
                        TechnicalDetails = GetOptionalStringList(arguments, "technicalDetails"),
                        ImplementationTasks = GetOptionalTaskList(arguments, "implementationTasks"),
                        Note = GetOptionalString(arguments, "note"),
                        CompletedDate = GetOptionalString(arguments, "completedDate"),
                        DoneSummary = GetOptionalString(arguments, "doneSummary"),
                        Remaining = GetOptionalString(arguments, "remaining"),
                        DependsOn = GetOptionalStringList(arguments, "dependsOn"),
                        FunctionalRequirements = GetOptionalStringList(arguments, "functionalRequirements"),
                        TechnicalRequirements = GetOptionalStringList(arguments, "technicalRequirements")
                    };

                    var result = await _workspaceAccessor.GetTodoService().UpdateAsync(id, update, cancellationToken).ConfigureAwait(false);
                    resultPayload = new { success = result.Success, error = result.Error, item = result.Item };
                    summary = result.Success
                        ? $"Updated todo {id}."
                        : $"Update failed for {id}: {result.Error}";
                    break;
                }
                case "todo_delete":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, ["id"]);
                    var id = RequireString(arguments, "id");
                    var result = await _workspaceAccessor.GetTodoService().DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                    resultPayload = new { success = result.Success, error = result.Error, item = result.Item };
                    summary = result.Success
                        ? $"Deleted todo {id}."
                        : $"Delete failed for {id}: {result.Error}";
                    break;
                }
                case "todo_toggle_done":
                {
                    EnsureOnlyProperties(arguments, normalizedToolName, ["id", "done"]);
                    var id = RequireString(arguments, "id");
                    var current = await _workspaceAccessor.GetTodoService().GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                    if (current is null)
                    {
                        resultPayload = new { success = false, error = $"Todo '{id}' not found." };
                        summary = $"Todo '{id}' not found.";
                    }
                    else
                    {
                        var targetDone = GetOptionalNullableBool(arguments, "done") ?? !current.Done;
                        var result = await _workspaceAccessor.GetTodoService().UpdateAsync(id, new TodoUpdateRequest { Done = targetDone }, cancellationToken).ConfigureAwait(false);
                        resultPayload = new { success = result.Success, error = result.Error, item = result.Item };
                        summary = result.Success
                            ? (targetDone ? $"Marked {id} done." : $"Reopened {id}.")
                            : $"Toggle failed for {id}: {result.Error}";
                    }

                    break;
                }
                default:
                    return BlockedToolOutcome(turnId, step, normalizedToolName, arguments, isMutation, $"Unsupported tool '{normalizedToolName}'.");
            }

            return new ToolExecutionOutcome(
                new VoiceToolCallRecordDto
                {
                    TurnId = turnId,
                    ToolName = normalizedToolName,
                    Step = step,
                    ArgumentsJson = argsJson,
                    Status = "executed",
                    IsMutation = isMutation,
                    ResultSummary = summary,
                    Error = null
                },
                resultPayload);
        }
        catch (VoiceToolValidationException vex)
        {
            _logger.LogWarning("{ExceptionDetail}", vex.ToString());
            return BlockedToolOutcome(turnId, step, normalizedToolName, arguments, isMutation, vex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice tool execution failed: Session={SessionId}; Turn={TurnId}; Tool={ToolName}; Step={Step}", state.SessionId, turnId, normalizedToolName, step);
            return new ToolExecutionOutcome(
                new VoiceToolCallRecordDto
                {
                    TurnId = turnId,
                    ToolName = normalizedToolName,
                    Step = step,
                    ArgumentsJson = argsJson,
                    Status = "failed",
                    IsMutation = isMutation,
                    ResultSummary = null,
                    Error = ex.Message
                },
                new { success = false, error = ex.Message });
        }

        static object MapTodoSummary(TodoFlatItem item)
            => new
            {
                id = item.Id,
                title = item.Title,
                section = item.Section,
                priority = item.Priority,
                done = item.Done,
                estimate = item.Estimate,
                remaining = item.Remaining
            };
    }

    private static ToolExecutionOutcome BlockedToolOutcome(
        string turnId,
        int step,
        string toolName,
        JsonElement arguments,
        bool isMutation,
        string error)
    {
        return new ToolExecutionOutcome(
            new VoiceToolCallRecordDto
            {
                TurnId = turnId,
                ToolName = toolName,
                Step = step,
                ArgumentsJson = JsonSerializer.Serialize(arguments, s_jsonOptions),
                Status = "blocked",
                IsMutation = isMutation,
                ResultSummary = null,
                Error = error
            },
            new { success = false, blocked = true, error });
    }

    private static VoiceTurnExecutionResult ErrorResult(string message, IReadOnlyList<VoiceToolCallRecordDto> toolCalls)
        => new("error", message, message, toolCalls.ToList(), message);
}

public sealed partial class VoiceConversationService
{
    private void AddTranscriptEntryIfEnabled(VoiceSessionState state, VoiceTranscriptEntryDto entry)
    {
        if (!_options.CurrentValue.LogTranscripts)
            return;

        lock (state.SyncRoot)
        {
            state.Transcript.Add(entry);
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
        }
    }

    private CopilotClientOptions BuildCopilotOptions(VoiceConversationOptions opts, string? sessionWorkspacePath = null)
    {
        var model = string.IsNullOrWhiteSpace(opts.CopilotModel) ? "auto" : opts.CopilotModel.Trim();

        // Session workspace path (from X-Workspace-Path at session creation) takes priority
        var workingDirectory = !string.IsNullOrWhiteSpace(sessionWorkspacePath)
            ? sessionWorkspacePath
            : !string.IsNullOrWhiteSpace(opts.WorkingDirectory)
                ? opts.WorkingDirectory
                : _workspaceAccessor.GetWorkspacePath();

        return new CopilotClientOptions
        {
            Model = model,
            Silent = true,
            Timeout = TimeSpan.FromSeconds(Math.Max(5, opts.CopilotTimeoutSeconds)),
            WorkingDirectory = workingDirectory
        };
    }

    private string BuildCopilotPrompt(
        VoiceSessionState state,
        string turnId,
        string userText,
        IReadOnlyList<string> toolResultsForPrompt,
        int step)
    {
        var opts = _options.CurrentValue;
        List<VoiceTranscriptEntryDto> transcriptSnapshot;
        lock (state.SyncRoot)
        {
            transcriptSnapshot = state.Transcript
                .TakeLast(Math.Max(1, opts.TranscriptContextEntryLimit))
                .ToList();
        }

        var sb = new StringBuilder();
        sb.AppendLine("You are a helpful, general-purpose voice assistant.");
        sb.AppendLine("You can answer ANY question — general knowledge, coding, math, science, creative writing, conversation, etc.");
        sb.AppendLine("You also have optional TODO management tools for task tracking, but MOST interactions should NOT use tools.");
        sb.AppendLine("Return ONLY one JSON object. No markdown. No code fences. No extra text.");
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: For general conversation, questions, explanations, advice, or ANYTHING that is NOT about managing TODOs, return a final_response immediately. Do NOT mention tools or limitations.");
        sb.AppendLine("Use a tool_call ONLY when the user explicitly asks to list, create, update, or delete TODO items.");
        sb.AppendLine("Delete and update operations must use exact todo IDs.");
        sb.AppendLine("If create requires note/remaining, call todo_create then todo_update.");
        sb.AppendLine();
        sb.AppendLine("Optional TODO tools (use ONLY when user asks about TODOs):");
        sb.AppendLine("- todo_list { keyword?, priority?, section?, id?, done?, limit? }");
        sb.AppendLine("- todo_search { keyword, priority?, section?, id?, done?, limit? }");
        sb.AppendLine("- todo_get { id }");
        sb.AppendLine("- todo_create { id, title, section, priority, estimate?, description?, technicalDetails?, implementationTasks?, dependsOn?, functionalRequirements?, technicalRequirements? }");
        sb.AppendLine("- todo_update { id, title?, priority?, section?, done?, estimate?, description?, technicalDetails?, implementationTasks?, note?, completedDate?, doneSummary?, remaining?, dependsOn?, functionalRequirements?, technicalRequirements? }");
        sb.AppendLine("- todo_delete { id }");
        sb.AppendLine("- todo_toggle_done { id, done? }");
        sb.AppendLine("implementationTasks items are objects: {\"task\":\"...\",\"done\":false}");
        sb.AppendLine();
        sb.AppendLine("Response schemas (choose one):");
        sb.AppendLine("{\"type\":\"tool_call\",\"toolName\":\"todo_list\",\"arguments\":{},\"reasoningSummary\":\"short\"}");
        sb.AppendLine("{\"type\":\"final_response\",\"displayText\":\"...\",\"speakText\":\"...\",\"reasoningSummary\":\"short\"}");
        sb.AppendLine("{\"type\":\"error_response\",\"userMessage\":\"...\",\"speakText\":\"...\"}");
        sb.AppendLine();
        sb.AppendLine($"SessionId: {state.SessionId}");
        sb.AppendLine($"TurnId: {turnId}");
        sb.AppendLine($"Step: {step}");
        sb.AppendLine($"Language: {state.Language}");
        sb.AppendLine();
        sb.AppendLine("Recent transcript context:");
        if (transcriptSnapshot.Count == 0)
        {
            sb.AppendLine("(none)");
        }
        else
        {
            foreach (var entry in transcriptSnapshot)
                sb.AppendLine($"- [{entry.TimestampUtc}] {entry.Role}/{entry.Category}: {entry.Text}");
        }

        sb.AppendLine();
        sb.AppendLine($"Current user transcript: {userText}");
        sb.AppendLine();
        sb.AppendLine("Tool results from this turn so far:");
        if (toolResultsForPrompt.Count == 0)
        {
            sb.AppendLine("(none)");
        }
        else
        {
            foreach (var result in toolResultsForPrompt)
                sb.AppendLine(result);
        }
        sb.AppendLine();
        sb.AppendLine("Return ONLY JSON now.");
        return sb.ToString();
    }

    private static string BuildJsonRepairPrompt(string invalidOutput)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Convert the following output into ONE valid JSON object matching one of these schemas:");
        sb.AppendLine("{\"type\":\"tool_call\",\"toolName\":\"...\",\"arguments\":{...},\"reasoningSummary\":\"...\"}");
        sb.AppendLine("{\"type\":\"final_response\",\"displayText\":\"...\",\"speakText\":\"...\",\"reasoningSummary\":\"...\"}");
        sb.AppendLine("{\"type\":\"error_response\",\"userMessage\":\"...\",\"speakText\":\"...\"}");
        sb.AppendLine("Return ONLY JSON. No code fences.");
        sb.AppendLine();
        sb.AppendLine(invalidOutput ?? string.Empty);
        return sb.ToString();
    }

    private bool TryParseModelEnvelope(string body, out ModelEnvelope? envelope, out string error)
    {
        envelope = null;
        error = string.Empty;

        var json = TryExtractJsonObject(body, out var extracted) ? extracted : body?.Trim();
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty model output.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Root JSON value must be an object.";
                return false;
            }

            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            {
                error = "Missing string property 'type'.";
                return false;
            }

            var type = (typeEl.GetString() ?? string.Empty).Trim().ToLowerInvariant();
            switch (type)
            {
                case "tool_call":
                    if (!root.TryGetProperty("toolName", out var toolNameEl) || toolNameEl.ValueKind != JsonValueKind.String)
                    {
                        error = "tool_call requires string property 'toolName'.";
                        return false;
                    }

                    if (!root.TryGetProperty("arguments", out var argsEl) || argsEl.ValueKind != JsonValueKind.Object)
                    {
                        error = "tool_call requires object property 'arguments'.";
                        return false;
                    }

                    envelope = new ModelEnvelope
                    {
                        Type = type,
                        ToolName = toolNameEl.GetString(),
                        Arguments = argsEl.Clone(),
                        ReasoningSummary = root.TryGetProperty("reasoningSummary", out var rs) && rs.ValueKind == JsonValueKind.String ? rs.GetString() : null
                    };
                    return true;

                case "final_response":
                    envelope = new ModelEnvelope
                    {
                        Type = type,
                        DisplayText = root.TryGetProperty("displayText", out var dt) && dt.ValueKind == JsonValueKind.String ? dt.GetString() : null,
                        SpeakText = root.TryGetProperty("speakText", out var st) && st.ValueKind == JsonValueKind.String ? st.GetString() : null,
                        ReasoningSummary = root.TryGetProperty("reasoningSummary", out var frs) && frs.ValueKind == JsonValueKind.String ? frs.GetString() : null
                    };
                    return true;

                case "error_response":
                    envelope = new ModelEnvelope
                    {
                        Type = type,
                        UserMessage = root.TryGetProperty("userMessage", out var um) && um.ValueKind == JsonValueKind.String ? um.GetString() : null,
                        SpeakText = root.TryGetProperty("speakText", out var est) && est.ValueKind == JsonValueKind.String ? est.GetString() : null
                    };
                    return true;

                default:
                    error = $"Unsupported response type '{type}'.";
                    return false;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            error = ex.Message;
            return false;
        }
    }

    private static bool TryExtractJsonObject(string? text, out string json)
    {
        json = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                var inside = trimmed.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                if (inside.StartsWith("{", StringComparison.Ordinal) && inside.EndsWith("}", StringComparison.Ordinal))
                {
                    json = inside;
                    return true;
                }
            }
        }

        var firstBrace = trimmed.IndexOf('{');
        var lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            json = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
            return true;
        }

        return false;
    }

    private static string BuildCopilotFailureMessage(CopilotResult result)
    {
        return result.State switch
        {
            CopilotResultState.Timeout => "Copilot CLI timed out while processing the voice request.",
            CopilotResultState.SpawnError => "Copilot CLI could not be started on the MCP server host.",
            CopilotResultState.Error => string.IsNullOrWhiteSpace(result.Stderr)
                ? "Copilot CLI returned an error."
                : $"Copilot CLI error: {TrimForUser(result.Stderr, 240)}",
            _ => "Copilot CLI failed."
        };
    }

    private static string TrimForUser(string value, int maxLen)
    {
        var normalized = (value ?? string.Empty).Trim().Replace("\r", " ").Replace("\n", " ");
        return normalized.Length <= maxLen ? normalized : normalized[..maxLen] + "…";
    }

    private static string NormalizeLanguage(string? language)
        => string.IsNullOrWhiteSpace(language) ? "en-US" : language.Trim();

    private void EnsureEnabled()
    {
        if (!_options.CurrentValue.Enabled)
            throw new InvalidOperationException("Voice conversation endpoints are disabled.");
    }

    private static VoiceTurnResponse BuildBusyTurnResponse(VoiceSessionState state, string sessionId)
        => new()
        {
            SessionId = sessionId,
            TurnId = state.LastTurnId ?? "turn-0000",
            Status = "busy",
            AssistantDisplayText = "Another voice turn is already running for this session.",
            AssistantSpeakText = "I am still processing the previous request.",
            ToolCalls = [],
            Error = "Session already has an active turn.",
            LatencyMs = 0,
            ModelRequested = null,
            ModelResolved = null
        };

    private static bool IsMutationTool(string toolName)
        => toolName is "todo_create" or "todo_update" or "todo_delete" or "todo_toggle_done";
}

public sealed partial class VoiceConversationService
{
    private static void EnsureOnlyProperties(JsonElement obj, string toolName, IEnumerable<string> allowedProperties)
    {
        var allowed = new HashSet<string>(allowedProperties, StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            if (!allowed.Contains(prop.Name))
                throw new VoiceToolValidationException($"{toolName} does not allow property '{prop.Name}'.");
        }
    }

    private static string RequireString(JsonElement obj, string propertyName)
    {
        var value = GetOptionalString(obj, propertyName);
        if (string.IsNullOrWhiteSpace(value))
            throw new VoiceToolValidationException($"Missing required string property '{propertyName}'.");
        return value.Trim();
    }

    private static string? GetOptionalString(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new VoiceToolValidationException($"Property '{propertyName}' must be a string.");
        return value.GetString()?.Trim();
    }

    private static bool? GetOptionalNullableBool(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new VoiceToolValidationException($"Property '{propertyName}' must be a boolean.");
        return value.GetBoolean();
    }

    private static int? GetOptionalInt(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
            throw new VoiceToolValidationException($"Property '{propertyName}' must be an integer.");
        return number;
    }

    private static IReadOnlyList<string>? GetOptionalStringList(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.Array)
            throw new VoiceToolValidationException($"Property '{propertyName}' must be an array of strings.");

        var list = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw new VoiceToolValidationException($"Property '{propertyName}' must contain only strings.");
            var text = item.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                list.Add(text);
        }

        return list;
    }

    private static IReadOnlyList<TodoFlatTask>? GetOptionalTaskList(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.Array)
            throw new VoiceToolValidationException($"Property '{propertyName}' must be an array.");

        var tasks = new List<TodoFlatTask>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new VoiceToolValidationException($"Property '{propertyName}' must contain task objects.");

            EnsureOnlyProperties(item, propertyName, ["task", "done"]);
            var taskText = RequireString(item, "task");
            var done = GetOptionalNullableBool(item, "done") ?? false;
            tasks.Add(new TodoFlatTask(taskText, done));
        }

        return tasks;
    }

    private sealed class VoiceSessionState
    {
        public VoiceSessionState(string sessionId, string language, string? deviceId, string? clientName, string? workspacePath, DateTimeOffset now)
        {
            SessionId = sessionId;
            Language = language;
            DeviceId = deviceId;
            ClientName = clientName;
            WorkspacePath = workspacePath;
            CreatedUtc = now;
            LastUpdatedUtc = now;
        }

        public object SyncRoot { get; } = new();
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string SessionId { get; }
        public string Language { get; }
        public string? DeviceId { get; }
        public string? ClientName { get; }
        public string? WorkspacePath { get; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset LastUpdatedUtc { get; set; }
        public bool IsTurnActive { get; set; }
        public string Status { get; set; } = "idle";
        public string? LastError { get; set; }
        public string? LastTurnId { get; set; }
        public int TurnCounter { get; set; }
        public CancellationTokenSource? ActiveTurnCts { get; set; }
        public CopilotInteractiveSession? InteractiveSession { get; set; }
        public List<VoiceTranscriptEntryDto> Transcript { get; } = [];
        public List<VoiceToolCallRecordDto> LastTurnToolCalls { get; } = [];
    }

    private sealed record VoiceTurnExecutionResult(
        string Status,
        string? AssistantDisplayText,
        string? AssistantSpeakText,
        IReadOnlyList<VoiceToolCallRecordDto> ToolCalls,
        string? Error);

    private sealed record ToolExecutionOutcome(
        VoiceToolCallRecordDto Record,
        object ResultForModel);

    private sealed class VoiceTurnGuardState
    {
        private readonly int _maxWrites;
        private readonly int _maxDeletes;
        private readonly HashSet<string> _mutationHashes = new(StringComparer.Ordinal);
        private int _writes;
        private int _deletes;

        public VoiceTurnGuardState(int maxWrites, int maxDeletes)
        {
            _maxWrites = Math.Max(0, maxWrites);
            _maxDeletes = Math.Max(0, maxDeletes);
        }

        public bool TryRegister(string toolName, string argsJson, bool isMutation, out string? error)
        {
            error = null;
            if (!isMutation)
                return true;

            var hashKey = $"{toolName}:{ComputeSha256(argsJson)}";
            if (!_mutationHashes.Add(hashKey))
            {
                error = "Duplicate mutating tool call blocked in the same turn.";
                return false;
            }

            if (_writes >= _maxWrites)
            {
                error = $"Per-turn mutation limit exceeded (max {_maxWrites}).";
                return false;
            }

            if (toolName == "todo_delete" && _deletes >= _maxDeletes)
            {
                error = $"Per-turn delete limit exceeded (max {_maxDeletes}).";
                return false;
            }

            _writes++;
            if (toolName == "todo_delete")
                _deletes++;

            return true;
        }

        private static string ComputeSha256(string text)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }
    }

    private sealed class VoiceToolValidationException : Exception
    {
        public VoiceToolValidationException(string message) : base(message)
        {
        }
    }

    private sealed class ModelEnvelope
    {
        public required string Type { get; init; }
        public string? ToolName { get; init; }
        public JsonElement Arguments { get; init; }
        public string? DisplayText { get; init; }
        public string? SpeakText { get; init; }
        public string? UserMessage { get; init; }
        public string? ReasoningSummary { get; init; }
    }
}
