namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided abstraction for filesystem operations used by UI.Core ViewModels.
/// </summary>
public interface IFileSystemService
{
    /// <summary>Returns true if the file at <paramref name="path"/> exists.</summary>
    bool FileExists(string path);

    /// <summary>Returns true if the directory at <paramref name="path"/> exists.</summary>
    bool DirectoryExists(string path);

    /// <summary>Reads the entire contents of a file as a string.</summary>
    string ReadAllText(string path);

    /// <summary>Asynchronously reads the entire contents of a file as a string.</summary>
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);

    /// <summary>Reads all lines of a file into a string array.</summary>
    string[] ReadAllLines(string path);

    /// <summary>Writes the specified string to a file, overwriting any existing content.</summary>
    void WriteAllText(string path, string content);

    /// <summary>Asynchronously writes the specified string to a file.</summary>
    Task WriteAllTextAsync(string path, string content, CancellationToken ct = default);

    /// <summary>Moves a file from <paramref name="source"/> to <paramref name="destination"/>.</summary>
    void MoveFile(string source, string destination);

    /// <summary>Deletes the file at <paramref name="path"/>.</summary>
    void DeleteFile(string path);

    /// <summary>Returns the UTC last-write time of the file at <paramref name="path"/>.</summary>
    DateTime GetLastWriteTimeUtc(string path);

    /// <summary>Enumerates files in a directory matching a search pattern.</summary>
    IEnumerable<FileEntry> EnumerateFiles(string directory, string searchPattern, bool recursive);

    /// <summary>Enumerates subdirectories in a directory matching a search pattern.</summary>
    IEnumerable<DirectoryEntry> EnumerateDirectories(string directory, string searchPattern, bool recursive);

    /// <summary>Returns the absolute path for the specified path string.</summary>
    string GetFullPath(string path);

    /// <summary>Returns the directory portion of the specified path.</summary>
    string? GetDirectoryName(string path);

    /// <summary>Combines path segments into a single path.</summary>
    string CombinePath(params string[] paths);
}

/// <summary>Represents a file entry returned by directory enumeration.</summary>
/// <param name="FullName">Absolute path to the file.</param>
/// <param name="Name">File name including extension.</param>
/// <param name="Extension">File extension including the leading dot.</param>
/// <param name="IsDirectory">True if this entry is a directory rather than a file.</param>
public sealed record FileEntry(string FullName, string Name, string Extension, bool IsDirectory);

/// <summary>Represents a directory entry returned by directory enumeration.</summary>
/// <param name="FullName">Absolute path to the directory.</param>
/// <param name="Name">Directory name.</param>
public sealed record DirectoryEntry(string FullName, string Name);
