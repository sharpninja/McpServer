using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace McpServer.SessionLog.Transcripts;

internal abstract class JsonTranscriptAdapterBase : ITranscriptSourceAdapter
{
    public abstract TranscriptSourceKind SourceKind { get; }

    public abstract Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default);

    protected static TranscriptSession BuildSession(
        TranscriptSourceKind sourceKind,
        string sessionId,
        IReadOnlyList<TranscriptEvent> events,
        IReadOnlyList<string> sourceFiles,
        string? nativeSessionId = null,
        string? model = null,
        string? workspacePath = null,
        IReadOnlyList<TranscriptDiagnostic>? diagnostics = null)
    {
        var sessionDiagnostics = diagnostics ?? [];
        var yaml = CanonicalSessionLogYamlWriter.Write(
            sourceKind,
            sessionId,
            nativeSessionId,
            model,
            workspacePath,
            events,
            sessionDiagnostics,
            sourceFiles);
        return new TranscriptSession(sourceKind, sessionId, events, yaml, nativeSessionId, model, workspacePath, sessionDiagnostics, sourceFiles);
    }

    protected static TranscriptEvent CreateEvent(
        string id,
        int order,
        string role,
        string nativeType,
        string? text,
        DateTimeOffset? timestampUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var blocks = string.IsNullOrWhiteSpace(text)
            ? Array.Empty<TranscriptContentBlock>()
            : [new TranscriptContentBlock("text", text)];
        return new TranscriptEvent(id, order, role, nativeType, blocks, timestampUtc, metadata);
    }
}

