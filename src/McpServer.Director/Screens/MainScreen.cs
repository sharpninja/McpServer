using System.Collections.ObjectModel;
using McpServer.Director.Auth;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Main Terminal.Gui window with tab navigation between all Director screens.
/// Tabs are filtered by role using <see cref="IAuthorizationPolicyService"/>.
/// Includes menu bar, auth status, and keyboard shortcuts.
/// </summary>
internal sealed class MainScreen : Window
{
    private readonly DirectorMcpContext _directorContext;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly IRoleContext _roleContext;
    private readonly HealthSnapshotsViewModel _healthVm;
    private readonly DispatcherLogsViewModel _dispatcherLogsVm;
    private readonly SessionLogListViewModel _sessionLogVm;
    private readonly SyncStatusViewModel _syncStatusVm;
    private readonly RunSyncViewModel _runSyncVm;
    private readonly TodoListViewModel _todoVm;
    private readonly TodoDetailViewModel _todoDetailVm;
    private readonly WorkspaceListViewModel _workspaceListVm;
    private readonly WorkspaceDetailViewModel _workspaceDetailVm;
    private readonly WorkspacePolicyViewModel _workspacePolicyVm;
    private readonly TunnelListViewModel _tunnelListVm;
    private Label _authLabel = null!;
    private TabView _tabView = null!;
    private Label _workspaceContextLabel = null!;
    private TextView _workspaceContextStatus = null!;
    private readonly ObservableCollection<WorkspacePickerItem> _workspacePickerSource = [];
    private bool _authRefreshQueued;
    private readonly ILoggerFactory _loggerFactory;

    public MainScreen(
        WorkspaceListViewModel workspaceListVm,
        WorkspaceDetailViewModel workspaceDetailVm,
        WorkspacePolicyViewModel workspacePolicyVm,
        HealthSnapshotsViewModel healthVm,
        DispatcherLogsViewModel dispatcherLogsVm,
        SessionLogListViewModel sessionLogVm,
        SyncStatusViewModel syncStatusVm,
        RunSyncViewModel runSyncVm,
        TodoListViewModel todoVm,
        TodoDetailViewModel todoDetailVm,
        TunnelListViewModel tunnelListVm,
        IAuthorizationPolicyService authorizationPolicy,
        IRoleContext roleContext,
        DirectorMcpContext directorContext,
        ILoggerFactory loggerFactory)
    {
        _healthVm = healthVm;
        _dispatcherLogsVm = dispatcherLogsVm;
        _sessionLogVm = sessionLogVm;
        _syncStatusVm = syncStatusVm;
        _runSyncVm = runSyncVm;
        _todoVm = todoVm;
        _todoDetailVm = todoDetailVm;
        _workspaceListVm = workspaceListVm;
        _workspaceDetailVm = workspaceDetailVm;
        _workspacePolicyVm = workspacePolicyVm;
        _tunnelListVm = tunnelListVm;
        _authorizationPolicy = authorizationPolicy;
        _roleContext = roleContext;
        _directorContext = directorContext;
        _loggerFactory = loggerFactory;

        Title = "McpServer Director";
        Width = Dim.Fill();
        Height = Dim.Fill();

        BuildUi();
    }

