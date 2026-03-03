using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for live workspace change-event streaming.
/// </summary>
internal sealed class EventStreamScreen : View
{
    private readonly Dispatcher _dispatcher;
    private readonly List<string> _lines = [];

    private Label _statusLabel = null!;
    private TextField _categoryField = null!;
    private TextView _eventsView = null!;
    private CancellationTokenSource? _streamCts;

    public EventStreamScreen(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
            Text = "",
        };

        var startBtn = new Button { X = Pos.Right(_categoryField) + 1, Y = 0, Text = "Start" };
        startBtn.Accepting += (_, _) => _ = Task.Run(StartAsync);

        var stopBtn = new Button { X = Pos.Right(startBtn) + 1, Y = 0, Text = "Stop" };
        stopBtn.Accepting += (_, _) => StopStreaming();

        var clearBtn = new Button { X = Pos.Right(stopBtn) + 1, Y = 0, Text = "Clear" };
        clearBtn.Accepting += (_, _) => Clear();

        Add(categoryLabel, _categoryField, startBtn, stopBtn, clearBtn);

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = "Not connected.",
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
    }

    public Task LoadAsync() => Task.CompletedTask;

    private async Task StartAsync()
    {
        StopStreaming();
        _streamCts = new CancellationTokenSource();
        var category = string.IsNullOrWhiteSpace(_categoryField.Text?.ToString()) ? null : _categoryField.Text?.ToString();

        SetStatus($"Subscribing to events{(category is null ? string.Empty : $" for '{category}'")}...");
        try
        {
            var streamResult = await _dispatcher.QueryAsync(new SubscribeToEventsQuery(category), _streamCts.Token).ConfigureAwait(false);
            if (streamResult.IsFailure || streamResult.Value is null)
            {
                SetStatus($"Subscribe failed: {streamResult.Error ?? "Unknown error"}");
                return;
            }

            SetStatus("Streaming events...");
            await foreach (var item in streamResult.Value)
            {
                var line = $"{item.Timestamp:O}  {item.Category}/{item.Action}  entity={item.EntityId ?? "-"}  uri={item.ResourceUri ?? "-"}";
                AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Streaming stopped.");
        }
        catch (Exception ex)
        {
            SetStatus($"Stream error: {ex.Message}");
        }
    }

    private void StopStreaming()
    {
        var cts = _streamCts;
        _streamCts = null;
        cts?.Cancel();
        cts?.Dispose();
    }

    private void Clear()
    {
        _lines.Clear();
        Application.Invoke(() =>
        {
            _eventsView.Text = "";
            _eventsView.SetNeedsDraw();
        });
    }

    private void AppendLine(string line)
    {
        _lines.Add(line);
        if (_lines.Count > 500)
            _lines.RemoveAt(0);

        var text = string.Join(Environment.NewLine, _lines);
        Application.Invoke(() =>
        {
            _eventsView.Text = text;
            _eventsView.MoveEnd();
            _eventsView.SetNeedsDraw();
        });
    }

    private void SetStatus(string status)
    {
        Application.Invoke(() => _statusLabel.Text = status);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            StopStreaming();
        base.Dispose(disposing);
    }
}
