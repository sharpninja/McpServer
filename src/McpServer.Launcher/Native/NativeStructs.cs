using System.Runtime.InteropServices;

namespace McpServer.Launcher.Native;

/// <summary>
/// Native Windows structures for process creation and management.
/// </summary>
internal static class NativeStructs
{
    /// <summary>
    /// Contains information used to specify the window station, desktop, standard handles,
    /// and appearance of the main window for a process at creation time.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct STARTUPINFO
    {
        /// <summary>Size of the structure in bytes.</summary>
        public int cb;

        /// <summary>Reserved; must be null.</summary>
        public string? lpReserved;

        /// <summary>Name of the desktop or window station.</summary>
        public string? lpDesktop;

        /// <summary>Title displayed in the title bar for console processes.</summary>
        public string? lpTitle;

        /// <summary>Ignored unless <see cref="dwFlags"/> includes STARTF_USEPOSITION.</summary>
        public int dwX;

        /// <summary>Ignored unless <see cref="dwFlags"/> includes STARTF_USEPOSITION.</summary>
        public int dwY;

        /// <summary>Ignored unless <see cref="dwFlags"/> includes STARTF_USESIZE.</summary>
        public int dwXSize;

        /// <summary>Ignored unless <see cref="dwFlags"/> includes STARTF_USESIZE.</summary>
        public int dwYSize;

        /// <summary>Screen buffer width in character columns.</summary>
        public int dwXCountChars;

        /// <summary>Screen buffer height in character rows.</summary>
        public int dwYCountChars;

        /// <summary>Initial text and background colors for a console window.</summary>
        public int dwFillAttribute;

        /// <summary>Bit field that determines which members are used.</summary>
        public int dwFlags;

        /// <summary>Window show state (SW_* value) when <see cref="dwFlags"/> includes STARTF_USESHOWWINDOW.</summary>
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
    /// Contains information about a newly created process and its primary thread.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        /// <summary>Handle to the newly created process.</summary>
        public IntPtr hProcess;

        /// <summary>Handle to the primary thread of the new process.</summary>
        public IntPtr hThread;

        /// <summary>Identifier for the new process.</summary>
        public int dwProcessId;

        /// <summary>Identifier for the primary thread of the new process.</summary>
        public int dwThreadId;
    }

    /// <summary>
    /// Contains the security descriptor for an object and specifies handle inheritance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        /// <summary>Size of the structure in bytes.</summary>
        public int nLength;

        /// <summary>Pointer to a SECURITY_DESCRIPTOR structure.</summary>
        public IntPtr lpSecurityDescriptor;

        /// <summary>Whether the returned handle is inherited when a new process is created.</summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;
    }
}
