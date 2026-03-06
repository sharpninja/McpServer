namespace McpServer.UI.Core.Services;

/// <summary>
/// Default no-op timer service used when host does not provide implementation.
/// </summary>
public sealed class NoOpTimerService : ITimerService
{
    /// <inheritdoc />
    public ITimerHandle CreateRecurring(TimeSpan interval, Func<CancellationToken, Task> callback)
        => new NoOpTimerHandle();

    /// <inheritdoc />
    public ITimerHandle CreateOneShot(TimeSpan delay, Func<CancellationToken, Task> callback)
        => new NoOpTimerHandle();

    private sealed class NoOpTimerHandle : ITimerHandle
    {
        public void Stop()
        {
        }

        public void Restart(TimeSpan? newInterval = null)
        {
        }

        public void Dispose()
        {
        }
    }
}
