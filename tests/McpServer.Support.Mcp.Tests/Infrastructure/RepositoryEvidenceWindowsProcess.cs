using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Tests.Infrastructure;

internal static partial class RepositoryEvidenceTestSupport
{
    private static Action? s_afterWindowsProcessCreationBeforeAssignment = null;

    /// <summary>
    /// Owns a Windows process created suspended and assigned to a kill-on-close job before its
    /// initial thread is allowed to execute.
    /// </summary>
    private sealed class WindowsSuspendedProcess : IDisposable
    {
        private const uint CreateSuspended = 0x00000004;
        private const uint CreateNoWindow = 0x08000000;
        private const uint StartfUseStdHandles = 0x00000100;
        private const uint DuplicateSameAccess = 0x00000002;
        private const uint JobObjectLimitKillOnJobClose = 0x00002000;
        private const int JobObjectBasicAccountingInformationClass = 1;
        private const int JobObjectExtendedLimitInformationClass = 9;
        private const uint StillActive = 259;

        private readonly SafeFileHandle _job;
        private readonly SafeFileHandle _process;
        private readonly SafeFileHandle _thread;

        private WindowsSuspendedProcess(
            SafeFileHandle job,
            SafeFileHandle process,
            SafeFileHandle thread)
        {
            _job = job;
            _process = process;
            _thread = thread;
        }

