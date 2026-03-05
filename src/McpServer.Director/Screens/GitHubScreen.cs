using System.Text;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for GitHub issues, pull requests, and sync workflows.
/// </summary>
internal sealed class GitHubScreen : View
{
    private readonly IssueListViewModel _issueListVm;
    private readonly IssueDetailViewModel _issueDetailVm;
    private readonly PullRequestListViewModel _pullListVm;
    private readonly GitHubSyncViewModel _syncVm;
    private readonly ILogger<GitHubScreen> _logger;

    private readonly List<GitHubIssueSummary> _issueRows = [];
    private readonly List<GitHubPullSummary> _pullRows = [];

    private TextField _issueStateField = null!;
    private TextField _pullStateField = null!;
    private TableView _issuesTable = null!;
    private TableView _pullsTable = null!;
    private TextView _detailView = null!;
    private TextView _statusLabel = null!;

    public GitHubScreen(
        IssueListViewModel issueListVm,
        IssueDetailViewModel issueDetailVm,
        PullRequestListViewModel pullListVm,
        GitHubSyncViewModel syncVm,
        ILogger<GitHubScreen>? logger = null)
    {
        _issueListVm = issueListVm;
        _issueDetailVm = issueDetailVm;
        _pullListVm = pullListVm;
        _syncVm = syncVm;
        _logger = logger ?? NullLogger<GitHubScreen>.Instance;

        Title = "GitHub";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var issueStateLabel = new Label { X = 0, Y = 0, Text = "Issues State:" };
        _issueStateField = new TextField { X = Pos.Right(issueStateLabel) + 1, Y = 0, Width = 10, Text = "open" };
        var pullStateLabel = new Label { X = Pos.Right(_issueStateField) + 2, Y = 0, Text = "PR State:" };
        _pullStateField = new TextField { X = Pos.Right(pullStateLabel) + 1, Y = 0, Width = 10, Text = "open" };

        var loadBtn = new Button { X = Pos.Right(_pullStateField) + 2, Y = 0, Text = "Load" };
        loadBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);
        Add(issueStateLabel, _issueStateField, pullStateLabel, _pullStateField, loadBtn);

