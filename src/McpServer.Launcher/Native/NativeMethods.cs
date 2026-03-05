using System.Runtime.InteropServices;

namespace McpServer.Launcher.Native;

/// <summary>
/// P/Invoke declarations for Windows process and token APIs.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>
    /// Creates a new process and its primary thread, running in the security context of the specified token.
    /// Uses DllImport because STARTUPINFO contains string fields not supported by LibraryImport source generation.
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
    [LibraryImport("advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool OpenProcessToken(
        IntPtr processHandle,
        int desiredAccess,
        out IntPtr tokenHandle);

    /// <summary>
    /// Creates a new access token that duplicates an existing token.
    /// </summary>
    [LibraryImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DuplicateTokenEx(
        IntPtr hExistingToken,
        int dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int impersonationLevel,
        int tokenType,
        out IntPtr phNewToken);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr hObject);

    /// <summary>
    /// Returns a pseudo-handle for the current process.
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
    internal static partial IntPtr GetCurrentProcess();

    /// <summary>
    /// Waits until the specified object is in the signaled state or the time-out interval elapses.
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "WaitForSingleObject", SetLastError = true)]
    internal static partial int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

    /// <summary>
    /// Retrieves the termination status of the specified process.
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetExitCodeProcess(IntPtr hProcess, out int lpExitCode);

    /// <summary>
    /// Retrieves the session identifier of the console session (the interactive desktop).
    /// </summary>
    [LibraryImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
    internal static partial int WTSGetActiveConsoleSessionId();

    /// <summary>
    /// Obtains the primary access token of the logged-on user for the specified session.
    /// Requires the caller to have the SE_TCB_NAME privilege (LocalSystem has this).
    /// </summary>
    [DllImport("wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WTSQueryUserToken(int sessionId, out IntPtr phToken);
}
