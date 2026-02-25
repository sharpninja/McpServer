using System.Text.Json;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen showing server health status and workspace initialization.
/// </summary>
internal sealed class HealthScreen : View
{
    private readonly HealthSnapshotsViewModel _viewModel;
    private readonly McpHttpClient _client;
    private TextView _statusLabel = null!;
    private Label _serverLabel = null!;
    private TextView _detailView = null!;

    public HealthScreen(HealthSnapshotsViewModel viewModel, McpHttpClient client)
    {
        _viewModel = viewModel;
        _client = client;
        Title = "Health";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _serverLabel = new Label { X = 0, Y = 0, Text = $"Server: {_client.BaseUrl}" };
        Add(_serverLabel);

        _statusLabel = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = false,
            Text = "Checking...",
        };
        Add(_statusLabel);

        _detailView = new TextView
        {
            X = 0, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(3),
            ReadOnly = true, WordWrap = true, Text = "",
        };
        Add(_detailView);

        var refreshBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Check Health" };
        refreshBtn.Accepting += (_, _) => _ = Task.Run(CheckHealthAsync);
        Add(refreshBtn);

        var initBtn = new Button { X = Pos.Right(refreshBtn) + 2, Y = Pos.AnchorEnd(1), Text = "Init Workspace" };
        initBtn.Accepting += (_, _) => _ = Task.Run(InitWorkspaceAsync);
        Add(initBtn);
    }

    public async Task CheckHealthAsync()
    {
        Application.Invoke(() => _statusLabel.Text = "⏳ Checking...");
        try
        {
            await _viewModel.CheckAsync().ConfigureAwait(false);
            Application.Invoke(() =>
            {
                var snapshot = _viewModel.SelectedItem;
                if (snapshot is null)
                {
                    _statusLabel.Text = "✗ Health check failed";
                    _detailView.Text = _viewModel.ErrorMessage ?? "No health snapshot was returned.";
                    return;
                }

                _statusLabel.Text = $"✓ {snapshot.Status} ({_viewModel.Items.Count} checks)";
                _detailView.Text = snapshot.RawPayload;
            });
        }
        catch (Exception ex)
        {
            Application.Invoke(() =>
            {
                _statusLabel.Text = "✗ Server unreachable";
                _detailView.Text = ex.Message;
            });
        }
    }

    private async Task InitWorkspaceAsync()
    {
        Application.Invoke(() => _statusLabel.Text = "⏳ Initializing...");
        try
        {
            await _client.PostRawAsync("/mcp/agents/definitions/seed").ConfigureAwait(false);
            var path = Uri.EscapeDataString(_client.WorkspacePath);
            // Server endpoint currently expects AgentEventType as a numeric enum value (Init = 7).
            var body = new { agentId = "system", eventType = 7, details = "Workspace initialized via Director TUI" };
            await _client.PostAsync<JsonElement>($"/mcp/agents/system/events?workspace={path}", body).ConfigureAwait(false);
            Application.Invoke(() => _statusLabel.Text = "✓ Workspace initialized");
        }
        catch (Exception ex)
        {
            Application.Invoke(() => _statusLabel.Text = $"✗ Init failed: {ex.Message}");
        }
    }
}
