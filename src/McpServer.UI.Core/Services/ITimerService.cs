namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided abstraction for creating recurring and one-shot timers.
/// </summary>
public interface ITimerService
{
    /// <summary>Creates a timer that fires at a recurring interval.</summary>
    ITimerHandle CreateRecurring(TimeSpan interval, Func<CancellationToken, Task> callback);

    /// <summary>Creates a timer that fires once after a delay.</summary>
    ITimerHandle CreateOneShot(TimeSpan delay, Func<CancellationToken, Task> callback);
}

/// <summary>Handle to a running timer that can be stopped or restarted.</summary>
public interface ITimerHandle : IDisposable
{
    /// <summary>Stops the timer without disposing.</summary>
    void Stop();

    /// <summary>Restarts the timer, optionally with a new interval.</summary>
    void Restart(TimeSpan? newInterval = null);
}
