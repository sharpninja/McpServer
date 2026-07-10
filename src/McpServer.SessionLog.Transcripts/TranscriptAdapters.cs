using System.Text.Json;

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
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (type.Equals("session_meta", StringComparison.Ordinal) && TranscriptUtilities.GetObject(record, "payload") is { } meta)
            {
                sessionId = TranscriptUtilities.GetString(meta, "id") ?? sessionId;
                workspacePath = TranscriptUtilities.GetString(meta, "cwd") ?? workspacePath;
                continue;
            }

            if (!type.Equals("response_item", StringComparison.Ordinal) || TranscriptUtilities.GetObject(record, "payload") is not { } payload)
                continue;

            var role = TranscriptUtilities.GetString(payload, "role");
            if (string.IsNullOrWhiteSpace(role))
                continue;

            var content = payload.TryGetProperty("content", out var contentElement)
                ? TranscriptUtilities.ExtractContentBlocks(contentElement)
                : [];
            events.Add(new TranscriptEvent(
                TranscriptUtilities.GetString(payload, "id") ?? "codex-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture),
                order++,
                role,
                type,
                content,
                TranscriptUtilities.ReadTimestamp(record)));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, workspacePath: workspacePath);
    }
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
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            sessionId = TranscriptUtilities.GetString(record, "sessionId") ?? sessionId;
            workspacePath = TranscriptUtilities.GetString(record, "cwd") ?? workspacePath;
            var nativeType = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (nativeType is not ("user" or "assistant"))
                continue;

            if (TranscriptUtilities.GetObject(record, "message") is not { } message)
                continue;

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

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId, model: model, workspacePath: workspacePath);
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
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            var role = TranscriptUtilities.GetString(record, "role") ?? (type is "system" or "user" or "assistant" ? type : null);
            if (string.IsNullOrWhiteSpace(role) && type.Equals("chat_message", StringComparison.Ordinal))
                role = TranscriptUtilities.GetString(record, "role");
            if (string.IsNullOrWhiteSpace(role))
                continue;

            model = TranscriptUtilities.GetString(record, "model") ?? model;
            var text = TranscriptUtilities.GetString(record, "content") ?? TranscriptUtilities.GetString(record, "message") ?? type;
            events.Add(CreateEvent("grok-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, role, type, text, TranscriptUtilities.ReadTimestamp(record)));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, model: model);
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

        var diagnostics = new List<TranscriptDiagnostic>();
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
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (TranscriptUtilities.GetObject(record, "data") is not { } data)
                continue;

            if (type.Equals("user.message", StringComparison.Ordinal))
            {
                events.Add(CreateEvent(TranscriptUtilities.GetString(record, "id") ?? "copilot-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "user", type, TranscriptUtilities.GetString(data, "message"), TranscriptUtilities.ReadTimestamp(record)));
            }
            else if (type.Equals("assistant.message", StringComparison.Ordinal))
            {
                model = TranscriptUtilities.GetString(data, "model") ?? model;
                events.Add(CreateEvent(TranscriptUtilities.GetString(record, "id") ?? "copilot-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "assistant", type, TranscriptUtilities.GetString(data, "content"), TranscriptUtilities.ReadTimestamp(record)));
            }
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, model: model);
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

        var records = await TranscriptUtilities.ReadJsonLinesAsync(path, cancellationToken).ConfigureAwait(false);
        var sessionId = records.Select(record => TranscriptUtilities.GetString(record, "sessionID")).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? TranscriptUtilities.DeriveSessionId(SourceKind, bundle.Files);
        var events = new List<TranscriptEvent>();
        var order = 1;
        foreach (var record in records)
        {
            var type = TranscriptUtilities.GetString(record, "type") ?? string.Empty;
            if (!type.Equals("text", StringComparison.Ordinal) || TranscriptUtilities.GetObject(record, "part") is not { } part)
                continue;

            events.Add(CreateEvent(TranscriptUtilities.GetString(part, "id") ?? "opencode-event-" + order.ToString(System.Globalization.CultureInfo.InvariantCulture), order++, "assistant", type, TranscriptUtilities.GetString(part, "text"), TranscriptUtilities.ReadTimestamp(record)));
        }

        return BuildSession(SourceKind, sessionId, events, bundle.Files, nativeSessionId: sessionId);
    }

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
