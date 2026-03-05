using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Authorization;

namespace McpServer.UI.Core.ViewModels.Base;

/// <summary>
/// Shared base class for area detail/edit ViewModels.
/// Provides common detail load/edit/save state properties.
/// </summary>
/// <typeparam name="TDetail">Detail model type.</typeparam>
public abstract partial class AreaDetailViewModelBase<TDetail> : ObservableObject
    where TDetail : class
{
    /// <summary>Initializes a new instance for the specified area.</summary>
    /// <param name="area">Logical MCP area represented by the ViewModel.</param>
    protected AreaDetailViewModelBase(McpArea area)
    {
        Area = area;
    }

    /// <summary>The logical area represented by this detail ViewModel.</summary>
    public McpArea Area { get; }

    /// <summary>Current detail model.</summary>
    [ObservableProperty]
    private TDetail? _detail;

    /// <summary>Whether a load or save operation is active.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Whether the detail has unsaved changes.</summary>
    [ObservableProperty]
    private bool _isDirty;

    /// <summary>Last error message.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Optional status message.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Timestamp of the last successful load/save operation.</summary>
    [ObservableProperty]
    private DateTimeOffset? _lastUpdatedAt;
}
