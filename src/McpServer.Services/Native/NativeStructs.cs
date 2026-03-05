using System.Runtime.InteropServices;

namespace McpServer.Support.Mcp.Native;

/// <summary>
/// Native Windows structures for process creation and management.
/// </summary>
internal static class NativeStructs
{
    /// <summary>
    /// Specifies the window station, desktop, standard handles, and appearance of a process at creation time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        /// <summary>Size of the structure in bytes.</summary>
        public int cb;

        /// <summary>Reserved.</summary>
        public string? lpReserved;

        /// <summary>Name of the desktop or window station.</summary>
        public string? lpDesktop;

        /// <summary>Title for console processes.</summary>
        public string? lpTitle;

        /// <summary>X offset (requires STARTF_USEPOSITION).</summary>
        public int dwX;

        /// <summary>Y offset (requires STARTF_USEPOSITION).</summary>
        public int dwY;

        /// <summary>Window width (requires STARTF_USESIZE).</summary>
        public int dwXSize;

        /// <summary>Window height (requires STARTF_USESIZE).</summary>
        public int dwYSize;

        /// <summary>Screen buffer width.</summary>
        public int dwXCountChars;

        /// <summary>Screen buffer height.</summary>
        public int dwYCountChars;

        /// <summary>Initial text and background colors.</summary>
        public int dwFillAttribute;

        /// <summary>Bit field determining which members are used.</summary>
        public int dwFlags;

        /// <summary>Window show state (SW_* value).</summary>
        public short wShowWindow;

        /// <summary>Reserved; must be zero.</summary>
        public short cbReserved2;

        /// <summary>Reserved; must be null.</summary>
        public IntPtr lpReserved2;

        /// <summary>Standard input handle.</summary>
        public IntPtr hStdInput;

        /// <summary>Standard output handle.</summary>
        public IntPtr hStdOutput;

        /// <summary>Standard error handle.</summary>
        public IntPtr hStdError;
    }

    /// <summary>
    /// Information about a newly created process and its primary thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        /// <summary>Handle to the newly created process.</summary>
        public IntPtr hProcess;

        /// <summary>Handle to the primary thread.</summary>
        public IntPtr hThread;

        /// <summary>Process identifier.</summary>
        public int dwProcessId;

        /// <summary>Primary thread identifier.</summary>
        public int dwThreadId;
    }

    /// <summary>
    /// Security descriptor and handle inheritance settings.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        /// <summary>Size of the structure in bytes.</summary>
        public int nLength;

        /// <summary>Pointer to a SECURITY_DESCRIPTOR structure.</summary>
        public IntPtr lpSecurityDescriptor;

        /// <summary>Whether the returned handle is inherited.</summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;
    }
}
