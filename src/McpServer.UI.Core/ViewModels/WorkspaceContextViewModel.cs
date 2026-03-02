using CommunityToolkit.Mvvm.ComponentModel;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Singleton ViewModel that holds the active workspace path.
/// Workspace-scoped ViewModels subscribe to <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
/// and react when <see cref="ActiveWorkspacePath"/> changes.
/// </summary>
public sealed partial class WorkspaceContextViewModel : ObservableObject
{
    /// <summary>The active workspace path selected in the Director UI.</summary>
    [ObservableProperty]
    private string? _activeWorkspacePath;
}
