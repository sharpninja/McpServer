using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Services;

/// <content>
/// TEST-MCP-USECASE-020: Windows CREATE_SUSPENDED plus kill-on-close job assignment before resume.
/// </content>
public sealed partial class ProcessRunner
{
    private const uint CreateSuspended = 0x00000004;
    private const uint CreateNoWindow = 0x08000000;
    private const uint CreateUnicodeEnvironment = 0x00000400;
    private const uint StartfUseStdHandles = 0x00000100;
    private const uint DuplicateSameAccess = 0x00000002;
    private const uint JobObjectLimitKillOnJobClose = 0x00002000;
    private const int JobObjectBasicAccountingInformationClass = 1;
    private const int JobObjectExtendedLimitInformationClass = 9;
    private const uint StillActive = 259;

#pragma warning disable CS0649 // Tests assign this hook through reflection.
    private static Action? s_afterWindowsProcessCreationBeforeAssignment;
#pragma warning restore CS0649

    /// <summary>
    /// Runs the resolved Windows process inside a job assigned before the initial thread resumes.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private async Task<ProcessRunResult> RunWindowsJobContainedAsync(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workingDirectory = string.IsNullOrWhiteSpace(startInfo.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(startInfo.WorkingDirectory);
        var commandLine = startInfo.ArgumentList.Count > 0
            ? BuildCommandLine(startInfo.FileName, startInfo.ArgumentList)
            : QuoteWindowsArgument(startInfo.FileName)
                + (string.IsNullOrWhiteSpace(startInfo.Arguments)
                    ? string.Empty
                    : " " + startInfo.Arguments);

        using var process = WindowsSuspendedProcess.Start(
            workingDirectory,
            commandLine,
            startInfo.Environment);
        try
        {
            if (!await process.WaitForEmptyJobAsync(cancellationToken).ConfigureAwait(false))
            {
                process.Terminate();
                _ = await process
                    .WaitForEmptyJobAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(false);
                throw new TimeoutException(
                    $"Process '{startInfo.FileName}' exceeded the caller cancellation timeout.");
            }

            var stderr = process.ReadStandardError();
            return new ProcessRunResult(
                process.GetExitCode(),
                process.ReadStandardOutput(),
                string.IsNullOrWhiteSpace(stderr) ? null : stderr);
        }
        catch (OperationCanceledException)
        {
            process.Terminate();
            try
            {
                _ = await process
                    .WaitForEmptyJobAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(2))
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }

