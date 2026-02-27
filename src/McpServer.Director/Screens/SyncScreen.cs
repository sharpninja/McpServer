using System.Text.Json;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Screens;

/// <summary>Terminal.Gui screen for ingestion sync management.</summary>
internal sealed class SyncScreen : View
{
    private readonly SyncStatusViewModel _syncStatusViewModel;
    private readonly RunSyncViewModel _runSyncViewModel;
    private TextView _statusLabel = null!;
    private TextView _detailView = null!;
    private readonly ILogger<SyncScreen> _logger;


    public SyncScreen(SyncStatusViewModel syncStatusViewModel, RunSyncViewModel runSyncViewModel,
        ILogger<SyncScreen>? logger = null)
    {
        _logger = logger ?? NullLogger<SyncScreen>.Instance;
        _syncStatusViewModel = syncStatusViewModel;
        _runSyncViewModel = runSyncViewModel;
        Title = "Sync";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
    }

    private void BuildUi()
    {
        _statusLabel = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = true,
            Text = "",
        };
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
            await _syncStatusViewModel.GetStatusCommand.ExecuteAsync(null).ConfigureAwait(false);
            var result = _syncStatusViewModel.LastResult;
            Application.Invoke(() =>
            {
                if (result is { IsSuccess: true, Value: not null })
                {
                    _statusLabel.Text = "✓ Sync status retrieved";
                    _detailView.Text = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    _statusLabel.Text = $"✗ {result?.Error ?? "Unknown sync status error"}";
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
    }

    public async Task RunSyncAsync()
    {
        SetStatus("⏳ Running sync...");
        try
        {
            await _runSyncViewModel.RunCommand.ExecuteAsync(null).ConfigureAwait(false);
            var result = _runSyncViewModel.LastResult;
            Application.Invoke(() =>
            {
                if (result is { IsSuccess: true, Value: not null })
                {
                    _statusLabel.Text = "✓ Sync completed";
                    _detailView.Text = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    _statusLabel.Text = $"✗ {result?.Error ?? "Unknown sync run error"}";
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            SetStatus($"✗ {ex.Message}");
        }
    }

    private void SetStatus(string text) => Application.Invoke(() => _statusLabel.Text = text);
}
