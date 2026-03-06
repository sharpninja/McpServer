namespace McpServer.UI.Core.Commands;

/// <summary>
/// Preview and markdown toggle operations.
/// </summary>
public interface IPreviewTarget
{
    void OpenPreviewInBrowser();
    void ToggleShowRawMarkdown();
}

