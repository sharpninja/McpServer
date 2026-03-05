using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Authorization;

namespace McpServer.UI.Core.ViewModels.Base;

/// <summary>
/// Shared base class for area list/grid ViewModels.
/// Provides common collection, selection, and load state properties.
/// </summary>
/// <typeparam name="TItem">Primary list item type.</typeparam>
public abstract partial class AreaListViewModelBase<TItem> : ObservableObject
    where TItem : class
{
    /// <summary>Initializes a new instance for the specified area.</summary>
    /// <param name="area">Logical MCP area represented by the ViewModel.</param>
    protected AreaListViewModelBase(McpArea area)
    {
        Area = area;
    }

    /// <summary>The logical area represented by this list ViewModel.</summary>
    public McpArea Area { get; }

    /// <summary>Items displayed in the list/grid.</summary>
    public ObservableCollection<TItem> Items { get; } = [];

    /// <summary>Index of the currently selected row. Set by the view.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedItem))]
    private int _selectedIndex = -1;

    /// <summary>Currently selected item, derived from <see cref="SelectedIndex"/>.</summary>
    public TItem? SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count
            ? Items[SelectedIndex]
            : null;

    /// <summary>Total item count from the last load.</summary>
    [ObservableProperty]
    private int _totalCount;

    /// <summary>Whether a refresh/load operation is in progress.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Last load error, if any.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Optional status text for the list view.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Timestamp of the last successful refresh.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastRefreshedAt;

    /// <summary>Clears the list and resets count.</summary>
    protected void ClearItems()
    {
        Items.Clear();
        TotalCount = 0;
    }

    /// <summary>Replaces the list contents with the specified items and updates counters.</summary>
    /// <param name="items">Items to display.</param>
    /// <param name="totalCount">Total count from the source.</param>
    protected void SetItems(IEnumerable<TItem> items, int totalCount)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);

        TotalCount = totalCount;
        LastRefreshedAt = DateTimeOffset.UtcNow;
    }
}
