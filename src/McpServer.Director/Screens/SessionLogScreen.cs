using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

/// <summary>Terminal.Gui screen for viewing and drilling into session logs.</summary>
internal sealed class SessionLogScreen : View
{
    private readonly SessionLogListViewModel _listViewModel;
    private readonly SessionLogDetailViewModel _detailViewModel;
    private volatile bool _isLoadingExplicitly;
    private readonly List<SessionRow> _rows = [];
    private readonly SemaphoreSlim _detailLoadGate = new(1, 1);
    private int _detailLoadRequestVersion;
    private TableView _table = null!;
    private TextView _detailView = null!;
    private Label _detailTitleLabel = null!;
    private TextView _statusLabel = null!;
    private readonly ILogger<SessionLogScreen> _logger;


    public SessionLogScreen(SessionLogListViewModel listViewModel,
        SessionLogDetailViewModel detailViewModel,
        ILogger<SessionLogScreen>? logger = null)
    {
        _logger = logger ?? NullLogger<SessionLogScreen>.Instance;
        _listViewModel = listViewModel;
        _detailViewModel = detailViewModel;
        Title = "Session Logs";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();

        // When the ViewModel reloads (e.g. workspace change), rebuild the table.
        _listViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SessionLogListViewModel.LastRefreshedAt) &&
                !_isLoadingExplicitly)
            {
                RebuildTableFromViewModel();
                QueueSelectedRowDetailRefresh();
            }
        };
    }

    private void BuildUi()
    {
        _table = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(35),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _table.SelectedCellChanged += (_, _) => QueueSelectedRowDetailRefresh();
        Add(_table);

        _detailTitleLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(_table),
            Width = Dim.Fill(),
            Text = "Detail: (select a session log row)",
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

        var viewBtn = new Button { X = Pos.Right(refreshBtn) + 2, Y = Pos.AnchorEnd(1), Text = "View Selected" };
        viewBtn.Accepting += (_, _) => _ = Task.Run(() => LoadSelectedDetailAsync(forceStatusMessage: true));
        Add(viewBtn);

        var clearBtn = new Button { X = Pos.Right(viewBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Clear Detail" };
        clearBtn.Accepting += (_, _) => ClearDetail();
        Add(clearBtn);
    }

    public async Task LoadAsync()
    {
        SetStatus("⏳ Loading session logs...");
        try
        {
            _isLoadingExplicitly = true;
            await _listViewModel.LoadAsync().ConfigureAwait(true);
            RebuildTableFromViewModel();
            if (_rows.Count > 0)
                await LoadSelectedDetailAsync(fallbackToFirst: true).ConfigureAwait(true);
            else
                ClearDetail("Detail: (no session logs)");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
        finally
        {
            _isLoadingExplicitly = false;
        }
    }

    private void RebuildTableFromViewModel()
    {
        var rows = _listViewModel.Items
            .Select(item => new SessionRow(
                item.SessionId,
                item.SourceType,
                item.Title,
                item.Status,
                FormatTimestamp(item.LastUpdated)))
            .ToList();

        _rows.Clear();
        _rows.AddRange(rows);

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
        SetStatus(_listViewModel.ErrorMessage is null
            ? $"✓ {rows.Count} logs"
            : $"✗ {_listViewModel.ErrorMessage}");
    }

    private void QueueSelectedRowDetailRefresh()
    {
        if (_rows.Count == 0)
            return;

        var requestVersion = Interlocked.Increment(ref _detailLoadRequestVersion);
        _ = Task.Run(async () =>
        {
            await Task.Delay(120).ConfigureAwait(true);
            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            await LoadSelectedDetailAsync(fallbackToFirst: false).ConfigureAwait(true);
        });
    }

    private async Task LoadSelectedDetailAsync(bool fallbackToFirst = false, bool forceStatusMessage = false)
    {
        var row = _table.SelectedRow;
        if (row < 0 || row >= _rows.Count)
        {
            if (!fallbackToFirst || _rows.Count == 0)
            {
                if (forceStatusMessage)
                    SetStatus("✗ Select a session log row first.");
                return;
            }

            row = 0;
        }

        var selected = _rows[row];
        var requestVersion = Interlocked.Increment(ref _detailLoadRequestVersion);
        await _detailLoadGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            _detailViewModel.SessionId = selected.Id;
            await _detailViewModel.LoadAsync().ConfigureAwait(true);

            if (requestVersion != Volatile.Read(ref _detailLoadRequestVersion))
                return;

            if (!string.IsNullOrWhiteSpace(_detailViewModel.ErrorMessage))
            {
                ShowDetailText($"Detail: {selected.Id}", _detailViewModel.ErrorMessage!);
                return;
            }

            if (_detailViewModel.Detail is null)
            {
                ShowDetailText($"Detail: {selected.Id}", "Session log detail not found.");
                return;
            }

            ShowDetailText($"Detail: {selected.Id}", FormatDetail(_detailViewModel.Detail));
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ShowDetailText($"Detail: {selected.Id}", ex.Message);
        }
        finally
        {
            _detailLoadGate.Release();
        }
    }

    private void ClearDetail() => ClearDetail("Detail: (cleared)");

    private void ClearDetail(string title)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = title;
            _detailView.Text = string.Empty;
        });
    }

    private void ShowDetailText(string title, string text)
    {
        Application.Invoke(() =>
        {
            _detailTitleLabel.Text = title;
            _detailView.Text = text;
        });
    }

    private static string FormatDetail(SessionLogDetail detail)
    {
        var lines = new List<string>
        {
            $"SessionId: {detail.SessionId}",
            $"Source: {detail.SourceType}",
            $"Title: {detail.Title}",
            $"Status: {detail.Status}",
            $"Model: {ValueOrDash(detail.Model)}",
            $"Started: {FormatTimestampOrDash(detail.Started)}",
            $"Updated: {FormatTimestampOrDash(detail.LastUpdated)}",
            $"EntryCount: {detail.EntryCount}",
            $"TotalTokens: {detail.TotalTokens?.ToString() ?? "—"}",
            $"CursorSessionLabel: {ValueOrDash(detail.CursorSessionLabel)}",
        };

        if (detail.Workspace is { } workspace)
        {
            lines.Add("");
            lines.Add("Workspace:");
            lines.Add($"  Project: {ValueOrDash(workspace.Project)}");
            lines.Add($"  Framework: {ValueOrDash(workspace.TargetFramework)}");
            lines.Add($"  Repository: {ValueOrDash(workspace.Repository)}");
            lines.Add($"  Branch: {ValueOrDash(workspace.Branch)}");
        }

        if (detail.CopilotStatistics is { } copilot)
        {
            lines.Add("");
            lines.Add("Copilot Statistics:");
            lines.Add($"  AvgSuccessScore: {copilot.AverageSuccessScore?.ToString() ?? "—"}");
            lines.Add($"  TotalNetTokens: {copilot.TotalNetTokens?.ToString() ?? "—"}");
            lines.Add($"  TotalNetPremiumRequests: {copilot.TotalNetPremiumRequests?.ToString() ?? "—"}");
            lines.Add($"  CompletedCount: {copilot.CompletedCount?.ToString() ?? "—"}");
            lines.Add($"  InProgressCount: {copilot.InProgressCount?.ToString() ?? "—"}");
        }

        lines.Add("");
        lines.Add($"Entries ({detail.Entries.Count}):");
        if (detail.Entries.Count == 0)
            return string.Join(Environment.NewLine, lines);

        for (var i = 0; i < detail.Entries.Count; i++)
        {
            var entry = detail.Entries[i];
            lines.Add("");
            lines.Add($"[{i + 1}] RequestId: {ValueOrDash(entry.RequestId)}");
            lines.Add($"  Status: {ValueOrDash(entry.Status)}");
            lines.Add($"  Timestamp: {FormatTimestampOrDash(entry.Timestamp)}");
            lines.Add($"  QueryTitle: {ValueOrDash(entry.QueryTitle)}");
            lines.Add($"  Model: {ValueOrDash(entry.Model)}");
            lines.Add($"  ModelProvider: {ValueOrDash(entry.ModelProvider)}");
            lines.Add($"  TokenCount: {entry.TokenCount?.ToString() ?? "—"}");
            lines.Add($"  FailureNote: {ValueOrDash(entry.FailureNote)}");
            lines.Add($"  Score: {entry.Score?.ToString() ?? "—"}");
            lines.Add($"  IsPremium: {(entry.IsPremium.HasValue ? entry.IsPremium.Value.ToString() : "—")}");
            AppendMultiLineBlock(lines, "  QueryText", entry.QueryText);
            AppendMultiLineBlock(lines, "  Interpretation", entry.Interpretation);
            AppendMultiLineBlock(lines, "  Response", entry.Response);
            AppendStringList(lines, "  Tags", entry.Tags);
            AppendStringList(lines, "  Context", entry.ContextList);
            AppendStringList(lines, "  DesignDecisions", entry.DesignDecisions);
            AppendStringList(lines, "  RequirementsDiscovered", entry.RequirementsDiscovered);
            AppendStringList(lines, "  FilesModified", entry.FilesModified);
            AppendStringList(lines, "  Blockers", entry.Blockers);

            lines.Add($"  Actions ({entry.Actions.Count}):");
            foreach (var action in entry.Actions)
            {
                var type = ValueOrDash(action.Type);
                var status = ValueOrDash(action.Status);
                var description = ValueOrDash(action.Description);
                var filePath = ValueOrDash(action.FilePath);
                lines.Add($"    {action.Order}. [{type}/{status}] {description} (file: {filePath})");
            }

            lines.Add($"  Dialog ({entry.ProcessingDialog.Count}):");
            foreach (var dialog in entry.ProcessingDialog)
            {
                var timestamp = FormatTimestampOrDash(dialog.Timestamp);
                var role = ValueOrDash(dialog.Role);
                var category = ValueOrDash(dialog.Category);
                var content = ValueOrDash(dialog.Content?.Replace(Environment.NewLine, " "));
                lines.Add($"    [{timestamp}] {role}/{category}: {content}");
            }

            lines.Add($"  Commits ({entry.Commits.Count}):");
            foreach (var commit in entry.Commits)
            {
                lines.Add($"    {ValueOrDash(commit.Sha)} @ {ValueOrDash(commit.Branch)}");
                lines.Add($"      Author: {ValueOrDash(commit.Author)}");
                lines.Add($"      Timestamp: {FormatTimestampOrDash(commit.Timestamp)}");
                lines.Add($"      Message: {ValueOrDash(commit.Message)}");
                lines.Add($"      Files: {(commit.FilesChanged.Count == 0 ? "—" : string.Join(", ", commit.FilesChanged))}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendStringList(List<string> lines, string label, IReadOnlyList<string> values)
        => lines.Add($"{label}: {(values.Count == 0 ? "—" : string.Join(", ", values))}");

    private static void AppendMultiLineBlock(List<string> lines, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"{label}: —");
            return;
        }

        lines.Add($"{label}:");
        foreach (var line in value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            lines.Add($"    {line}");
    }

    private static string ValueOrDash(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string FormatTimestampOrDash(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : FormatTimestamp(value);

    private static string FormatTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (!DateTimeOffset.TryParse(value, out var parsed))
            return value;
        return parsed.LocalDateTime.ToString("g");
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);

    private sealed record SessionRow(string Id, string Source, string Title, string Status, string Updated);
}
