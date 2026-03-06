using System;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services.Infrastructure;

/// <summary>
/// Host implementation of <see cref="ITimerService"/> backed by <see cref="System.Threading.Timer"/>.
/// </summary>
public sealed class TimerService : ITimerService
{
    public ITimerHandle CreateRecurring(TimeSpan interval, Func<CancellationToken, Task> callback)
        => new TimerHandle(interval, callback, recurring: true);

    public ITimerHandle CreateOneShot(TimeSpan delay, Func<CancellationToken, Task> callback)
        => new TimerHandle(delay, callback, recurring: false);

    private sealed class TimerHandle : ITimerHandle
    {
        private Timer? _timer;
        private CancellationTokenSource? _cts;
        private readonly Func<CancellationToken, Task> _callback;
        private readonly bool _recurring;
        private TimeSpan _interval;

        public TimerHandle(TimeSpan interval, Func<CancellationToken, Task> callback, bool recurring)
        {
            _interval = interval;
            _callback = callback;
            _recurring = recurring;
            _cts = new CancellationTokenSource();
            _timer = new Timer(OnTick, null, interval, recurring ? interval : Timeout.InfiniteTimeSpan);
        }

        private async void OnTick(object? state)
        {
            var cts = _cts;
            if (cts is null || cts.IsCancellationRequested)
                return;

            try
            {
                await _callback(cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        public void Stop()
        {
            _timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Restart(TimeSpan? newInterval = null)
        {
            if (newInterval.HasValue)
                _interval = newInterval.Value;

            _timer?.Change(_interval, _recurring ? _interval : Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _timer?.Dispose();
            _timer = null;
        }
    }
}

