namespace McpServer.Launcher.Native;

/// <summary>
/// Win32 constants for token access, process creation, and window styles.
/// </summary>
internal static class NativeConstants
{
    // ── Token access rights ──────────────────────────────────────────────

    /// <summary>Required to duplicate an access token.</summary>
    internal const int TOKEN_DUPLICATE = 0x0002;

    /// <summary>Required to query an access token.</summary>
    internal const int TOKEN_QUERY = 0x0008;

    /// <summary>Required to attach a primary token to a process.</summary>
    internal const int TOKEN_ASSIGN_PRIMARY = 0x0001;

    /// <summary>All possible access rights for an access token.</summary>
    internal const int TOKEN_ALL_ACCESS = 0x000F01FF;

    // ── Security impersonation levels ────────────────────────────────────

    /// <summary>SecurityImpersonation level for DuplicateTokenEx.</summary>
    internal const int SECURITY_IMPERSONATION = 2;

    // ── Token types ──────────────────────────────────────────────────────

    /// <summary>Primary token type for DuplicateTokenEx.</summary>
    internal const int TOKEN_PRIMARY = 1;

    // ── Logon flags for CreateProcessWithTokenW ──────────────────────────

    /// <summary>Log on, load the user profile in HKEY_USERS, then create the process.</summary>
    internal const int LOGON_WITH_PROFILE = 1;

    /// <summary>Log on, but use the specified credentials on the network only.</summary>
    internal const int LOGON_NETCREDENTIALS_ONLY = 2;

    // ── Process creation flags ───────────────────────────────────────────

    /// <summary>New process has a new console.</summary>
    internal const int CREATE_NEW_CONSOLE = 0x00000010;

    /// <summary>Process is created without a console window.</summary>
    internal const int CREATE_NO_WINDOW = 0x08000000;

    /// <summary>The environment block uses Unicode characters.</summary>
    internal const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    // ── STARTUPINFO flags ────────────────────────────────────────────────

    /// <summary>The wShowWindow member contains additional information.</summary>
    internal const int STARTF_USESHOWWINDOW = 0x00000001;

    /// <summary>The hStdInput, hStdOutput, and hStdError members contain additional information.</summary>
    internal const int STARTF_USESTDHANDLES = 0x00000100;

    // ── ShowWindow commands ──────────────────────────────────────────────

    /// <summary>Hides the window.</summary>
    internal const short SW_HIDE = 0;

    /// <summary>Activates and displays a window in its normal size.</summary>
    internal const short SW_SHOWNORMAL = 1;

    /// <summary>Activates the window and displays it as minimized.</summary>
    internal const short SW_SHOWMINIMIZED = 2;

    /// <summary>Activates the window and displays it as maximized.</summary>
    internal const short SW_SHOWMAXIMIZED = 3;

    // ── Wait constants ───────────────────────────────────────────────────

    /// <summary>The object is signaled.</summary>
    internal const int WAIT_OBJECT_0 = 0;

    /// <summary>The time-out interval elapsed.</summary>
    internal const int WAIT_TIMEOUT = 0x00000102;

    /// <summary>Infinite timeout.</summary>
    internal const int INFINITE = unchecked((int)0xFFFFFFFF);
}
