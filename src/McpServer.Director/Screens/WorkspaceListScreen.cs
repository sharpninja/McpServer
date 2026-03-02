using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen that binds to <see cref="WorkspaceListViewModel"/>.
/// Displays workspaces in a TableView and a detail pane.
/// </summary>
internal sealed class WorkspaceListScreen : View
{
    private readonly WorkspaceListViewModel _listVm;
    private readonly WorkspaceDetailViewModel _detailVm;
    private readonly DirectorMcpContext? _directorContext;
    private readonly ViewModelBinder _binder = new();
    private readonly List<WorkspaceSummary> _rows = [];
    private readonly SemaphoreSlim _detailLoadGate = new(1, 1);
    private int _detailLoadRequestVersion;
    private string? _lastAutoDetailWorkspacePath;
    private TableView _tableView = null!;
    private TextView _detailView = null!;
    private Label _detailTitleLabel = null!;

    public WorkspaceListScreen(WorkspaceListViewModel listVm, WorkspaceDetailViewModel detailVm, DirectorMcpContext? directorContext = null)
    {
        _listVm = listVm;
        _detailVm = detailVm;
        _directorContext = directorContext;
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
        _tableView = new TableView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Percent(35),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _tableView.SelectedCellChanged += (_, _) => QueueSelectedRowDetailRefresh();
        Add(_tableView);

        _detailTitleLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(_tableView),
            Width = Dim.Fill(),
            Text = "Detail: (select a workspace row)",
        };
        Add(_detailTitleLabel);

        _detailView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_detailTitleLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_detailView);

        // Button bar
        var refreshBtn = new Button
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Text = "Refresh",
        };
        Add(refreshBtn);

        var detailBtn = new Button
        {
            X = Pos.Right(refreshBtn) + 2,
            Y = Pos.AnchorEnd(1),
            Text = "Load Detail",
        };
        Add(detailBtn);

        var countLabel = new Label
        {
            X = Pos.Right(detailBtn) + 2,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "",
        };
        Add(countLabel);

        // Bindings
        _binder.BindProperty(_listVm, nameof(_listVm.IsLoading), () =>
        {
            statusLabel.Text = _listVm.IsLoading ? "⏳ Loading workspaces..." : "Workspaces";
            refreshBtn.Enabled = !_listVm.IsLoading;
            detailBtn.Enabled = !_listVm.IsLoading;
        });

        _binder.BindProperty(_listVm, nameof(_listVm.ErrorMessage), () =>
        {
            errorField.Visible = !string.IsNullOrEmpty(_listVm.ErrorMessage);
            errorField.Text = _listVm.ErrorMessage ?? "";
        });

        _binder.BindProperty(_listVm, nameof(_listVm.TotalCount), () =>
        {
            countLabel.Text = $"Total: {_listVm.TotalCount}";
        });

        _binder.BindCollection(_listVm.Workspaces, _tableView, items =>
        {
            _rows.Clear();
            _rows.AddRange(items);
            return new EnumerableTableSource<WorkspaceSummary>(
                items,
                new Dictionary<string, Func<WorkspaceSummary, object>>
                {
                    ["Name"] = ws => ws.Name,
                    ["Path"] = ws => ws.WorkspacePath,
                    ["Primary"] = ws => ws.IsPrimary ? "✓" : "",
                    ["Enabled"] = ws => ws.IsEnabled ? "✓" : "✗",
                });
        });

        _binder.BindButton(refreshBtn, LoadAsync);
        _binder.BindButton(detailBtn, () => LoadSelectedDetailAsync(forceStatusMessage: true));
    }

    /// <summary>Triggers initial data load.</summary>
    public async Task LoadAsync()
    {
        await _listVm.LoadAsync().ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_listVm.ErrorMessage))
        {
            ClearDetail($"Detail: {_listVm.ErrorMessage}");
            return;
        }

        if (_rows.Count == 0)
        {
            ClearDetail("Detail: (no workspaces)");
            return;
        }

        _lastAutoDetailWorkspacePath = null;
        await LoadSelectedDetailAsync(fallbackToFirst: true).ConfigureAwait(false);
    }

    private async Task LoadSelectedDetailAsync(bool fallbackToFirst = true, bool forceStatusMessage = false)
    {
        var row = _tableView.SelectedRow;
        if (row < 0 || row >= _rows.Count)
        {
            if (!fallbackToFirst || _rows.Count == 0)
            {
                if (forceStatusMessage)
                    ShowDetailText("Detail: (none selected)", "Select a workspace row first.");
                return;
            }

            row = 0;
        }

        var selected = _rows[row];
        var requestVersion = Interlocked.Increment(ref _detailLoadRequestVersion);
        await _detailLoadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            _detailVm.WorkspacePath = selected.WorkspacePath;
            await _detailVm.LoadAsync().ConfigureAwait(false);

            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            if (!string.IsNullOrWhiteSpace(_detailVm.ErrorMessage))
            {
                ShowDetailText($"Detail: {selected.Name}", _detailVm.ErrorMessage!);
                return;
            }

            if (_detailVm.Detail is null)
            {
                ShowDetailText($"Detail: {selected.Name}", "(workspace not found)");
                return;
            }

            ShowDetailText(
                $"Detail: {_detailVm.Detail.Name}",
                FormatDetail(_detailVm.Detail));
        }
        finally
        {
            _detailLoadGate.Release();
        }
    }

    private void QueueSelectedRowDetailRefresh()
    {
        var row = _tableView.SelectedRow;
        if (row < 0 || row >= _rows.Count)
            return;

        var workspacePath = _rows[row].WorkspacePath;
        if (string.Equals(_lastAutoDetailWorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase))
            return;

        _lastAutoDetailWorkspacePath = workspacePath;
        _ = Task.Run(() => LoadSelectedDetailAsync(fallbackToFirst: false));
    }

    private void ClearDetail(string title) => ShowDetailText(title, "");

    private void ShowDetailText(string title, string text)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = title;
            _detailView.Text = text;
        });
    }

    private static string FormatDetail(WorkspaceDetail detail)
    {
        var lines = new List<string>
        {
            $"Path: {detail.WorkspacePath}",
            $"Name: {detail.Name}",
            $"Todo Path: {detail.TodoPath}",
            $"Data Dir: {detail.DataDirectory ?? ""}",
            $"Tunnel Provider: {detail.TunnelProvider ?? ""}",
            $"Run As: {detail.RunAs ?? ""}",
            $"Primary: {detail.IsPrimary}",
            $"Enabled: {detail.IsEnabled}",
            $"Created: {detail.DateTimeCreated:yyyy-MM-dd HH:mm:ss zzz}",
            $"Modified: {detail.DateTimeModified:yyyy-MM-dd HH:mm:ss zzz}",
        };

        AppendList(lines, "Banned Licenses", detail.BannedLicenses);
        AppendList(lines, "Banned Countries", detail.BannedCountriesOfOrigin);
        AppendList(lines, "Banned Organizations", detail.BannedOrganizations);
        AppendList(lines, "Banned Individuals", detail.BannedIndividuals);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendList(List<string> lines, string title, IReadOnlyList<string> values)
    {
        lines.Add("");
        lines.Add($"{title}:");
        if (values.Count == 0)
        {
            lines.Add("  (none)");
            return;
        }

        lines.AddRange(values.Select(v => $"  - {v}"));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) _binder.Dispose();
        base.Dispose(disposing);
    }
}
