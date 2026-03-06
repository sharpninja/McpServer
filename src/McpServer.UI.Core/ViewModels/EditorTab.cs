using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Represents an editor tab for TODO content or Copilot markdown output.
/// </summary>
public partial class EditorTab : ObservableObject
{
    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _content = "";
    [ObservableProperty] private bool _isMarkdown;

    /// <summary>Creates the primary editor tab for a TODO item.</summary>
    public static EditorTab CreateEditorTab(string todoId, string content)
        => new() { Header = todoId, Content = content, IsMarkdown = false };

    /// <summary>Creates a Copilot response tab rendered as markdown.</summary>
    public static EditorTab CreateCopilotTab(string action, string content)
        => new() { Header = char.ToUpper(action[0]) + action[1..], Content = content, IsMarkdown = true };
}
