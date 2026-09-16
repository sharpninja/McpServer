namespace McpServer.QBAgent;

/// <summary>
/// In-place heartbeat glyph at the top-right of the terminal. Does not write a newline.
/// </summary>
internal static class QBAgentHeartbeatOverlay
{
    internal static readonly char[] Frames = ['|', '/', '-', '\\'];

    internal static char FrameAt(int beat)
        => Frames[Math.Abs(beat) % Frames.Length];

    /// <summary>ANSI: save cursor, write <paramref name="glyph"/> at row 1 / <paramref name="column"/>, restore.</summary>
    internal static string Paint(int column, char glyph)
    {
        column = Math.Max(1, column);
        return $"\u001b[s\u001b[1;{column}H{glyph}\u001b[u";
    }

    /// <summary>ANSI: overwrite the heartbeat cell with a space so the glyph disappears.</summary>
    internal static string Hide(int column)
    {
        column = Math.Max(1, column);
        return $"\u001b[s\u001b[1;{column}H \u001b[u";
    }
}
