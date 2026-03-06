using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services.Infrastructure;

/// <summary>
/// Host implementation of <see cref="IFileSystemService"/> backed by <see cref="System.IO"/>.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
        => File.ReadAllTextAsync(path, ct);

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, content, ct);

    public void MoveFile(string source, string destination) => File.Move(source, destination);

    public void DeleteFile(string path) => File.Delete(path);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public IEnumerable<FileEntry> EnumerateFiles(string directory, string searchPattern, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return new DirectoryInfo(directory)
            .EnumerateFiles(searchPattern, option)
            .Select(fi => new FileEntry(fi.FullName, fi.Name, fi.Extension, false));
    }

    public IEnumerable<DirectoryEntry> EnumerateDirectories(string directory, string searchPattern, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return new DirectoryInfo(directory)
            .EnumerateDirectories(searchPattern, option)
            .Select(di => new DirectoryEntry(di.FullName, di.Name));
    }

    public string GetFullPath(string path) => Path.GetFullPath(path);

    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string CombinePath(params string[] paths) => Path.Combine(paths);
}

