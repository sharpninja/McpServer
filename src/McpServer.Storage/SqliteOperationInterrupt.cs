using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// Registers a cancellation callback whose native SQLite interrupt remains valid only for the
/// lifetime of one operation. Close/dispose races are tolerated without classifying a cancellation
/// until an interrupt was actually issued against a live handle.
/// </summary>
internal sealed class SqliteOperationInterrupt : IDisposable
{
    private readonly object _sync = new();
    private readonly CancellationTokenRegistration _registration;
    private readonly bool _handleReferenceAdded;
    private sqlite3? _handle;
    private Thread? _managedThread;
    private int _interruptIssued;
    private int _managedWaitInterruptionIssued;
    private bool _disposed;

    private SqliteOperationInterrupt(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        sqlite3? handle = null;
        var handleReferenceAdded = false;
        try
        {
            handle = connection.Handle;
            handle!.DangerousAddRef(ref handleReferenceAdded);
        }
        catch (Exception)
        {
            if (handleReferenceAdded)
                handle?.DangerousRelease();
            handle = null;
            handleReferenceAdded = false;
        }

        _handle = handle;
        _handleReferenceAdded = handleReferenceAdded;
        _registration = cancellationToken.UnsafeRegister(
            static state => ((SqliteOperationInterrupt)state!).TryInterrupt(),
            this);
    }

    /// <summary>
    /// Gets a value indicating whether this registration successfully issued a native interrupt.
    /// </summary>
    internal bool InterruptIssued => Volatile.Read(ref _interruptIssued) != 0;

    /// <summary>
    /// Gets a value indicating whether caller cancellation interrupted the provider's managed
    /// lock-retry wait on the registered operation thread.
    /// </summary>
    internal bool ManagedWaitInterruptionIssued =>
        Volatile.Read(ref _managedWaitInterruptionIssued) != 0;

    /// <summary>
    /// Creates an operation-scoped native SQLite cancellation registration.
    /// </summary>
    /// <param name="connection">Connection whose current native handle belongs to the operation.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>A registration that must be disposed before the connection is reused.</returns>
    internal static SqliteOperationInterrupt Register(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new SqliteOperationInterrupt(connection, cancellationToken);
    }

    /// <summary>
    /// Pins managed-wait interruption to the current thread for one synchronous SQLite call.
    /// The scope must end before the caller awaits or performs unrelated work.
    /// </summary>
    /// <returns>A scope that removes the current thread from cancellation targeting.</returns>
    internal IDisposable BeginManagedWaitInterruption()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_managedThread is not null)
            {
                throw new InvalidOperationException(
                    "A managed SQLite wait-interruption scope is already active.");
            }

            var thread = Thread.CurrentThread;
            _managedThread = thread;
            return new ManagedWaitScope(this, thread);
        }
    }

    /// <summary>Stops targeting a completed synchronous provider call.</summary>
    private void EndManagedWaitInterruption(Thread thread)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_managedThread, thread))
                _managedThread = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        _registration.Dispose();
        sqlite3? handle;
        lock (_sync)
        {
            handle = _handle;
            _handle = null;
            _managedThread = null;
        }

        if (_handleReferenceAdded)
            handle?.DangerousRelease();
    }

    private void TryInterrupt()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            var managedThread = _managedThread;
            if (managedThread is not null &&
                managedThread != Thread.CurrentThread &&
                (managedThread.ThreadState & ThreadState.WaitSleepJoin) != 0 &&
                Interlocked.CompareExchange(
                    ref _managedWaitInterruptionIssued,
                    1,
                    0) == 0)
            {
                try
                {
                    managedThread.Interrupt();
                }
                catch (ThreadStateException)
                {
                    Volatile.Write(ref _managedWaitInterruptionIssued, 0);
                    // The operation thread completed between registration and cancellation.
                }
            }

            if (_handle is null)
                return;

            try
            {
                raw.sqlite3_interrupt(_handle);
                Volatile.Write(ref _interruptIssued, 1);
            }
            catch (Exception)
            {
                // A close, dispose, or pre-handle operation won the race.
            }
        }
    }

    /// <summary>Removes one exact operation thread from managed-wait cancellation targeting.</summary>
    private sealed class ManagedWaitScope(
        SqliteOperationInterrupt owner,
        Thread thread) : IDisposable
    {
        private SqliteOperationInterrupt? _owner = owner;

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.Exchange(ref _owner, null)?
                .EndManagedWaitInterruption(thread);
        }
    }
}
