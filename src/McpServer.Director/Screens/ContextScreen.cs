using System.Text;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen for context search, pack, source listing, and index rebuild actions.
/// </summary>
internal sealed class ContextScreen : View
{
    private readonly Dispatcher _dispatcher;

    private Label _statusLabel = null!;
    private TextField _queryField = null!;
    private TextField _sourceTypeField = null!;
    private TextField _limitField = null!;
    private TextView _outputView = null!;

    public ContextScreen(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        Title = "Context";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;

        BuildUi();
    }

    private void BuildUi()
    {
        var queryLabel = new Label { X = 0, Y = 0, Text = "Query:" };
        _queryField = new TextField
        {
            X = Pos.Right(queryLabel) + 1,
            Y = 0,
            Width = Dim.Percent(45),
            Text = "",
        };

        var sourceLabel = new Label { X = Pos.Right(_queryField) + 2, Y = 0, Text = "Source:" };
        _sourceTypeField = new TextField
        {
            X = Pos.Right(sourceLabel) + 1,
            Y = 0,
            Width = 14,
            Text = "",
        };

        var limitLabel = new Label { X = Pos.Right(_sourceTypeField) + 2, Y = 0, Text = "Limit:" };
        _limitField = new TextField
        {
            X = Pos.Right(limitLabel) + 1,
            Y = 0,
            Width = 5,
            Text = "20",
        };

        Add(queryLabel, _queryField, sourceLabel, _sourceTypeField, limitLabel, _limitField);

        var searchBtn = new Button { X = 0, Y = 1, Text = "Search" };
        searchBtn.Accepting += (_, _) => _ = Task.Run(SearchAsync);

        var packBtn = new Button { X = Pos.Right(searchBtn) + 1, Y = 1, Text = "Pack" };
        packBtn.Accepting += (_, _) => _ = Task.Run(PackAsync);

        var sourcesBtn = new Button { X = Pos.Right(packBtn) + 1, Y = 1, Text = "Sources" };
        sourcesBtn.Accepting += (_, _) => _ = Task.Run(ListSourcesAsync);

        var rebuildBtn = new Button { X = Pos.Right(sourcesBtn) + 1, Y = 1, Text = "Rebuild Index" };
        rebuildBtn.Accepting += (_, _) => _ = Task.Run(RebuildIndexAsync);

        Add(searchBtn, packBtn, sourcesBtn, rebuildBtn);

        _statusLabel = new Label
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Text = "Context tools ready.",
        };
        Add(_statusLabel);

        _outputView = new TextView
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
        Add(_outputView);
    }

    public async Task LoadAsync()
    {
        await ListSourcesAsync().ConfigureAwait(false);
    }

    private async Task SearchAsync()
    {
        var query = BuildSearchQuery();
        SetStatus("Searching context...");
        var result = await _dispatcher.QueryAsync(query).ConfigureAwait(false);

        var text = FormatSearchResult(result);
        Application.Invoke(() =>
        {
            _outputView.Text = text;
            SetStatus("Search complete.");
        });
    }

    private async Task PackAsync()
    {
        var query = BuildPackQuery();
        SetStatus("Building context pack...");
        var result = await _dispatcher.QueryAsync(query).ConfigureAwait(false);

        var text = FormatPackResult(result);
        Application.Invoke(() =>
        {
            _outputView.Text = text;
            SetStatus("Pack complete.");
        });
    }

    private async Task ListSourcesAsync()
    {
        SetStatus("Loading context sources...");
        var result = await _dispatcher.QueryAsync(new ListContextSourcesQuery()).ConfigureAwait(false);

        var text = FormatSourcesResult(result);
        Application.Invoke(() =>
        {
            _outputView.Text = text;
            SetStatus("Sources loaded.");
        });
    }

    private async Task RebuildIndexAsync()
    {
        SetStatus("Rebuilding context index...");
        var result = await _dispatcher.SendAsync(new RebuildContextIndexCommand()).ConfigureAwait(false);

        var text = FormatRebuildResult(result);
        Application.Invoke(() =>
        {
            _outputView.Text = text;
            SetStatus("Rebuild request complete.");
        });
    }

    private SearchContextQuery BuildSearchQuery()
        => new()
        {
            Query = _queryField.Text?.ToString() ?? string.Empty,
            SourceType = string.IsNullOrWhiteSpace(_sourceTypeField.Text?.ToString())
                ? null
                : _sourceTypeField.Text?.ToString(),
            Limit = ParseLimit()
        };

    private PackContextQuery BuildPackQuery()
        => new()
        {
            Query = _queryField.Text?.ToString() ?? string.Empty,
            Limit = ParseLimit()
        };

    private int ParseLimit()
        => int.TryParse(_limitField.Text?.ToString(), out var value) && value > 0 ? value : 20;

    private void SetStatus(string message)
    {
        Application.Invoke(() => _statusLabel.Text = message);
    }

    private static string FormatSearchResult(Result<ContextSearchPayload> result)
    {
        if (result.IsFailure || result.Value is null)
            return $"Search failed: {result.Error ?? "Unknown error"}";

        var payload = result.Value;
        var sb = new StringBuilder();
        sb.AppendLine($"Query: {payload.Query}");
        sb.AppendLine($"Source keys: {string.Join(", ", payload.SourceKeys)}");
        sb.AppendLine($"Chunks: {payload.Chunks.Count}");
        sb.AppendLine();

        for (var i = 0; i < payload.Chunks.Count; i++)
        {
            var chunk = payload.Chunks[i];
            sb.AppendLine($"[{i + 1}] {chunk.DocumentId}#{chunk.ChunkIndex}  score={chunk.Score:F4}  tokens={chunk.TokenCount}");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatPackResult(Result<ContextPackPayload> result)
    {
        if (result.IsFailure || result.Value is null)
            return $"Pack failed: {result.Error ?? "Unknown error"}";

        var payload = result.Value;
        var sb = new StringBuilder();
        sb.AppendLine($"QueryId: {payload.QueryId}");
        sb.AppendLine($"Source keys: {string.Join(", ", payload.SourceKeys)}");
        sb.AppendLine($"Chunks: {payload.Chunks.Count}");
        sb.AppendLine();

        for (var i = 0; i < payload.Chunks.Count; i++)
        {
            var chunk = payload.Chunks[i];
            sb.AppendLine($"[{i + 1}] {chunk.DocumentId}#{chunk.ChunkIndex}  score={chunk.Score:F4}  tokens={chunk.TokenCount}");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatSourcesResult(Result<ContextSourcesPayload> result)
    {
        if (result.IsFailure || result.Value is null)
            return $"Source listing failed: {result.Error ?? "Unknown error"}";

        var payload = result.Value;
        var sb = new StringBuilder();
        sb.AppendLine($"Sources: {payload.Sources.Count}");
        sb.AppendLine();
        foreach (var source in payload.Sources)
            sb.AppendLine($"{source.SourceType,-14} {source.SourceKey} ({source.IngestedAt ?? "n/a"})");
        return sb.ToString().TrimEnd();
    }

    private static string FormatRebuildResult(Result<ContextRebuildResult> result)
    {
        if (result.IsFailure || result.Value is null)
            return $"Rebuild failed: {result.Error ?? "Unknown error"}";

        var payload = result.Value;
        return $"Status: {payload.Status ?? "unknown"}\nRecorded: {payload.RecordedAt:O}";
    }
}
