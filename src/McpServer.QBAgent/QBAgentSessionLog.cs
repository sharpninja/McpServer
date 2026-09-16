using System.Text.Json;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent;

/// <summary>One JSONL record in a qbagent session log.</summary>
internal sealed class QBAgentSessionLogRecord
{
    public string Ts { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? Name { get; set; }
}

/// <summary>
/// JSONL session transcripts under <c>~/.qbagent/sessions/{session-id}.jsonl</c>.
/// </summary>
internal sealed class QBAgentSessionLog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly object _gate = new();
    private readonly TimeProvider _clock;
    private string? _lastRole;
    private string? _lastContent;

    public QBAgentSessionLog(string sessionId, string filePath, TimeProvider? clock = null)
    {
        SessionId = sessionId;
        FilePath = filePath;
        _clock = clock ?? TimeProvider.System;
    }

    public string SessionId { get; }

    public string FilePath { get; }

    public static string DefaultRoot()
    {
        var home = Environment.GetEnvironmentVariable("QBAGENT_HOME");
        if (string.IsNullOrWhiteSpace(home))
            home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".qbagent");
        return Path.Combine(home, "sessions");
    }

    public static string NewSessionId(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var utc = clock.GetUtcNow().UtcDateTime;
        return $"qbagent-{utc:yyyyMMddTHHmmssfffZ}";
    }

    public static QBAgentSessionLog CreateNew(string root, TimeProvider? clock = null, string? workspace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        clock ??= TimeProvider.System;
        Directory.CreateDirectory(root);
        var sessionId = NewSessionId(clock);
        var path = Path.Combine(root, sessionId + ".jsonl");
        var log = new QBAgentSessionLog(sessionId, path, clock);
        log.Append("meta", "session-start", name: workspace);
        return log;
    }

    public static QBAgentSessionLog Open(string sessionId, string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var path = Path.Combine(root, sessionId + ".jsonl");
        if (!File.Exists(path))
            throw new FileNotFoundException($"No qbagent session log for '{sessionId}' at {path}.", path);
        return new QBAgentSessionLog(sessionId, path);
    }

    public static QBAgentSessionLog OpenLatest(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (!Directory.Exists(root))
            throw new FileNotFoundException($"No qbagent sessions in {root}.", root);

        var latest = Directory.GetFiles(root, "qbagent-*.jsonl")
            .OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
        if (latest is null)
            throw new FileNotFoundException($"No qbagent sessions in {root}.", root);

        var id = Path.GetFileNameWithoutExtension(latest);
        return new QBAgentSessionLog(id, latest);
    }

    public void Append(string role, string? content, string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        lock (_gate)
        {
            if (string.Equals(_lastRole, role, StringComparison.Ordinal)
                && string.Equals(_lastContent, content, StringComparison.Ordinal))
            {
                return;
            }

            var record = new QBAgentSessionLogRecord
            {
                Ts = _clock.GetUtcNow().UtcDateTime.ToString("o"),
                SessionId = SessionId,
                Role = role,
                Content = content,
                Name = name,
            };
            File.AppendAllText(FilePath, JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine);
            _lastRole = role;
            _lastContent = content;
        }
    }

    public void AppendChatMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var role = message.Role.Value;
        var name = message.Contents.OfType<FunctionCallContent>().FirstOrDefault()?.Name
                   ?? message.Contents.OfType<FunctionResultContent>().FirstOrDefault()?.CallId;
        Append(role, message.Text, name);
    }

    public IReadOnlyList<QBAgentSessionLogRecord> ReadAll()
    {
        if (!File.Exists(FilePath))
            return [];

        var records = new List<QBAgentSessionLogRecord>();
        foreach (var line in File.ReadLines(FilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var record = JsonSerializer.Deserialize<QBAgentSessionLogRecord>(line, JsonOptions);
                if (record is not null)
                    records.Add(record);
            }
            catch (JsonException)
            {
            }
        }

        return records;
    }

    public IReadOnlyList<ChatMessage> ToChatMessages()
    {
        var messages = new List<ChatMessage>();
        foreach (var record in ReadAll())
        {
            if (string.Equals(record.Role, "meta", StringComparison.OrdinalIgnoreCase))
                continue;
            var role = record.Role.ToLowerInvariant() switch
            {
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                "tool" => ChatRole.Tool,
                _ => ChatRole.User,
            };
            messages.Add(new ChatMessage(role, record.Content ?? string.Empty));
        }

        return messages;
    }
}
