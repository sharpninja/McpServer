using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>Terminal.Gui screen for ingestion sync management.</summary>
internal sealed class SyncScreen : View
{
    private readonly McpHttpClient _client;
    private Label _statusLabel = null!;
    private TextView _detailView = null!;

    public SyncScreen(McpHttpClient client)
    {
        _client = client;
        Title = "Sync";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _statusLabel = new Label { X = 0, Y = 0, Width = Dim.Fill(), Text = "" };
        Add(_statusLabel);

        _detailView = new TextView
        {
            X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(2),
            ReadOnly = true, WordWrap = true,
        };
        Add(_detailView);

        var statusBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Check Status" };
        statusBtn.Accepting += (_, _) => _ = Task.Run(CheckStatusAsync);

        var runBtn = new Button { X = Pos.Right(statusBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Run Sync" };
        runBtn.Accepting += (_, _) => _ = Task.Run(RunSyncAsync);

        Add(statusBtn, runBtn);
    }

    public async Task CheckStatusAsync()
    {
        SetStatus("⏳ Checking sync status...");
        try
        {
            var json = await _client.GetStringAsync("/mcp/sync/status").ConfigureAwait(false);
            Application.Invoke(() =>
            {
                _statusLabel.Text = "✓ Sync status retrieved";
                _detailView.Text = json;
            });
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private async Task RunSyncAsync()
    {
        SetStatus("⏳ Running sync...");
        try
        {
            await _client.PostRawAsync("/mcp/sync/run").ConfigureAwait(false);
            SetStatus("✓ Sync completed");
        }
        catch (Exception ex) { SetStatus($"✗ {ex.Message}"); }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);
}
