using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for Tool Registry bucket list operations.
/// </summary>
[ViewModelCommand("bucket-list", Description = "List tool buckets")]
public sealed class BucketListViewModel : AreaListViewModelBase<BucketDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly ILogger<BucketListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BucketListViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public BucketListViewModel(
        Dispatcher dispatcher,
        ILogger<BucketListViewModel> logger)
        : base(McpArea.ToolRegistry)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Loads registered tool buckets.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading buckets...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ListBucketsQuery(), ct).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load buckets.";
                StatusMessage = "Bucket load failed.";
                return;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} buckets.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Bucket load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
