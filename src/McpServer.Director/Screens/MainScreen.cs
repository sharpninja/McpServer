using System.Collections.ObjectModel;
using McpServer.Cqrs;
using McpServer.Director.Auth;
using McpServer.Director.Helpers;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Navigation;
using McpServer.UI.Core.Services;
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
    private readonly SessionLogDetailViewModel _sessionLogDetailVm;
    private readonly TodoListViewModel _todoVm;
    private readonly TodoDetailViewModel _todoDetailVm;
    private readonly WorkspaceListViewModel _workspaceListVm;
    private readonly WorkspaceDetailViewModel _workspaceDetailVm;
    private readonly WorkspacePolicyViewModel _workspacePolicyVm;
    private readonly TunnelListViewModel _tunnelListVm;
    private readonly TemplateListViewModel _templateListVm;
    private readonly TemplateDetailViewModel _templateDetailVm;
    private readonly ToolListViewModel _toolListVm;
    private readonly ToolDetailViewModel _toolDetailVm;
    private readonly BucketListViewModel _bucketListVm;
    private readonly BucketDetailViewModel _bucketDetailVm;
    private readonly IssueListViewModel _issueListVm;
    private readonly IssueDetailViewModel _issueDetailVm;
    private readonly PullRequestListViewModel _pullRequestListVm;
    private readonly GitHubSyncViewModel _gitHubSyncVm;
    private readonly FrListViewModel _frListVm;
    private readonly FrDetailViewModel _frDetailVm;
    private readonly TrListViewModel _trListVm;
    private readonly TrDetailViewModel _trDetailVm;
    private readonly TestListViewModel _testListVm;
    private readonly TestDetailViewModel _testDetailVm;
    private readonly MappingListViewModel _mappingListVm;
    private readonly RequirementsGenerateViewModel _requirementsGenerateVm;
    private readonly AgentDefinitionListViewModel _agentDefinitionListVm;
    private readonly AgentDefinitionDetailViewModel _agentDefinitionDetailVm;
    private readonly WorkspaceAgentListViewModel _workspaceAgentListVm;
    private readonly WorkspaceAgentDetailViewModel _workspaceAgentDetailVm;
    private readonly AgentEventsViewModel _agentEventsVm;
    private readonly AgentPoolViewModel _agentPoolVm;
    private readonly EventStreamViewModel _eventStreamVm;
    private readonly WorkspaceContextViewModel _workspaceContextVm;
    private Label _authLabel = null!;
    private TabView _tabView = null!;
    private Label _workspaceContextLabel = null!;
    private TextView _workspaceContextStatus = null!;
    private readonly ObservableCollection<WorkspacePickerItem> _workspacePickerSource = [];
    private bool _authRefreshQueued;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IBrowserLauncher _browserLauncher;
    private readonly Dispatcher _dispatcher;
    private readonly ITabRegistry _tabRegistry;
    private readonly IServiceProvider _serviceProvider;

    public MainScreen(
        WorkspaceListViewModel workspaceListVm,
        WorkspaceDetailViewModel workspaceDetailVm,
        WorkspacePolicyViewModel workspacePolicyVm,
        HealthSnapshotsViewModel healthVm,
        DispatcherLogsViewModel dispatcherLogsVm,
        SessionLogListViewModel sessionLogVm,
        SessionLogDetailViewModel sessionLogDetailVm,
        TodoListViewModel todoVm,
        TodoDetailViewModel todoDetailVm,
        TunnelListViewModel tunnelListVm,
        TemplateListViewModel templateListVm,
        TemplateDetailViewModel templateDetailVm,
        ToolListViewModel toolListVm,
        ToolDetailViewModel toolDetailVm,
        BucketListViewModel bucketListVm,
        BucketDetailViewModel bucketDetailVm,
        IssueListViewModel issueListVm,
        IssueDetailViewModel issueDetailVm,
        PullRequestListViewModel pullRequestListVm,
        GitHubSyncViewModel gitHubSyncVm,
        FrListViewModel frListVm,
        FrDetailViewModel frDetailVm,
        TrListViewModel trListVm,
        TrDetailViewModel trDetailVm,
        TestListViewModel testListVm,
        TestDetailViewModel testDetailVm,
        MappingListViewModel mappingListVm,
        RequirementsGenerateViewModel requirementsGenerateVm,
        AgentDefinitionListViewModel agentDefinitionListVm,
        AgentDefinitionDetailViewModel agentDefinitionDetailVm,
        WorkspaceAgentListViewModel workspaceAgentListVm,
        WorkspaceAgentDetailViewModel workspaceAgentDetailVm,
        AgentEventsViewModel agentEventsVm,
        AgentPoolViewModel agentPoolVm,
        EventStreamViewModel eventStreamVm,
        WorkspaceContextViewModel workspaceContextVm,
        IAuthorizationPolicyService authorizationPolicy,
        IRoleContext roleContext,
        DirectorMcpContext directorContext,
        Dispatcher dispatcher,
        ITabRegistry tabRegistry,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IBrowserLauncher browserLauncher)
    {
        _healthVm = healthVm;
        _dispatcherLogsVm = dispatcherLogsVm;
        _sessionLogVm = sessionLogVm;
        _sessionLogDetailVm = sessionLogDetailVm;
        _todoVm = todoVm;
        _todoDetailVm = todoDetailVm;
        _workspaceListVm = workspaceListVm;
        _workspaceDetailVm = workspaceDetailVm;
        _workspacePolicyVm = workspacePolicyVm;
        _tunnelListVm = tunnelListVm;
        _templateListVm = templateListVm;
        _templateDetailVm = templateDetailVm;
        _toolListVm = toolListVm;
        _toolDetailVm = toolDetailVm;
        _bucketListVm = bucketListVm;
        _bucketDetailVm = bucketDetailVm;
        _issueListVm = issueListVm;
        _issueDetailVm = issueDetailVm;
        _pullRequestListVm = pullRequestListVm;
        _gitHubSyncVm = gitHubSyncVm;
        _frListVm = frListVm;
        _frDetailVm = frDetailVm;
        _trListVm = trListVm;
        _trDetailVm = trDetailVm;
        _testListVm = testListVm;
        _testDetailVm = testDetailVm;
        _mappingListVm = mappingListVm;
        _requirementsGenerateVm = requirementsGenerateVm;
        _agentDefinitionListVm = agentDefinitionListVm;
        _agentDefinitionDetailVm = agentDefinitionDetailVm;
        _workspaceAgentListVm = workspaceAgentListVm;
        _workspaceAgentDetailVm = workspaceAgentDetailVm;
        _agentEventsVm = agentEventsVm;
        _agentPoolVm = agentPoolVm;
        _eventStreamVm = eventStreamVm;
        _workspaceContextVm = workspaceContextVm;
        _authorizationPolicy = authorizationPolicy;
        _roleContext = roleContext;
        _directorContext = directorContext;
        _dispatcher = dispatcher;
        _tabRegistry = tabRegistry;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        _browserLauncher = browserLauncher;

        Title = "McpServer Director";
        Width = Dim.Fill();
        Height = Dim.Fill();

        ConfigureTabRegistry();
        BuildUi();

        // Seed the shared workspace context from the initial active workspace (e.g. from marker file).
        // Done after BuildUi so screen PropertyChanged subscriptions are active.
        _workspaceContextVm.ActiveWorkspacePath = _directorContext.ActiveWorkspacePath;
    }

    private void ConfigureTabRegistry()
    {
        _tabRegistry.Clear();

        bool HasWorkspaceOrControl(IServiceProvider _) =>
            _directorContext.HasActiveWorkspaceConnection || _directorContext.HasControlConnection;
        bool HasControlConnection(IServiceProvider _) => _directorContext.HasControlConnection;
        bool Always(IServiceProvider _) => true;

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Todo,
            "TODO",
            McpRoles.Viewer,
            _ => new TodoScreen(_todoVm, _todoDetailVm, directorContext: _directorContext),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.SessionLogs,
            "Sessions",
            McpRoles.Viewer,
            _ => new SessionLogScreen(_sessionLogVm, _sessionLogDetailVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Health,
            "Health",
            McpRoles.Viewer,
            _ => new HealthScreen(_healthVm),
            HasControlConnection));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Workspaces,
            "Workspaces",
            McpRoles.Admin,
            _ => new WorkspaceListScreen(_workspaceListVm, _workspaceDetailVm, _directorContext),
            HasControlConnection));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Agents,
            "Agents",
            McpRoles.AgentManager,
            _ => new AgentScreen(
                _agentDefinitionListVm,
                _agentDefinitionDetailVm,
                _workspaceAgentListVm,
                _workspaceAgentDetailVm,
                _agentEventsVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Agents,
            "Agent Pool",
            McpRoles.AgentManager,
            _ => new AgentPoolScreen(_agentPoolVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Agents,
            "Chat",
            McpRoles.AgentManager,
            _ => new AgentChatScreen(_directorContext),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Policy,
            "Policy",
            McpRoles.Admin,
            _ => new WorkspacePolicyScreen(_workspacePolicyVm),
            HasControlConnection));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Tunnels,
            "Tunnels",
            McpRoles.Viewer,
            _ => new TunnelScreen(_tunnelListVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Templates,
            "Templates",
            McpRoles.Viewer,
            _ => new TemplatesScreen(_templateListVm, _templateDetailVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Context,
            "Context",
            McpRoles.Viewer,
            _ => new ContextScreen(_dispatcher),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Repo,
            "Repo",
            McpRoles.Viewer,
            _ => new RepoScreen(_dispatcher),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.ToolRegistry,
            "Tools",
            McpRoles.Viewer,
            _ => new ToolRegistryScreen(
                _toolListVm,
                _toolDetailVm,
                _bucketListVm,
                _bucketDetailVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.GitHub,
            "GitHub",
            McpRoles.Viewer,
            _ => new GitHubScreen(
                _issueListVm,
                _issueDetailVm,
                _pullRequestListVm,
                _gitHubSyncVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Requirements,
            "Requirements",
            McpRoles.Viewer,
            _ => new RequirementsScreen(
                _frListVm,
                _frDetailVm,
                _trListVm,
                _trDetailVm,
                _testListVm,
                _testDetailVm,
                _mappingListVm,
                _requirementsGenerateVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.Events,
            "Events",
            McpRoles.Viewer,
            _ => new EventStreamScreen(_eventStreamVm),
            HasWorkspaceOrControl));

        _tabRegistry.RegisterTab(new TabRegistration(
            McpArea.DispatcherLogs,
            "Logs",
            McpRoles.Viewer,
            _ => new DispatcherLogsScreen(_dispatcherLogsVm),
            Always));
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

            // Propagate workspace change to the shared ViewModel;
            // subscribed ViewModels react and refresh themselves.
            _workspaceContextVm.ActiveWorkspacePath = _directorContext.ActiveWorkspacePath;
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
            ViewModelBinder.EnableScrollBars(this);

            _ = Task.Run(async () =>
            {
                var logsScreen = _tabView.Tabs
                    .Select(t => t.View)
                    .OfType<DispatcherLogsScreen>()
                    .FirstOrDefault();
                if (logsScreen is not null)
                    await logsScreen.LoadAsync().ConfigureAwait(true);

                if (!_directorContext.HasControlConnection)
                    return;

                // Load workspaces and auto-select context first
                if (_authorizationPolicy.CanViewArea(McpArea.Workspaces))
                {
                    await _workspaceListVm.LoadAsync().ConfigureAwait(true);
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
        var dlg = new LoginDialog(_browserLauncher);
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

        if (view is AgentPoolScreen aps)
        {
            _ = Task.Run(aps.LoadAsync);
            return;
        }

        if (view is AgentChatScreen acs)
        {
            _ = Task.Run(acs.LoadAsync);
            return;
        }

        if (view is TunnelScreen tunnel)
        {
            _ = Task.Run(tunnel.LoadAsync);
            return;
        }

        if (view is TemplatesScreen tmpl)
        {
            _ = Task.Run(tmpl.LoadAsync);
            return;
        }

        if (view is ContextScreen context)
        {
            _ = Task.Run(context.LoadAsync);
            return;
        }

        if (view is RepoScreen repo)
        {
            _ = Task.Run(repo.LoadAsync);
            return;
        }

        if (view is ToolRegistryScreen tools)
        {
            _ = Task.Run(tools.LoadAllAsync);
            return;
        }

        if (view is GitHubScreen github)
        {
            _ = Task.Run(github.LoadAllAsync);
            return;
        }

        if (view is RequirementsScreen requirements)
        {
            _ = Task.Run(requirements.LoadAllAsync);
            return;
        }

        if (view is EventStreamScreen events)
        {
            _ = Task.Run(events.LoadAsync);
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

        foreach (var registration in _tabRegistry.Registrations)
        {
            if (!_authorizationPolicy.CanViewArea(registration.Area))
                continue;

            if (registration.AvailabilityPredicate is not null &&
                !registration.AvailabilityPredicate(_serviceProvider))
                continue;

            if (registration.ScreenFactory(_serviceProvider) is not View view)
                continue;

            _tabView.AddTab(new Tab
            {
                DisplayText = registration.DisplayText,
                View = view
            }, andSelect: selectFirst);
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
            WordWrap = true,
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

        var preferred = WorkspaceAutoSelector.SelectPreferred(_workspaceListVm.Workspaces.ToList());
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
