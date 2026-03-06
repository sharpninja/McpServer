using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// Log viewer view model backed by an app log source abstraction.
/// </summary>
public partial class LogViewModel : ViewModelBase
{
    private readonly IClipboardService _clipboardService;
    private readonly IAppLogService _appLogService;
    private readonly IUiDispatcherService _uiDispatcher;
    private readonly List<ILogEntry> _allEntries = new();

    [ObservableProperty] private ObservableCollection<ILogEntry> _logEntries = new();
    [ObservableProperty] private ILogEntry? _selectedEntry;
    [ObservableProperty] private string _statusText = "Ready";

    /// <summary>Available minimum-level filter options.</summary>
    public static string[] LevelFilterOptions { get; } =
        ["All", "Trace", "Debug", "Information", "Warning", "Error", "Critical"];

    [ObservableProperty] private string _selectedLevelFilter = "Information";
    [ObservableProperty] private bool _isPaused;

    /// <summary>Pause toggle button text.</summary>
    public string PauseButtonText => IsPaused ? "▶ Resume" : "⏸ Pause";

    /// <summary>Raised when a new entry is added so views can auto-scroll.</summary>
    public event Action? NewEntryAdded;

    /// <summary>
    /// Initializes the log view model with clipboard and log source abstractions.
    /// </summary>
    public LogViewModel(
        IClipboardService clipboardService,
        IAppLogService appLogService,
        IUiDispatcherService uiDispatcher)
    {
        _clipboardService = clipboardService;
        _appLogService = appLogService;
        _uiDispatcher = uiDispatcher;

        foreach (var entry in _appLogService.Entries)
            _allEntries.Add(entry);
        ApplyFilter();
        if (LogEntries.Count > 0)
            SelectedEntry = LogEntries[^1];

        _appLogService.EntryAdded += OnEntryAdded;
    }

    partial void OnSelectedLevelFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        LogLevel? minLevel = SelectedLevelFilter switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => null
        };

        LogEntries.Clear();
        foreach (var entry in _allEntries)
        {
            if (minLevel == null || entry.Level >= minLevel.Value)
                LogEntries.Add(entry);
        }
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText = SelectedLevelFilter == "All"
            ? $"{LogEntries.Count} log entries"
            : $"{LogEntries.Count} of {_allEntries.Count} log entries (≥{SelectedLevelFilter})";
    }

    private bool PassesFilter(ILogEntry entry)
    {
        if (SelectedLevelFilter == "All") return true;
        LogLevel minLevel = SelectedLevelFilter switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Trace
        };
        return entry.Level >= minLevel;
    }

    private readonly List<ILogEntry> _pauseBuffer = new();

    private void OnEntryAdded(ILogEntry entry)
    {
        _uiDispatcher.Post(() =>
        {
            _allEntries.Add(entry);
            if (IsPaused)
            {
                _pauseBuffer.Add(entry);
                UpdateStatus();
                return;
            }
            if (PassesFilter(entry))
            {
                LogEntries.Add(entry);
                SelectedEntry = entry;
                NewEntryAdded?.Invoke();
            }
            UpdateStatus();
        });
    }

    partial void OnIsPausedChanged(bool oldValue, bool newValue)
    {
        OnPropertyChanged(nameof(PauseButtonText));
        if (!newValue && _pauseBuffer.Count > 0)
        {
            foreach (var entry in _pauseBuffer)
            {
                if (PassesFilter(entry))
                    LogEntries.Add(entry);
            }
            if (LogEntries.Count > 0)
            {
                SelectedEntry = LogEntries[^1];
                NewEntryAdded?.Invoke();
            }
            _pauseBuffer.Clear();
            UpdateStatus();
        }
    }

    /// <summary>Copies selected log entry to clipboard.</summary>
    protected async Task CopySelectedAsync()
    {
        if (SelectedEntry != null)
        {
            await _clipboardService.SetTextAsync(SelectedEntry.Display).ConfigureAwait(true);
            StatusText = "Copied to clipboard";
        }
    }

    /// <summary>Copies all visible log entries to clipboard.</summary>
    protected async Task CopyAllAsync()
    {
        if (LogEntries.Count == 0) return;
        var sb = new StringBuilder();
        foreach (var entry in LogEntries)
            sb.AppendLine(entry.Display);
        await _clipboardService.SetTextAsync(sb.ToString()).ConfigureAwait(true);
        StatusText = $"Copied {LogEntries.Count} entries to clipboard";
    }

    /// <summary>Clears all captured log entries.</summary>
    protected void ClearLog()
    {
        _appLogService.Clear();
        _allEntries.Clear();
        LogEntries.Clear();
        StatusText = "Log cleared";
    }
}