    private void BuildUi()
    {
        // Menu bar
        var menuBar = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File",
                [
                    new MenuItem("_Refresh", "", () => RefreshCurrentTab()),
                    null!,
                    new MenuItem("_Login...", "", ShowLoginDialog),
                    new MenuItem("L_ogout", "", () =>
                    {
                        OidcAuthService.Logout();
                        QueueAuthStateChangedRefresh();
                    }),
                    null!,
                    new MenuItem("_Quit", "", () => Application.RequestStop()),
                ]),
                new MenuBarItem("_Help",
                [
                    new MenuItem("_About", "", () =>
                    {
                        MessageBox.Query("About",
                            "McpServer Director\nTerminal UI for workspace & agent management\n\nPowered by Terminal.Gui v2 + CommunityToolkit.Mvvm",
                            "OK");
                    }),
                ]),
            ],
        };
        Add(menuBar);

        // Auth status label
        _authLabel = new Label
        {
            X = Pos.AnchorEnd(40),
            Y = 0,
            Width = 39,
            Text = "",
        };
        Add(_authLabel);
        UpdateAuthStatus();

        // Tab view
        _tabView = new TabView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
        };
        Add(_tabView);
        RebuildTabs();
        _tabView.SelectedTabChanged += (_, e) => RefreshTab(e.NewTab);

        _directorContext.ActiveWorkspaceChanged += (_, _) =>
        {
            Application.Invoke(() =>
            {
                UpdateWorkspaceContextStatus();
            });

            _workspacePolicyVm.WorkspacePath = _directorContext.ActiveWorkspacePath ?? "";
            if (_tabView.SelectedTab?.View is not WorkspaceListScreen)
                RefreshCurrentTab();
        };
        _workspaceListVm.Workspaces.CollectionChanged += (_, _) =>
        {
            Application.Invoke(RefreshWorkspacePickerItems);
        };

        BuildWorkspaceContextRow();

        // Status bar
        var statusBar = new StatusBar { Y = Pos.AnchorEnd(1) };
        statusBar.Add(new Shortcut { Key = Key.Tab.WithShift, Title = "Next Tab" });
        statusBar.Add(new Shortcut { Key = Key.F2, Title = "Login" });
        statusBar.Add(new Shortcut { Key = Key.F5, Title = "Refresh" });
        statusBar.Add(new Shortcut { Key = Key.W.WithCtrl, Title = "Workspace" });
        statusBar.Add(new Shortcut { Key = Key.C.WithCtrl, Title = "Copy" });
        statusBar.Add(new Shortcut { Key = Key.Q.WithCtrl, Title = "Quit" });
        Add(statusBar);

        Application.KeyDown += OnApplicationKeyDown;
        Disposing += (_, _) => Application.KeyDown -= OnApplicationKeyDown;

        // Key bindings
        KeyDown += (_, e) =>
        {
            HandleGlobalShortcutKey(e);
        };

        // Auto-load on startup
        Loaded += (_, _) =>
        {
            _ = Task.Run(async () =>
            {
                var logsScreen = _tabView.Tabs
                    .Select(t => t.View)
                    .OfType<DispatcherLogsScreen>()
                    .FirstOrDefault();
                if (logsScreen is not null)
                    await logsScreen.LoadAsync().ConfigureAwait(false);

                if (!_directorContext.HasControlConnection)
                    return;

                // Load workspaces and auto-select context first
                if (_authorizationPolicy.CanViewArea(McpArea.Workspaces))
                {
                    await _workspaceListVm.LoadAsync().ConfigureAwait(false);
                    Application.Invoke(() =>
                    {
                        RefreshWorkspacePickerItems();
                        TryAutoSelectWorkspaceContext();
                    });
                }

                // Refresh the initially selected tab after workspace context is settled
                Application.Invoke(RefreshCurrentTab);
            });
        };
    }

    private void ShowLoginDialog()
    {
        // Avoid mutating the main screen layout while the modal dialog is drawing/running.
        // Terminal.Gui can throw during nested draw when parent views are rebuilt from a dialog callback.
        var dlg = new LoginDialog();
        Application.Run(dlg);
        QueueAuthStateChangedRefresh();
    }

    private void HandleAuthStateChanged()
    {
        UpdateAuthStatus();
        _directorContext.RefreshBearerTokens();
        RebuildTabs();
        RefreshCurrentTab();
    }

    private void QueueAuthStateChangedRefresh()
    {
        if (_authRefreshQueued)
            return;

        _authRefreshQueued = true;
        Application.Invoke(() =>
        {
            _authRefreshQueued = false;
            HandleAuthStateChanged();
        });
    }

    private void UpdateAuthStatus()
    {
        var user = OidcAuthService.GetCurrentUser();
        var rolesSuffix = user is not null && user.Roles.Count > 0
            ? $" ({string.Join(',', user.Roles)})"
            : "";
        var text = user is null
            ? "🔒 Not logged in [F2]"
            : user.IsExpired
                ? $"⚠ {user.Username}{rolesSuffix} (expired) [F2]"
                : $"🔓 {user.Username}{rolesSuffix} [F2]";
        Application.Invoke(() => _authLabel.Text = text);
    }

    /// <summary>
    /// Copies text from the currently focused control to the system clipboard.
    /// Supports TextView (selected text or all), TextField, Label, and TableView (selected row).
    /// </summary>
    private static void CopyFocusedText()
    {
        var focused = Application.Top?.MostFocused;
        if (focused is null) return;

        string? textToCopy = null;

        switch (focused)
        {
            case TextView tv:
                // If there's a selection, copy it; otherwise copy all text
                var selected = tv.SelectedText;
                textToCopy = !string.IsNullOrEmpty(selected) ? selected : tv.Text;
                break;

            case TextField tf:
                // If there's a selection, copy it; otherwise copy all text
                var tfSelected = tf.SelectedText;
                textToCopy = !string.IsNullOrEmpty(tfSelected) ? tfSelected : tf.Text;
                break;

            case Label lbl:
                textToCopy = lbl.Text;
                break;

            case TableView table:
                if (table.Table is { } source && table.SelectedRow >= 0 && table.SelectedRow < source.Rows)
                {
                    var parts = new List<string>();
                    for (var col = 0; col < source.Columns; col++)
                        parts.Add(source[table.SelectedRow, col]?.ToString() ?? "");
                    textToCopy = string.Join("\t", parts);
                }
                break;
        }

        if (!string.IsNullOrEmpty(textToCopy))
        {
            Clipboard.TrySetClipboardData(textToCopy);
        }
    }

    private void RefreshCurrentTab()
        => RefreshTab(_tabView.SelectedTab);

    private void RefreshTab(Tab? tab)
        => RefreshTabView(tab?.View);

    private void RefreshTabView(View? view)
    {
        if (view is HealthScreen hs)
        {
            _ = Task.Run(hs.CheckHealthAsync);
            return;
        }

        if (view is SessionLogScreen ss)
        {
            _ = Task.Run(ss.LoadAsync);
            return;
        }

        if (view is DispatcherLogsScreen dls)
        {
            _ = Task.Run(dls.LoadAsync);
            return;
        }

        if (view is TodoScreen ts)
        {
            _ = Task.Run(ts.LoadAsync);
            return;
        }

        if (view is WorkspaceListScreen ws)
        {
            _ = Task.Run(ws.LoadAsync);
            return;
        }

        if (view is AgentScreen ags)
        {
            _ = Task.Run(ags.LoadAllAsync);
            return;
        }

        if (view is SyncScreen sync)
        {
            _ = Task.Run(sync.CheckStatusAsync);
        }

        if (view is TunnelScreen tunnel)
        {
            _ = Task.Run(tunnel.LoadAsync);
        }
    }

    private void RebuildTabs()
    {
        var previousTab = _tabView.SelectedTab?.DisplayText?.ToString();

        // TabView.RemoveAll() is inherited from View and removes the control's
        // internal subviews (including the tab strip). Remove hosted tabs only.
        foreach (var tab in _tabView.Tabs.ToList())
        {
            _tabView.RemoveTab(tab);
            // Avoid disposing removed tab views here. Terminal.Gui can still hold transient
            // internal references during redraw and throw when disposed views are re-drawn.
        }

        var selectFirst = true;

        if ((_directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection) && _authorizationPolicy.CanViewArea(McpArea.Todo))
        {
            _tabView.AddTab(new Tab { DisplayText = "TODO", View = new TodoScreen(_todoVm, _todoDetailVm, directorContext: _directorContext) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if ((_directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection) && _authorizationPolicy.CanViewArea(McpArea.SessionLogs))
        {
            _tabView.AddTab(new Tab { DisplayText = "Sessions", View = new SessionLogScreen(_sessionLogVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_directorContext.HasControlConnection && _authorizationPolicy.CanViewArea(McpArea.Health))
        {
            _tabView.AddTab(new Tab { DisplayText = "Health", View = new HealthScreen(_healthVm, _directorContext.GetRequiredControlHttpClient()) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_directorContext.HasControlConnection && _authorizationPolicy.CanViewArea(McpArea.Workspaces))
        {
            _tabView.AddTab(new Tab { DisplayText = "Workspaces", View = new WorkspaceListScreen(_workspaceListVm, _workspaceDetailVm, _directorContext) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if ((_directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection) && _authorizationPolicy.CanViewArea(McpArea.Agents))
        {
            _tabView.AddTab(new Tab { DisplayText = "Agents", View = new AgentScreen(_directorContext) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if ((_directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection) && _authorizationPolicy.CanViewArea(McpArea.Sync))
        {
            _tabView.AddTab(new Tab { DisplayText = "Sync", View = new SyncScreen(_syncStatusVm, _runSyncVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_directorContext.HasControlConnection && _authorizationPolicy.CanViewArea(McpArea.Policy))
        {
            _tabView.AddTab(new Tab { DisplayText = "Policy", View = new WorkspacePolicyScreen(_workspacePolicyVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if ((_directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection) && _authorizationPolicy.CanViewArea(McpArea.Tunnels))
        {
            _tabView.AddTab(new Tab { DisplayText = "Tunnels", View = new TunnelScreen(_tunnelListVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_authorizationPolicy.CanViewArea(McpArea.DispatcherLogs))
        {
            _tabView.AddTab(new Tab { DisplayText = "Logs", View = new DispatcherLogsScreen(_dispatcherLogsVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (selectFirst)
        {
            var empty = new View { Width = Dim.Fill(), Height = Dim.Fill() };
            empty.Add(new Label
            {
                Text = "No tabs available for the current role/login state.",
                X = 1,
                Y = 1,
            });
            _tabView.AddTab(new Tab { DisplayText = "Info", View = empty }, andSelect: true);
        }
        else if (!string.IsNullOrWhiteSpace(previousTab))
        {
            var match = _tabView.Tabs.FirstOrDefault(t => string.Equals(t.DisplayText?.ToString(), previousTab, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                _tabView.SelectedTab = match;
        }

        RefreshWorkspacePickerItems();
        UpdateWorkspaceContextStatus();
    }

    private void BuildWorkspaceContextRow()
    {
        var rowY = Pos.AnchorEnd(2);

        _workspaceContextLabel = new Label
        {
            X = 0,
            Y = rowY,
            Text = "Workspace:",
        };
        Add(_workspaceContextLabel);

        _workspaceContextStatus = new TextView
        {
            X = Pos.Right(_workspaceContextLabel) + 1,
            Y = rowY,
            Width = Dim.Fill(),
            Height = 1,
            ReadOnly = true,
            WordWrap = false,
            Text = "",
        };
        Add(_workspaceContextStatus);

        RefreshWorkspacePickerItems();
        UpdateWorkspaceContextStatus();
    }

    private void RefreshWorkspacePickerItems()
    {
        _workspacePickerSource.Clear();
        foreach (var ws in _workspaceListVm.Workspaces)
        {
            var label = ws.IsPrimary
                ? $"* {ws.Name}"
                : $"  {ws.Name}";
            _workspacePickerSource.Add(new WorkspacePickerItem(ws.WorkspacePath, label));
        }
    }

    private void ShowWorkspaceSelectionDialog()
    {
        RefreshWorkspacePickerItems();

        if (_workspacePickerSource.Count == 0)
        {
            UpdateWorkspaceContextStatus("No workspaces available. Open Workspaces tab and refresh.");
            return;
        }

        var snapshot = new ObservableCollection<WorkspacePickerItem>(_workspacePickerSource.ToList());
        var selectedPath = _directorContext.ActiveWorkspacePath;
        string? chosenPath = null;

        var dlg = new Dialog
        {
            Title = "Select Workspace",
            Width = 72,
            Height = Math.Min(Math.Max(snapshot.Count + 6, 10), 20),
        };

        var listView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = Dim.Fill(2),
        };
        listView.SetSource(snapshot);
        dlg.Add(listView);

        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            for (var i = 0; i < snapshot.Count; i++)
            {
                if (string.Equals(snapshot[i].WorkspacePath, selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    listView.SelectedItem = i;
                    listView.EnsureSelectedItemVisible();
                    break;
                }
            }
        }

        void CommitSelection()
        {
            var index = listView.SelectedItem;
            if (index < 0 || index >= snapshot.Count)
                return;

            chosenPath = snapshot[index].WorkspacePath;
            Application.RequestStop();
        }

        listView.OpenSelectedItem += (_, _) => CommitSelection();

        var activateBtn = new Button { Text = "Activate" };
        activateBtn.Accepting += (_, _) => CommitSelection();
        dlg.AddButton(activateBtn);

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => Application.RequestStop();
        dlg.AddButton(cancelBtn);

        listView.SetFocus();
        Application.Run(dlg);

        if (string.IsNullOrWhiteSpace(chosenPath))
            return;

        if (!_directorContext.TrySetActiveWorkspace(chosenPath, out var error))
            UpdateWorkspaceContextStatus($"Context switch failed: {error}");
    }

    private void CycleTab()
    {
        var tabs = _tabView.Tabs.ToList();
        if (tabs.Count < 2) return;
        var idx = tabs.IndexOf(_tabView.SelectedTab!);
        _tabView.SelectedTab = tabs[(idx + 1) % tabs.Count];
    }

    private void OnApplicationKeyDown(object? sender, Key e)
    {
        if (!Visible)
            return;

        if (e.Handled)
            return;

        if (e.KeyCode == (KeyCode.W | KeyCode.CtrlMask))
        {
            ShowWorkspaceSelectionDialog();
            e.Handled = true;
        }
    }

    private void HandleGlobalShortcutKey(Key e)
    {
        if (e.Handled)
            return;

        if (e.KeyCode == (KeyCode.Tab | KeyCode.ShiftMask))
        {
            CycleTab();
            e.Handled = true;
        }
        else if (e.KeyCode == KeyCode.F2)
        {
            ShowLoginDialog();
            e.Handled = true;
        }
        else if (e.KeyCode == KeyCode.F5)
        {
            RefreshCurrentTab();
            e.Handled = true;
        }
        else if (e.KeyCode == (KeyCode.C | KeyCode.CtrlMask))
        {
            CopyFocusedText();
            e.Handled = true;
        }
        else if (e.KeyCode == (KeyCode.Q | KeyCode.CtrlMask))
        {
            Application.RequestStop();
            e.Handled = true;
        }
    }

    private void TryAutoSelectWorkspaceContext()
    {
        if (_directorContext.HasActiveWorkspaceConnection)
            return;
        if (_workspacePickerSource.Count == 0)
            return;

        var preferred = _workspaceListVm.Workspaces.FirstOrDefault(w => w.IsPrimary)?.WorkspacePath
                        ?? _workspaceListVm.Workspaces.FirstOrDefault()?.WorkspacePath;
        if (string.IsNullOrWhiteSpace(preferred))
            return;

        _directorContext.TrySetActiveWorkspace(preferred, out _);
    }

    private void UpdateWorkspaceContextStatus(string? explicitStatus = null)
    {
        if (_workspaceContextStatus is null)
            return;

        var control = _directorContext.ControlClient?.BaseUrl ?? "(none)";
        var active = _directorContext.ActiveWorkspacePath ?? "(none)";
        _workspaceContextStatus.Text = explicitStatus ?? $"Control: {control} | Active: {active}";
    }

    private sealed record WorkspacePickerItem(string WorkspacePath, string DisplayText)
    {
        public override string ToString() => DisplayText;
    }
}
