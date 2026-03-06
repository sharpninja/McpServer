using McpServer.UI.Core.ViewModels;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal.Gui screen showing server health status and workspace initialization.
/// </summary>
internal sealed class HealthScreen : View
{
    private readonly HealthSnapshotsViewModel _viewModel;
    private TextView _statusLabel = null!;
    private Label _serverLabel = null!;
    private Label _workspaceLabel = null!;
    private TextView _detailView = null!;
    private readonly ILogger<HealthScreen> _logger;


    public HealthScreen(HealthSnapshotsViewModel viewModel, ILogger<HealthScreen>? logger = null)
    {
        _logger = logger ?? NullLogger<HealthScreen>.Instance;
        _viewModel = viewModel;
        Title = "Health";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _serverLabel = new Label { X = 0, Y = 0, Text = "Server: (unknown)" };
        Add(_serverLabel);

        _workspaceLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Text = $"Workspace: {_viewModel.ActiveWorkspacePath ?? "(none)"}",
        };
        Add(_workspaceLabel);

        _statusLabel = new TextView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "Checking...",
        };
        Add(_statusLabel);

        _detailView = new TextView
        {
            X = 0, Y = 4, Width = Dim.Fill(), Height = Dim.Fill(4),
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
            await _viewModel.CheckAsync().ConfigureAwait(true);
            Application.Invoke(() =>
            {
                var snapshot = _viewModel.SelectedItem;
                if (snapshot is null)
                {
                    _statusLabel.Text = "✗ Health check failed";
                    _detailView.Text = _viewModel.ErrorMessage ?? "No health snapshot was returned.";
                    _workspaceLabel.Text = $"Workspace: {_viewModel.ActiveWorkspacePath ?? "(none)"}";
                    return;
                }

                _serverLabel.Text = $"Server: {snapshot.ServerBaseUrl ?? "(unknown)"}";
                _workspaceLabel.Text = $"Workspace: {_viewModel.ActiveWorkspacePath ?? "(none)"}";
                _statusLabel.Text = $"✓ {snapshot.Status} ({_viewModel.Items.Count} checks)";
                _detailView.Text = snapshot.RawPayload;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
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
            var result = await _viewModel.InitializeActiveWorkspaceAsync().ConfigureAwait(true);
            Application.Invoke(() =>
            {
                if (!result.IsSuccess || result.Value is null)
                {
                    _statusLabel.Text = $"✗ Init failed: {result.Error ?? "Unknown error"}";
                    return;
                }

                var seededText = result.Value.SeededDefinitions is int seeded
                    ? $" (seeded {seeded})"
                    : "";
                _statusLabel.Text = $"✓ Workspace initialized{seededText}";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Application.Invoke(() => _statusLabel.Text = $"✗ Init failed: {ex.Message}");
        }
    }
}
