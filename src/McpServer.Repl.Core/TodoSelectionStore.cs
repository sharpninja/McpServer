using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Repl.Core;

/// <summary>
/// Persists the currently selected TODO for selected TODO workflow commands.
/// </summary>
public interface ITodoSelectionStore
{
    /// <summary>Loads the persisted TODO selection if one exists.</summary>
    /// <returns>The persisted selection, or <see langword="null"/> when none is available.</returns>
    ITodoSelectionState? Load();

    /// <summary>Saves the current TODO selection.</summary>
    /// <param name="selection">The selection to persist.</param>
    void Save(ITodoSelectionState selection);

    /// <summary>Clears the persisted TODO selection.</summary>
    /// <param name="id">Optional selected TODO id to clear. When supplied, a different persisted id is preserved.</param>
    void Clear(string? id = null);
}

/// <summary>
/// File-backed TODO selection store scoped to a workspace, agent, and active plugin session.
/// </summary>
public sealed class FileTodoSelectionStore : ITodoSelectionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _selectionFilePath;

    /// <summary>Initializes a new instance of the <see cref="FileTodoSelectionStore"/> class.</summary>
    /// <param name="selectionFilePath">Absolute or relative JSON file path used for selection persistence.</param>
    public FileTodoSelectionStore(string selectionFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionFilePath);
        _selectionFilePath = Path.GetFullPath(selectionFilePath);
    }

    /// <summary>
    /// Creates a store beneath <c>{workspace}/.mcpServer/{agent}/repl</c>.
    /// </summary>
    /// <param name="workspacePath">Optional workspace root. Environment variables and current directory are used when omitted.</param>
    /// <param name="agentName">Optional agent name. Environment variables are used when omitted.</param>
    /// <returns>A workspace-scoped file-backed selection store.</returns>
    public static FileTodoSelectionStore CreateForWorkspace(string? workspacePath = null, string? agentName = null)
    {
        var workspace = ResolveWorkspacePath(workspacePath);
        var agentKey = ResolveAgentKey(agentName);
        var cacheDirectory = Path.Combine(workspace, ".mcpServer", agentKey);
        var sessionId = ResolveSessionId(cacheDirectory);
        var fileName = string.IsNullOrWhiteSpace(sessionId)
            ? "todo-selection.json"
            : $"todo-selection-{HashForFileName(sessionId)}.json";
        return new FileTodoSelectionStore(Path.Combine(cacheDirectory, "repl", fileName));
    }

    /// <inheritdoc />
    public ITodoSelectionState? Load()
    {
        if (!File.Exists(_selectionFilePath))
            return null;

        try
        {
            var record = JsonSerializer.Deserialize<PersistedTodoSelection>(
                File.ReadAllText(_selectionFilePath),
                JsonOptions);
            if (record is null || string.IsNullOrWhiteSpace(record.Id))
                return null;

            return new TodoSelectionState(
                record.Id,
                record.Title ?? string.Empty,
                record.Section ?? string.Empty,
                record.Priority ?? string.Empty,
                record.Done,
                record.SelectedAt);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void Save(ITodoSelectionState selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var directory = Path.GetDirectoryName(_selectionFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var record = new PersistedTodoSelection(
            selection.Id,
            selection.Title,
            selection.Section,
            selection.Priority,
            selection.Done,
            selection.SelectedAt);
        var tempPath = $"{_selectionFilePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(record, JsonOptions), Encoding.UTF8);
        File.Move(tempPath, _selectionFilePath, overwrite: true);
    }

    /// <inheritdoc />
    public void Clear(string? id = null)
    {
        if (!File.Exists(_selectionFilePath))
            return;

        if (!string.IsNullOrWhiteSpace(id))
        {
            var current = Load();
            if (current is not null && !string.Equals(current.Id, id, StringComparison.Ordinal))
                return;
        }

        File.Delete(_selectionFilePath);
    }

    private static string ResolveWorkspacePath(string? workspacePath)
    {
        var candidates = new[]
        {
            workspacePath,
            Environment.GetEnvironmentVariable("MCP_WORKSPACE_PATH"),
            Environment.GetEnvironmentVariable("MCPSERVER_WORKSPACE_PATH"),
            Environment.GetEnvironmentVariable("MCP_WORKSPACE_START_DIR"),
            Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR"),
            Environment.CurrentDirectory,
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return Path.GetFullPath(candidate);
        }

        return Environment.CurrentDirectory;
    }

    private static string ResolveAgentKey(string? agentName)
    {
        var agent = agentName
            ?? Environment.GetEnvironmentVariable("MCP_AGENT_NAME")
            ?? Environment.GetEnvironmentVariable("PLUGIN_AGENT_NAME")
            ?? Environment.GetEnvironmentVariable("PLUGIN_AGENT_DEFAULT")
            ?? Environment.GetEnvironmentVariable("MCP_PLUGIN_HOST")
            ?? "default";
        var normalized = agent.Trim().ToLowerInvariant();
        return normalized switch
        {
            "claude" or "claudecode" or "claude-code" => "claude",
            "claudecowork" or "claude-cowork" => "cowork",
            "codex" => "codex",
            "copilot" => "copilot",
            "grok" or "grokcode" or "grok-code" => "grok",
            "cline" or "cline-v2" => "cline",
            "opencode" or "open-code" => "opencode",
            _ => SanitizeAgentKey(normalized),
        };
    }

    private static string SanitizeAgentKey(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasSeparator = false;
        foreach (var ch in value)
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        var sanitized = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
    }

    private static string ResolveSessionId(string cacheDirectory)
    {
        var envSession = Environment.GetEnvironmentVariable("MCP_SESSION_ID");
        if (!string.IsNullOrWhiteSpace(envSession))
            return envSession;

        var sessionStatePath = Path.Combine(cacheDirectory, "session-state.yaml");
        if (!File.Exists(sessionStatePath))
            return string.Empty;

        foreach (var line in File.ReadLines(sessionStatePath))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("sessionId:", StringComparison.Ordinal))
                continue;

            return trimmed["sessionId:".Length..].Trim().Trim('\'', '"');
        }

        return string.Empty;
    }

    private static string HashForFileName(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..16];

    private sealed record PersistedTodoSelection(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("section")] string? Section,
        [property: JsonPropertyName("priority")] string? Priority,
        [property: JsonPropertyName("done")] bool Done,
        [property: JsonPropertyName("selectedAt")] DateTimeOffset SelectedAt);
}

internal sealed class NullTodoSelectionStore : ITodoSelectionStore
{
    public static NullTodoSelectionStore Instance { get; } = new();

    private NullTodoSelectionStore()
    {
    }

    public ITodoSelectionState? Load() => null;

    public void Save(ITodoSelectionState selection)
    {
    }

    public void Clear(string? id = null)
    {
    }
}
