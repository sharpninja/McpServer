using System.Runtime.InteropServices;

namespace McpServer.Support.Mcp.Native;

/// <summary>
/// P/Invoke declarations for Windows process creation, token, and terminal services APIs.
/// Used by <see cref="DesktopProcessLauncher"/> for interactive desktop process launches.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// Creates a new process running in the security context of the specified token.
    /// </summary>
    [DllImport("advapi32.dll", EntryPoint = "CreateProcessWithTokenW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcessWithTokenW(
        IntPtr hToken,
        int dwLogonFlags,
        string? lpApplicationName,
        string? lpCommandLine,
        int dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref NativeStructs.STARTUPINFO lpStartupInfo,
        out NativeStructs.PROCESS_INFORMATION lpProcessInformation);

    /// <summary>
    /// Opens the access token associated with a process.
    /// </summary>
    [DllImport("advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenProcessToken(
        IntPtr processHandle,
        int desiredAccess,
        out IntPtr tokenHandle);

    /// <summary>
    /// Creates a new access token that duplicates an existing token.
    /// </summary>
    [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        int dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int impersonationLevel,
        int tokenType,
        out IntPtr phNewToken);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Returns a pseudo-handle for the current process.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
    internal static extern IntPtr GetCurrentProcess();

    /// <summary>
    /// Creates an anonymous pipe.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "CreatePipe", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreatePipe(
        out IntPtr hReadPipe,
        out IntPtr hWritePipe,
        ref NativeStructs.SECURITY_ATTRIBUTES lpPipeAttributes,
        int nSize);

    /// <summary>
    /// Sets the inheritance of the specified handle.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "SetHandleInformation", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);

    /// <summary>
    /// Waits until the specified object is in the signaled state or the time-out interval elapses.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    internal static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

    /// <summary>
    /// Retrieves the termination status of the specified process.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

    /// <summary>
    /// Retrieves the session identifier of the console session (the interactive desktop).
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
    internal static extern int WTSGetActiveConsoleSessionId();

    /// <summary>
    /// Obtains the primary access token of the logged-on user for the specified session.
    /// Requires the caller to have the SE_TCB_NAME privilege (LocalSystem has this).
    /// </summary>
    [DllImport("wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSQueryUserToken(int sessionId, out IntPtr phToken);

    /// <summary>
    /// Sets information for an access token.
    /// Used to assign the token to an interactive desktop session via <c>TokenSessionId</c>.
    /// </summary>
    [DllImport("advapi32.dll", EntryPoint = "SetTokenInformation", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        ref int tokenInformation,
        int tokenInformationLength);
}
