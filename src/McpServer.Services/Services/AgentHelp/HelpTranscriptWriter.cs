using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-003: Append-only JSONL transcript writer for Agent Help sessions.
/// TR-MCP-HELP-003: Persists one JSON object per line under the configured transcript directory.
/// </summary>
public sealed class HelpTranscriptWriter
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly IOptionsMonitor<AgentHelpOptions> _options;
    private readonly ILogger<HelpTranscriptWriter> _logger;

    /// <summary>
    /// TR-MCP-HELP-003: Creates a new transcript writer.
    /// </summary>
    public HelpTranscriptWriter(
        IOptionsMonitor<AgentHelpOptions> options,
        ILogger<HelpTranscriptWriter> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-HELP-003: Appends a transcript entry to the session JSONL file.
    /// </summary>
    /// <param name="workspaceDataRoot">Workspace-local data root directory.</param>
    /// <param name="entry">Transcript entry to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AppendAsync(
        string workspaceDataRoot,
        AgentHelpTranscriptEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDataRoot);
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();

        var transcriptDir = Path.Combine(workspaceDataRoot, _options.CurrentValue.TranscriptDirectory);
        Directory.CreateDirectory(transcriptDir);

        var filePath = Path.Combine(transcriptDir, $"{SanitizeFileName(entry.SessionId)}.jsonl");
        var line = JsonSerializer.Serialize(entry, s_jsonOptions) + Environment.NewLine;
        var bytes = Encoding.UTF8.GetBytes(line);

        await using var stream = new FileStream(
            filePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Appended Agent Help transcript entry: Session={SessionId}; Role={Role}; Category={Category}",
            entry.SessionId,
            entry.Role,
            entry.Category);
    }

    /// <summary>
    /// FR-MCP-HELP-003: Reads all transcript entries for a session from JSONL storage.
    /// </summary>
    public async Task<IReadOnlyList<AgentHelpTranscriptEntry>> ReadAllAsync(
        string workspaceDataRoot,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = Path.Combine(
            workspaceDataRoot,
            _options.CurrentValue.TranscriptDirectory,
            $"{SanitizeFileName(sessionId)}.jsonl");

        if (!File.Exists(filePath))
            return [];

        var entries = new List<AgentHelpTranscriptEntry>();
        await foreach (var line in ReadLinesAsync(filePath, cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var entry = JsonSerializer.Deserialize<AgentHelpTranscriptEntry>(line, s_jsonOptions);
                if (entry is not null)
                    entries.Add(entry);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Skipped invalid Agent Help transcript line in {FilePath}", filePath);
            }
        }

        return entries;
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        string filePath,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                yield break;

            yield return line;
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        return builder.ToString();
    }
}