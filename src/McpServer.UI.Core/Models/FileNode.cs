using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace McpServer.UI.Core.Models;

public partial class FileNode : ObservableObject
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsDirectory { get; set; }
    public ObservableCollection<FileNode> Children { get; set; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    public FileNode(string path, bool isDirectory)
    {
        Path = path;
        Name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(Name) && isDirectory) 
            Name = path; // Root case
        
        IsDirectory = isDirectory;
    }
}

