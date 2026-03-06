using System;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Host UI dispatcher abstraction for marshaling state updates to UI thread/context.
/// </summary>
public interface IUiDispatcherService
{
    /// <summary>Posts an action for execution on the UI context.</summary>
    void Post(Action action);
}
