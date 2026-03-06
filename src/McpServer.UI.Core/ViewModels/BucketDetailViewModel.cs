using System.Collections.ObjectModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for bucket detail, browse, sync, and install workflows.
/// </summary>
[ViewModelCommand("bucket-detail", Description = "Manage tool buckets and bucket tools")]
public sealed class BucketDetailViewModel : AreaDetailViewModelBase<BucketDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<BucketDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BucketDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public BucketDetailViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<BucketDetailViewModel> logger)
        : base(McpArea.ToolRegistry)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>Manifest tools discovered by the latest browse operation.</summary>
    public ObservableCollection<BucketToolManifest> BrowsedTools { get; } = [];

    /// <summary>
    /// Adds a bucket.
    /// </summary>
    /// <param name="command">Add-bucket payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public Task<BucketMutationOutcome?> AddAsync(AddBucketCommand command, CancellationToken ct = default)
        => SendBucketMutationAsync(command, $"Adding bucket '{command.Name}'...", ct);

    /// <summary>
    /// Removes a bucket.
    /// </summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="uninstallTools">Whether to uninstall bucket-owned tools.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public Task<BucketMutationOutcome?> RemoveAsync(string name, bool uninstallTools = false, CancellationToken ct = default)
        => SendBucketMutationAsync(new RemoveBucketCommand(name, uninstallTools), $"Removing bucket '{name}'...", ct);

    /// <summary>
    /// Browses manifest tools from a bucket.
    /// </summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Browse outcome on success, otherwise null.</returns>
    public async Task<BucketBrowseOutcome?> BrowseAsync(string name, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Browsing bucket '{name}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new BrowseBucketQuery(name), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Bucket browse failed.";
                StatusMessage = "Bucket browse failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Bucket browse failed.";
                StatusMessage = "Bucket browse failed.";
                return result.Value;
            }

            BrowsedTools.Clear();
            foreach (var tool in result.Value.Tools)
                BrowsedTools.Add(tool);

            StatusMessage = $"Loaded {BrowsedTools.Count} manifests from '{name}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Bucket browse failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Synchronizes a bucket with its remote source.
    /// </summary>
    /// <param name="name">Bucket name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sync outcome on success, otherwise null.</returns>
    public async Task<BucketSyncOutcome?> SyncAsync(string name, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Syncing bucket '{name}'...";
        try
        {
            var result = await _dispatcher.SendAsync(new SyncBucketCommand(name), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Bucket sync failed.";
                StatusMessage = "Bucket sync failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Bucket sync failed.";
                StatusMessage = "Bucket sync failed.";
                return result.Value;
            }

            StatusMessage = $"Synced '{name}' (added {result.Value.Added}, updated {result.Value.Updated}, unchanged {result.Value.Unchanged}).";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Bucket sync failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Installs a manifest tool from a bucket into the current workspace scope.
    /// </summary>
    /// <param name="bucketName">Bucket name.</param>
    /// <param name="toolName">Tool manifest name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tool mutation outcome on success, otherwise null.</returns>
    public async Task<ToolMutationOutcome?> InstallAsync(string bucketName, string toolName, CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Installing '{toolName}' from '{bucketName}'...";
        try
        {
            var command = new InstallFromBucketCommand(bucketName, toolName, _workspaceContext.ActiveWorkspacePath);
            var result = await _dispatcher.SendAsync(command, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Install failed.";
                StatusMessage = "Install failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Install failed.";
                StatusMessage = "Install failed.";
                return result.Value;
            }

            StatusMessage = $"Installed '{toolName}' from '{bucketName}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Install failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<BucketMutationOutcome?> SendBucketMutationAsync<TCommand>(TCommand command, string pendingStatus, CancellationToken ct)
        where TCommand : ICommand<BucketMutationOutcome>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Bucket mutation failed.";
                StatusMessage = "Bucket mutation failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Bucket mutation failed.";
                StatusMessage = "Bucket mutation failed.";
                return result.Value;
            }

            if (result.Value.Bucket is not null)
                Detail = result.Value.Bucket;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = "Bucket operation completed.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Bucket mutation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