internal sealed class CodexTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.Codex;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var path = bundle.Files[0];
        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        string? workspacePath = null;
        string? model = null;
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var unknownRecordCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var unknownResponseItemCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var eventMsgCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var nonConversationCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var encryptedReasoningCount = 0;
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            switch (type)
            {
                case "session_meta":
                    if (TranscriptUtilities.GetObject(record, "payload") is { } meta)
                    {
                        sessionId = TranscriptUtilities.GetString(meta, "id") ?? sessionId;
                        workspacePath = TranscriptUtilities.GetString(meta, "cwd") ?? workspacePath;
                    }
                    else
                    {
                        diagnostics.Add(new TranscriptDiagnostic("codex_malformed_session_meta", "Codex session_meta record is missing an object payload.", "warning", path));
                    }

                    continue;
                case "turn_context":
                    if (TranscriptUtilities.GetObject(record, "payload") is { } turnContext)
                    {
                        workspacePath ??= TranscriptUtilities.GetString(turnContext, "cwd");
                        model ??= TranscriptUtilities.GetString(turnContext, "model");
                    }

                    continue;
                case "event_msg":
                    // UI mirror of response_item records; conversation text is normalized from response_item.
                    Increment(eventMsgCounts, PayloadType(record) ?? "<none>");
                    continue;
                case "world_state":
                case "compacted":
                    Increment(nonConversationCounts, type);
                    continue;
                case "response_item":
                    break;
                default:
                    Increment(unknownRecordCounts, string.IsNullOrWhiteSpace(type) ? "<missing>" : type);
                    continue;
            }

            if (TranscriptUtilities.GetObject(record, "payload") is not { } payload)
            {
                diagnostics.Add(new TranscriptDiagnostic("codex_missing_payload", "Codex response_item record is missing an object payload.", "warning", path));
                continue;
            }

            var payloadType = TranscriptUtilities.GetString(payload, "type") ?? "message";
            var timestamp = TranscriptUtilities.ReadTimestamp(record);
            switch (payloadType)
            {
                case "message":
                    var role = TranscriptUtilities.GetString(payload, "role");
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        diagnostics.Add(new TranscriptDiagnostic("codex_missing_role", "Codex response_item record is missing a role and was not normalized.", "warning", path));
                        continue;
                    }

                    var content = payload.TryGetProperty("content", out var contentElement)
                        ? TranscriptUtilities.ExtractContentBlocks(contentElement)
                        : [];
                    events.Add(new TranscriptEvent(
                        EventId(payload, order),
                        order++,
                        role,
                        type,
                        content,
                        timestamp));
                    continue;
                case "reasoning":
                    var reasoningBlocks = new List<TranscriptContentBlock>();
                    if (payload.TryGetProperty("summary", out var summaryElement))
                        reasoningBlocks.AddRange(TranscriptUtilities.ExtractContentBlocks(summaryElement));
                    if (payload.TryGetProperty("content", out var reasoningContent))
                        reasoningBlocks.AddRange(TranscriptUtilities.ExtractContentBlocks(reasoningContent));
                    if (reasoningBlocks.Count == 0)
                    {
                        // Encrypted-only reasoning carries no recoverable text; counted once below.
                        encryptedReasoningCount++;
                        continue;
                    }

                    events.Add(new TranscriptEvent(
                        EventId(payload, order),
                        order++,
                        "assistant",
                        "reasoning",
                        reasoningBlocks,
                        timestamp));
                    continue;
                case "function_call":
                case "custom_tool_call":
                case "local_shell_call":
                case "web_search_call":
                    var toolName = TranscriptUtilities.GetString(payload, "name") ?? payloadType;
                    var toolInput = TranscriptUtilities.GetString(payload, "arguments") ?? TranscriptUtilities.GetString(payload, "input");
                    if (toolInput is null && payload.TryGetProperty("action", out var actionElement))
                        toolInput = actionElement.GetRawText();
                    var callText = string.IsNullOrWhiteSpace(toolInput) ? toolName : toolName + "\n" + toolInput;
                    events.Add(new TranscriptEvent(
                        EventId(payload, order),
                        order++,
                        "assistant",
                        payloadType,
                        [new TranscriptContentBlock("tool_call", callText)],
                        timestamp,
                        ToolMetadata(payload, toolName)));
                    continue;
                case "function_call_output":
                case "custom_tool_call_output":
                case "local_shell_call_output":
                    var outputText = TranscriptUtilities.GetString(payload, "output");
                    if (outputText is null && payload.TryGetProperty("output", out var outputElement))
                    {
                        var outputBlocks = TranscriptUtilities.ExtractContentBlocks(outputElement);
                        outputText = outputBlocks.Count > 0 ? TranscriptUtilities.JoinText(outputBlocks) : outputElement.GetRawText();
                    }

                    events.Add(new TranscriptEvent(
                        EventId(payload, order),
                        order++,
                        "tool",
                        payloadType,
                        string.IsNullOrWhiteSpace(outputText) ? [] : [new TranscriptContentBlock("tool_output", outputText)],
                        timestamp,
                        ToolMetadata(payload, toolName: null)));
                    continue;
                default:
                    Increment(unknownResponseItemCounts, payloadType);
                    continue;
            }
        }

        AppendAggregateDiagnostics(diagnostics, path, unknownRecordCounts, unknownResponseItemCounts, eventMsgCounts, nonConversationCounts, encryptedReasoningCount);
        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, model: model, workspacePath: workspacePath, diagnostics: diagnostics);
    }

    private static string? PayloadType(JsonElement record)
        => TranscriptUtilities.GetObject(record, "payload") is { } payload ? TranscriptUtilities.GetString(payload, "type") : null;

    private static string EventId(JsonElement payload, int order)
        => TranscriptUtilities.GetString(payload, "id")
           ?? TranscriptUtilities.GetString(payload, "call_id")
           ?? "codex-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static IReadOnlyDictionary<string, string> ToolMetadata(JsonElement payload, string? toolName)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (TranscriptUtilities.GetString(payload, "call_id") is { } callId)
            metadata["call_id"] = callId;
        if (toolName is not null)
            metadata["name"] = toolName;
        if (TranscriptUtilities.GetString(payload, "status") is { } status)
            metadata["status"] = status;
        return metadata;
    }

    private static void Increment(Dictionary<string, int> counts, string key)
        => counts[key] = counts.TryGetValue(key, out var current) ? current + 1 : 1;

    private static void AppendAggregateDiagnostics(
        List<TranscriptDiagnostic> diagnostics,
        string path,
        Dictionary<string, int> unknownRecordCounts,
        Dictionary<string, int> unknownResponseItemCounts,
        Dictionary<string, int> eventMsgCounts,
        Dictionary<string, int> nonConversationCounts,
        int encryptedReasoningCount)
    {
        foreach (var (type, count) in unknownRecordCounts)
            diagnostics.Add(new TranscriptDiagnostic("codex_unknown_record", "Codex JSONL record type '" + type + "' was not normalized (" + Format(count) + ").", "warning", path));
        foreach (var (type, count) in unknownResponseItemCounts)
            diagnostics.Add(new TranscriptDiagnostic("codex_unknown_response_item", "Codex response_item payload type '" + type + "' was not normalized (" + Format(count) + ").", "warning", path));
        if (eventMsgCounts.Count > 0)
            diagnostics.Add(new TranscriptDiagnostic("codex_event_msg_skipped", "Codex event_msg records were skipped as UI mirrors of response_item records: " + Format(eventMsgCounts.Values.Sum()) + " across types " + FormatCounts(eventMsgCounts) + ".", "info", path));
        if (nonConversationCounts.Count > 0)
            diagnostics.Add(new TranscriptDiagnostic("codex_nonconversation_skipped", "Codex non-conversation records were skipped: " + FormatCounts(nonConversationCounts) + ".", "info", path));
        if (encryptedReasoningCount > 0)
            diagnostics.Add(new TranscriptDiagnostic("codex_encrypted_reasoning", "Codex reasoning records without recoverable summary text were skipped: " + Format(encryptedReasoningCount) + ".", "info", path));
    }

    private static string Format(int count)
        => count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " record(s)";

    private static string FormatCounts(Dictionary<string, int> counts)
        => string.Join(", ", counts.Select(pair => pair.Key + "=" + pair.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
}

internal sealed class ClaudeTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.Claude;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var path = bundle.Files[0];
        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        string? model = null;
        string? workspacePath = null;
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            sessionId = TranscriptUtilities.GetString(record, "sessionId") ?? sessionId;
            workspacePath = TranscriptUtilities.GetString(record, "cwd") ?? workspacePath;
            var nativeType = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (nativeType is not ("user" or "assistant"))
            {
                diagnostics.Add(new TranscriptDiagnostic("claude_unknown_record", "Claude JSONL record type '" + (string.IsNullOrWhiteSpace(nativeType) ? "<missing>" : nativeType) + "' was not normalized.", "warning", path));
                continue;
            }

            if (TranscriptUtilities.GetObject(record, "message") is not { } message)
            {
                diagnostics.Add(new TranscriptDiagnostic("claude_missing_message", "Claude user/assistant record is missing a message object and was not normalized.", "warning", path));
                continue;
            }

            var role = TranscriptUtilities.GetString(message, "role") ?? nativeType;
            model = TranscriptUtilities.GetString(message, "model") ?? model;
            var content = message.TryGetProperty("content", out var contentElement)
                ? TranscriptUtilities.ExtractContentBlocks(contentElement)
                : [];
            events.Add(new TranscriptEvent(
                TranscriptUtilities.GetString(record, "uuid") ?? "claude-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture),
                order++,
                role,
                nativeType,
                content,
                TranscriptUtilities.ReadTimestamp(record)));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, model: model, workspacePath: workspacePath, diagnostics: diagnostics);
    }
}

