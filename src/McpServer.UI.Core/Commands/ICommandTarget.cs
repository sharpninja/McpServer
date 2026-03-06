namespace McpServer.UI.Core.Commands;

/// <summary>
/// Union interface for all ViewModel operations invoked by CQRS command handlers.
/// Extends granular service interfaces so existing code that registers ICommandTarget still works,
/// while handlers can depend on specific interfaces.
/// </summary>
public interface ICommandTarget
    : INavigationTarget,
      IRequestDetailsTarget,
      IPreviewTarget,
      IArchiveTarget,
      ISessionDataTarget,
      IClipboardTarget,
      IConfigTarget,
      IUiDispatchTarget,
      ITodoCopilotTarget
{
}