        var leftPane = new View
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent(58),
            Height = Dim.Fill(4),
        };
        Add(leftPane);

        var detailFrame = new FrameView
        {
            Title = "Detail",
            X = Pos.Right(leftPane),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
        };
        Add(detailFrame);

        var issuesFrame = new FrameView
        {
            Title = "Issues",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(60),
        };
        _issuesTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _issuesTable.SelectedCellChanged += (_, _) => _ = Task.Run(RefreshSelectedIssueDetailAsync);
        issuesFrame.Add(_issuesTable);
        leftPane.Add(issuesFrame);

        var pullsFrame = new FrameView
        {
            Title = "Pull Requests",
            X = 0,
            Y = Pos.Bottom(issuesFrame),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _pullsTable = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            FullRowSelect = true,
            MultiSelect = false,
        };
        _pullsTable.SelectedCellChanged += (_, _) => RefreshSelectedPullDetail();
        pullsFrame.Add(_pullsTable);
        leftPane.Add(pullsFrame);

        _detailView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = "Select an issue or pull request to view details.",
        };
        detailFrame.Add(_detailView);

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
        refreshBtn.Accepting += (_, _) => _ = Task.Run(LoadAllAsync);

        var newIssueBtn = new Button { X = Pos.Right(refreshBtn) + 1, Y = Pos.AnchorEnd(1), Text = "New Issue" };
        newIssueBtn.Accepting += (_, _) => ShowCreateIssueDialog();

        var editIssueBtn = new Button { X = Pos.Right(newIssueBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Edit Issue" };
        editIssueBtn.Accepting += (_, _) => ShowEditIssueDialog();

        var commentIssueBtn = new Button { X = Pos.Right(editIssueBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Comment Issue" };
        commentIssueBtn.Accepting += (_, _) => ShowIssueCommentDialog();

        var closeIssueBtn = new Button { X = Pos.Right(commentIssueBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Close Issue" };
        closeIssueBtn.Accepting += (_, _) => _ = Task.Run(CloseSelectedIssueAsync);

        var reopenIssueBtn = new Button { X = Pos.Right(closeIssueBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Reopen Issue" };
        reopenIssueBtn.Accepting += (_, _) => _ = Task.Run(ReopenSelectedIssueAsync);

        var commentPrBtn = new Button { X = Pos.Right(reopenIssueBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Comment PR" };
        commentPrBtn.Accepting += (_, _) => ShowPullCommentDialog();

        var syncFromBtn = new Button { X = Pos.Right(commentPrBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Sync From" };
        syncFromBtn.Accepting += (_, _) => _ = Task.Run(SyncFromGitHubAsync);

        var syncToBtn = new Button { X = Pos.Right(syncFromBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Sync To" };
        syncToBtn.Accepting += (_, _) => _ = Task.Run(SyncToGitHubAsync);

        var syncOneBtn = new Button { X = Pos.Right(syncToBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Sync One" };
        syncOneBtn.Accepting += (_, _) => ShowSingleSyncDialog();

        var labelsBtn = new Button { X = Pos.Right(syncOneBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Labels" };
        labelsBtn.Accepting += (_, _) => _ = Task.Run(ShowLabelsDialogAsync);

        Add(refreshBtn, newIssueBtn, editIssueBtn, commentIssueBtn, closeIssueBtn, reopenIssueBtn, commentPrBtn,
            syncFromBtn, syncToBtn, syncOneBtn, labelsBtn);
    }

    public async Task LoadAllAsync()
    {
        await LoadIssuesAsync().ConfigureAwait(false);
        await LoadPullsAsync().ConfigureAwait(false);
    }

    private async Task LoadIssuesAsync()
    {
        try
        {
            _issueListVm.StateFilter = _issueStateField.Text?.ToString();
            await _issueListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _issueRows.Clear();
                _issueRows.AddRange(_issueListVm.Items);
                _issuesTable.Table = new EnumerableTableSource<GitHubIssueSummary>(
                    _issueRows,
                    new Dictionary<string, Func<GitHubIssueSummary, object>>
                    {
                        ["#"] = i => i.Number,
                        ["Title"] = i => i.Title,
                        ["State"] = i => i.State ?? string.Empty,
                        ["Url"] = i => i.Url ?? string.Empty,
                    });
                if (_issueRows.Count > 0 &&
                    (_issuesTable.SelectedRow < 0 || _issuesTable.SelectedRow >= _issueRows.Count))
                {
                    _issuesTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_issueListVm.ErrorMessage))
                SetStatus(_issueListVm.ErrorMessage);
            else
                SetStatus(_issueListVm.StatusMessage ?? $"Loaded {_issueRows.Count} issues.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Issue load failed: {ex.Message}");
        }
    }

    private async Task LoadPullsAsync()
    {
        try
        {
            _pullListVm.StateFilter = _pullStateField.Text?.ToString();
            await _pullListVm.LoadAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _pullRows.Clear();
                _pullRows.AddRange(_pullListVm.Items);
                _pullsTable.Table = new EnumerableTableSource<GitHubPullSummary>(
                    _pullRows,
                    new Dictionary<string, Func<GitHubPullSummary, object>>
                    {
                        ["#"] = p => p.Number,
                        ["Title"] = p => p.Title,
                        ["State"] = p => p.State ?? string.Empty,
                        ["Url"] = p => p.Url ?? string.Empty,
                    });
                if (_pullRows.Count > 0 &&
                    (_pullsTable.SelectedRow < 0 || _pullsTable.SelectedRow >= _pullRows.Count))
                {
                    _pullsTable.SelectedRow = 0;
                }
            });

            if (!string.IsNullOrWhiteSpace(_pullListVm.ErrorMessage))
                SetStatus(_pullListVm.ErrorMessage);
            else
                SetStatus(_pullListVm.StatusMessage ?? $"Loaded {_pullRows.Count} pull requests.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"Pull request load failed: {ex.Message}");
        }
    }

    private GitHubIssueSummary? GetSelectedIssue()
    {
        var row = _issuesTable.SelectedRow;
        return row >= 0 && row < _issueRows.Count ? _issueRows[row] : null;
    }

    private GitHubPullSummary? GetSelectedPull()
    {
        var row = _pullsTable.SelectedRow;
        return row >= 0 && row < _pullRows.Count ? _pullRows[row] : null;
    }

    private async Task RefreshSelectedIssueDetailAsync()
    {
        var selected = GetSelectedIssue();
        if (selected is null)
            return;

        var detail = await _issueDetailVm.LoadAsync(selected.Number).ConfigureAwait(false);
        if (detail is null)
        {
            SetStatus(_issueDetailVm.ErrorMessage ?? "Issue detail load failed.");
            return;
        }

        SetDetail(FormatIssueDetail(detail));
        SetStatus(_issueDetailVm.StatusMessage ?? $"Loaded issue #{detail.Number}.");
    }

    private void RefreshSelectedPullDetail()
    {
        var selected = GetSelectedPull();
        if (selected is null)
            return;

        SetDetail(FormatPullSummary(selected));
    }

    private void ShowCreateIssueDialog()
    {
        var dlg = new Dialog { Title = "Create Issue", Width = 90, Height = 18 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "Title:" });
        var titleField = new TextField { X = 10, Y = 1, Width = Dim.Fill(2), Text = string.Empty };
        dlg.Add(titleField);

        dlg.Add(new Label { X = 1, Y = 3, Text = "Body:" });
        var bodyView = new TextView { X = 1, Y = 4, Width = Dim.Fill(2), Height = Dim.Fill(3), WordWrap = true, Text = string.Empty };
        dlg.Add(bodyView);

        var createBtn = new Button { Text = "Create" };
        createBtn.Accepting += (_, _) =>
        {
            var title = titleField.Text?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                SetStatus("Issue title is required.");
                return;
            }

            var body = bodyView.Text?.ToString();
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var create = await _issueDetailVm.CreateAsync(title, body).ConfigureAwait(false);
                if (create is null)
                {
                    SetStatus(_issueDetailVm.ErrorMessage ?? "Issue create failed.");
                    return;
                }

                await LoadIssuesAsync().ConfigureAwait(false);
                SelectIssue(create.Number);
                await RefreshSelectedIssueDetailAsync().ConfigureAwait(false);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(createBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowEditIssueDialog()
    {
        var selected = GetSelectedIssue();
        if (selected is null)
        {
            SetStatus("Select an issue first.");
            return;
        }

        _ = Task.Run(async () =>
        {
            var detail = await _issueDetailVm.LoadAsync(selected.Number).ConfigureAwait(false);
            if (detail is null)
            {
                SetStatus(_issueDetailVm.ErrorMessage ?? "Issue detail load failed.");
                return;
            }

            Application.Invoke(() => OpenIssueEditDialog(detail));
        });
    }

    private void OpenIssueEditDialog(GitHubIssueDetail detail)
    {
        var dlg = new Dialog { Title = $"Edit Issue #{detail.Number}", Width = 92, Height = 23 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "Title:" });
        var titleField = new TextField { X = 10, Y = 1, Width = Dim.Fill(2), Text = detail.Title };
        dlg.Add(titleField);

        dlg.Add(new Label { X = 1, Y = 3, Text = "Body:" });
        var bodyView = new TextView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Height = 8,
            WordWrap = true,
            Text = detail.Body ?? string.Empty,
        };
        dlg.Add(bodyView);

        dlg.Add(new Label { X = 1, Y = 13, Text = "Add Labels CSV:" });
        var addLabels = new TextField { X = 20, Y = 13, Width = Dim.Fill(2), Text = string.Empty };
        dlg.Add(addLabels);

        dlg.Add(new Label { X = 1, Y = 14, Text = "Remove Labels CSV:" });
        var removeLabels = new TextField { X = 20, Y = 14, Width = Dim.Fill(2), Text = string.Empty };
        dlg.Add(removeLabels);

        var saveBtn = new Button { Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var command = new UpdateIssueCommand
            {
                Number = detail.Number,
                Title = titleField.Text?.ToString(),
                Body = bodyView.Text?.ToString(),
                AddLabels = ParseCsvOrNull(addLabels.Text?.ToString()),
                RemoveLabels = ParseCsvOrNull(removeLabels.Text?.ToString())
            };

            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _issueDetailVm.UpdateAsync(command).ConfigureAwait(false);
                if (outcome is not { Success: true })
                {
                    SetStatus(_issueDetailVm.ErrorMessage ?? outcome?.ErrorMessage ?? "Issue save failed.");
                    return;
                }

                await LoadIssuesAsync().ConfigureAwait(false);
                SelectIssue(detail.Number);
                await RefreshSelectedIssueDetailAsync().ConfigureAwait(false);
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(saveBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private void ShowIssueCommentDialog()
    {
        var selected = GetSelectedIssue();
        if (selected is null)
        {
            SetStatus("Select an issue first.");
            return;
        }

        ShowCommentDialog($"Issue #{selected.Number} Comment", async text =>
        {
            var outcome = await _issueDetailVm.CommentAsync(selected.Number, text).ConfigureAwait(false);
            if (outcome is not { Success: true })
            {
                SetStatus(_issueDetailVm.ErrorMessage ?? outcome?.ErrorMessage ?? "Issue comment failed.");
                return;
            }

            await RefreshSelectedIssueDetailAsync().ConfigureAwait(false);
        });
    }

    private async Task CloseSelectedIssueAsync()
    {
        var selected = GetSelectedIssue();
        if (selected is null)
        {
            SetStatus("Select an issue first.");
            return;
        }

        var outcome = await _issueDetailVm.CloseAsync(selected.Number).ConfigureAwait(false);
        if (outcome is not { Success: true })
        {
            SetStatus(_issueDetailVm.ErrorMessage ?? outcome?.ErrorMessage ?? "Issue close failed.");
            return;
        }

        await LoadIssuesAsync().ConfigureAwait(false);
        SelectIssue(selected.Number);
        await RefreshSelectedIssueDetailAsync().ConfigureAwait(false);
    }

    private async Task ReopenSelectedIssueAsync()
    {
        var selected = GetSelectedIssue();
        if (selected is null)
        {
            SetStatus("Select an issue first.");
            return;
        }

        var outcome = await _issueDetailVm.ReopenAsync(selected.Number).ConfigureAwait(false);
        if (outcome is not { Success: true })
        {
            SetStatus(_issueDetailVm.ErrorMessage ?? outcome?.ErrorMessage ?? "Issue reopen failed.");
            return;
        }

        await LoadIssuesAsync().ConfigureAwait(false);
        SelectIssue(selected.Number);
        await RefreshSelectedIssueDetailAsync().ConfigureAwait(false);
    }

    private void ShowPullCommentDialog()
    {
        var selected = GetSelectedPull();
        if (selected is null)
        {
            SetStatus("Select a pull request first.");
            return;
        }

        ShowCommentDialog($"PR #{selected.Number} Comment", async text =>
        {
            var outcome = await _pullListVm.CommentAsync(selected.Number, text).ConfigureAwait(false);
            if (outcome is not { Success: true })
            {
                SetStatus(_pullListVm.ErrorMessage ?? outcome?.ErrorMessage ?? "PR comment failed.");
                return;
            }

            SetStatus($"Comment posted to PR #{selected.Number}.");
        });
    }

    private void ShowCommentDialog(string title, Func<string, Task> onSubmitAsync)
    {
        var dlg = new Dialog { Title = title, Width = 88, Height = 16 };
        var view = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            WordWrap = true,
            Text = string.Empty,
        };
        dlg.Add(view);

        var submitBtn = new Button { Text = "Submit" };
        submitBtn.Accepting += (_, _) =>
        {
            var text = view.Text?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return;

            Application.RequestStop();
            _ = Task.Run(() => onSubmitAsync(text));
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(submitBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task SyncFromGitHubAsync()
    {
        var outcome = await _syncVm.SyncFromGitHubAsync("open", 30).ConfigureAwait(false);
        if (outcome is null)
        {
            SetStatus(_syncVm.ErrorMessage ?? "Sync from GitHub failed.");
            return;
        }

        await LoadIssuesAsync().ConfigureAwait(false);
        SetStatus(_syncVm.StatusMessage ?? "Sync from GitHub completed.");
    }

    private async Task SyncToGitHubAsync()
    {
        var outcome = await _syncVm.SyncToGitHubAsync().ConfigureAwait(false);
        if (outcome is null)
        {
            SetStatus(_syncVm.ErrorMessage ?? "Sync to GitHub failed.");
            return;
        }

        await LoadIssuesAsync().ConfigureAwait(false);
        SetStatus(_syncVm.StatusMessage ?? "Sync to GitHub completed.");
    }

    private void ShowSingleSyncDialog()
    {
        var selected = GetSelectedIssue();
        var defaultIssue = selected?.Number.ToString() ?? string.Empty;

        var dlg = new Dialog { Title = "Sync Single Issue", Width = 56, Height = 11 };
        dlg.Add(new Label { X = 1, Y = 1, Text = "Issue #:" });
        var issueField = new TextField { X = 12, Y = 1, Width = 40, Text = defaultIssue };
        dlg.Add(issueField);

        dlg.Add(new Label { X = 1, Y = 3, Text = "Direction:" });
        var directionField = new TextField { X = 12, Y = 3, Width = 40, Text = "from-github" };
        dlg.Add(directionField);

        var syncBtn = new Button { Text = "Sync" };
        syncBtn.Accepting += (_, _) =>
        {
            if (!int.TryParse(issueField.Text?.ToString(), out var issueNumber) || issueNumber <= 0)
            {
                SetStatus("Enter a valid issue number.");
                return;
            }

            var direction = directionField.Text?.ToString() ?? "from-github";
            Application.RequestStop();
            _ = Task.Run(async () =>
            {
                var outcome = await _syncVm.SyncSingleIssueAsync(issueNumber, direction).ConfigureAwait(false);
                if (outcome is null || !outcome.Success)
                {
                    SetStatus(_syncVm.ErrorMessage ?? "Single issue sync failed.");
                    return;
                }

                await LoadIssuesAsync().ConfigureAwait(false);
                SelectIssue(issueNumber);
                SetStatus(_syncVm.StatusMessage ?? "Single issue sync completed.");
            });
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(syncBtn);
        dlg.AddButton(cancelBtn);
        Application.Run(dlg);
    }

    private async Task ShowLabelsDialogAsync()
    {
        var labels = await _syncVm.LoadLabelsAsync().ConfigureAwait(false);
        if (labels is null)
        {
            SetStatus(_syncVm.ErrorMessage ?? "Label load failed.");
            return;
        }

        Application.Invoke(() =>
        {
            var dlg = new Dialog { Title = "Repository Labels", Width = 80, Height = 20 };
            var lines = _syncVm.Labels
                .Select(l => $"{l.Name} ({l.Color ?? "-"})  {l.Description ?? string.Empty}")
                .ToList();

            var list = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
            };
            list.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(lines));
            dlg.Add(list);

            var closeBtn = new Button { Text = "Close" };
            closeBtn.Accepting += (_, _) => Application.RequestStop();
            dlg.AddButton(closeBtn);
            Application.Run(dlg);
        });
    }

    private void SelectIssue(int issueNumber)
    {
        var idx = _issueRows.FindIndex(i => i.Number == issueNumber);
        if (idx >= 0)
            Application.Invoke(() => _issuesTable.SelectedRow = idx);
    }

    private static string FormatIssueDetail(GitHubIssueDetail detail)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Issue #{detail.Number}");
        sb.AppendLine($"Title: {detail.Title}");
        sb.AppendLine($"State: {detail.State}");
        sb.AppendLine($"Author: {detail.Author ?? string.Empty}");
        sb.AppendLine($"Url: {detail.Url ?? string.Empty}");
        sb.AppendLine($"Labels: {string.Join(", ", detail.Labels.Select(l => l.Name))}");
        sb.AppendLine($"Assignees: {string.Join(", ", detail.Assignees)}");
        sb.AppendLine($"Milestone: {detail.Milestone ?? string.Empty}");
        sb.AppendLine($"Created: {detail.CreatedAt ?? string.Empty}");
        sb.AppendLine($"Updated: {detail.UpdatedAt ?? string.Empty}");
        sb.AppendLine($"Closed: {detail.ClosedAt ?? string.Empty}");
        sb.AppendLine("Body:");
        sb.AppendLine(detail.Body ?? string.Empty);
        if (detail.Comments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Comments:");
            foreach (var comment in detail.Comments)
                sb.AppendLine($"- {comment.CreatedAt ?? ""} {comment.Author ?? ""}: {comment.Body ?? ""}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatPullSummary(GitHubPullSummary pull)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Pull Request #{pull.Number}");
        sb.AppendLine($"Title: {pull.Title}");
        sb.AppendLine($"State: {pull.State ?? string.Empty}");
        sb.AppendLine($"Url: {pull.Url ?? string.Empty}");
        return sb.ToString().TrimEnd();
    }

    private void SetStatus(string text)
        => Application.Invoke(() => _statusLabel.Text = text);

    private void SetDetail(string text)
        => Application.Invoke(() => _detailView.Text = text);

    private static IReadOnlyList<string>? ParseCsvOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var values = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToArray();
        return values.Length == 0 ? null : values;
    }
}