internal sealed class GrokTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.Grok;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var path = bundle.Files[0];
        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        string? model = null;
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            var role = TranscriptUtilities.GetString(record, "role") ?? (type is "system" or "user" or "assistant" ? type : null);
            if (string.IsNullOrWhiteSpace(role))
            {
                var diagnosticCode = type.Equals("chat_message", StringComparison.Ordinal) ? "grok_missing_role" : "grok_unknown_record";
                var diagnosticMessage = type.Equals("chat_message", StringComparison.Ordinal)
                    ? "Grok chat_message record is missing a role and was not normalized."
                    : "Grok JSONL record type '" + (string.IsNullOrWhiteSpace(type) ? "<missing>" : type) + "' was not normalized.";
                diagnostics.Add(new TranscriptDiagnostic(diagnosticCode, diagnosticMessage, "warning", path));
                continue;
            }

            model = TranscriptUtilities.GetString(record, "model") ?? model;
            var text = TranscriptUtilities.GetString(record, "content") ?? TranscriptUtilities.GetString(record, "message") ?? type;
            events.Add(CreateEvent("grok-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, role, type, text, TranscriptUtilities.ReadTimestamp(record)));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, model: model, diagnostics: diagnostics);
    }
}

internal sealed class ClineTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.Cline;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var sessionFile = bundle.Files.First(file => Path.GetFileName(file).Equals("session.json", StringComparison.OrdinalIgnoreCase));
        var messagesFile = bundle.Files.First(file => Path.GetFileName(file).Equals("messages.json", StringComparison.OrdinalIgnoreCase));
        await using var sessionStream = File.OpenRead(sessionFile);
        using var sessionDocument = await JsonDocument.ParseAsync(sessionStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        await using var messagesStream = File.OpenRead(messagesFile);
        using var messagesDocument = await JsonDocument.ParseAsync(messagesStream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var sessionRoot = sessionDocument.RootElement;
        var messagesRoot = messagesDocument.RootElement;
        var sessionId = TranscriptUtilities.GetString(sessionRoot, "session_id") ?? TranscriptUtilities.GetString(messagesRoot, "sessionId") ?? TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        var model = TranscriptUtilities.GetString(sessionRoot, "model");
        var workspacePath = TranscriptUtilities.GetString(sessionRoot, "workspace_root") ?? TranscriptUtilities.GetString(sessionRoot, "cwd");
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var order = 1;
        if (messagesRoot.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var message in messages.EnumerateArray())
            {
                var role = TranscriptUtilities.GetString(message, "role") ?? "unknown";
                var text = TranscriptUtilities.GetString(message, "content");
                events.Add(CreateEvent(TranscriptUtilities.GetString(message, "id") ?? "cline-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, role, "message", text, TranscriptUtilities.ReadTimestamp(message)));
            }
        }
        else
        {
            diagnostics.Add(new TranscriptDiagnostic("cline_missing_messages", "Cline messages file is missing a messages array.", "warning", messagesFile));
        }
        if (TranscriptUtilities.GetObject(sessionRoot, "metadata") is { } metadata)
        {
            var diagnosticText = TranscriptUtilities.GetString(metadata, "diagnostic");
            if (!string.IsNullOrWhiteSpace(diagnosticText))
            {
                diagnostics.Add(new TranscriptDiagnostic("cline-session-diagnostic", diagnosticText));
                events.Add(CreateEvent("cline-diagnostic-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "diagnostic", "diagnostic", diagnosticText, TranscriptUtilities.ReadTimestamp(sessionRoot)));
            }
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, model: model, workspacePath: workspacePath, diagnostics: diagnostics);
    }
}

internal sealed class CopilotTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.Copilot;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var path = bundle.Files[0];
        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        string? model = null;
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (TranscriptUtilities.GetObject(record, "data") is not { } data)
            {
                diagnostics.Add(new TranscriptDiagnostic("copilot_missing_data", "Copilot event record is missing a data object and was not normalized.", "warning", path));
                continue;
            }

            if (type.Equals("user.message", StringComparison.Ordinal))
            {
                events.Add(CreateEvent(TranscriptUtilities.GetString(record, "id") ?? "copilot-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "user", type, TranscriptUtilities.GetString(data, "message"), TranscriptUtilities.ReadTimestamp(record)));
            }
            else if (type.Equals("assistant.message", StringComparison.Ordinal))
            {
                model = TranscriptUtilities.GetString(data, "model") ?? model;
                events.Add(CreateEvent(TranscriptUtilities.GetString(record, "id") ?? "copilot-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "assistant", type, TranscriptUtilities.GetString(data, "content"), TranscriptUtilities.ReadTimestamp(record)));
            }
            else
            {
                diagnostics.Add(new TranscriptDiagnostic("copilot_unknown_record", "Copilot event type '" + (string.IsNullOrWhiteSpace(type) ? "<missing>" : type) + "' was not normalized.", "warning", path));
            }
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, model: model, diagnostics: diagnostics);
    }
}

internal sealed class OpenCodeTranscriptAdapter : JsonTranscriptAdapterBase
{
    public override TranscriptSourceKind SourceKind => TranscriptSourceKind.OpenCode;

    public override async Task<TranscriptSession> NormalizeAsync(TranscriptBundle bundle, CancellationToken cancellationToken = default)
    {
        var path = bundle.Files[0];
        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
            return await NormalizeExportAsync(bundle, cancellationToken).ConfigureAwait(false);
        if (OpenCodeSqliteUtilities.IsSnapshotPath(path))
            return await NormalizeSqliteSnapshotAsync(bundle, cancellationToken).ConfigureAwait(false);


        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = records.Select(record => TranscriptUtilities.GetString(record, "sessionID")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        var diagnostics = new List<TranscriptDiagnostic>();
        var events = new List<TranscriptEvent>();
        var order = 1;
        var openSteps = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (TranscriptUtilities.GetObject(record, "part") is not { } part)
            {
                diagnostics.Add(new TranscriptDiagnostic("opencode_jsonl_missing_part", "OpenCode JSONL record is missing a part object.", "warning", path));
                continue;
            }

            var stepKey = TranscriptUtilities.GetString(part, "messageID") ?? TranscriptUtilities.GetString(part, "messageId") ?? TranscriptUtilities.GetString(part, "id");
            if (type.Equals("step_start", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(stepKey))
                openSteps[stepKey] = stepKey;
            else if (type.Equals("step_finish", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(stepKey))
                openSteps.Remove(stepKey);

            if (!type.Equals("text", StringComparison.Ordinal))
            {
                var diagnosticType = string.IsNullOrWhiteSpace(type) ? "<missing>" : type;
                diagnostics.Add(new TranscriptDiagnostic("opencode_jsonl_unknown_record", "Unsupported OpenCode JSONL record type: " + diagnosticType + ".", "warning", path));
                continue;
            }

            events.Add(CreateEvent(TranscriptUtilities.GetString(part, "id") ?? "opencode-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "assistant", type, TranscriptUtilities.GetString(part, "text"), TranscriptUtilities.ReadTimestamp(record)));
        }

        foreach (var stepKey in openSteps.Keys.OrderBy(item => item, StringComparer.Ordinal))
        {
            diagnostics.Add(new TranscriptDiagnostic("opencode_jsonl_incomplete_step", "OpenCode JSONL step did not have a matching finish record: " + stepKey + ".", "warning", path));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, diagnostics: diagnostics);
    }

    private static async Task<TranscriptSession> NormalizeSqliteSnapshotAsync(TranscriptBundle bundle, CancellationToken cancellationToken)
    {
        var sourcePath = bundle.Files[0];
        var snapshotPath = await OpenCodeSqliteUtilities.CreateSnapshotAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenCodeSqliteUtilities.OpenReadOnlyAsync(snapshotPath, cancellationToken).ConfigureAwait(false);
            var tables = await OpenCodeSqliteUtilities.ReadTableNamesAsync(connection, cancellationToken).ConfigureAwait(false);
            if (!tables.Contains("session") || !tables.Contains("message"))
                throw new InvalidDataException("OpenCode SQLite snapshot is missing required session/message tables: " + sourcePath);

            var diagnostics = new List<TranscriptDiagnostic>();
            var sessionColumns = await OpenCodeSqliteUtilities.ReadColumnNamesAsync(connection, "session", cancellationToken).ConfigureAwait(false);
            var messageColumns = await OpenCodeSqliteUtilities.ReadColumnNamesAsync(connection, "message", cancellationToken).ConfigureAwait(false);
            var partColumns = tables.Contains("part")
                ? await OpenCodeSqliteUtilities.ReadColumnNamesAsync(connection, "part", cancellationToken).ConfigureAwait(false)
                : [];
            var toolEventColumns = tables.Contains("tool_event")
                ? await OpenCodeSqliteUtilities.ReadColumnNamesAsync(connection, "tool_event", cancellationToken).ConfigureAwait(false)
                : [];

            var sessionMetadata = await ReadSqliteSessionMetadataAsync(connection, sessionColumns, bundle.Files, cancellationToken).ConfigureAwait(false);
            var messageProjection = await ReadSqliteMessagesAsync(connection, messageColumns, partColumns, toolEventColumns, sessionMetadata.SessionId, sessionMetadata.Model, diagnostics, sourcePath, cancellationToken).ConfigureAwait(false);
            return BuildSession(
                TranscriptSourceKind.OpenCode,
                sessionMetadata.SessionId,
                messageProjection.Events,
                bundle.Files,
                nativeSessionId: sessionMetadata.SessionId,
                model: messageProjection.Model ?? sessionMetadata.Model,
                workspacePath: sessionMetadata.WorkspacePath,
                diagnostics: diagnostics);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            OpenCodeSqliteUtilities.DeleteSnapshotDirectory(snapshotPath);
        }
    }

    private static async Task<OpenCodeSqliteSessionMetadata> ReadSqliteSessionMetadataAsync(
        SqliteConnection connection,
        IReadOnlySet<string> sessionColumns,
        IReadOnlyList<string> sourceFiles,
        CancellationToken cancellationToken)
    {
        var sessionId = TranscriptUtilities.DeriveSessionId(TranscriptSourceKind.OpenCode, sourceFiles);
        var orderColumn = OpenCodeSqliteUtilities.OrderColumnOrFallback(sessionColumns, "time_created", "created_at", "id");
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(sessionColumns, "id", "id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(sessionColumns, "workspace_path", "workspace_path") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(sessionColumns, "model", "model")
            + " FROM " + OpenCodeSqliteUtilities.QuoteIdentifier("session")
            + " ORDER BY " + orderColumn + " LIMIT 1;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return new OpenCodeSqliteSessionMetadata(sessionId, null, null);

        return new OpenCodeSqliteSessionMetadata(
            ReadNullableString(reader, 0) ?? sessionId,
            ReadNullableString(reader, 2),
            ReadNullableString(reader, 1));
    }

    private static async Task<OpenCodeSqliteMessageProjection> ReadSqliteMessagesAsync(
        SqliteConnection connection,
        IReadOnlySet<string> messageColumns,
        IReadOnlySet<string> partColumns,
        IReadOnlySet<string> toolEventColumns,
        string sessionId,
        string? seedModel,
        List<TranscriptDiagnostic> diagnostics,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var orderColumn = OpenCodeSqliteUtilities.OrderColumnOrFallback(messageColumns, "time_created", "created_at", "id");
        var timestampColumn = SelectTimestampColumnOrNull(messageColumns);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(messageColumns, "id", "id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(messageColumns, "role", "role") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(messageColumns, "model_id", "model_id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(messageColumns, "provider_id", "provider_id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(messageColumns, "content_json", "content_json") + ", "
            + timestampColumn + " AS " + OpenCodeSqliteUtilities.QuoteIdentifier("timestamp")
            + " FROM " + OpenCodeSqliteUtilities.QuoteIdentifier("message")
            + " WHERE " + OpenCodeSqliteUtilities.QuoteIdentifier("session_id") + " = $sessionId"
            + " ORDER BY " + orderColumn + ", " + OpenCodeSqliteUtilities.QuoteIdentifier("id") + ";";
        command.Parameters.AddWithValue("$sessionId", sessionId);

        var rows = new List<OpenCodeSqliteMessageRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(new OpenCodeSqliteMessageRow(
                    ReadNullableString(reader, 0) ?? "opencode-sqlite-message-" + (rows.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ReadNullableString(reader, 1) ?? "unknown",
                    ReadNullableString(reader, 2),
                    ReadNullableString(reader, 3),
                    ReadNullableString(reader, 4),
                    ReadTimestampFromDatabase(reader.IsDBNull(5) ? null : reader.GetValue(5))));
            }
        }

        var messageIds = rows.Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        var events = new List<TranscriptEvent>();
        var order = 1;
        var model = seedModel;
        foreach (var row in rows)
        {
            model ??= row.ModelId;
            var blocks = new List<TranscriptContentBlock>();
            if (!string.IsNullOrWhiteSpace(row.ContentJson))
                blocks.AddRange(ExtractBlocksFromOpenCodeJson(row.ContentJson, "text", diagnostics, sourcePath));
            blocks.AddRange(await ReadSqlitePartsAsync(connection, partColumns, row.Id, diagnostics, sourcePath, cancellationToken).ConfigureAwait(false));

            events.Add(new TranscriptEvent(
                row.Id,
                order++,
                row.Role,
                "message",
                blocks,
                row.TimestampUtc,
                string.IsNullOrWhiteSpace(row.ProviderId) ? null : new Dictionary<string, string>(StringComparer.Ordinal) { ["providerId"] = row.ProviderId }));
        }

        events.AddRange(await ReadSqliteToolEventsAsync(connection, toolEventColumns, sessionId, messageIds, order, diagnostics, sourcePath, cancellationToken).ConfigureAwait(false));
        return new OpenCodeSqliteMessageProjection(ReorderEvents(events), model);
    }

    private static async Task<IReadOnlyList<TranscriptEvent>> ReadSqliteToolEventsAsync(
        SqliteConnection connection,
        IReadOnlySet<string> toolEventColumns,
        string sessionId,
        IReadOnlySet<string> messageIds,
        int startOrder,
        List<TranscriptDiagnostic> diagnostics,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        if (toolEventColumns.Count == 0 || !toolEventColumns.Contains("session_id"))
            return [];

        var timestampColumn = SelectTimestampColumnOrNull(toolEventColumns);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(toolEventColumns, "id", "id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(toolEventColumns, "message_id", "message_id") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(toolEventColumns, "tool_name", "tool_name") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(toolEventColumns, "status", "status") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(toolEventColumns, "payload_json", "payload_json") + ", "
            + timestampColumn + " AS " + OpenCodeSqliteUtilities.QuoteIdentifier("timestamp")
            + " FROM " + OpenCodeSqliteUtilities.QuoteIdentifier("tool_event")
            + " WHERE " + OpenCodeSqliteUtilities.QuoteIdentifier("session_id") + " = $sessionId"
            + " ORDER BY " + OpenCodeSqliteUtilities.OrderColumnOrFallback(toolEventColumns, "time_created", "created_at", "id") + ";";
        command.Parameters.AddWithValue("$sessionId", sessionId);

        var events = new List<TranscriptEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = ReadNullableString(reader, 0) ?? "opencode-sqlite-tool-" + (events.Count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var messageId = ReadNullableString(reader, 1);
            if (!string.IsNullOrWhiteSpace(messageId) && !messageIds.Contains(messageId))
            {
                diagnostics.Add(new TranscriptDiagnostic("opencode_orphan_tool_event", "OpenCode SQLite tool event references a missing message: " + messageId + ".", "warning", sourcePath));
            }
            var toolName = ReadNullableString(reader, 2);
            var status = ReadNullableString(reader, 3);
            var payloadJson = ReadNullableString(reader, 4);
            var timestampUtc = ReadTimestampFromDatabase(reader.IsDBNull(5) ? null : reader.GetValue(5));
            var blocks = string.IsNullOrWhiteSpace(payloadJson)
                ? Array.Empty<TranscriptContentBlock>()
                : ExtractBlocksFromOpenCodeJson(payloadJson, "tool_event", diagnostics, sourcePath);
            var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(messageId))
                metadata["messageId"] = messageId;
            if (!string.IsNullOrWhiteSpace(toolName))
                metadata["toolName"] = toolName;
            if (!string.IsNullOrWhiteSpace(status))
                metadata["status"] = status;

            events.Add(new TranscriptEvent(id, startOrder + events.Count, "tool", "tool_event", blocks, timestampUtc, metadata));
        }

        return events;
    }

    private static IReadOnlyList<TranscriptEvent> ReorderEvents(IReadOnlyList<TranscriptEvent> events)
    {
        return events
            .OrderBy(item => item.TimestampUtc ?? DateTimeOffset.MaxValue)
            .ThenBy(item => item.Order)
            .Select((item, index) => new TranscriptEvent(item.Id, index + 1, item.Role, item.NativeType, item.Content, item.TimestampUtc, item.Metadata))
            .ToArray();
    }
    private static async Task<IReadOnlyList<TranscriptContentBlock>> ReadSqlitePartsAsync(
        SqliteConnection connection,
        IReadOnlySet<string> partColumns,
        string messageId,
        List<TranscriptDiagnostic> diagnostics,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        if (partColumns.Count == 0 || !partColumns.Contains("message_id"))
            return [];

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(partColumns, "type", "type") + ", "
            + OpenCodeSqliteUtilities.SelectColumnOrNull(partColumns, "json", "json")
            + " FROM " + OpenCodeSqliteUtilities.QuoteIdentifier("part")
            + " WHERE " + OpenCodeSqliteUtilities.QuoteIdentifier("message_id") + " = $messageId"
            + " ORDER BY " + OpenCodeSqliteUtilities.OrderColumnOrFallback(partColumns, "time_created", "id") + ";";
        command.Parameters.AddWithValue("$messageId", messageId);

        var blocks = new List<TranscriptContentBlock>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var type = ReadNullableString(reader, 0) ?? "text";
            var json = ReadNullableString(reader, 1);
            if (!string.IsNullOrWhiteSpace(json))
                blocks.AddRange(ExtractBlocksFromOpenCodeJson(json, type, diagnostics, sourcePath));
        }

        return blocks;
    }

    private static IReadOnlyList<TranscriptContentBlock> ExtractBlocksFromOpenCodeJson(string json, string fallbackType, List<TranscriptDiagnostic> diagnostics, string sourcePath)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
                return [new TranscriptContentBlock(fallbackType, root.GetString())];
            if (root.ValueKind == JsonValueKind.Array)
                return TranscriptUtilities.ExtractContentBlocks(root);
            if (root.ValueKind != JsonValueKind.Object)
                return [];

            foreach (var propertyName in new[] { "text", "content", "value" })
            {
                if (!root.TryGetProperty(propertyName, out var property))
                    continue;
                if (property.ValueKind == JsonValueKind.String)
                    return [new TranscriptContentBlock(fallbackType, property.GetString())];
                if (property.ValueKind == JsonValueKind.Array)
                    return TranscriptUtilities.ExtractContentBlocks(property);
            }

            if (root.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
                return TranscriptUtilities.ExtractContentBlocks(parts);

            diagnostics.Add(new TranscriptDiagnostic("opencode_part_without_text", "OpenCode SQLite part did not contain text content.", path: sourcePath));
            return [];
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new TranscriptDiagnostic("opencode_malformed_part_json", "OpenCode SQLite part JSON could not be parsed: " + exception.Message, "error", sourcePath));
            return [];
        }
    }

    private static string SelectTimestampColumnOrNull(IReadOnlySet<string> columns)
    {
        if (columns.Contains("time_created"))
            return OpenCodeSqliteUtilities.QuoteIdentifier("time_created");
        if (columns.Contains("created_at"))
            return OpenCodeSqliteUtilities.QuoteIdentifier("created_at");
        return "NULL";
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);

    private static DateTimeOffset? ReadTimestampFromDatabase(object? value)
    {
        if (value is null)
            return null;
        if (value is long number)
            return number > 10_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds(number) : DateTimeOffset.FromUnixTimeSeconds(number);
        var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (long.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsedNumber))
            return parsedNumber > 10_000_000_000 ? DateTimeOffset.FromUnixTimeMilliseconds(parsedNumber) : DateTimeOffset.FromUnixTimeSeconds(parsedNumber);
        if (DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedDate))
            return parsedDate.ToUniversalTime();
        return null;
    }

    private sealed record OpenCodeSqliteSessionMetadata(string SessionId, string? Model, string? WorkspacePath);

    private sealed record OpenCodeSqliteMessageProjection(IReadOnlyList<TranscriptEvent> Events, string? Model);

    private sealed record OpenCodeSqliteMessageRow(string Id, string Role, string? ModelId, string? ProviderId, string? ContentJson, DateTimeOffset? TimestampUtc);

    private static async Task<TranscriptSession> NormalizeExportAsync(TranscriptBundle bundle, CancellationToken cancellationToken)
    {
        var path = bundle.Files[0];
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        var info = TranscriptUtilities.GetObject(root, "info");
        var sessionId = info is { } infoValue ? TranscriptUtilities.GetString(infoValue, "id") ?? TranscriptUtilities.DeriveSessionId(TranscriptSourceKind.OpenCode, bundle.Files) : TranscriptUtilities.DeriveSessionId(TranscriptSourceKind.OpenCode, bundle.Files);
        var model = info is { } modelInfo && TranscriptUtilities.GetObject(modelInfo, "model") is { } modelObject
            ? TranscriptUtilities.GetString(modelObject, "id")
            : null;
        var workspacePath = info is { } workspaceInfo ? TranscriptUtilities.GetString(workspaceInfo, "path") : null;
        var events = new List<TranscriptEvent>();
        var order = 1;
        if (root.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            foreach (var message in messages.EnumerateArray())
            {
                if (TranscriptUtilities.GetObject(message, "info") is not { } messageInfo)
                    continue;

                var role = TranscriptUtilities.GetString(messageInfo, "role") ?? "unknown";
                var blocks = new List<TranscriptContentBlock>();
                if (message.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        var text = TranscriptUtilities.GetString(part, "text");
                        if (!string.IsNullOrWhiteSpace(text))
                            blocks.Add(new TranscriptContentBlock(TranscriptUtilities.GetString(part, "type") ?? "text", text));
                    }
                }

                events.Add(new TranscriptEvent(
                    TranscriptUtilities.GetString(messageInfo, "id") ?? "opencode-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    order++,
                    role,
                    "message",
                    blocks,
                    null));
            }
        }

        return BuildSession(TranscriptSourceKind.OpenCode, sessionId, events, bundle.Files, nativeSessionId: sessionId, model: model, workspacePath: workspacePath);
    }
}
