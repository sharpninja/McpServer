using System;
using System.Threading.Tasks;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;

namespace McpServer.UI.Core.Commands;

public static class CqrsRelayFactory
{
    public static CqrsRelayCommand<bool> Create(Dispatcher dispatcher, Action action, Func<bool>? canExecute = null)
        => new(
            dispatcher,
            _ => new InvokeUiActionCommand(() =>
            {
                action();
                return Task.CompletedTask;
            }),
            canExecute is null ? null : new Predicate<object?>(_ => canExecute()));

    public static CqrsRelayCommand<bool> Create(Dispatcher dispatcher, Func<Task> action, Func<bool>? canExecute = null)
        => new(
            dispatcher,
            _ => new InvokeUiActionCommand(action),
            canExecute is null ? null : new Predicate<object?>(_ => canExecute()));

    public static CqrsRelayCommand<bool> Create<T>(Dispatcher dispatcher, Action<T?> action, Func<bool>? canExecute = null)
        => new(
            dispatcher,
            parameter => new InvokeUiActionCommand(() =>
            {
                action((T?)parameter);
                return Task.CompletedTask;
            }),
            canExecute is null ? null : new Predicate<object?>(_ => canExecute()));

    public static CqrsRelayCommand<bool> Create<T>(Dispatcher dispatcher, Action<T?> action, Func<T?, bool>? canExecute)
        => new(
            dispatcher,
            parameter => new InvokeUiActionCommand(() =>
            {
                action((T?)parameter);
                return Task.CompletedTask;
            }),
            canExecute is null ? null : new Predicate<object?>(parameter => canExecute((T?)parameter)));

    public static CqrsRelayCommand<bool> Create<T>(Dispatcher dispatcher, Func<T?, Task> action, Func<bool>? canExecute = null)
        => new(
            dispatcher,
            parameter => new InvokeUiActionCommand(() => action((T?)parameter)),
            canExecute is null ? null : new Predicate<object?>(_ => canExecute()));

    public static CqrsRelayCommand<bool> Create<T>(Dispatcher dispatcher, Func<T?, Task> action, Func<T?, bool>? canExecute)
        => new(
            dispatcher,
            parameter => new InvokeUiActionCommand(() => action((T?)parameter)),
            canExecute is null ? null : new Predicate<object?>(parameter => canExecute((T?)parameter)));
}

