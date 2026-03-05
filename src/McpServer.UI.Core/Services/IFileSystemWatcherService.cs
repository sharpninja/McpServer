namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided abstraction for watching filesystem changes.
/// </summary>
public interface IFileSystemWatcherService
{
    /// <summary>
    /// Watches a directory for changes matching the specified filter.
    /// </summary>
    /// <param name="directory">Directory to watch.</param>
    /// <param name="filter">File name filter (e.g. "*.yaml").</param>
    /// <param name="onChanged">Callback invoked with the full path of the changed file.</param>
    /// <returns>A handle that can be used to stop watching.</returns>
    IWatcherHandle Watch(string directory, string filter, Action<string> onChanged);
}

/// <summary>Handle to an active filesystem watcher that can be stopped.</summary>
public interface IWatcherHandle : IDisposable
{
    /// <summary>Stops watching without disposing.</summary>
    void Stop();
}
