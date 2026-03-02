using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;

namespace McpServer.Cqrs.Mvvm;

/// <summary>
/// TR-MCP-DIR-003: An <see cref="IAsyncRelayCommand"/> that dispatches a CQRS command through the <see cref="Dispatcher"/>.
/// The ViewModel creates the command message via the factory, dispatches it, and stores the result.
/// </summary>
/// <typeparam name="TResult">The result value type from the CQRS command.</typeparam>
public sealed class CqrsRelayCommand<TResult> : IAsyncRelayCommand, INotifyPropertyChanged
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<ICommand<TResult>> _commandFactory;
    private Task? _executionTask;
    private bool _isRunning;

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Occurs when <see cref="CanExecute"/> changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Initializes a new <see cref="CqrsRelayCommand{TResult}"/>.</summary>
    /// <param name="dispatcher">The CQRS dispatcher.</param>
    /// <param name="commandFactory">Factory that creates the command message from current ViewModel state.</param>
    public CqrsRelayCommand(Dispatcher dispatcher, Func<ICommand<TResult>> commandFactory)
    {
        _dispatcher = dispatcher;
        _commandFactory = commandFactory;
    }

    /// <summary>The result of the last dispatch, or <c>null</c> if not yet executed.</summary>
    public Result<TResult>? LastResult { get; private set; }

    /// <summary>Whether the last execution succeeded.</summary>
    public bool Succeeded => LastResult?.IsSuccess == true;

    /// <summary>The success value from the last execution, or <c>default</c>.</summary>
    public TResult? Value => LastResult is { IsSuccess: true } r ? r.Value : default;

    /// <summary>The error message from the last execution, or <c>null</c>.</summary>
    public string? Error => LastResult?.Error;

    /// <inheritdoc />
    public Task? ExecutionTask => _executionTask;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public bool CanBeCanceled => false;

    /// <inheritdoc />
    public bool IsCancellationRequested => false;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning;

    /// <inheritdoc />
    public void Execute(object? parameter) => ExecuteAsync(parameter);

    /// <inheritdoc />
    public Task ExecuteAsync(object? parameter) => DispatchAsync(CancellationToken.None);

    /// <inheritdoc />
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Cancel() { /* Not cancellable */ }

    /// <summary>
    /// Dispatches the CQRS command through the Dispatcher and stores the result.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The dispatch result.</returns>
    public async Task<Result<TResult>> DispatchAsync(CancellationToken ct = default)
    {
        _isRunning = true;
        OnPropertyChanged(nameof(IsRunning));
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            var command = _commandFactory();
            _executionTask = Task.Run(async () =>
            {
                LastResult = await _dispatcher.SendAsync(command, ct).ConfigureAwait(false);
            }, ct);

            await _executionTask.ConfigureAwait(false);
            return LastResult!.Value;
        }
        finally
        {
            _isRunning = false;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(LastResult));
            OnPropertyChanged(nameof(Succeeded));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(Error));
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// TR-MCP-DIR-003: An <see cref="IAsyncRelayCommand"/> that dispatches a CQRS query through the <see cref="Dispatcher"/>.
/// </summary>
/// <typeparam name="TResult">The result value type from the CQRS query.</typeparam>
public sealed class CqrsQueryCommand<TResult> : IAsyncRelayCommand, INotifyPropertyChanged
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<IQuery<TResult>> _queryFactory;
    private Task? _executionTask;
    private bool _isRunning;

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Occurs when <see cref="CanExecute"/> changes.</summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>Initializes a new <see cref="CqrsQueryCommand{TResult}"/>.</summary>
    /// <param name="dispatcher">The CQRS dispatcher.</param>
    /// <param name="queryFactory">Factory that creates the query message from current ViewModel state.</param>
    public CqrsQueryCommand(Dispatcher dispatcher, Func<IQuery<TResult>> queryFactory)
    {
        _dispatcher = dispatcher;
        _queryFactory = queryFactory;
    }

    /// <summary>The result of the last dispatch, or <c>null</c> if not yet executed.</summary>
    public Result<TResult>? LastResult { get; private set; }

    /// <summary>Whether the last execution succeeded.</summary>
    public bool Succeeded => LastResult?.IsSuccess == true;

    /// <summary>The success value from the last execution, or <c>default</c>.</summary>
    public TResult? Value => LastResult is { IsSuccess: true } r ? r.Value : default;

    /// <summary>The error message from the last execution, or <c>null</c>.</summary>
    public string? Error => LastResult?.Error;

    /// <inheritdoc />
    public Task? ExecutionTask => _executionTask;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public bool CanBeCanceled => false;

    /// <inheritdoc />
    public bool IsCancellationRequested => false;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => !_isRunning;

    /// <inheritdoc />
    public void Execute(object? parameter) => ExecuteAsync(parameter);

    /// <inheritdoc />
    public Task ExecuteAsync(object? parameter) => DispatchAsync(CancellationToken.None);

    /// <inheritdoc />
    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public void Cancel() { /* Not cancellable */ }

    /// <summary>
    /// Dispatches the CQRS query through the Dispatcher and stores the result.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The dispatch result.</returns>
    public async Task<Result<TResult>> DispatchAsync(CancellationToken ct = default)
    {
        _isRunning = true;
        OnPropertyChanged(nameof(IsRunning));
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        try
        {
            var query = _queryFactory();
            _executionTask = Task.Run(async () =>
            {
                LastResult = await _dispatcher.QueryAsync(query, ct).ConfigureAwait(false);
            }, ct);

            await _executionTask.ConfigureAwait(false);
            return LastResult!.Value;
        }
        finally
        {
            _isRunning = false;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(LastResult));
            OnPropertyChanged(nameof(Succeeded));
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(Error));
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
