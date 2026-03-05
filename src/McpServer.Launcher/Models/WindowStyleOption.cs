namespace McpServer.Launcher.Models;

/// <summary>
/// Window style for a launched process, mapping to Win32 SW_* constants.
/// </summary>
public enum WindowStyleOption
{
    /// <summary>Normal window (SW_SHOWNORMAL).</summary>
    Normal = 0,

    /// <summary>Hidden window (SW_HIDE).</summary>
    Hidden = 1,

    /// <summary>Minimized window (SW_SHOWMINIMIZED).</summary>
    Minimized = 2,

    /// <summary>Maximized window (SW_SHOWMAXIMIZED).</summary>
    Maximized = 3
}
