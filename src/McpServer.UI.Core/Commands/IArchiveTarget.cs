using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Commands;

/// <summary>
/// Archive operations for current file and tree items.
/// </summary>
public interface IArchiveTarget
{
    void Archive();
    void ArchiveTreeItem(FileNode? node);
}

