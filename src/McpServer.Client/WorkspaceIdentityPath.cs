using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Client;

/// <summary>Filesystem semantics used to compare canonical workspace identities.</summary>
public enum WorkspacePathPlatform
{
    /// <summary>Windows paths compare without case and use backslash separators.</summary>
    Windows,

    /// <summary>Case-sensitive paths preserve case and slash semantics.</summary>
    CaseSensitive,
}

/// <summary>
/// Repository-wide canonical workspace path identity policy. Synchronous entry points are purely
/// lexical and never perform filesystem I/O. Cancellable asynchronous entry points resolve the deepest
/// existing ancestor to its physical target and append any nonexistent suffix lexically, so symbolic-link,
/// junction, mapped-drive, UNC, and device aliases retain one physical identity where required.
/// Persisted opaque identifiers are not filesystem paths and must not use this type.
/// </summary>
public static class WorkspaceIdentityPath
{
    private const string WindowsPrefix = "windows:";
    private const string CaseSensitivePrefix = "case-sensitive:";
    private const uint FileListDirectory = 0x00000001;
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint OpenExisting = 3;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const int ErrorFileNotFound = 2;
    private const int ErrorPathNotFound = 3;
    private const int MaxAncestorDepth = 512;
    private const uint ThreadTerminate = 0x0001;

    private static readonly TimeSpan s_nativeCleanupTimeout = TimeSpan.FromMilliseconds(250);
    private static Action? s_beforeUnixPhysicalResolution = null;
    private static Action? s_beforeWindowsNativeOpen = null;
    private static Action? s_beforeWindowsWorkerExit = null;
    private static Func<ProcessStartInfo, Process>? s_unixResolverProcessFactory = null;
    private static Func<Process, bool>? s_unixResolverTerminationOverride = null;
    private static int s_activeNativeResolverWorkers;
    private static int s_nativeResolverInvocationCount;
    private static Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>>?
        s_physicalPathResolverOverride = null;

    /// <summary>Comparer implementing repository-wide lexical workspace identity semantics.</summary>
    public static IEqualityComparer<string> Comparer { get; } = new CanonicalWorkspacePathComparer();

    /// <summary>
    /// Returns a canonical, case-preserving lexical workspace path without filesystem I/O.
    /// </summary>
    public static string NormalizePath(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var trimmed = workspacePath.Trim();
        return NormalizePath(trimmed, DetectPlatform(trimmed));
    }

    /// <summary>
    /// Returns a canonical path under an explicit platform policy without consulting the filesystem.
    /// Use <see cref="ResolveAsync"/> when physical alias resolution is required.
    /// </summary>
    public static string NormalizePath(
        string workspacePath,
        WorkspacePathPlatform platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var trimmed = workspacePath.Trim();
        var nativeInput = platform == WorkspacePathPlatform.Windows &&
                          OperatingSystem.IsWindows()
            ? NormalizeLocalAdministrativeShareAlias(trimmed)
            : trimmed;
        var lexicalInput = IsOpaqueWorkspaceIdentifier(nativeInput)
            ? nativeInput
            : IsNativePlatform(platform)
                ? Path.GetFullPath(nativeInput)
                : nativeInput;
        return NormalizeLexicalPath(lexicalInput, platform);
    }

    /// <summary>
    /// Returns deterministic lexical identity without consulting the filesystem. Root separators
    /// are retained, dot segments are collapsed, and non-native syntax is handled explicitly.
    /// </summary>
    public static string NormalizeLexicalPath(
        string workspacePath,
        WorkspacePathPlatform platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var trimmed = workspacePath.Trim();
        return platform == WorkspacePathPlatform.Windows
            ? NormalizeWindowsLexicalPath(trimmed)
            : NormalizeCaseSensitiveLexicalPath(trimmed);
    }

    /// <summary>Returns an empty identity for blank input; otherwise returns the canonical path.</summary>
    public static string NormalizePathOrEmpty(string? workspacePath) =>
        string.IsNullOrWhiteSpace(workspacePath)
            ? string.Empty
            : NormalizePath(workspacePath);

