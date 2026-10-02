using System.Data;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using McpServer.Client;
using Microsoft.Data.Sqlite;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// Opens a SQLite connection on an observable worker so inherited synchronous OpenAsync behavior
/// cannot block the async caller indefinitely. Alias-aware data-source resolution precedes worker
/// creation, Windows pre-handle I/O receives finite CancelSynchronousIo attempts, and workers remain
/// counted until their connection and timeout state are actually finalized.
/// </summary>
internal static class SqliteBoundedConnectionOpener
{
    private const uint ThreadTerminate = 0x0001;
    private const int ErrorOperationAborted = 995;
    private static readonly TimeSpan s_nativeCleanupTimeout = TimeSpan.FromMilliseconds(250);
    private static Action? s_beforeDataSourcePhysicalResolution = null;
    private static Action? s_beforePinnedDataSourceOpen = null;
    private static Action? s_beforeWorkerExit = null;
    private static int s_activeWorkerCount;
    private static int s_workerInvocationCount;

    /// <summary>Gets the number of currently running open workers.</summary>
    internal static int ActiveWorkerCount => Volatile.Read(ref s_activeWorkerCount);

    /// <summary>Gets the total number of open workers started by this process.</summary>
    internal static int WorkerInvocationCount => Volatile.Read(ref s_workerInvocationCount);

