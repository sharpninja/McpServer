using System.Collections.ObjectModel;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for repository list/read/write operations.
/// </summary>
internal sealed class RepoScreen : View
{
    private readonly Dispatcher _dispatcher;
    private readonly List<RepoEntrySummary> _entries = [];

    private Label _statusLabel = null!;
    private TextField _listPathField = null!;
    private ListView _entriesList = null!;
    private TextField _filePathField = null!;
    private TextView _contentView = null!;

    public RepoScreen(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        Title = "Repo";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        BuildUi();
    }

    private void BuildUi()
    {
        var listPathLabel = new Label { X = 0, Y = 0, Text = "List Path:" };
        _listPathField = new TextField
        {
            X = Pos.Right(listPathLabel) + 1,
            Y = 0,
            Width = Dim.Percent(60),
            Text = "",
        };

        var listBtn = new Button { X = Pos.Right(_listPathField) + 1, Y = 0, Text = "List" };
        listBtn.Accepting += (_, _) => _ = Task.Run(ListAsync);

        Add(listPathLabel, _listPathField, listBtn);

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "Repository browser ready.",
        };
        Add(_statusLabel);

        _entriesList = new ListView
        {
            X = 0,
            Y = 2,
            Width = Dim.Percent(35),
            Height = Dim.Fill(3),
        };
        _entriesList.OpenSelectedItem += (_, args) => HandleOpenEntry(args.Item);
        Add(_entriesList);

        var filePathLabel = new Label { X = Pos.Right(_entriesList) + 1, Y = 2, Text = "File Path:" };
        _filePathField = new TextField
        {
            X = Pos.Right(filePathLabel) + 1,
            Y = 2,
            Width = Dim.Fill(2),
            Text = "",
        };
        Add(filePathLabel, _filePathField);

        var readBtn = new Button { X = Pos.Right(_entriesList) + 1, Y = 3, Text = "Read" };
        readBtn.Accepting += (_, _) => _ = Task.Run(ReadFileAsync);

        var writeBtn = new Button { X = Pos.Right(readBtn) + 1, Y = 3, Text = "Write" };
        writeBtn.Accepting += (_, _) => _ = Task.Run(WriteFileAsync);
        Add(readBtn, writeBtn);

        _contentView = new TextView
        {
            X = Pos.Right(_entriesList) + 1,
            Y = 4,
            Width = Dim.Fill(1),
            Height = Dim.Fill(4),
            WordWrap = true,
            Text = "",
        };
        Add(_contentView);
    }

    public async Task LoadAsync()
    {
        await ListAsync().ConfigureAwait(true);
    }

    private async Task ListAsync()
    {
        var path = string.IsNullOrWhiteSpace(_listPathField.Text?.ToString())
            ? null
            : _listPathField.Text?.ToString();

        SetStatus("Listing repository entries...");
        var result = await _dispatcher.QueryAsync(new ListRepoEntriesQuery { Path = path }).ConfigureAwait(true);

        if (result.IsFailure || result.Value is null)
        {
            var error = result.Error ?? "Unknown error";
            SetStatus($"List failed: {error}");
            return;
        }

        var payload = result.Value;
        _entries.Clear();
        _entries.AddRange(payload.Entries);

        var rows = _entries.Select(e => e.IsDirectory ? $"[D] {e.Name}" : $"[F] {e.Name}").ToList();
        Application.Invoke(() => _entriesList.SetSource(new ObservableCollection<string>(rows)));
        SetStatus($"Listed {_entries.Count} entries at '{payload.Path ?? "."}'.");
    }

    private void HandleOpenEntry(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;

        var selected = _entries[index];
        var path = CombinePath(_listPathField.Text?.ToString(), selected.Name);

        if (selected.IsDirectory)
        {
            _listPathField.Text = path;
            _ = Task.Run(ListAsync);
            return;
        }

        _filePathField.Text = path;
        _ = Task.Run(ReadFileAsync);
    }

    private async Task ReadFileAsync()
    {
        var path = _filePathField.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Read failed: file path is required.");
            return;
        }

        SetStatus($"Reading '{path}'...");
        var result = await _dispatcher.QueryAsync(new GetRepoFileQuery(path)).ConfigureAwait(true);

        if (result.IsFailure || result.Value is null)
        {
            var error = result.Error ?? "Unknown error";
            SetStatus($"Read failed: {error}");
            return;
        }

        var payload = result.Value;
        Application.Invoke(() => _contentView.Text = payload.Content ?? string.Empty);
        SetStatus(payload.Exists ? $"Loaded '{payload.Path}'." : $"File does not exist: '{payload.Path}'.");
    }

    private async Task WriteFileAsync()
    {
        var path = _filePathField.Text?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Write failed: file path is required.");
            return;
        }

        var content = _contentView.Text?.ToString() ?? string.Empty;
        SetStatus($"Writing '{path}'...");
        var result = await _dispatcher.SendAsync(new WriteRepoFileCommand(path, content)).ConfigureAwait(true);

        if (result.IsFailure || result.Value is null)
        {
            var error = result.Error ?? "Unknown error";
            SetStatus($"Write failed: {error}");
            return;
        }

        var payload = result.Value;
        SetStatus(payload.Written ? $"Wrote '{payload.Path ?? path}'." : $"Write not confirmed for '{path}'.");
    }

    private void SetStatus(string message)
    {
        Application.Invoke(() => _statusLabel.Text = message);
    }

    private static string CombinePath(string? root, string name)
    {
        if (string.IsNullOrWhiteSpace(root))
            return name;

        return $"{root.TrimEnd('/', '\\')}/{name}";
    }
}
