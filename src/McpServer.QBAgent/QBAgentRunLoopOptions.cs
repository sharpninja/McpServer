namespace McpServer.QBAgent;

/// <summary>Optional run-loop settings for timestamps, progress, and local TODO listing.</summary>
public sealed class QBAgentRunLoopOptions
{
    /// <summary>Clock used for local sortable timestamps. Defaults to <see cref="TimeProvider.System"/>.</summary>
    public TimeProvider Clock { get; init; } = TimeProvider.System;

    /// <summary>
    /// How often to rotate the in-place heartbeat glyph while a QuadBrain turn runs.
    /// <see cref="Timeout.InfiniteTimeSpan"/> or a non-positive value disables the glyph.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromMilliseconds(120);

    /// <summary>
    /// Terminal width used to place the glyph in the top-right cell. Defaults to <see cref="Console.WindowWidth"/>.
    /// </summary>
    public Func<int>? TerminalWidth { get; init; }

    /// <summary>When true, print Arbiter intent JSON. Default false (envelope suppressed).</summary>
    public bool ShowIntent { get; init; }

    /// <summary>JSONL session transcript. When set, user and assistant turns are appended.</summary>
    internal QBAgentSessionLog? SessionLog { get; set; }

    /// <summary>Local handler for <c>list open todo</c>. When null, the prompt is sent to QuadBrain.</summary>
    public Func<CancellationToken, Task<string>>? ListOpenTodos { get; init; }
}
