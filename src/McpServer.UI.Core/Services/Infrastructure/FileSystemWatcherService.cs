using System;
using System.IO;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services.Infrastructure;

/// <summary>
/// Host implementation of <see cref="IFileSystemWatcherService"/> backed by <see cref="FileSystemWatcher"/>.
/// </summary>
public sealed class FileSystemWatcherService : IFileSystemWatcherService
{
    public IWatcherHandle Watch(string directory, string filter, Action<string> onChanged)
        => new WatcherHandle(directory, filter, onChanged);

    private sealed class WatcherHandle : IWatcherHandle
    {
        private FileSystemWatcher? _watcher;

        public WatcherHandle(string directory, string filter, Action<string> onChanged)
        {
            _watcher = new FileSystemWatcher(directory, filter)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _watcher.Changed += (_, e) => onChanged(e.FullPath);
            _watcher.Created += (_, e) => onChanged(e.FullPath);
            _watcher.Renamed += (_, e) => onChanged(e.FullPath);
        }

        public void Stop()
        {
            if (_watcher is not null)
                _watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }
    }
}

