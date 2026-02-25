using McpServer.Director.Auth;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.ViewModels;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Main Terminal.Gui window with tab navigation between all Director screens.
/// Tabs are filtered by role using <see cref="IAuthorizationPolicyService"/>.
/// Includes menu bar, auth status, and keyboard shortcuts.
/// </summary>
internal sealed class MainScreen : Window
{
    private readonly McpHttpClient? _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly IRoleContext _roleContext;
    private readonly HealthSnapshotsViewModel _healthVm;
    private readonly SessionLogListViewModel _sessionLogVm;
    private readonly TodoListViewModel _todoVm;
    private readonly TodoDetailViewModel _todoDetailVm;
    private readonly WorkspaceListViewModel _workspaceListVm;
    private readonly WorkspacePolicyViewModel _workspacePolicyVm;
    private Label _authLabel = null!;
    private TabView _tabView = null!;

    public MainScreen(
        WorkspaceListViewModel workspaceListVm,
        WorkspacePolicyViewModel workspacePolicyVm,
        HealthSnapshotsViewModel healthVm,
        SessionLogListViewModel sessionLogVm,
        TodoListViewModel todoVm,
        TodoDetailViewModel todoDetailVm,
        IAuthorizationPolicyService authorizationPolicy,
        IRoleContext roleContext,
        McpHttpClient? client = null)
    {
        _healthVm = healthVm;
        _sessionLogVm = sessionLogVm;
        _todoVm = todoVm;
        _todoDetailVm = todoDetailVm;
        _workspaceListVm = workspaceListVm;
        _workspacePolicyVm = workspacePolicyVm;
        _authorizationPolicy = authorizationPolicy;
        _roleContext = roleContext;
        _client = client;

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
                        UpdateAuthStatus();
                        RebuildTabs();
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
            Height = Dim.Fill(1),
        };
        Add(_tabView);
        RebuildTabs();

        // Status bar
        var statusBar = new StatusBar { Y = Pos.AnchorEnd(1) };
        statusBar.Add(new Shortcut { Key = Key.F2, Title = "Login" });
        statusBar.Add(new Shortcut { Key = Key.F5, Title = "Refresh" });
        statusBar.Add(new Shortcut { Key = Key.C.WithCtrl, Title = "Copy" });
        statusBar.Add(new Shortcut { Key = Key.Q.WithCtrl, Title = "Quit" });
        Add(statusBar);

        // Key bindings
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == KeyCode.F2)
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
        };

        // Auto-load on startup
        Loaded += (_, _) =>
        {
            if (_client is not null)
            {
                _ = Task.Run(async () =>
                {
                    // Load health + workspaces on startup
                    if (_tabView.Tabs.FirstOrDefault()?.View is HealthScreen hs)
                        await hs.CheckHealthAsync().ConfigureAwait(false);
                    if (_authorizationPolicy.CanViewArea(McpArea.Workspaces))
                        await _workspaceListVm.LoadAsync().ConfigureAwait(false);
                });
            }
        };
    }

    private void ShowLoginDialog()
    {
        var dlg = new LoginDialog(username =>
        {
            UpdateAuthStatus();
            // Re-attach bearer token to client
            _client?.TrySetCachedBearerToken();
            RebuildTabs();
        });
        Application.Run(dlg);
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
    {
        if (_tabView.SelectedTab?.View is HealthScreen hs)
        {
            _ = Task.Run(hs.CheckHealthAsync);
            return;
        }

        if (_tabView.SelectedTab?.View is SessionLogScreen ss)
        {
            _ = Task.Run(ss.LoadAsync);
            return;
        }

        if (_tabView.SelectedTab?.View is TodoScreen ts)
        {
            _ = Task.Run(ts.LoadAsync);
            return;
        }

        if (_tabView.SelectedTab?.View is WorkspaceListScreen)
        {
            _ = Task.Run(() => _workspaceListVm.LoadAsync());
        }
    }

    private void RebuildTabs()
    {
        var previousTab = _tabView.SelectedTab?.DisplayText?.ToString();

        _tabView.RemoveAll();

        var selectFirst = true;

        if (_client is not null && _authorizationPolicy.CanViewArea(McpArea.Health))
        {
            _tabView.AddTab(new Tab { DisplayText = "Health", View = new HealthScreen(_healthVm, _client) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_authorizationPolicy.CanViewArea(McpArea.Workspaces))
        {
            _tabView.AddTab(new Tab { DisplayText = "Workspaces", View = new WorkspaceListScreen(_workspaceListVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_client is not null && _authorizationPolicy.CanViewArea(McpArea.Agents))
        {
            _tabView.AddTab(new Tab { DisplayText = "Agents", View = new AgentScreen(_client) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_client is not null && _authorizationPolicy.CanViewArea(McpArea.Todo))
        {
            _tabView.AddTab(new Tab { DisplayText = "TODO", View = new TodoScreen(_todoVm, _todoDetailVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_client is not null && _authorizationPolicy.CanViewArea(McpArea.SessionLogs))
        {
            _tabView.AddTab(new Tab { DisplayText = "Sessions", View = new SessionLogScreen(_sessionLogVm) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_client is not null && _authorizationPolicy.CanViewArea(McpArea.Sync))
        {
            _tabView.AddTab(new Tab { DisplayText = "Sync", View = new SyncScreen(_client) }, andSelect: selectFirst);
            selectFirst = false;
        }

        if (_authorizationPolicy.CanViewArea(McpArea.Policy))
        {
            _tabView.AddTab(new Tab { DisplayText = "Policy", View = new WorkspacePolicyScreen(_workspacePolicyVm) }, andSelect: selectFirst);
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
    }
}
