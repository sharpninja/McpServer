using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.Models;

/// <summary>
/// Single message in the AI chat transcript.
/// </summary>
public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _role = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;
}
