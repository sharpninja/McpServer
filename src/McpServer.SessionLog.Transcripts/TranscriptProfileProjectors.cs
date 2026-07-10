using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.SessionLog.Transcripts;

internal abstract class TranscriptProfileProjectorBase : ITranscriptProfileProjector
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public abstract TranscriptCompatibilityProfile Profile { get; }

    public string Project(TranscriptSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var builder = new StringBuilder();
        foreach (var record in EnumerateRecords(session))
            builder.Append(JsonSerializer.Serialize(record, SerializerOptions)).Append('\n');

        return builder.ToString();
    }

    protected abstract IEnumerable<IReadOnlyDictionary<string, object?>> EnumerateRecords(TranscriptSession session);

    protected static string? Timestamp(TranscriptEvent item) =>
        item.TimestampUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    protected static string Text(TranscriptEvent item) => TranscriptUtilities.JoinText(item.Content);

    protected static IReadOnlyList<IReadOnlyDictionary<string, object?>> TextBlocks(string text, string type = "text")
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return [new Dictionary<string, object?>
        {
            ["type"] = type,
            ["text"] = text
        }];
    }
}

internal sealed class ClaudeTranscriptProfileProjector : TranscriptProfileProjectorBase
{
    public override TranscriptCompatibilityProfile Profile => TranscriptCompatibilityProfile.Claude;

    protected override IEnumerable<IReadOnlyDictionary<string, object?>> EnumerateRecords(TranscriptSession session)
    {
        foreach (var item in session.Events.OrderBy(item => item.Order))
        {
            var text = Text(item);
            yield return new Dictionary<string, object?>
            {
                ["type"] = item.Role,
                ["uuid"] = item.Id,
                ["sessionId"] = session.SessionId,
                ["timestamp"] = Timestamp(item),
                ["message"] = new Dictionary<string, object?>
                {
                    ["role"] = item.Role,
                    ["content"] = TextBlocks(text)
                }
            };
        }
    }
}

internal sealed class CodexTranscriptProfileProjector : TranscriptProfileProjectorBase
{
    public override TranscriptCompatibilityProfile Profile => TranscriptCompatibilityProfile.Codex;

    protected override IEnumerable<IReadOnlyDictionary<string, object?>> EnumerateRecords(TranscriptSession session)
    {
        yield return new Dictionary<string, object?>
        {
            ["type"] = "session_meta",
            ["payload"] = new Dictionary<string, object?>
            {
                ["id"] = session.SessionId,
                ["source"] = session.SourceKind.ToString(),
                ["cwd"] = session.WorkspacePath,
                ["model"] = session.Model
            }
        };

        foreach (var item in session.Events.OrderBy(item => item.Order))
        {
            var text = Text(item);
            yield return new Dictionary<string, object?>
            {
                ["type"] = "response_item",
                ["timestamp"] = Timestamp(item),
                ["payload"] = new Dictionary<string, object?>
                {
                    ["id"] = item.Id,
                    ["role"] = item.Role,
                    ["content"] = TextBlocks(text, "output_text")
                }
            };
        }
    }
}

internal sealed class GrokTranscriptProfileProjector : TranscriptProfileProjectorBase
{
    public override TranscriptCompatibilityProfile Profile => TranscriptCompatibilityProfile.Grok;

    protected override IEnumerable<IReadOnlyDictionary<string, object?>> EnumerateRecords(TranscriptSession session)
    {
        foreach (var item in session.Events.OrderBy(item => item.Order))
        {
            yield return new Dictionary<string, object?>
            {
                ["type"] = "chat_message",
                ["id"] = item.Id,
                ["session_id"] = session.SessionId,
                ["role"] = item.Role,
                ["content"] = Text(item),
                ["timestamp"] = Timestamp(item),
                ["model"] = session.Model
            };
        }
    }
}
