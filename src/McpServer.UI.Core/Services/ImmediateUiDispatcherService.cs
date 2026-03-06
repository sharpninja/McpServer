using System;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Default dispatcher that executes work inline when no UI dispatcher is supplied by host.
/// </summary>
public sealed class ImmediateUiDispatcherService : IUiDispatcherService
{
    /// <inheritdoc />
    public void Post(Action action) => action();
}
