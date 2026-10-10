using System.Collections.Concurrent;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Process-lifetime store of Cli brain-slot session ids so Grok <c>--resume</c> and Codex
/// <c>exec resume</c> reuse the same CLI conversation across QuadBrain turns.
/// </summary>
public sealed class CliBrainSlotSessionStore
{
    private readonly ConcurrentDictionary<string, string> _sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets the persisted CLI session id for <paramref name="slotId"/>, if any.</summary>
    public bool TryGet(string slotId, out string sessionId)
        => _sessions.TryGetValue(slotId, out sessionId!);

    /// <summary>Stores or replaces the CLI session id for <paramref name="slotId"/>.</summary>
    public void Set(string slotId, string sessionId)
        => _sessions[slotId] = sessionId;

    /// <summary>Drops a stored CLI session id so the next turn starts a new conversation.</summary>
    public void Remove(string slotId)
        => _sessions.TryRemove(slotId, out _);
}
