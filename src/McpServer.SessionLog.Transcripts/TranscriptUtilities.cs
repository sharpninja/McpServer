using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace McpServer.SessionLog.Transcripts;

internal static class TranscriptUtilities
{
    internal static string ComputeShortHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    internal static string DeriveSessionId(TranscriptSourceKind sourceKind, IEnumerable<string> files)
    {
        var joined = string.Join("|", files.Select(Path.GetFullPath).Order(StringComparer.OrdinalIgnoreCase));
        return sourceKind.ToString().ToLowerInvariant() + "-derived-" + ComputeShortHash(joined);
    }

    internal static DateTimeOffset? ReadTimestamp(JsonElement element)
    {
        foreach (var propertyName in new[] { "timestamp", "ts", "created_at", "started_at" })
        {
            if (!element.TryGetProperty(propertyName, out var property))
                continue;

            if (property.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(property.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                return parsed.ToUniversalTime();

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var unixMs))
                return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToUniversalTime();
        }

        return null;
    }

    internal static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    internal static JsonElement? GetObject(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;
    }

    internal static IReadOnlyList<TranscriptContentBlock> ExtractContentBlocks(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
            return [new TranscriptContentBlock("text", content.GetString())];

        if (content.ValueKind != JsonValueKind.Array)
            return [];

        var blocks = new List<TranscriptContentBlock>();
        foreach (var item in content.EnumerateArray())
        {
            var type = GetString(item, "type") ?? "text";
            var text = GetString(item, "text") ?? GetString(item, "content");
            if (text is null && item.TryGetProperty("content", out var nestedContent) && nestedContent.ValueKind == JsonValueKind.Array)
            {
                var nested = ExtractContentBlocks(nestedContent);
                foreach (var block in nested)
                    blocks.Add(block);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(text))
                blocks.Add(new TranscriptContentBlock(type, text));
        }

        return blocks;
    }

    internal static string JoinText(IEnumerable<TranscriptContentBlock> blocks)
    {
        return string.Join("\n", blocks.Select(block => block.Text).Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    internal static async Task<IReadOnlyList<JsonElement>> ReadJsonLinesAsync(string path, CancellationToken cancellationToken)
    {
        const long maxSourceFileBytes = 256L * 1024L * 1024L;
        const int maxLineBytes = 8 * 1024 * 1024;
        const int maxRecords = 2_000_000;

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length > maxSourceFileBytes)
            throw new InvalidDataException("Transcript source exceeds the 256 MiB per-file limit: " + path);

        var records = new List<JsonElement>();
        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (Encoding.UTF8.GetByteCount(line) > maxLineBytes)
                throw new InvalidDataException("Transcript JSONL line exceeds the 8 MiB limit: " + path);

            if (records.Count >= maxRecords)
                throw new InvalidDataException("Transcript JSONL record count exceeds the 2,000,000 record limit: " + path);

            using var document = JsonDocument.Parse(line);
            records.Add(document.RootElement.Clone());
        }

        return records;
    }

    internal static string NormalizePath(string path) =>
        Path.GetFullPath(path).Replace('\\', '/');
}

internal static class CanonicalSessionLogYamlWriter
{
    internal static string Write(
        TranscriptSourceKind sourceKind,
        string sessionId,
        string? nativeSessionId,
        string? model,
        string? workspacePath,
        IReadOnlyList<TranscriptEvent> events,
        IReadOnlyList<TranscriptDiagnostic> diagnostics,
        IReadOnlyList<string> sourceFiles)
    {
        var sb = new StringBuilder();
        sb.Append("sourceType: ").AppendLine(sourceKind.ToString());
        sb.Append("sessionId: ").AppendLine(Scalar(sessionId));
        if (!string.IsNullOrWhiteSpace(nativeSessionId))
            sb.Append("nativeSessionId: ").AppendLine(Scalar(nativeSessionId));
        if (!string.IsNullOrWhiteSpace(model))
            sb.Append("model: ").AppendLine(Scalar(model));
        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            sb.AppendLine("workspace:");
            sb.Append("  repository: ").AppendLine(Scalar(workspacePath));
        }

        sb.AppendLine("provenance:");
        sb.Append("  sourceFiles:").AppendLine();
        foreach (var file in sourceFiles)
            sb.Append("    - ").AppendLine(Scalar(TranscriptUtilities.NormalizePath(file)));

        if (diagnostics.Count > 0)
        {
            sb.AppendLine("diagnostics:");
            foreach (var diagnostic in diagnostics)
            {
                sb.AppendLine("  - code: " + Scalar(diagnostic.Code));
                sb.AppendLine("    severity: " + Scalar(diagnostic.Severity));
                sb.AppendLine("    message: " + Scalar(diagnostic.Message));
            }
        }

        sb.AppendLine("turns:");
        foreach (var item in events.OrderBy(item => item.Order))
        {
            sb.Append("  - requestId: ").AppendLine(Scalar(item.Id));
            sb.Append("    role: ").AppendLine(Scalar(item.Role));
            sb.Append("    nativeType: ").AppendLine(Scalar(item.NativeType));
            if (item.TimestampUtc is not null)
                sb.Append("    timestamp: ").AppendLine(Scalar(item.TimestampUtc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
            var text = TranscriptUtilities.JoinText(item.Content);
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine("    queryText: |");
                foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
                    sb.Append("      ").AppendLine(line);
            }
        }

        return sb.ToString();
    }

    private static string Scalar(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "''";

        if (value.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':' or '/' or '\\'))
            return value;

        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }
}