    /// <summary>
    /// Opens an isolated worker-owned connection cloned from a closed caller connection. The caller
    /// instance is read only before the worker starts and is never retained or touched by that worker.
    /// A successful clone is published only after the real worker thread has exited.
    /// </summary>
    internal static async Task<SqliteConnection> OpenIsolatedAsync(
        SqliteConnection connection,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (timeout <= TimeSpan.Zero || timeout == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "A finite positive timeout is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (connection.State == ConnectionState.Open)
            return connection;
        if (connection.State != ConnectionState.Closed)
        {
            throw new InvalidOperationException(
                $"SQLite connection must be closed before opening; current state is {connection.State}.");
        }

        var connectionString = connection.ConnectionString;
        var defaultTimeout = connection.DefaultTimeout;
        ValidateBoundedDataSource(connection);

        var workerOutcome = new TaskCompletionSource<SqliteOpenOutcome>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<SqliteOpenOutcome>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var timeoutSignal = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationState = new OpenCancellationState();
        using var callerRegistration = cancellationToken.UnsafeRegister(
            static state =>
            {
                var tuple = ((OpenCancellationState State, TaskCompletionSource Signal))state!;
                tuple.State.Request(OpenCancellationKind.Caller);
                tuple.Signal.TrySetResult();
            },
            (cancellationState, cancellationSignal));
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var timeoutRegistration = timeoutSource.Token.UnsafeRegister(
            static state =>
            {
                var tuple = ((OpenCancellationState State, TaskCompletionSource Signal))state!;
                tuple.State.Request(OpenCancellationKind.Timeout);
                tuple.Signal.TrySetResult();
            },
            (cancellationState, timeoutSignal));

        var thread = new Thread(
            () =>
            {
                Exception? failure = null;
                SqliteConnection? openedConnection = null;
                SqliteConnection? workerConnection = null;
                PinnedSqliteDataSourceLease? dataSourceLease = null;
                try
                {
                    cancellationState.AttachThread(
                        OperatingSystem.IsWindows() ? GetCurrentThreadId() : 0);
                    workerConnection = new SqliteConnection(connectionString)
                    {
                        DefaultTimeout = 1,
                    };
                    ValidateBoundedDataSource(workerConnection, invokeBoundaryHook: false);
                    if (cancellationState.RequestedKind == OpenCancellationKind.None)
                    {
                        Volatile.Read(ref s_beforePinnedDataSourceOpen)?.Invoke();
                        if (cancellationState.RequestedKind == OpenCancellationKind.None)
                        {
                            dataSourceLease = PinLinuxDataSource(
                                workerConnection,
                                connectionString);
                            if (cancellationState.RequestedKind == OpenCancellationKind.None)
                            {
                                workerConnection.Open();
                                if (workerConnection.State == ConnectionState.Open)
                                    dataSourceLease?.CompleteOpen();
                            }
                        }
                    }

                    if (cancellationState.RequestedKind == OpenCancellationKind.None &&
                        workerConnection.State == ConnectionState.Open)
                    {
                        workerConnection.DefaultTimeout = defaultTimeout;
                        openedConnection = workerConnection;
                        workerConnection = null;
                    }
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    if (workerConnection is not null &&
                        cancellationState.RequestedKind != OpenCancellationKind.None)
                    {
                        CloseAfterInterruptedOpen(workerConnection);
                    }

                    try
                    {
                        dataSourceLease?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        failure ??= exception;
                    }

                    try
                    {
                        workerConnection?.Dispose();
                    }
                    catch (Exception exception)
                    {
                        failure ??= exception;
                    }

                    workerOutcome.TrySetResult(new SqliteOpenOutcome(openedConnection, failure));
                    Volatile.Read(ref s_beforeWorkerExit)?.Invoke();
                }
            })
        {
            IsBackground = true,
            Name = "McpSqliteIsolatedOpen",
        };

        Interlocked.Increment(ref s_activeWorkerCount);
        try
        {
            thread.Start();
        }
        catch
        {
            Interlocked.Decrement(ref s_activeWorkerCount);
            cancellationState.Complete();
            throw;
        }

        Interlocked.Increment(ref s_workerInvocationCount);
        _ = TrackIsolatedWorkerUntilActuallyGoneAsync(
            thread,
            workerOutcome.Task,
            cancellationState,
            completion);

        var winner = await Task.WhenAny(
                completion.Task,
                cancellationSignal.Task,
                timeoutSignal.Task)
            .ConfigureAwait(false);
        if (!ReferenceEquals(winner, completion.Task))
        {
            var cleanupDeadline = Task.Delay(s_nativeCleanupTimeout);
            while (!completion.Task.IsCompleted && !cleanupDeadline.IsCompleted)
            {
                cancellationState.TryCancelNativeWork();
                await Task.WhenAny(
                        completion.Task,
                        cleanupDeadline,
                        Task.Delay(TimeSpan.FromMilliseconds(10)))
                    .ConfigureAwait(false);
            }
        }

        var cancellationKind = cancellationState.RequestedKind;
        if (!completion.Task.IsCompleted)
        {
            ThrowRequestedCancellation(
                cancellationKind,
                timeout,
                cancellationToken,
                failure: null);
        }

        var outcome = await completion.Task.ConfigureAwait(false);
        if (outcome.Failure is null && outcome.Connection is not null)
        {
            if (cancellationKind != OpenCancellationKind.None)
            {
                outcome.Connection.Dispose();
                ThrowRequestedCancellation(
                    cancellationKind,
                    timeout,
                    cancellationToken,
                    failure: null);
            }

            return outcome.Connection;
        }

        if (outcome.Failure is not null &&
            IsGenuineCancellation(outcome.Failure, cancellationState, cancellationToken) &&
            cancellationKind != OpenCancellationKind.None)
        {
            ThrowRequestedCancellation(
                cancellationKind,
                timeout,
                cancellationToken,
                outcome.Failure);
        }

        if (outcome.Failure is not null)
            ExceptionDispatchInfo.Capture(outcome.Failure).Throw();

        ThrowRequestedCancellation(
            cancellationKind,
            timeout,
            cancellationToken,
            failure: null);
        throw new InvalidOperationException("SQLite isolated opening produced no connection.");
    }

    /// <summary>
    /// Retains truthful worker accounting and disposes an unpublished successful clone when
    /// cancellation won before the worker's real exit.
    /// </summary>
    private static async Task TrackIsolatedWorkerUntilActuallyGoneAsync(
        Thread thread,
        Task<SqliteOpenOutcome> workerOutcome,
        OpenCancellationState cancellationState,
        TaskCompletionSource<SqliteOpenOutcome> completion)
    {
        while (thread.IsAlive)
            await Task.Delay(TimeSpan.FromMilliseconds(5)).ConfigureAwait(false);

        var outcome = await workerOutcome.ConfigureAwait(false);
        if (cancellationState.RequestedKind != OpenCancellationKind.None &&
            outcome.Connection is not null)
        {
            outcome.Connection.Dispose();
            outcome = outcome with { Connection = null };
        }

        cancellationState.Complete();
        Interlocked.Decrement(ref s_activeWorkerCount);
        completion.TrySetResult(outcome);
    }

    /// <summary>Outcome owned by the isolated opener until the worker is gone.</summary>
    private sealed record SqliteOpenOutcome(
        SqliteConnection? Connection,
        Exception? Failure);

    /// <summary>
    /// Opens and returns a worker-owned clone while leaving the caller connection untouched.
    /// </summary>
    internal static Task<SqliteConnection> OpenAsync(
        SqliteConnection connection,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        OpenIsolatedAsync(connection, timeout, cancellationToken);




    private static void ThrowRequestedCancellation(
        OpenCancellationKind cancellationKind,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        Exception? failure)
    {
        if (cancellationKind == OpenCancellationKind.Caller)
        {
            throw new OperationCanceledException(
                "SQLite connection opening was cancelled by the caller.",
                failure,
                cancellationToken);
        }

        if (cancellationKind == OpenCancellationKind.Timeout)
        {
            throw new TimeoutException(
                $"SQLite connection opening exceeded the finite timeout of {timeout}.",
                failure);
        }

        throw new InvalidOperationException(
            "SQLite bounded opening lost its cancellation classification.",
            failure);
    }

    private static void ValidateBoundedDataSource(
        SqliteConnection connection,
        bool invokeBoundaryHook = true)
    {
        var options = new SqliteConnectionStringBuilder(connection.ConnectionString);
        var dataSource = options.DataSource;
        if (options.Mode == SqliteOpenMode.Memory ||
            string.IsNullOrWhiteSpace(dataSource) ||
            string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase) ||
            (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
             dataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            var withoutScheme = dataSource["file:".Length..];
            var queryIndex = withoutScheme.IndexOf('?');
            if (queryIndex >= 0)
                withoutScheme = withoutScheme[..queryIndex];

            dataSource = Uri.UnescapeDataString(withoutScheme);
        }

        var lexicalPath = Path.GetFullPath(dataSource);
        _ = BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(
            lexicalPath,
            "SQLite connection opening");
        if (invokeBoundaryHook)
            Volatile.Read(ref s_beforeDataSourcePhysicalResolution)?.Invoke();

        var physicalCandidate =
            BoundedFileSystemPolicy.EnsureLexicalPathSupportsBoundedNativeIo(
                lexicalPath,
                "SQLite connection opening");
        if (!OperatingSystem.IsLinux())
            return;

        // The isolated worker re-checks the lexical data source, then s_beforePinnedDataSourceOpen
        // runs, then PinLinuxDataSource performs the atomic no-cross-device open. Repeating
        // openat2-follow resolution on the worker would reject a still-safe alias before that hook
        // can fire. Caller-side checks resolve symlink prefixes through the mount table so an
        // absolute same-filesystem alias is not mistaken for a FUSE crossing.
        if (!invokeBoundaryHook)
            return;

        var physicalPath = LinuxPhysicalPathResolver.ResolveSymlinksOnApprovedMounts(
            physicalCandidate,
            "SQLite connection opening");
        BoundedFileSystemPolicy.EnsurePathSupportsBoundedNativeIo(
            physicalPath,
            "SQLite connection opening");
    }

    /// <summary>
    /// Pins the exact Linux database file with openat2 and routes SQLite through its descriptor path.
    /// </summary>
    private static PinnedSqliteDataSourceLease? PinLinuxDataSource(
        SqliteConnection connection,
        string originalConnectionString)
    {
        if (!OperatingSystem.IsLinux())
            return null;

        var options = new SqliteConnectionStringBuilder(connection.ConnectionString);
        var dataSource = options.DataSource;
        if (options.Mode == SqliteOpenMode.Memory ||
            string.IsNullOrWhiteSpace(dataSource) ||
            string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase) ||
            (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
             dataSource.Contains("mode=memory", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var isFileUri = dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase);
        var query = string.Empty;
        if (isFileUri)
        {
            var withoutScheme = dataSource["file:".Length..];
            var queryIndex = withoutScheme.IndexOf('?');
            if (queryIndex >= 0)
            {
                query = withoutScheme[queryIndex..];
                withoutScheme = withoutScheme[..queryIndex];
            }

            dataSource = Uri.UnescapeDataString(withoutScheme);
        }

        var lexicalPath = Path.GetFullPath(dataSource);
        var access = options.Mode == SqliteOpenMode.ReadOnly
            ? FileAccess.Read
            : FileAccess.ReadWrite;
        var create = options.Mode == SqliteOpenMode.ReadWriteCreate;
        var handle = LinuxPhysicalPathResolver.OpenFileNoCrossDevice(
            lexicalPath,
            access,
            create,
            out _);
        try
        {
            var descriptorPath = $"/proc/self/fd/{handle.DangerousGetHandle()}";
            options.DataSource = isFileUri
                ? "file:" + descriptorPath + query
                : descriptorPath;
            connection.ConnectionString = options.ToString();
            return new PinnedSqliteDataSourceLease(
                connection,
                originalConnectionString,
                handle);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Accesses Microsoft.Data.Sqlite's state field so the public connection-string setter can
    /// restore both visible identity and pool identity after a successful pinned open. The
    /// connection is exclusively owned by this worker until completion is published.
    /// </summary>
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_state")]
    private static extern ref ConnectionState GetConnectionStateBackingField(
        SqliteConnection connection);

    /// <summary>
    /// Owns a Linux descriptor only through the native SQLite open and restores the connection's
    /// public identity and future-open pool identity without changing the active native connection.
    /// </summary>
    private sealed class PinnedSqliteDataSourceLease : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly string _originalConnectionString;
        private IDisposable? _handle;
        private int _completed;

        internal PinnedSqliteDataSourceLease(
            SqliteConnection connection,
            string originalConnectionString,
            IDisposable handle)
        {
            _connection = connection;
            _originalConnectionString = originalConnectionString;
            _handle = handle;
        }

        /// <summary>
        /// Releases the pin after SQLite owns its native handle, prevents descriptor-keyed pooling,
        /// and restores the exact public connection string before the caller observes completion.
        /// </summary>
        internal void CompleteOpen()
        {
            DisposeHandle();
            try
            {
                // A descriptor number can be reused for another file. Mark this descriptor-keyed
                // pool stale before the inner connection can ever be returned to it.
                SqliteConnection.ClearPool(_connection);
            }
            finally
            {
                RestoreConnectionIdentity();
                Volatile.Write(ref _completed, 1);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeHandle();
            if (Volatile.Read(ref _completed) != 0)
                return;

            if (_connection.State == ConnectionState.Closed)
            {
                RestoreClosedConnectionString();
                return;
            }

            try
            {
                SqliteConnection.ClearPool(_connection);
            }
            finally
            {
                RestoreConnectionIdentity();
            }
        }

        /// <summary>Restores both the public string and pool group while the connection is closed.</summary>
        private void RestoreClosedConnectionString()
        {
            try
            {
                _connection.ConnectionString = _originalConnectionString;
            }
            catch (ObjectDisposedException)
            {
                // A disposed connection has no reusable public connection string to restore.
            }
        }

        /// <summary>
        /// Restores the public string and future-open pool group while retaining the already-open
        /// inner connection. The owning worker has exclusive access until completion is published.
        /// </summary>
        private void RestoreConnectionIdentity()
        {
            ref var state = ref GetConnectionStateBackingField(_connection);
            var openState = state;
            state = ConnectionState.Closed;
            try
            {
                _connection.ConnectionString = _originalConnectionString;
            }
            finally
            {
                state = openState;
            }
        }

        /// <summary>Releases the exact pinned descriptor at most once.</summary>
        private void DisposeHandle() =>
            Interlocked.Exchange(ref _handle, null)?.Dispose();
    }


    private static void CloseAfterInterruptedOpen(SqliteConnection connection)
    {
        try
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }
        catch (Exception)
        {
            // A concurrent close or disposal already owns cleanup.
        }
    }

    private static bool IsGenuineCancellation(
        Exception failure,
        OpenCancellationState cancellationState,
        CancellationToken callerToken)
    {
        if (failure is OperationCanceledException operationCanceledException)
        {
            if (cancellationState.RequestedKind == OpenCancellationKind.Caller &&
                callerToken.IsCancellationRequested &&
                operationCanceledException.CancellationToken == callerToken)
            {
                return true;
            }

            return cancellationState.SynchronousIoCancellationIssued &&
                   (operationCanceledException.CancellationToken == default ||
                    IsWindowsOperationAborted(operationCanceledException.InnerException));
        }

        return cancellationState.SynchronousIoCancellationIssued &&
               IsWindowsOperationAborted(failure);
    }

    private static bool IsWindowsOperationAborted(Exception? exception) =>
        OperatingSystem.IsWindows() &&
        exception is IOException ioException &&
        (ioException.HResult & 0xFFFF) == ErrorOperationAborted;

    /// <summary>Cancellation origin retained separately so timeouts never impersonate caller cancellation.</summary>
    private enum OpenCancellationKind
    {
        None,
        Caller,
        Timeout,
    }

    /// <summary>Coordinates finite Windows native-I/O cancellation and worker lifecycle state.</summary>
    private sealed class OpenCancellationState
    {
        private readonly object _sync = new();
        private uint _threadId;
        private int _requestedKind;
        private int _synchronousIoCancellationIssued;
        private bool _completed;

        internal OpenCancellationKind RequestedKind =>
            (OpenCancellationKind)Volatile.Read(ref _requestedKind);

        internal bool SynchronousIoCancellationIssued =>
            Volatile.Read(ref _synchronousIoCancellationIssued) != 0;

        internal void AttachThread(uint threadId)
        {
            lock (_sync)
            {
                if (_completed)
                    return;
                _threadId = threadId;
            }

            if (RequestedKind != OpenCancellationKind.None)
                TryCancelNativeWork();
        }

        internal void Request(OpenCancellationKind kind)
        {
            lock (_sync)
            {
                if (_completed)
                    return;
                _ = Interlocked.CompareExchange(
                    ref _requestedKind,
                    (int)kind,
                    (int)OpenCancellationKind.None);
            }
        }

        internal void Complete()
        {
            lock (_sync)
            {
                _completed = true;
                _threadId = 0;
            }
        }

        internal void TryCancelNativeWork()
        {
            uint threadId;
            lock (_sync)
            {
                if (_completed)
                    return;
                threadId = _threadId;
            }

            if (OperatingSystem.IsWindows() && threadId != 0)
            {
                var threadHandle = OpenThread(ThreadTerminate, inheritHandle: false, threadId);
                if (threadHandle != IntPtr.Zero)
                {
                    try
                    {
                        if (CancelSynchronousIo(threadHandle))
                            Volatile.Write(ref _synchronousIoCancellationIssued, 1);
                    }
                    finally
                    {
                        _ = CloseHandle(threadHandle);
                    }
                }
            }


        }
    }


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