        /// <summary>
        /// Runs a Windows command whose complete process tree is contained before user code starts.
        /// </summary>
        [SupportedOSPlatform("windows")]
        internal static async Task<RepositoryProcessResult> RunAsync(
            string workingDirectory,
            string fileName,
            IReadOnlyList<string> arguments,
            string? standardInput,
            TimeSpan timeout)
        {
            var prefix = Path.Combine(
                Path.GetTempPath(),
                "mcp-process-" + Guid.NewGuid().ToString("N"));
            var inputPath = prefix + ".stdin";
            var outputPath = prefix + ".stdout";
            var errorPath = prefix + ".stderr";
            try
            {
                await using var input = new FileStream(
                    inputPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                await using var output = new FileStream(
                    outputPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                await using var error = new FileStream(
                    errorPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                if (standardInput is not null)
                {
                    var inputBytes = Encoding.UTF8.GetBytes(standardInput);
                    await input.WriteAsync(inputBytes).ConfigureAwait(false);
                    await input.FlushAsync().ConfigureAwait(false);
                    input.Position = 0;
                }

                using var childInput = DuplicateInheritable(input.SafeFileHandle);
                using var childOutput = DuplicateInheritable(output.SafeFileHandle);
                using var childError = DuplicateInheritable(error.SafeFileHandle);
                using var process = Start(
                    Path.GetFullPath(workingDirectory),
                    fileName,
                    arguments,
                    childInput,
                    childOutput,
                    childError);

                if (!await process.WaitForEmptyJobAsync(timeout).ConfigureAwait(false))
                {
                    process.Terminate();
                    var cleaned = await process
                        .WaitForEmptyJobAsync(TimeSpan.FromSeconds(2))
                        .ConfigureAwait(false);
                    throw new TimeoutException(
                        cleaned
                            ? $"Process '{fileName}' exceeded the {timeout} timeout."
                            : $"Process '{fileName}' exceeded the {timeout} timeout; " +
                              "its Windows job did not report empty within the finite cleanup bound.");
                }

                var exitCode = process.GetExitCode();
                output.Position = 0;
                using var outputBytes = new MemoryStream();
                await output.CopyToAsync(outputBytes).ConfigureAwait(false);
                error.Position = 0;
                using var errorReader = new StreamReader(
                    error,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    leaveOpen: true);
                var standardError = await errorReader.ReadToEndAsync().ConfigureAwait(false);
                return new RepositoryProcessResult(
                    exitCode,
                    outputBytes.ToArray(),
                    standardError);
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
                File.Delete(errorPath);
            }
        }

        /// <summary>
        /// Creates the process suspended, assigns it to the job, and only then resumes execution.
        /// </summary>
        [SupportedOSPlatform("windows")]
        internal static WindowsSuspendedProcess Start(
            string workingDirectory,
            string fileName,
            IReadOnlyList<string> arguments,
            SafeFileHandle standardInput,
            SafeFileHandle standardOutput,
            SafeFileHandle standardError)
        {
            var job = CreateConfiguredJob();
            SafeFileHandle? processHandle = null;
            SafeFileHandle? threadHandle = null;
            try
            {
                var startup = new StartupInfo
                {
                    Size = (uint)Marshal.SizeOf<StartupInfo>(),
                    Flags = StartfUseStdHandles,
                    StandardInput = standardInput.DangerousGetHandle(),
                    StandardOutput = standardOutput.DangerousGetHandle(),
                    StandardError = standardError.DangerousGetHandle(),
                };
                var commandLine = new StringBuilder(BuildCommandLine(fileName, arguments));
                if (!CreateProcess(
                        applicationName: null,
                        commandLine,
                        processAttributes: IntPtr.Zero,
                        threadAttributes: IntPtr.Zero,
                        inheritHandles: true,
                        creationFlags: CreateSuspended | CreateNoWindow,
                        environment: IntPtr.Zero,
                        currentDirectory: workingDirectory,
                        ref startup,
                        out var processInformation))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }

                processHandle = new SafeFileHandle(
                    processInformation.ProcessHandle,
                    ownsHandle: true);
                threadHandle = new SafeFileHandle(
                    processInformation.ThreadHandle,
                    ownsHandle: true);
                Volatile.Read(ref s_afterWindowsProcessCreationBeforeAssignment)?.Invoke();
                if (!AssignProcessToJobObject(job, processHandle))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                if (ResumeThread(threadHandle) == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());

                return new WindowsSuspendedProcess(
                    job,
                    processHandle,
                    threadHandle);
            }
            catch
            {
                if (processHandle is not null && !processHandle.IsInvalid)
                    _ = TerminateProcess(processHandle, 1);
                threadHandle?.Dispose();
                processHandle?.Dispose();
                job.Dispose();
                throw;
            }
        }

        [SupportedOSPlatform("windows")]
        private static SafeFileHandle DuplicateInheritable(SafeFileHandle source)
        {
            var currentProcess = GetCurrentProcess();
            if (!DuplicateHandle(
                    currentProcess,
                    source,
                    currentProcess,
                    out var duplicate,
                    desiredAccess: 0,
                    inheritHandle: true,
                    DuplicateSameAccess))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return duplicate;
        }

        [SupportedOSPlatform("windows")]
        private static SafeFileHandle CreateConfiguredJob()
        {
            var job = CreateJobObject(IntPtr.Zero, null);
            if (job.IsInvalid)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            try
            {
                var information = new JobObjectExtendedLimitInformation
                {
                    BasicLimitInformation = new JobObjectBasicLimitInformation
                    {
                        LimitFlags = JobObjectLimitKillOnJobClose,
                    },
                };
                var size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
                var pointer = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(information, pointer, fDeleteOld: false);
                    if (!SetInformationJobObject(
                            job,
                            JobObjectExtendedLimitInformationClass,
                            pointer,
                            (uint)size))
                    {
                        throw new Win32Exception(Marshal.GetLastPInvokeError());
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                }

                return job;
            }
            catch
            {
                job.Dispose();
                throw;
            }
        }

        [SupportedOSPlatform("windows")]
        private async Task<bool> WaitForEmptyJobAsync(TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            do
            {
                if (GetActiveProcessCount() == 0)
                    return true;
                await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            }
            while (stopwatch.Elapsed < timeout);

            return GetActiveProcessCount() == 0;
        }

        [SupportedOSPlatform("windows")]
        private uint GetActiveProcessCount()
        {
            if (!QueryInformationJobObject(
                    _job,
                    JobObjectBasicAccountingInformationClass,
                    out JobObjectBasicAccountingInformation information,
                    (uint)Marshal.SizeOf<JobObjectBasicAccountingInformation>(),
                    out _))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return information.ActiveProcesses;
        }

        [SupportedOSPlatform("windows")]
        private int GetExitCode()
        {
            if (!GetExitCodeProcess(_process, out var exitCode))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            if (exitCode == StillActive)
                throw new InvalidOperationException("The direct Windows process is still active.");
            return unchecked((int)exitCode);
        }

        [SupportedOSPlatform("windows")]
        private void Terminate()
        {
            if (!TerminateJobObject(_job, 1))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        private static string BuildCommandLine(
            string fileName,
            IReadOnlyList<string> arguments) =>
            string.Join(
                ' ',
                new[] { fileName }
                    .Concat(arguments)
                    .Select(QuoteWindowsArgument));

        private static string QuoteWindowsArgument(string argument)
        {
            if (argument.Length > 0 &&
                !argument.Any(character =>
                    char.IsWhiteSpace(character) || character == '"'))
            {
                return argument;
            }

            var quoted = new StringBuilder(argument.Length + 2);
            quoted.Append('"');
            var backslashes = 0;
            foreach (var character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    quoted.Append('\\', (backslashes * 2) + 1);
                    quoted.Append('"');
                    backslashes = 0;
                    continue;
                }

                quoted.Append('\\', backslashes);
                backslashes = 0;
                quoted.Append(character);
            }

            quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _thread.Dispose();
            _process.Dispose();
            _job.Dispose();
        }

        [DllImport(
            "kernel32.dll",
            EntryPoint = "CreateProcessW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcess(
            string? applicationName,
            StringBuilder commandLine,
            IntPtr processAttributes,
            IntPtr threadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
            uint creationFlags,
            IntPtr environment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);

        [DllImport(
            "kernel32.dll",
            EntryPoint = "CreateJobObjectW",
            CharSet = CharSet.Unicode,
            SetLastError = true)]
        private static extern SafeFileHandle CreateJobObject(
            IntPtr jobAttributes,
            string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetInformationJobObject(
            SafeFileHandle job,
            int informationClass,
            IntPtr information,
            uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryInformationJobObject(
            SafeFileHandle job,
            int informationClass,
            out JobObjectBasicAccountingInformation information,
            uint informationLength,
            out uint returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(
            SafeFileHandle job,
            SafeFileHandle process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(SafeFileHandle thread);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateJobObject(
            SafeFileHandle job,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(
            SafeFileHandle process,
            uint exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(
            SafeFileHandle process,
            out uint exitCode);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateHandle(
            IntPtr sourceProcess,
            SafeFileHandle sourceHandle,
            IntPtr targetProcess,
            out SafeFileHandle targetHandle,
            uint desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            uint options);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo
        {
            internal uint Size;
            internal string? Reserved;
            internal string? Desktop;
            internal string? Title;
            internal uint X;
            internal uint Y;
            internal uint XSize;
            internal uint YSize;
            internal uint XCountChars;
            internal uint YCountChars;
            internal uint FillAttribute;
            internal uint Flags;
            internal ushort ShowWindow;
            internal ushort Reserved2;
            internal IntPtr Reserved2Pointer;
            internal IntPtr StandardInput;
            internal IntPtr StandardOutput;
            internal IntPtr StandardError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            internal IntPtr ProcessHandle;
            internal IntPtr ThreadHandle;
            internal uint ProcessId;
            internal uint ThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicAccountingInformation
        {
            internal long TotalUserTime;
            internal long TotalKernelTime;
            internal long ThisPeriodTotalUserTime;
            internal long ThisPeriodTotalKernelTime;
            internal uint TotalPageFaultCount;
            internal uint TotalProcesses;
            internal uint ActiveProcesses;
            internal uint TotalTerminatedProcesses;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            internal long PerProcessUserTimeLimit;
            internal long PerJobUserTimeLimit;
            internal uint LimitFlags;
            internal UIntPtr MinimumWorkingSetSize;
            internal UIntPtr MaximumWorkingSetSize;
            internal uint ActiveProcessLimit;
            internal UIntPtr Affinity;
            internal uint PriorityClass;
            internal uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            internal ulong ReadOperationCount;
            internal ulong WriteOperationCount;
            internal ulong OtherOperationCount;
            internal ulong ReadTransferCount;
            internal ulong WriteTransferCount;
            internal ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            internal JobObjectBasicLimitInformation BasicLimitInformation;
            internal IoCounters IoInfo;
            internal UIntPtr ProcessMemoryLimit;
            internal UIntPtr JobMemoryLimit;
            internal UIntPtr PeakProcessMemoryUsed;
            internal UIntPtr PeakJobMemoryUsed;
        }
    }
}
