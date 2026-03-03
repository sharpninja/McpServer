using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Lightweight binding helper that connects CommunityToolkit.Mvvm ViewModels
/// to Terminal.Gui v2 controls via INotifyPropertyChanged and INotifyCollectionChanged.
/// </summary>
internal sealed class ViewModelBinder : IDisposable
{
    private readonly List<Action> _cleanupActions = [];

    /// <summary>
    /// Binds a ViewModel property to a Terminal.Gui control update action.
    /// When the property changes, the action is invoked on the UI thread.
    /// </summary>
    public void BindProperty<TViewModel>(
        TViewModel viewModel,
        string propertyName,
        Action onChanged)
        where TViewModel : INotifyPropertyChanged
    {
        void Handler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == propertyName || string.IsNullOrEmpty(e.PropertyName))
            {
                Application.Invoke(() => onChanged());
            }
        }

        viewModel.PropertyChanged += Handler;
        _cleanupActions.Add(() => viewModel.PropertyChanged -= Handler);

        // Initial sync
        Application.Invoke(() => onChanged());
    }

    /// <summary>
    /// Binds a ViewModel string property to a Label's Text.
    /// </summary>
    public void BindLabel<TViewModel>(
        TViewModel viewModel,
        string propertyName,
        Label label,
        Func<TViewModel, string> getter)
        where TViewModel : INotifyPropertyChanged
    {
        BindProperty(viewModel, propertyName, () =>
        {
            label.Text = getter(viewModel);
        });
    }

    /// <summary>
    /// Binds a ViewModel bool property to a View's Visible.
    /// </summary>
    public void BindVisible<TViewModel>(
        TViewModel viewModel,
        string propertyName,
        View view,
        Func<TViewModel, bool> getter)
        where TViewModel : INotifyPropertyChanged
    {
        BindProperty(viewModel, propertyName, () =>
        {
            view.Visible = getter(viewModel);
        });
    }

    /// <summary>
    /// Binds an ObservableCollection to a TableView, rebuilding rows on changes.
    /// </summary>
    public void BindCollection<T>(
        ObservableCollection<T> collection,
        TableView tableView,
        Func<IReadOnlyList<T>, EnumerableTableSource<T>> sourceFactory)
    {
        void Refresh()
        {
            Application.Invoke(() =>
            {
                tableView.Table = sourceFactory(collection.ToList());
                tableView.SetNeedsDraw();
            });
        }

        void Handler(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

        collection.CollectionChanged += Handler;
        _cleanupActions.Add(() => collection.CollectionChanged -= Handler);

        // Initial sync
        Refresh();
    }

    /// <summary>
    /// Binds a Button click to an async action (e.g., ViewModel command).
    /// </summary>
    public void BindButton(Button button, Func<Task> asyncAction)
    {
        void Handler(object? sender, CommandEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await asyncAction().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                    Application.Invoke(() =>
                        MessageBox.ErrorQuery("Error", ex.Message, "OK"));
                }
            });
        }

        button.Accepting += Handler;
        _cleanupActions.Add(() => button.Accepting -= Handler);
    }

    /// <summary>
    /// Binds a TextField's text changes back to a ViewModel property setter.
    /// </summary>
    public void BindTextField(TextField textField, Action<string> setter)
    {
        void Handler(object? sender, EventArgs e)
        {
            setter(textField.Text ?? "");
        }

        textField.TextChanged += Handler;
        _cleanupActions.Add(() => textField.TextChanged -= Handler);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var cleanup in _cleanupActions)
        {
            try { cleanup(); } catch { /* ignore */ }
        }
        _cleanupActions.Clear();
    }

    /// <summary>Enables auto-show scrollbars on all scrollable descendant views recursively.</summary>
    internal static void EnableScrollBars(View root)
    {
        foreach (var view in root.Subviews)
        {
            if (view is TextView or TableView or ListView)
            {
                view.VerticalScrollBar.AutoShow = true;
                view.HorizontalScrollBar.AutoShow = true;
            }

            // TabView tab content isn't in the normal Subviews tree
            if (view is TabView tabView)
            {
                foreach (var tab in tabView.Tabs)
                {
                    if (tab.View is not null)
                        EnableScrollBars(tab.View);
                }
            }

            EnableScrollBars(view);
        }
    }
}