            throw new TimeoutException(
                $"Process '{startInfo.FileName}' exceeded the caller cancellation timeout.");
        }
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

    /// <summary>
    /// Owns a Windows process created suspended and assigned to a kill-on-close job before its
    /// initial thread is allowed to execute.
    /// </summary>
    private sealed class WindowsSuspendedProcess : IDisposable
    {
        private readonly SafeFileHandle _job;
        private readonly SafeFileHandle _process;
        private readonly SafeFileHandle _thread;
        private readonly FileStream _stdin;
        private readonly FileStream _stdout;
        private readonly FileStream _stderr;
        private readonly SafeFileHandle _childInput;
        private readonly SafeFileHandle _childOutput;
        private readonly SafeFileHandle _childError;
        private readonly string _stdoutPath;
        private readonly string _stderrPath;
        private readonly string _stdinPath;

        private WindowsSuspendedProcess(
            SafeFileHandle job,
            SafeFileHandle process,
            SafeFileHandle thread,
            FileStream stdin,
            FileStream stdout,
            FileStream stderr,
            SafeFileHandle childInput,
            SafeFileHandle childOutput,
            SafeFileHandle childError,
            string stdoutPath,
            string stderrPath,
            string stdinPath)
        {
            _job = job;
            _process = process;
            _thread = thread;
            _stdin = stdin;
            _stdout = stdout;
            _stderr = stderr;
            _childInput = childInput;
            _childOutput = childOutput;
            _childError = childError;
            _stdoutPath = stdoutPath;
            _stderrPath = stderrPath;
            _stdinPath = stdinPath;
        }

        /// <summary>
        /// Creates the process suspended, assigns it to the job, then resumes the initial thread.
        /// </summary>
        [SupportedOSPlatform("windows")]
        internal static WindowsSuspendedProcess Start(
            string workingDirectory,
            string commandLine,
            IDictionary<string, string?> environment)
        {
            var prefix = Path.Combine(
                Path.GetTempPath(),
                "mcp-process-" + Guid.NewGuid().ToString("N"));
            var inputPath = prefix + ".stdin";
            var outputPath = prefix + ".stdout";
            var errorPath = prefix + ".stderr";
            FileStream? input = null;
            FileStream? output = null;
            FileStream? error = null;
            SafeFileHandle? childInput = null;
            SafeFileHandle? childOutput = null;
            SafeFileHandle? childError = null;
            IntPtr environmentBlock = IntPtr.Zero;
            try
            {
                input = new FileStream(
                    inputPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                output = new FileStream(
                    outputPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                error = new FileStream(
                    errorPath,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete);
                childInput = DuplicateInheritable(input.SafeFileHandle);
                childOutput = DuplicateInheritable(output.SafeFileHandle);
                childError = DuplicateInheritable(error.SafeFileHandle);
                environmentBlock = CreateEnvironmentBlock(environment);

                var job = CreateConfiguredJob();
                SafeFileHandle? processHandle = null;
                SafeFileHandle? threadHandle = null;
                try
                {
                    var startup = new StartupInfo
                    {
                        Size = (uint)Marshal.SizeOf<StartupInfo>(),
                        Flags = StartfUseStdHandles,
                        StandardInput = childInput!.DangerousGetHandle(),
                        StandardOutput = childOutput!.DangerousGetHandle(),
                        StandardError = childError!.DangerousGetHandle(),
                    };
                    var commandLineBuilder = new StringBuilder(commandLine);
                    if (!CreateProcess(
                            applicationName: null,
                            commandLineBuilder,
                            processAttributes: IntPtr.Zero,
                            threadAttributes: IntPtr.Zero,
                            inheritHandles: true,
                            creationFlags: CreateSuspended | CreateNoWindow | CreateUnicodeEnvironment,
                            environment: environmentBlock,
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

                    var owned = new WindowsSuspendedProcess(
                        job,
                        processHandle,
                        threadHandle,
                        input,
                        output,
                        error,
                        childInput,
                        childOutput,
                        childError,
                        outputPath,
                        errorPath,
                        inputPath);
                    input = null;
                    output = null;
                    error = null;
                    childInput = null;
                    childOutput = null;
                    childError = null;
                    return owned;
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
            finally
            {
                if (environmentBlock != IntPtr.Zero)
                    Marshal.FreeHGlobal(environmentBlock);
                childInput?.Dispose();
                childOutput?.Dispose();
                childError?.Dispose();
                input?.Dispose();
                output?.Dispose();
                error?.Dispose();
            }
        }

        [SupportedOSPlatform("windows")]
        internal async Task<bool> WaitForEmptyJobAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (GetActiveProcessCount() == 0)
                    return true;
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
            }
        }

        [SupportedOSPlatform("windows")]
        internal uint GetActiveProcessCount()
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
        internal int GetExitCode()
        {
            if (!GetExitCodeProcess(_process, out var exitCode))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            if (exitCode == StillActive)
                throw new InvalidOperationException("The direct Windows process is still active.");
            return unchecked((int)exitCode);
        }

        [SupportedOSPlatform("windows")]
        internal void Terminate()
        {
            if (!TerminateJobObject(_job, 1))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        internal string ReadStandardOutput()
        {
            _stdout.Flush();
            _stdout.Position = 0;
            using var reader = new StreamReader(_stdout, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return reader.ReadToEnd();
        }

        internal string ReadStandardError()
        {
            _stderr.Flush();
            _stderr.Position = 0;
            using var reader = new StreamReader(_stderr, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return reader.ReadToEnd();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _thread.Dispose();
            _process.Dispose();
            _job.Dispose();
            _childInput.Dispose();
            _childOutput.Dispose();
            _childError.Dispose();
            _stdin.Dispose();
            _stdout.Dispose();
            _stderr.Dispose();
            TryDelete(_stdoutPath);
            TryDelete(_stderrPath);
            TryDelete(_stdinPath);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
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

        private static IntPtr CreateEnvironmentBlock(IDictionary<string, string?> environment)
        {
            var builder = new StringBuilder();
            foreach (var pair in environment)
            {
                if (pair.Value is null)
                    continue;
                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value);
                builder.Append('\0');
            }

            builder.Append('\0');
            var bytes = Encoding.Unicode.GetBytes(builder.ToString());
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
            return pointer;
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
