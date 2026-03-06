using System.Collections.Specialized;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for live workspace change-event streaming.
/// </summary>
internal sealed class EventStreamScreen : View
{
    private readonly EventStreamViewModel _viewModel;
    private readonly ViewModelBinder _binder = new();
    private NotifyCollectionChangedEventHandler? _eventsChangedHandler;

    private Label _statusLabel = null!;
    private TextField _categoryField = null!;
    private TextView _eventsView = null!;

    public EventStreamScreen(EventStreamViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = "Events";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        var categoryLabel = new Label { X = 0, Y = 0, Text = "Category:" };
        _categoryField = new TextField
        {
            X = Pos.Right(categoryLabel) + 1,
            Y = 0,
            Width = 24,
            Text = _viewModel.CategoryFilter ?? string.Empty,
        };

        var startBtn = new Button { X = Pos.Right(_categoryField) + 1, Y = 0, Text = "Start" };
        startBtn.Accepting += (_, _) => _ = Task.Run(StartAsync);

        var stopBtn = new Button { X = Pos.Right(startBtn) + 1, Y = 0, Text = "Stop" };
        stopBtn.Accepting += (_, _) => _viewModel.StopStreaming();

        var clearBtn = new Button { X = Pos.Right(stopBtn) + 1, Y = 0, Text = "Clear" };
        clearBtn.Accepting += (_, _) => Clear();

        Add(categoryLabel, _categoryField, startBtn, stopBtn, clearBtn);

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = _viewModel.StatusMessage ?? "Not connected.",
        };
        Add(_statusLabel);

        _eventsView = new TextView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
            Text = "",
        };
        Add(_eventsView);

        _binder.BindProperty(_viewModel, nameof(_viewModel.StatusMessage), UpdateStatus);
        _binder.BindProperty(_viewModel, nameof(_viewModel.ErrorMessage), UpdateStatus);
        _binder.BindProperty(_viewModel, nameof(_viewModel.IsStreaming), UpdateStatus);

        _eventsChangedHandler = (_, _) => RefreshEvents();
        _viewModel.Items.CollectionChanged += _eventsChangedHandler;
        RefreshEvents();
    }

    public Task LoadAsync() => Task.CompletedTask;

    private async Task StartAsync()
    {
        _viewModel.CategoryFilter = string.IsNullOrWhiteSpace(_categoryField.Text?.ToString())
            ? null
            : _categoryField.Text?.ToString();
        await _viewModel.StartAsync().ConfigureAwait(true);
    }

    private void Clear()
    {
        _viewModel.ClearEvents();
        RefreshEvents();
    }

    private void UpdateStatus()
    {
        var error = _viewModel.ErrorMessage;
        var status = _viewModel.StatusMessage ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(error))
            status = $"{status} | Error: {error}";
        _statusLabel.Text = status;
    }

    private void RefreshEvents()
    {
        var text = string.Join(
            Environment.NewLine,
            _viewModel.Items.Select(FormatEventLine));

        Application.Invoke(() =>
        {
            _eventsView.Text = text;
            _eventsView.MoveEnd();
            _eventsView.SetNeedsDraw();
        });
    }

    private static string FormatEventLine(ChangeEventItem item)
    {
        return $"{item.Timestamp:O}  {item.Category}/{item.Action}  entity={item.EntityId ?? "-"}  uri={item.ResourceUri ?? "-"}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.StopStreaming();
            if (_eventsChangedHandler is not null)
                _viewModel.Items.CollectionChanged -= _eventsChangedHandler;
            _binder.Dispose();
        }
        base.Dispose(disposing);
    }
}
