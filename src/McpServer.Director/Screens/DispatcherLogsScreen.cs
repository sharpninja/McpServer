using McpServer.Cqrs;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for local CQRS dispatcher log history captured in-memory by the Director process.
/// </summary>
internal sealed class DispatcherLogsScreen : View
{
    private readonly DispatcherLogsViewModel _viewModel;
    private TableView _table = null!;
    private TextView _detailView = null!;
    private TextView _statusView = null!;
    private Label _detailTitleLabel = null!;
    private readonly List<DispatchLogRecord> _rows = [];

    public DispatcherLogsScreen(DispatcherLogsViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = "Logs";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(38),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _table.SelectedCellChanged += (_, _) =>
        {
            if (_rows.Count > 0)
                ShowSelectedDetail();
        };
        Add(_table);

        _detailTitleLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(_table),
            Width = Dim.Fill(),
            Text = "Detail: (select a row)",
        };
        Add(_detailTitleLabel);

        _detailView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_detailTitleLabel),
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            ReadOnly = true,
            WordWrap = false,
            Text = "",
        };
        Add(_detailView);

        _statusView = new TextView
        {
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = false,
            Text = "",
        };
        Add(_statusView);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Refresh" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAsync);
        Add(refreshBtn);

        var viewBtn = new Button { X = Pos.Right(refreshBtn) + 2, Y = Pos.AnchorEnd(1), Text = "View Selected" };
        viewBtn.Accepting += (_, _) => ShowSelectedDetail();
        Add(viewBtn);

        var clearBtn = new Button { X = Pos.Right(viewBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Clear Detail" };
        clearBtn.Accepting += (_, _) => ClearDetail();
        Add(clearBtn);
    }

    public async Task LoadAsync()
    {
        SetStatus("⏳ Loading dispatcher logs...");
        try
        {
            await _viewModel.LoadAsync().ConfigureAwait(false);

            var rows = _viewModel.Items
                .Select(r => new DispatchRow(
                    r.FinishedAt.ToLocalTime().ToString("HH:mm:ss"),
                    r.OperationName,
                    r.Outcome,
                    r.ElapsedMilliseconds,
                    r.CorrelationId,
                    r.Error ?? ""))
                .ToList();

            _rows.Clear();
            _rows.AddRange(_viewModel.Items);

            Application.Invoke(() =>
            {
                _table.Table = new EnumerableTableSource<DispatchRow>(rows,
                    new Dictionary<string, Func<DispatchRow, object>>
                    {
                        ["Time"] = r => r.Time,
                        ["Operation"] = r => r.Operation,
                        ["Outcome"] = r => r.Outcome,
                        ["ms"] = r => r.ElapsedMs,
                        ["Correlation"] = r => r.CorrelationId,
                    });
            });

            SetStatus(_viewModel.ErrorMessage is null
                ? $"✓ {_rows.Count} dispatch logs (active: {_viewModel.ActiveDispatchCount})"
                : $"✗ {_viewModel.ErrorMessage}");

            if (_viewModel.ErrorMessage is null && _rows.Count > 0)
                ShowSelectedDetail(fallbackToFirst: true);
            else if (_rows.Count == 0)
                ClearDetail("Detail: (no dispatcher logs yet)");
        }
        catch (Exception ex)
        {
            SetStatus($"✗ {ex.Message}");
        }
    }

    private void ShowSelectedDetail(bool fallbackToFirst = false)
    {
        var rowIndex = _table.SelectedRow;
        if (rowIndex < 0 || rowIndex >= _rows.Count)
        {
            if (!fallbackToFirst || _rows.Count == 0)
            {
                SetStatus("✗ Select a log row first.");
                return;
            }

            rowIndex = 0;
        }

        var record = _rows[rowIndex];
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = $"Detail: {record.OperationName} [{record.CorrelationId}]";
            _detailView.Text = FormatRecord(record);
        });
    }

    private void ClearDetail() => ClearDetail("Detail: (cleared)");

    private void ClearDetail(string title)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = title;
            _detailView.Text = "";
        });
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusView.Text = text);

    private static string FormatRecord(DispatchLogRecord record)
    {
        var lines = new List<string>
        {
            $"Operation: {record.OperationName}",
            $"Outcome: {record.Outcome}",
            $"Correlation: {record.CorrelationId}",
            $"Started: {record.StartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff zzz}",
            $"Finished: {record.FinishedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff zzz}",
            $"Elapsed: {record.ElapsedMilliseconds} ms",
        };

        if (!string.IsNullOrWhiteSpace(record.UserName) || !string.IsNullOrWhiteSpace(record.UserId))
            lines.Add($"User: {record.UserName ?? ""} ({record.UserId ?? ""})".Trim());

        if (record.Roles.Count > 0)
            lines.Add($"Roles: {string.Join(", ", record.Roles)}");

        if (!string.IsNullOrWhiteSpace(record.Error))
            lines.Add($"Error: {record.Error}");

        lines.Add("");
        lines.Add($"Entries ({record.Entries.Count}):");

        foreach (var entry in record.Entries)
        {
            lines.Add($"[{entry.Timestamp.ToLocalTime():HH:mm:ss.fff}] {entry.Level}: {entry.Message}");
            if (!string.IsNullOrWhiteSpace(entry.ExceptionType) || !string.IsNullOrWhiteSpace(entry.ExceptionMessage))
                lines.Add($"    EX: {entry.ExceptionType}: {entry.ExceptionMessage}".TrimEnd());
        }

        return string.Join(Environment.NewLine, lines);
    }

    private sealed record DispatchRow(
        string Time,
        string Operation,
        string Outcome,
        long ElapsedMs,
        string CorrelationId,
        string Error);
}
