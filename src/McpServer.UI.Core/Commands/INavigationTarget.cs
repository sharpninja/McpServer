using System.Threading.Tasks;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Models.Json;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Navigation operations: back/forward, refresh, tree item navigation.
/// </summary>
public interface INavigationTarget
{
    void NavigateBack();
    void NavigateForward();
    Task RefreshAsync();
    void PhoneNavigateSection(string? sectionKey);
    void GenerateAndNavigate(FileNode? node);
    void OpenTreeItem(FileNode? node);
    void TreeItemTapped(FileNode? node);
    void JsonNodeDoubleTapped(JsonTreeNode? node);
}