    /// <summary>Returns the durable identity key selected from the path's syntax.</summary>
    public static string GetStorageKey(string workspacePath)
    {
        if (string.IsNullOrEmpty(workspacePath))
            return string.Empty;

        var platform = DetectPlatform(workspacePath);
        return GetStorageKey(workspacePath, platform);
    }

    /// <summary>Returns a durable identity key using an explicit path platform.</summary>
    public static string GetStorageKey(string workspacePath, WorkspacePathPlatform platform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var normalized = NormalizePath(workspacePath, platform);
        return BuildStorageKey(normalized, platform);
    }

    /// <summary>
    /// Resolves the canonical path and durable storage key while bounding native physical work.
    /// </summary>
    /// <param name="workspacePath">Workspace path to normalize.</param>
    /// <param name="platform">Path syntax and comparison policy.</param>
    /// <param name="timeout">Finite upper bound for physical-path resolution.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The canonical path and corresponding durable storage key.</returns>
    /// <exception cref="TimeoutException">Physical resolution exceeded <paramref name="timeout"/>.</exception>
    public static async Task<(string NormalizedPath, string StorageKey)> ResolveAsync(
        string workspacePath,
        WorkspacePathPlatform platform,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), "A finite positive timeout is required.");

        cancellationToken.ThrowIfCancellationRequested();
        var trimmed = workspacePath.Trim();
        var nativeInput = platform == WorkspacePathPlatform.Windows &&
                          OperatingSystem.IsWindows()
            ? NormalizeLocalAdministrativeShareAlias(trimmed)
            : trimmed;
        var isOpaque = IsOpaqueWorkspaceIdentifier(nativeInput);
        var lexicalInput = isOpaque
            ? nativeInput
            : IsNativePlatform(platform)
                ? Path.GetFullPath(nativeInput)
                : nativeInput;
        var lexical = NormalizeLexicalPath(lexicalInput, platform);
        if (isOpaque)
            return (lexical, BuildStorageKey(lexical, platform));

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            var resolver = s_physicalPathResolverOverride;
            string? resolved;
            if (resolver is not null)
            {
                resolved = await resolver(lexical, platform, timeoutSource.Token)
                    .WaitAsync(timeoutSource.Token)
                    .ConfigureAwait(false);
            }
            else if (!IsNativePlatform(platform))
            {
                resolved = null;
            }
            else
            {
                resolved = await ResolvePhysicalPathBoundedAsync(
                        lexical,
                        platform,
                        timeoutSource.Token)
                    .WaitAsync(timeoutSource.Token)
                    .ConfigureAwait(false);
            }

            var normalized = resolved ?? lexical;
            return (normalized, BuildStorageKey(normalized, platform));
        }
        catch (OperationCanceledException exception)
            when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "Workspace physical identity resolution was cancelled by the caller.",
                exception,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested &&
                  timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Workspace physical identity resolution exceeded {timeout}.",
                exception);
        }
    }

    /// <summary>
    /// Returns a durable identity key while bounding native physical-path resolution.
    /// </summary>
    /// <param name="workspacePath">Workspace path to normalize.</param>
    /// <param name="platform">Path syntax and comparison policy.</param>
    /// <param name="timeout">Finite upper bound for physical-path resolution.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The canonical durable storage key.</returns>
    /// <exception cref="TimeoutException">Physical resolution exceeded <paramref name="timeout"/>.</exception>
    public static async Task<string> GetStorageKeyAsync(
        string workspacePath,
        WorkspacePathPlatform platform,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var resolution = await ResolveAsync(
                workspacePath,
                platform,
                timeout,
                cancellationToken)
            .ConfigureAwait(false);
        return resolution.StorageKey;
    }

    /// <summary>
    /// Executes physical path resolution through the platform's bounded native-I/O strategy.
    /// Windows uses cancellable synchronous I/O on an observable worker; Linux uses atomic
    /// mount-contained lookup and reserves a helper process only for controlled test injection.
    /// </summary>
    private static Task<string?> ResolvePhysicalPathBoundedAsync(
        string lexicalPath,
        WorkspacePathPlatform platform,
        CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsWindows() &&
            platform == WorkspacePathPlatform.Windows)
        {
            return ResolveWindowsPhysicalPathAsync(lexicalPath, cancellationToken);
        }

        return ResolveUnixPhysicalPathAsync(lexicalPath, cancellationToken);
    }

    /// <summary>
    /// Resolves a case-sensitive path after lexical mount classification. Linux then uses an atomic
    /// <c>openat2(RESOLVE_NO_XDEV)</c> lookup so an alias replacement or mount swap cannot cross
    /// into a potentially unbounded filesystem after classification.
    /// </summary>
    private static async Task<string?> ResolveUnixPhysicalPathAsync(
        string lexicalPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();

        var workerOwnedByCall = true;
        Interlocked.Increment(ref s_activeNativeResolverWorkers);
        try
        {
            _ = BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(
                lexicalPath,
                "Workspace physical identity resolution");
            Volatile.Read(ref s_beforeUnixPhysicalResolution)?.Invoke();
            var physicalCandidate =
                BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(
                    lexicalPath,
                    "Workspace physical identity resolution");
            cancellationToken.ThrowIfCancellationRequested();

            var processFactory = Volatile.Read(ref s_unixResolverProcessFactory);
            if (processFactory is not null)
            {
                Interlocked.Increment(ref s_nativeResolverInvocationCount);
                return await ResolveUnixWithHelperAsync(
                        physicalCandidate,
                        processFactory,
                        cancellationToken,
                        () => workerOwnedByCall = false)
                    .ConfigureAwait(false);
            }

            Interlocked.Increment(ref s_nativeResolverInvocationCount);
            cancellationToken.ThrowIfCancellationRequested();
            return LinuxPhysicalPathResolver.TryResolveWithNonexistentSuffix(physicalCandidate);
        }
        finally
        {
            if (workerOwnedByCall)
                Interlocked.Decrement(ref s_activeNativeResolverWorkers);
        }
    }

    private static async Task<string?> ResolveUnixWithHelperAsync(
        string physicalCandidate,
        Func<ProcessStartInfo, Process> processFactory,
        CancellationToken cancellationToken,
        Action transferWorkerOwnership)
    {
        var startInfo = new ProcessStartInfo("realpath")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = "/",
        };
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(physicalCandidate);

        var process = processFactory(startInfo);
        ArgumentNullException.ThrowIfNull(process);
        var transferred = false;
        try
        {
            if (!HasProcessStarted(process) && !process.Start())
                throw new InvalidOperationException("Unable to start the Unix resolver helper.");

            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!await TryStopUnixResolverProcessAsync(
                        process,
                        standardOutput,
                        standardError)
                    .ConfigureAwait(false))
                {
                    transferred = true;
                    transferWorkerOwnership();
                    TrackUnixResolverUntilActuallyGone(process, standardOutput, standardError);
                }

                throw;
            }

            try
            {
                var output = await DrainUnixResolverAsync(standardOutput, standardError)
                    .ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    throw new IOException(
                        $"Unix physical path resolution failed with exit code {process.ExitCode}: " +
                        output.StandardError.Trim());
                }

                var resolved = output.StandardOutput.TrimEnd('\r', '\n');
                return string.IsNullOrWhiteSpace(resolved)
                    ? null
                    : NormalizeCaseSensitiveLexicalPath(resolved);
            }
            catch (TimeoutException exception)
            {
                transferred = true;
                transferWorkerOwnership();
                TrackUnixResolverUntilActuallyGone(process, standardOutput, standardError);
                throw new IOException(
                    "The Unix resolver helper exited but its redirected pipes remained open.",
                    exception);
            }
        }
        finally
        {
            if (!transferred)
                process.Dispose();
        }
    }

    private static bool HasProcessStarted(Process process)
    {
        try
        {
            _ = process.Id;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<bool> TryStopUnixResolverProcessAsync(
        Process process,
        Task<string> standardOutput,
        Task<string> standardError)
    {
        var terminationOverride = Volatile.Read(ref s_unixResolverTerminationOverride);
        bool terminationRequested;
        try
        {
            if (terminationOverride is not null)
            {
                terminationRequested = terminationOverride(process);
            }
            else
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                terminationRequested = true;
            }
        }
        catch (InvalidOperationException)
        {
            terminationRequested = true;
        }

        if (!terminationRequested)
            return false;

        try
        {
            await process
                .WaitForExitAsync()
                .WaitAsync(s_nativeCleanupTimeout)
                .ConfigureAwait(false);
            await Task
                .WhenAll(standardOutput, standardError)
                .WaitAsync(s_nativeCleanupTimeout)
                .ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static void TrackUnixResolverUntilActuallyGone(
        Process process,
        Task<string> standardOutput,
        Task<string> standardError)
    {
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                    await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    // A test or host disposed the observable process after confirming its exit.
                }
                finally
                {
                    process.Dispose();
                    Interlocked.Decrement(ref s_activeNativeResolverWorkers);
                }
            });
    }

    private static async Task<(string StandardOutput, string StandardError)> DrainUnixResolverAsync(
        Task<string> standardOutput,
        Task<string> standardError)
    {
        await Task
            .WhenAll(standardOutput, standardError)
            .WaitAsync(s_nativeCleanupTimeout)
            .ConfigureAwait(false);
        return (await standardOutput.ConfigureAwait(false), await standardError.ConfigureAwait(false));
    }


    /// <summary>
    /// Resolves a Windows path on a cancellable synchronous-I/O thread. Public completion and
    /// cancellation pumping are finite; if native cancellation fails, the still-running worker
    /// remains counted until the thread actually exits.
    /// </summary>
    private static async Task<string?> ResolveWindowsPhysicalPathAsync(
        string lexicalPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsRemoteWindowsPath(lexicalPath))
        {
            throw new NotSupportedException(
                "Windows physical identity resolution rejects remote UNC paths because CreateFile " +
                "cannot guarantee a finite local cancellation contract.");
        }

        var nativeThreadId = new TaskCompletionSource<uint>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var workerOutcome = new TaskCompletionSource<(
            string? Resolved,
            Exception? Failure,
            bool Cancelled)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<string?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(
            () =>
            {
                string? resolved = null;
                Exception? failure = null;
                var cancelled = false;
                try
                {
                    nativeThreadId.TrySetResult(GetCurrentThreadId());
                    cancellationToken.ThrowIfCancellationRequested();
                    resolved = TryResolveWindowsPhysicalPathWithNonexistentSuffix(
                        lexicalPath,
                        cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException exception)
                {
                    cancelled = true;
                    failure = exception;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }

                workerOutcome.TrySetResult((resolved, failure, cancelled));
                Volatile.Read(ref s_beforeWindowsWorkerExit)?.Invoke();
            })
        {
            IsBackground = true,
            Name = "McpWorkspaceIdentityResolver",
        };
        Interlocked.Increment(ref s_activeNativeResolverWorkers);
        try
        {
            thread.Start();
        }
        catch
        {
            Interlocked.Decrement(ref s_activeNativeResolverWorkers);
            throw;
        }

        Interlocked.Increment(ref s_nativeResolverInvocationCount);
        _ = TrackWindowsResolverUntilActuallyGoneAsync(
            thread,
            workerOutcome.Task,
            completion,
            cancellationToken);

        uint threadId;
        try
        {
            threadId = await nativeThreadId.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The worker checks cancellation before its first native open.
            throw;
        }

        var cancellationPump = CancelWindowsSynchronousIoWithinCleanupBoundAsync(
            threadId,
            completion.Task,
            cancellationToken);
        try
        {
            return await completion.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await cancellationPump.ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Observes a native resolver thread without blocking another worker and publishes its outcome
    /// only after the runtime reports that the real thread has exited.
    /// </summary>
    private static async Task TrackWindowsResolverUntilActuallyGoneAsync(
        Thread thread,
        Task<(string? Resolved, Exception? Failure, bool Cancelled)> workerOutcome,
        TaskCompletionSource<string?> completion,
        CancellationToken cancellationToken)
    {
        while (thread.IsAlive)
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);

        var outcome = await workerOutcome.ConfigureAwait(false);
        Interlocked.Decrement(ref s_activeNativeResolverWorkers);
        if (outcome.Cancelled)
        {
            completion.TrySetException(
                outcome.Failure ?? new OperationCanceledException(cancellationToken));
        }
        else if (outcome.Failure is not null)
        {
            completion.TrySetException(outcome.Failure);
        }
        else
        {
            completion.TrySetResult(outcome.Resolved);
        }
    }

    /// <summary>
    /// Finds the deepest existing Windows ancestor using one cancellable native open at a time.
    /// Cancellation is checked after every open, so one interrupt can never be followed by another
    /// uncancellable probe.
    /// </summary>
    private static string? TryResolveWindowsPhysicalPathWithNonexistentSuffix(
        string lexicalPath,
        CancellationToken cancellationToken)
    {
        var existingAncestor = lexicalPath;
        var suffix = new Stack<string>();
        for (var depth = 0; depth < MaxAncestorDepth; depth++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Read(ref s_beforeWindowsNativeOpen)?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            using var handle = CreateFile(
                existingAncestor,
                FileListDirectory | FileReadAttributes,
                FileShareRead | FileShareWrite | FileShareDelete,
                IntPtr.Zero,
                OpenExisting,
                FileFlagBackupSemantics,
                IntPtr.Zero);
            var error = handle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
            cancellationToken.ThrowIfCancellationRequested();
            if (!handle.IsInvalid)
            {
                var resolved = GetWindowsFinalPath(handle);
                if (resolved is null)
                {
                    throw CreateWindowsPhysicalResolutionException(
                        existingAncestor,
                        Marshal.GetLastWin32Error());
                }

                while (suffix.TryPop(out var suffixSegment))
                {
                    resolved = resolved.EndsWith((char)92)
                        ? resolved + suffixSegment
                        : resolved + (char)92 + suffixSegment;
                }

                return NormalizeWindowsLexicalPath(resolved);
            }

            if (error is not ErrorFileNotFound and not ErrorPathNotFound)
                throw CreateWindowsPhysicalResolutionException(existingAncestor, error);

            cancellationToken.ThrowIfCancellationRequested();
            using var entryHandle = CreateFile(
                existingAncestor,
                0,
                FileShareRead | FileShareWrite | FileShareDelete,
                IntPtr.Zero,
                OpenExisting,
                FileFlagBackupSemantics | FileFlagOpenReparsePoint,
                IntPtr.Zero);
            var entryError = entryHandle.IsInvalid ? Marshal.GetLastWin32Error() : 0;
            cancellationToken.ThrowIfCancellationRequested();
            if (!entryHandle.IsInvalid)
            {
                throw new IOException(
                    $"Windows path entry '{existingAncestor}' exists but its physical target " +
                    "cannot be resolved safely.");
            }

            if (entryError is not ErrorFileNotFound and not ErrorPathNotFound)
                throw CreateWindowsPhysicalResolutionException(existingAncestor, entryError);

            var parent = Path.GetDirectoryName(existingAncestor);
            var segment = Path.GetFileName(existingAncestor);
            if (string.IsNullOrEmpty(parent) ||
                string.IsNullOrEmpty(segment) ||
                string.Equals(parent, existingAncestor, StringComparison.Ordinal))
            {
                throw new IOException(
                    $"Windows physical identity resolution could not find a safe existing " +
                    $"ancestor for '{lexicalPath}'.");
            }

            suffix.Push(segment);
            existingAncestor = NormalizeWindowsLexicalPath(parent);
        }

        throw new IOException(
            $"Windows physical identity resolution exceeded {MaxAncestorDepth} ancestors for " +
            $"'{lexicalPath}'.");
    }

    /// <summary>Creates an I/O failure that preserves the native Windows error as its cause.</summary>
    private static IOException CreateWindowsPhysicalResolutionException(string path, int error) =>
        error == 0
            ? new IOException($"Windows physical identity resolution failed for '{path}'.")
            : new IOException(
                $"Windows physical identity resolution failed for '{path}' with native error {error}.",
                new System.ComponentModel.Win32Exception(error));

    /// <summary>Returns the normalized DOS or UNC path represented by an open Windows handle.</summary>
    private static string? GetWindowsFinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(512);
        var length = GetFinalPathNameByHandle(
            handle,
            buffer,
            (uint)buffer.Capacity,
            0);
        if (length == 0)
            return null;

        if (length >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)length + 1));
            length = GetFinalPathNameByHandle(
                handle,
                buffer,
                (uint)buffer.Capacity,
                0);
            if (length == 0)
                return null;
        }

        var resolved = buffer.ToString();
        const string deviceUncPrefix = @"\\?\UNC\";
        const string devicePrefix = @"\\?\";
        if (resolved.StartsWith(deviceUncPrefix, StringComparison.OrdinalIgnoreCase))
            resolved = @"\\" + resolved[deviceUncPrefix.Length..];
        else if (resolved.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase) &&
                 resolved.Length >= devicePrefix.Length + 2 &&
                 resolved[devicePrefix.Length + 1] == ':')
            resolved = resolved[devicePrefix.Length..];

        return resolved;
    }

    /// <summary>
    /// Reissues native cancellation only for the finite cleanup interval. A failed
    /// <c>CancelSynchronousIo</c> attempt never turns public cancellation into an unbounded join.
    /// </summary>
    private static async Task CancelWindowsSynchronousIoWithinCleanupBoundAsync(
        uint threadId,
        Task completion,
        CancellationToken cancellationToken)
    {
        var cancellationSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.UnsafeRegister(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            cancellationSignal);

        var winner = await Task.WhenAny(completion, cancellationSignal.Task).ConfigureAwait(false);
        if (ReferenceEquals(winner, completion))
            return;

        var cleanupDeadline = Task.Delay(s_nativeCleanupTimeout);
        while (!completion.IsCompleted && !cleanupDeadline.IsCompleted)
        {
            CancelWindowsSynchronousIo(threadId);
            await Task.WhenAny(
                    completion,
                    cleanupDeadline,
                    Task.Delay(TimeSpan.FromMilliseconds(5)))
                .ConfigureAwait(false);
        }
    }


    /// <summary>Attempts to interrupt synchronous I/O issued by the resolver thread.</summary>
    private static void CancelWindowsSynchronousIo(uint threadId)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var threadHandle = OpenThread(ThreadTerminate, inheritHandle: false, threadId);
        if (threadHandle == IntPtr.Zero)
            return;

        try
        {
            _ = CancelSynchronousIo(threadHandle);
        }
        finally
        {
            _ = CloseHandle(threadHandle);
        }
    }


    /// <summary>Compares two paths using syntax-appropriate lexical identity semantics.</summary>
    public static bool AreEquivalent(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return string.Equals(left, right, StringComparison.Ordinal);

        return string.Equals(GetStorageKey(left), GetStorageKey(right), StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns whether a lexical candidate path is the root or is contained beneath it without
    /// consulting the filesystem. Use <see cref="IsWithinRootAsync"/> for physical containment.
    /// </summary>
    public static bool IsWithinRoot(string rootPath, string candidatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);

        var rootPlatform = DetectPlatform(rootPath);
        var candidatePlatform = DetectPlatform(candidatePath);
        if (candidatePlatform != rootPlatform)
            return false;

        return IsNormalizedPathWithinRoot(
            NormalizePath(rootPath, rootPlatform),
            NormalizePath(candidatePath, candidatePlatform),
            rootPlatform);
    }

    /// <summary>
    /// Asynchronously evaluates physical root containment with a finite shared deadline.
    /// </summary>
    /// <param name="rootPath">Trusted containing root.</param>
    /// <param name="candidatePath">Candidate path.</param>
    /// <param name="timeout">Finite shared physical-resolution deadline.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns><see langword="true"/> only when both physical identities resolve within one root.</returns>
    public static async Task<bool> IsWithinRootAsync(
        string rootPath,
        string candidatePath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
            throw new ArgumentOutOfRangeException(nameof(timeout), "A finite positive timeout is required.");

        var rootPlatform = DetectPlatform(rootPath);
        var candidatePlatform = DetectPlatform(candidatePath);
        if (candidatePlatform != rootPlatform)
            return false;

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            var normalizedRoot = await ResolveAsync(
                    rootPath,
                    rootPlatform,
                    timeout,
                    deadline.Token)
                .ConfigureAwait(false);
            var normalizedCandidate = await ResolveAsync(
                    candidatePath,
                    candidatePlatform,
                    timeout,
                    deadline.Token)
                .ConfigureAwait(false);
            return IsNormalizedPathWithinRoot(
                normalizedRoot.NormalizedPath,
                normalizedCandidate.NormalizedPath,
                rootPlatform);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool IsNormalizedPathWithinRoot(
        string normalizedRoot,
        string normalizedCandidate,
        WorkspacePathPlatform platform)
    {
        var comparison = platform == WorkspacePathPlatform.Windows
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (string.Equals(normalizedRoot, normalizedCandidate, comparison))
            return true;

        var separator = platform == WorkspacePathPlatform.Windows ? (char)92 : '/';
        var rootWithSeparator = normalizedRoot.EndsWith(separator)
            ? normalizedRoot
            : normalizedRoot + separator;
        return normalizedCandidate.StartsWith(rootWithSeparator, comparison);
    }


    /// <summary>Builds a storage key from an already normalized lexical or physical path.</summary>
    private static string BuildStorageKey(
        string normalizedPath,
        WorkspacePathPlatform platform) =>
        platform == WorkspacePathPlatform.Windows
            ? WindowsPrefix + normalizedPath.ToUpperInvariant()
            : CaseSensitivePrefix + normalizedPath;

    /// <summary>
    /// Identifies a non-rooted URI-scheme-shaped workspace value whose colon is part of an
    /// opaque logical identifier rather than a filesystem root.
    /// </summary>
    private static bool IsOpaqueWorkspaceIdentifier(string value)
    {
        if (value.StartsWith("/", StringComparison.Ordinal) ||
            value.StartsWith(((char)92).ToString(), StringComparison.Ordinal))
        {
            return false;
        }

        var colon = value.IndexOf(':');
        if (colon <= 1 || !char.IsAsciiLetter(value[0]))
            return false;

        for (var index = 1; index < colon; index++)
        {
            var character = value[index];
            if (!char.IsAsciiLetterOrDigit(character) &&
                character is not '+' and not '-' and not '.')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Detects Windows or case-sensitive semantics from absolute path syntax.</summary>
    public static WorkspacePathPlatform DetectPlatform(string workspacePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        var path = workspacePath.Trim();

        if ((path.Length >= 3 &&
             char.IsAsciiLetter(path[0]) &&
             path[1] == ':' &&
             IsWindowsSeparator(path[2])) ||
            (path.Length >= 2 && path[0] == (char)92 && path[1] == (char)92) ||
            path.StartsWith("//", StringComparison.Ordinal))
        {
            return WorkspacePathPlatform.Windows;
        }

        if (path.StartsWith("/", StringComparison.Ordinal))
            return WorkspacePathPlatform.CaseSensitive;

        return OperatingSystem.IsWindows()
            ? WorkspacePathPlatform.Windows
            : WorkspacePathPlatform.CaseSensitive;
    }

    /// <summary>Identifies UNC/device paths whose native open depends on remote I/O.</summary>
    private static bool IsRemoteWindowsPath(string path)
    {
        const string deviceUncPrefix = @"\\?\UNC\";
        const string devicePrefix = @"\\?\";
        if (path.StartsWith(deviceUncPrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        return path.StartsWith(@"\\", StringComparison.Ordinal) &&
               !path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWindowsLexicalPath(string path)
    {
        var normalized = path.Replace('/', (char)92);
        var root = GetWindowsRoot(normalized, out var remainder);
        var segments = CollapseSegments(
            remainder.Split((char)92, StringSplitOptions.RemoveEmptyEntries),
            rooted: root.Length > 0);
        if (segments.Count == 0)
            return root.Length > 0 ? root : ".";

        return root + string.Join((char)92, segments);
    }

    private static string NormalizeCaseSensitiveLexicalPath(string path)
    {
        var rooted = path.StartsWith("/", StringComparison.Ordinal);
        var segments = CollapseSegments(
            path.Split('/', StringSplitOptions.RemoveEmptyEntries),
            rooted);
        if (segments.Count == 0)
            return rooted ? "/" : ".";

        var suffix = string.Join('/', segments);
        return rooted ? "/" + suffix : suffix;
    }

    private static List<string> CollapseSegments(IEnumerable<string> segments, bool rooted)
    {
        var result = new List<string>();
        foreach (var segment in segments)
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (result.Count > 0 && result[^1] != "..")
                {
                    result.RemoveAt(result.Count - 1);
                }
                else if (!rooted)
                {
                    result.Add(segment);
                }

                continue;
            }

            result.Add(segment);
        }

        return result;
    }

    private static string GetWindowsRoot(string path, out string remainder)
    {
        const string deviceUncPrefix = @"\\?\UNC\";
        const string devicePrefix = @"\\?\";
        if (path.StartsWith(deviceUncPrefix, StringComparison.OrdinalIgnoreCase))
            return GetUncRoot(path, deviceUncPrefix.Length, out remainder);

        if (path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (path.Length >= devicePrefix.Length + 3 &&
                char.IsAsciiLetter(path[devicePrefix.Length]) &&
                path[devicePrefix.Length + 1] == ':' &&
                path[devicePrefix.Length + 2] == (char)92)
            {
                var length = devicePrefix.Length + 3;
                remainder = path[length..];
                return path[..length];
            }

            var deviceRootEnd = path.IndexOf((char)92, devicePrefix.Length);
            if (deviceRootEnd < 0)
            {
                remainder = string.Empty;
                return path + (char)92;
            }

            remainder = path[(deviceRootEnd + 1)..];
            return path[..(deviceRootEnd + 1)];
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal))
            return GetUncRoot(path, 2, out remainder);

        if (path.Length >= 3 &&
            char.IsAsciiLetter(path[0]) &&
            path[1] == ':' &&
            path[2] == (char)92)
        {
            remainder = path[3..];
            return path[..3];
        }

        if (path.StartsWith((char)92))
        {
            remainder = path[1..];
            return ((char)92).ToString();
        }

        remainder = path;
        return string.Empty;
    }

    private static string GetUncRoot(string path, int serverStart, out string remainder)
    {
        var serverEnd = path.IndexOf((char)92, serverStart);
        if (serverEnd < 0)
        {
            remainder = string.Empty;
            return path + (char)92;
        }

        var shareEnd = path.IndexOf((char)92, serverEnd + 1);
        if (shareEnd < 0)
        {
            remainder = string.Empty;
            return path + (char)92;
        }

        remainder = path[(shareEnd + 1)..];
        return path[..(shareEnd + 1)];
    }



    private static string NormalizeLocalAdministrativeShareAlias(string path)
    {
        if (path.Length < 2 ||
            path[0] != (char)92 ||
            path[1] != (char)92 ||
            (path.Length >= 4 &&
             path[2] == '?' &&
             path[3] == (char)92))
        {
            return path;
        }

        var segments = path[2..].Split(
            (char)92,
            3,
            StringSplitOptions.None);
        if (segments.Length < 2 ||
            !IsLocalMachineName(segments[0]) ||
            segments[1].Length != 2 ||
            !char.IsAsciiLetter(segments[1][0]) ||
            segments[1][1] != '$')
        {
            return path;
        }

        var driveRoot = string.Concat(
            char.ToUpperInvariant(segments[1][0]),
            ':',
            (char)92);
        return segments.Length == 2 || string.IsNullOrEmpty(segments[2])
            ? driveRoot
            : driveRoot + segments[2];
    }

    private static bool IsLocalMachineName(string serverName) =>
        string.Equals(serverName, ".", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(serverName, "localhost", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            serverName,
            Environment.MachineName,
            StringComparison.OrdinalIgnoreCase);



    private static bool IsNativePlatform(WorkspacePathPlatform platform) =>
        platform == WorkspacePathPlatform.Windows
            ? OperatingSystem.IsWindows()
            : !OperatingSystem.IsWindows();

    private static bool IsWindowsSeparator(char value) => value is '/' || value == (char)92;

    private sealed class CanonicalWorkspacePathComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y) => AreEquivalent(x, y);

        public int GetHashCode(string obj) =>
            StringComparer.Ordinal.GetHashCode(GetStorageKey(obj));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(
        SafeFileHandle fileHandle,
        StringBuilder filePath,
        uint filePathLength,
        uint flags);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenThread(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint threadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CancelSynchronousIo(IntPtr threadHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
