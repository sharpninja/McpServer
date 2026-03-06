using System;
using System.Threading.Tasks;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// UI thread dispatching and background work tracking.
/// </summary>
public interface IUiDispatchTarget
{
    void DispatchToUi(Action action);
    void TrackBackgroundWork(Task task);
    string StatusMessage { get; set; }
}

