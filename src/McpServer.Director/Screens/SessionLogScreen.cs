using McpServer.UI.Core.ViewModels;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

/// <summary>Terminal.Gui screen for viewing session logs.</summary>
internal sealed class SessionLogScreen : View
{
    private readonly SessionLogListViewModel _viewModel;
    private TableView _table = null!;
    private TextView _statusLabel = null!;
    private readonly ILogger<SessionLogScreen> _logger;


    public SessionLogScreen(SessionLogListViewModel viewModel,
        ILogger<SessionLogScreen>? logger = null)
    {
        _logger = logger ?? NullLogger<SessionLogScreen>.Instance;
        _viewModel = viewModel;
        Title = "Session Logs";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _table = new TableView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2),
            FullRowSelect = true,
            MultiSelect = false,
        };
        Add(_table);

        _statusLabel = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_statusLabel);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);
        Add(refreshBtn);
    }

    public async Task LoadAsync()
    {
        SetStatus("⏳ Loading session logs...");
        try
        {
            await _viewModel.LoadAsync().ConfigureAwait(false);

            var rows = _viewModel.Items
                .Select(item => new SessionRow(
                    item.SessionId,
                    item.SourceType,
                    item.Title,
                    item.Status,
                    item.LastUpdated ?? ""))
                .ToList();

            Application.Invoke(() =>
            {
                _table.Table = new EnumerableTableSource<SessionRow>(rows,
                    new Dictionary<string, Func<SessionRow, object>>
                    {
                        ["ID"] = r => r.Id,
                        ["Source"] = r => r.Source,
                        ["Title"] = r => r.Title,
                        ["Status"] = r => r.Status,
                        ["Updated"] = r => r.Updated,
                    });
            });
            SetStatus(_viewModel.ErrorMessage is null
                ? $"✓ {rows.Count} logs"
                : $"✗ {_viewModel.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private sealed record SessionRow(string Id, string Source, string Title, string Status, string Updated);
}
