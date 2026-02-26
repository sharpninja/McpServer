using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for tunnel provider management.
/// Displays a grid of tunnel providers with enable/disable toggle, status, and action buttons.
/// </summary>
internal sealed class TunnelScreen : View
{
    private readonly TunnelListViewModel _vm;
    private readonly ViewModelBinder _binder = new();
    private readonly List<TunnelProviderSnapshot> _rows = [];
    private TableView _tableView = null!;
    private Label _statusLabel = null!;
    private Button _enableDisableBtn = null!;
    private Button _startStopBtn = null!;
    private Button _restartBtn = null!;
    private Button _refreshBtn = null!;

    /// <summary>Initializes a new instance of the <see cref="TunnelScreen"/> class.</summary>
    public TunnelScreen(TunnelListViewModel vm)
    {
        _vm = vm;
        Title = "Tunnels";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _statusLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Tunnel Providers",
        };
        Add(_statusLabel);

        var errorField = new TextField
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "",
            ReadOnly = true,
            Visible = false,
        };
        var errorColorScheme = Colors.ColorSchemes.TryGetValue("Error", out var errScheme) ? errScheme : null;
        if (errorColorScheme is not null)
            errorField.ColorScheme = errorColorScheme;
        Add(errorField);

        _tableView = new TableView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _tableView.SelectedCellChanged += (_, _) => UpdateActionButtons();
        Add(_tableView);

        // Button bar
        _enableDisableBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Enable", Enabled = false };
        _enableDisableBtn.Accepting += (_, _) => _ = Task.Run(ToggleEnableAsync);

        _startStopBtn = new Button { X = Pos.Right(_enableDisableBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Start", Enabled = false };
        _startStopBtn.Accepting += (_, _) => _ = Task.Run(ToggleStartStopAsync);

        _restartBtn = new Button { X = Pos.Right(_startStopBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Restart", Enabled = false };
        _restartBtn.Accepting += (_, _) => _ = Task.Run(RestartSelectedAsync);

        _refreshBtn = new Button { X = Pos.Right(_restartBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        _refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);

        var countLabel = new Label
        {
            X = Pos.Right(_refreshBtn) + 2,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "",
        };

        Add(_enableDisableBtn, _startStopBtn, _restartBtn, _refreshBtn, countLabel);

        // Bindings
        _binder.BindProperty(_vm, nameof(_vm.IsLoading), () =>
        {
            _statusLabel.Text = _vm.IsLoading ? "⏳ Loading tunnel providers..." : "Tunnel Providers";
            _refreshBtn.Enabled = !_vm.IsLoading;
        });

        _binder.BindProperty(_vm, nameof(_vm.ErrorMessage), () =>
        {
            errorField.Visible = !string.IsNullOrEmpty(_vm.ErrorMessage);
            errorField.Text = _vm.ErrorMessage ?? "";
        });

        _binder.BindProperty(_vm, nameof(_vm.TotalCount), () =>
        {
            countLabel.Text = $"Providers: {_vm.TotalCount}";
        });

        _binder.BindCollection(_vm.Items, _tableView, items =>
        {
            _rows.Clear();
            _rows.AddRange(items);
            UpdateActionButtons();
            return new EnumerableTableSource<TunnelProviderSnapshot>(
                items,
                new Dictionary<string, Func<TunnelProviderSnapshot, object>>
                {
                    ["Provider"] = p => p.Provider,
                    ["Enabled"] = p => p.Enabled ? "✓" : "✗",
                    ["Status"] = p => p.IsRunning ? "🟢 Running" : "⚫ Stopped",
                    ["Public URL"] = p => p.PublicUrl ?? "",
                    ["Error"] = p => p.Error ?? "",
                });
        });
    }

    /// <summary>Triggers initial data load.</summary>
    public async Task LoadAsync()
    {
        await _vm.LoadAsync().ConfigureAwait(false);
        Application.Invoke(UpdateActionButtons);
    }

    private TunnelProviderSnapshot? GetSelectedProvider()
    {
        var row = _tableView.SelectedRow;
        return row >= 0 && row < _rows.Count ? _rows[row] : null;
    }

    private void UpdateActionButtons()
    {
        var selected = GetSelectedProvider();
        if (selected is null)
        {
            _enableDisableBtn.Enabled = false;
            _startStopBtn.Enabled = false;
            _restartBtn.Enabled = false;
            return;
        }

        _enableDisableBtn.Enabled = true;
        _enableDisableBtn.Text = selected.Enabled ? "Disable" : " Enable";

        _startStopBtn.Enabled = true;
        _startStopBtn.Text = selected.IsRunning ? " Stop" : "Start";

        _restartBtn.Enabled = selected.Enabled;
    }

    private async Task ToggleEnableAsync()
    {
        var selected = GetSelectedProvider();
        if (selected is null) return;

        if (selected.Enabled)
            await _vm.DisableAsync(selected.Provider).ConfigureAwait(false);
        else
            await _vm.EnableAsync(selected.Provider).ConfigureAwait(false);

        Application.Invoke(UpdateActionButtons);
    }

    private async Task ToggleStartStopAsync()
    {
        var selected = GetSelectedProvider();
        if (selected is null) return;

        if (selected.IsRunning)
            await _vm.StopAsync(selected.Provider).ConfigureAwait(false);
        else
            await _vm.StartAsync(selected.Provider).ConfigureAwait(false);

        Application.Invoke(UpdateActionButtons);
    }

    private async Task RestartSelectedAsync()
    {
        var selected = GetSelectedProvider();
        if (selected is null) return;

        await _vm.RestartAsync(selected.Provider).ConfigureAwait(false);
        Application.Invoke(UpdateActionButtons);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
