using System.Text.Json;

namespace McpServer.SessionLog.Transcripts;

/// <summary>Default recursive transcript bundle detector.</summary>
public sealed class TranscriptBundleDetector : ITranscriptBundleDetector
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TranscriptBundle>> DetectAsync(string path, bool recursive, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Transcript path is required.", nameof(path));

        var fullPath = Path.GetFullPath(path);
        if (File.Exists(fullPath))
        {
            var sourceKind = await DetectFileAsync(fullPath, cancellationToken).ConfigureAwait(false);
            return sourceKind == TranscriptSourceKind.Auto
                ? []
                : [new TranscriptBundle(fullPath, sourceKind, [fullPath])];
        }

        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException("Transcript path does not exist: " + fullPath);

        var bundles = new List<TranscriptBundle>();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var clineRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sessionPath in Directory.EnumerateFiles(fullPath, "session.json", searchOption).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(sessionPath)!;
            var messagesPath = Path.Combine(directory, "messages.json");
            if (File.Exists(messagesPath) && await DetectFileAsync(sessionPath, cancellationToken).ConfigureAwait(false) == TranscriptSourceKind.Cline)
            {
                clineRoots.Add(directory);
                bundles.Add(new TranscriptBundle(directory, TranscriptSourceKind.Cline, [sessionPath, messagesPath]));
            }
        }

        foreach (var candidate in Directory.EnumerateFiles(fullPath, "*.*", searchOption).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var extension = Path.GetExtension(candidate);
            if (!extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(candidate)!;
            if (clineRoots.Contains(directory))
                continue;

            var sourceKind = await DetectFileAsync(candidate, cancellationToken).ConfigureAwait(false);
            if (sourceKind != TranscriptSourceKind.Auto)
                bundles.Add(new TranscriptBundle(candidate, sourceKind, [candidate]));
        }

        return bundles;
    }

    private static async Task<TranscriptSourceKind> DetectFileAsync(string path, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(path);
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return await DetectJsonFileAsync(path, cancellationToken).ConfigureAwait(false);

        if (!extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase))
            return TranscriptSourceKind.Auto;

        var fileName = Path.GetFileName(path);
        if (fileName.Equals("chat_history.jsonl", StringComparison.OrdinalIgnoreCase))
            return TranscriptSourceKind.Grok;

        var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
        foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)).Take(12))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            var type = TranscriptUtilities.GetString(root, "type") ?? string.Empty;
            if (type.Equals("session_meta", StringComparison.Ordinal) || type.Equals("response_item", StringComparison.Ordinal))
                return TranscriptSourceKind.Codex;
            if (type.Equals("last-prompt", StringComparison.Ordinal) || type.Equals("permission-mode", StringComparison.Ordinal) || root.TryGetProperty("uuid", out _))
                return TranscriptSourceKind.Claude;
            if (type.StartsWith("session.", StringComparison.Ordinal) || type.StartsWith("assistant.", StringComparison.Ordinal) || type.Equals("user.message", StringComparison.Ordinal))
                return TranscriptSourceKind.Copilot;
            if (type.Equals("system", StringComparison.Ordinal) || type.Equals("mcp_config_resolved", StringComparison.Ordinal) || type.Equals("chat_message", StringComparison.Ordinal))
                return TranscriptSourceKind.Grok;
            if (type.Equals("step_start", StringComparison.Ordinal) || type.Equals("step_finish", StringComparison.Ordinal) || (type.Equals("text", StringComparison.Ordinal) && root.TryGetProperty("part", out _)))
                return TranscriptSourceKind.OpenCode;
        }

        return TranscriptSourceKind.Auto;
    }

    private static async Task<TranscriptSourceKind> DetectJsonFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return TranscriptSourceKind.Auto;

        if (root.TryGetProperty("info", out _) && root.TryGetProperty("messages", out _))
            return TranscriptSourceKind.OpenCode;

        var provider = TranscriptUtilities.GetString(root, "provider");
        var agent = TranscriptUtilities.GetString(root, "agent");
        if (provider?.Equals("cline", StringComparison.OrdinalIgnoreCase) == true || agent?.Equals("cline", StringComparison.OrdinalIgnoreCase) == true)
            return TranscriptSourceKind.Cline;

        return TranscriptSourceKind.Auto;
    }
}
