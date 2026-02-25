using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen that binds to <see cref="WorkspaceListViewModel"/>.
/// Displays workspaces in a TableView and provides a Refresh button.
/// </summary>
internal sealed class WorkspaceListScreen : View
{
    private readonly WorkspaceListViewModel _vm;
    private readonly ViewModelBinder _binder = new();

    public WorkspaceListScreen(WorkspaceListViewModel vm)
    {
        _vm = vm;
        Title = "Workspaces";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        BuildUi();
    }

    private void BuildUi()
    {
        // Status label
        var statusLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Loading...",
        };
        Add(statusLabel);

        // Error field (TextField so text is selectable/copyable with Ctrl+C)
        var errorColorScheme = Colors.ColorSchemes.TryGetValue("Error", out var errScheme) ? errScheme : null;
        var errorField = new TextField
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "",
            ReadOnly = true,
            Visible = false,
        };
        if (errorColorScheme is not null)
            errorField.ColorScheme = errorColorScheme;
        Add(errorField);

        // Table
        var tableView = new TableView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            FullRowSelect = true,
        };
        Add(tableView);

        // Button bar
        var refreshBtn = new Button
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Text = "Refresh",
        };
        Add(refreshBtn);

        var countLabel = new Label
        {
            X = Pos.Right(refreshBtn) + 2,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "",
        };
        Add(countLabel);

        // Bindings
        _binder.BindProperty(_vm, nameof(_vm.IsLoading), () =>
        {
            statusLabel.Text = _vm.IsLoading ? "⏳ Loading workspaces..." : "Workspaces";
            refreshBtn.Enabled = !_vm.IsLoading;
        });

        _binder.BindProperty(_vm, nameof(_vm.ErrorMessage), () =>
        {
            errorField.Visible = !string.IsNullOrEmpty(_vm.ErrorMessage);
            errorField.Text = _vm.ErrorMessage ?? "";
        });

        _binder.BindProperty(_vm, nameof(_vm.TotalCount), () =>
        {
            countLabel.Text = $"Total: {_vm.TotalCount}";
        });

        _binder.BindCollection(_vm.Workspaces, tableView, items =>
            new EnumerableTableSource<WorkspaceSummary>(
                items,
                new Dictionary<string, Func<WorkspaceSummary, object>>
                {
                    ["Name"] = ws => ws.Name,
                    ["Path"] = ws => ws.WorkspacePath,
                    ["Port"] = ws => ws.WorkspacePort,
                    ["Primary"] = ws => ws.IsPrimary ? "✓" : "",
                    ["Enabled"] = ws => ws.IsEnabled ? "✓" : "✗",
                }));

        _binder.BindButton(refreshBtn, () => _vm.LoadAsync());
    }

    /// <summary>Triggers initial data load.</summary>
    public Task LoadAsync() => _vm.LoadAsync();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
