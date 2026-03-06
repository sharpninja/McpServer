using System;
using System.Threading.Tasks;
using McpServer.Cqrs;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Wraps a ViewModel action delegate so it can be dispatched through the CQRS dispatcher.
/// </summary>
public sealed record InvokeUiActionCommand(Func<Task> Action) : ICommand<bool>;

public sealed class InvokeUiActionHandler : ICommandHandler<InvokeUiActionCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(InvokeUiActionCommand command, CallContext context)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            await command.Action().ConfigureAwait(true);
            return Result<bool>.Success(true);
        }

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            await command.Action().ConfigureAwait(true)).ConfigureAwait(true);
        return Result<bool>.Success(true);
    }
}

