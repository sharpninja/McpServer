using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using McpServer.Client;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Converters;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Models.Json;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Commands;
using StatusViewModel = McpServer.UI.Core.ViewModels.StatusViewModel;
using UiCoreWorkspaceCatalogChangeEvent = McpServer.UI.Core.ViewModels.WorkspaceCatalogChangeEvent;
using UiCoreWorkspaceCatalogChangeKind = McpServer.UI.Core.ViewModels.WorkspaceCatalogChangeKind;

namespace McpServer.UI.Core.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, ICommandTarget
{
    private const string AgentsReadmeFileName = "AGENTS-README-FIRST.yaml";

    private static readonly JsonSerializerOptions CopilotJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new WorkspaceInfoConverter() }
    };

    /// <summary>
    /// Target path resolved for the current OS (Windows path on Windows, /mnt/... on Linux).
    /// </summary>
    private static string GetResolvedTargetPath() => PathConverter.ToDisplayPath(AppSettings.ResolveSessionsRootPath());

    private static string GetHtmlCacheDir() => AppSettings.ResolveHtmlCacheDirectory();
    private static string GetMcpBaseUrl() => AppSettings.ResolveMcpBaseUrl();
    private const string McpSessionPrefix = "MCP_SESSION://";
    private const string McpAgentPrefix = "MCP_AGENT://";

    private readonly IClipboardService _clipboardService;
    private readonly ISystemNotificationService _systemNotificationService;
    private string _defaultMcpBaseUrl = "";
    private string? _defaultMcpApiKey;
    private string? _activeMcpApiKey;
    private Uri _defaultMcpBaseUri = new("http://localhost");
    private string _activeMcpBaseUrl = "";
    private string? _activeBearerToken;
    private McpAgentEventStreamService _agentEventStreamService = null!;
    private CancellationTokenSource? _agentEventListenerCts;
    private bool _agentEventListenerStarted;

    // Shared pre-authenticated clients — created once at connection time.
    private McpServerClient _mcpClient = null!;
    private McpServerClient _mcpPromptClient = null!;

    internal McpSessionLogService McpSessionService = null!;
    private McpTodoService _mcpTodoService = null!;
    private McpWorkspaceService _mcpWorkspaceService = null!;
    private McpVoiceConversationService _mcpVoiceService = null!;
    private UiCoreAppRuntime _uiCoreRuntime = null!;
    private bool _suppressWorkspaceSelectionChanged;
    private bool _hasCompletedInitialSwitch;
    internal McpServer.Cqrs.Dispatcher _dispatcher = null!;
    private readonly McpServiceFactory _serviceFactory = new();
    private readonly McpServer.UI.Core.Services.IFileSystemService _fs = new Services.Infrastructure.FileSystemService();
    private readonly McpServer.UI.Core.Services.IProcessLauncherService _processLauncher = new Services.Infrastructure.ProcessLauncherService();
    private readonly McpServer.UI.Core.Services.ITimerService _timerService = new Services.Infrastructure.TimerService();
    private readonly McpServer.UI.Core.Services.IJsonParsingService _jsonParser = new Services.Infrastructure.JsonParsingService();
    private static readonly ILogger _logger = AppLogService.Instance.CreateLogger("ViewModel");

    protected IClipboardService ClipboardService => _clipboardService;
    protected UiCoreAppRuntime UiCoreRuntime => _uiCoreRuntime;
    protected McpServerClient McpClient => _mcpClient;
    protected McpVoiceConversationService McpVoiceService => _mcpVoiceService;
    protected McpServer.Cqrs.Dispatcher CqrsDispatcher => _dispatcher;

    protected virtual void NotifyHistoryNavigationCanExecuteChanged()
    {
    }

    protected virtual void NotifyRequestNavigationCanExecuteChanged()
    {
    }

    protected virtual void NotifyRefreshCanExecuteChanged()
    {
    }

    protected virtual void NotifyArchiveCanExecuteChanged()
    {
    }

    /// <summary>Raised when the active workspace path changes. Child VMs subscribe to refresh reactively.</summary>
    public event Action<string>? WorkspacePathChanged;

    internal List<UnifiedSessionLog> _mcpSessions = new();
    internal Dictionary<string, UnifiedSessionLog> _mcpSessionsByPath = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Cached sessions from ReloadFromMcpAsyncInternal, consumed once by the auto-triggered ALL_JSON load.</summary>
    private IReadOnlyList<UnifiedSessionLog>? _cachedSessionsForAutoLoad;
    private bool _isRestoringTreeSelection;
    private bool _restoreRequestDetailsAfterRefresh;
    private string? _pendingDetailRequestId;
    private string? _pendingDetailSourcePath;
    private McpServer.UI.Core.Services.ITimerHandle? _mcpAutoRefreshTimer;
    private bool _isRefreshing;
    private McpServer.UI.Core.Services.ITimerHandle? _workspaceHealthTimer;
    private bool _isWorkspaceHealthCheckRunning;
    private bool _pendingWorkspaceHealthRefresh;
    private McpServer.UI.Core.Services.IWatcherHandle? _agentsReadmeWatcher;
    private string? _agentsReadmeWatchedFilePath;
    private int _agentsReadmeReloadVersion;

    [ObservableProperty]
    private ObservableCollection<FileNode> _nodes = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedNodePathDisplay))]
    private FileNode? _selectedNode;

    /// <summary>Null-safe path for binding (avoids 'Value is null' when SelectedNode is null).</summary>
    public string SelectedNodePathDisplay => SelectedNode?.Path ?? "";

    [ObservableProperty]
    private ObservableCollection<string> _changeLog = new();

    [ObservableProperty]
    private bool _isMarkdownVisible = true;

    [ObservableProperty]
    private bool _isJsonVisible = false;

    [ObservableProperty]
    private bool _isRequestDetailsVisible = false;

    // Phone navigation section visibility (for NavigationView-based phone UI)
    [ObservableProperty]
    private bool _isPhoneSessionsVisible = true;

    [ObservableProperty]
    private bool _isPhoneViewerVisible = false;

    [ObservableProperty]
    private bool _isPhoneHistoryVisible = false;

    protected void PhoneNavigateSection(string? sectionKey)
        => _ = _dispatcher.SendAsync(new Commands.PhoneNavigateSectionCommand(sectionKey));

    internal void PhoneNavigateSectionInternal(string? sectionKey)
    {
        IsPhoneSessionsVisible = sectionKey == "Sessions";
        IsPhoneViewerVisible = sectionKey == "Viewer";
        IsPhoneHistoryVisible = sectionKey == "History";
    }

    [ObservableProperty]
    private UnifiedRequestEntry? _selectedUnifiedRequest;

    [ObservableProperty]
    private ObservableCollection<JsonTreeNode> _jsonTree = new();

    [ObservableProperty]
    private JsonLogSummary _jsonLogSummary = new();

    [ObservableProperty]
    private ObservableCollection<SearchableEntry> _searchableEntries = new();

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _requestIdFilter = "";

    [ObservableProperty]
    private string _displayFilter = "";

    [ObservableProperty]
    private string _modelFilter = "";

    [ObservableProperty]
    private string _timestampFilter = "";

    [ObservableProperty]
    private string _agentFilter = "";

    /// <summary>Distinct values for filter ComboBoxes (includes "" for "All").</summary>
    [ObservableProperty]
    private ObservableCollection<string> _distinctRequestIds = new() { "" };

    [ObservableProperty]
    private ObservableCollection<string> _distinctDisplayTexts = new() { "" };

    [ObservableProperty]
    private ObservableCollection<string> _distinctModels = new() { "" };

    [ObservableProperty]
    private ObservableCollection<string> _distinctAgents = new() { "" };

    [ObservableProperty]
    private ObservableCollection<string> _distinctTimestamps = new() { "" };

    [ObservableProperty]
    private ObservableCollection<SearchableEntry> _filteredSearchEntries = new();

    [ObservableProperty]
    private SearchableEntry? _selectedSearchEntry;

    [ObservableProperty]
    private JsonTreeNode? _selectedJsonNode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppVersionDisplay))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _appVersion = ResolveAppVersion();

    public string AppVersionDisplay => $"v{AppVersion}";

    /// <summary>Window title including the full SemVer from GitVersion.</summary>
    public string WindowTitle => $"McpServerManager {AppVersionDisplay}";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    partial void OnStatusMessageChanged(string value)
    {
        StatusViewModel.Instance.AddStatus(value);
    }

    [ObservableProperty]
    private ObservableCollection<WorkspaceConnectionOption> _workspaceConnections = new();

    [ObservableProperty]
    private WorkspaceConnectionOption? _selectedWorkspaceConnection;

    [ObservableProperty]
    private IBrush _workspaceHealthIndicatorBrush = Brushes.Gray;

    [ObservableProperty]
    private string _workspaceHealthIndicatorTooltip = "Select a workspace";

    [ObservableProperty]
    private bool _isSwitchingWorkspace;

    [ObservableProperty]
    private string _agentsReadmeFilePath = "";

    [ObservableProperty]
    private string _agentsReadmeFileTimestampText = "";

    [ObservableProperty]
    private string _agentsReadmeLoadedAtLocalText = "";

    [ObservableProperty]
    private string _agentsReadmeStatusText = "Waiting for workspace selection...";

    [ObservableProperty]
    private string _agentsReadmeContent = "";

    /// <summary>True while an async operation (MCP refresh, JSON load, etc.) is in progress.</summary>
    [ObservableProperty]
    private bool _isBusy;

    partial void OnIsBusyChanged(bool value)
    {
        _logger.LogDebug($"[ViewModel] IsBusy changed to {value}");
    }

    /// <summary>True when markdown preview was opened in the system browser.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowMarkdownLoadingPlaceholder))]
    private bool _isPreviewOpenedInBrowser;

    /// <summary>True when markdown is selected but preview not yet loaded (show loading message).</summary>
    public bool ShowMarkdownLoadingPlaceholder => !IsPreviewOpenedInBrowser;

    /// <summary>Path to the current preview HTML file (for "Open in browser" button).</summary>
    [ObservableProperty]
    private string? _currentPreviewHtmlPath;

    /// <summary>When true, show markdown as raw text instead of rendered (use if a doc does not display).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRenderedMarkdown))]
    private bool _showMarkdownAsRawText;

    /// <summary>True when the rendered markdown viewer should be visible (inverse of ShowMarkdownAsRawText).</summary>
    public bool ShowRenderedMarkdown => !ShowMarkdownAsRawText;

    /// <summary>Raw markdown of the selected file when preview is opened externally.</summary>
    [ObservableProperty]
    private string _currentPreviewMarkdownText = "";

    private string? _currentMarkdownPath;

    /// <summary>Path we last navigated to; used to avoid reloading markdown when selection is restored to same path (e.g. after tree rebuild).</summary>
    private string? _lastNavigatedPath;

    private CancellationTokenSource? _markdownPreviewCts;

    /// <summary>Cancels any in-flight markdown preview task (e.g. pandoc generation).</summary>
    public void CancelMarkdownPreview()
    {
        _markdownPreviewCts?.Cancel();
        _markdownPreviewCts?.Dispose();
        _markdownPreviewCts = null;
    }

    // Navigation History
    private readonly Stack<FileNode> _backStack = new();
    private readonly Stack<FileNode> _forwardStack = new();
    private bool _isNavigatingHistory;

    public MainWindowViewModel(IClipboardService clipboardService)
        : this(clipboardService, GetMcpBaseUrl(), mcpApiKey: null, bearerToken: null, systemNotificationService: null)
    {
    }

    public MainWindowViewModel(IClipboardService clipboardService, string mcpBaseUrl)
        : this(clipboardService, mcpBaseUrl, mcpApiKey: null, bearerToken: null, systemNotificationService: null)
    {
    }

    public MainWindowViewModel(IClipboardService clipboardService, string mcpBaseUrl, string? mcpApiKey)
        : this(clipboardService, mcpBaseUrl, mcpApiKey, bearerToken: null, systemNotificationService: null)
    {
    }

    public MainWindowViewModel(
        IClipboardService clipboardService,
        string mcpBaseUrl,
        string? mcpApiKey,
        string? bearerToken,
        ISystemNotificationService? systemNotificationService = null)
    {
        _clipboardService = clipboardService;
        _systemNotificationService = systemNotificationService ?? NoOpSystemNotificationService.Instance;
        _activeBearerToken = string.IsNullOrWhiteSpace(bearerToken) ? null : bearerToken.Trim();
        InitializeMcpEndpoint(mcpBaseUrl, mcpApiKey);
    }

    private void InitializeMcpEndpoint(string mcpBaseUrl, string? initialApiKey = null)
    {
        _defaultMcpBaseUrl = NormalizeMcpBaseUrl(mcpBaseUrl);
        _defaultMcpApiKey = string.IsNullOrWhiteSpace(initialApiKey)
            ? McpServerRestClientFactory.TryResolveApiKey(_defaultMcpBaseUrl)
            : initialApiKey.Trim();
        _activeMcpApiKey = _defaultMcpApiKey;
        _defaultMcpBaseUri = new Uri(_defaultMcpBaseUrl, UriKind.Absolute);

        // Create shared pre-authenticated clients — used by ALL services for the session lifetime.
        _mcpClient = McpServerRestClientFactory.Create(
            _defaultMcpBaseUrl,
            timeout: TimeSpan.FromSeconds(30),
            apiKey: _activeMcpApiKey,
            bearerToken: _activeBearerToken);

        _mcpPromptClient = McpServerRestClientFactory.Create(
            _defaultMcpBaseUrl,
            timeout: TimeSpan.FromMinutes(15),
            apiKey: _activeMcpApiKey,
            bearerToken: _activeBearerToken);

        // Create services once via factory — they share the pre-authenticated clients.
        McpSessionService = _serviceFactory.CreateSessionLogService(_mcpClient);
        _mcpTodoService = _serviceFactory.CreateTodoService(_mcpClient, _mcpPromptClient);
        _mcpWorkspaceService = _serviceFactory.CreateWorkspaceService(_mcpClient, _defaultMcpBaseUri);
        _mcpVoiceService = _serviceFactory.CreateVoiceService(
            _defaultMcpBaseUrl,
            apiKey: _activeMcpApiKey,
            bearerToken: _activeBearerToken,
            resolveBaseUrl: () => _activeMcpBaseUrl,
            resolveBearerToken: () => _activeBearerToken,
            resolveApiKey: () => _activeMcpApiKey,
            resolveWorkspacePath: () => SelectedWorkspaceConnection?.WorkspaceRootPath
                ?? _mcpClient.WorkspacePath);
        _activeMcpBaseUrl = _defaultMcpBaseUrl;
        _agentEventStreamService = _serviceFactory.CreateEventStreamService(
            _activeMcpBaseUrl,
            apiKey: _activeMcpApiKey,
            bearerToken: _activeBearerToken,
            resolveBaseUrl: () => _activeMcpBaseUrl,
            resolveBearerToken: () => _activeBearerToken,
            resolveApiKey: () => _activeMcpApiKey,
            resolveWorkspacePath: () => SelectedWorkspaceConnection?.WorkspaceRootPath
                ?? _mcpClient.WorkspacePath);

        _uiCoreRuntime = new UiCoreAppRuntime(
            commandTarget: this,
            todoService: _mcpTodoService,
            workspaceService: _mcpWorkspaceService,
            voiceService: _mcpVoiceService,
            sessionLogService: McpSessionService,
            eventStreamService: _agentEventStreamService,
            workspaceContext: new McpServer.UI.Core.ViewModels.WorkspaceContextViewModel
            {
                ActiveWorkspacePath = _mcpClient.WorkspacePath ?? string.Empty
            });
        _dispatcher = _uiCoreRuntime.GetRequiredService<McpServer.Cqrs.Dispatcher>();

        // Pre-populate the workspace picker with a placeholder.
        // No switch is triggered here — the real switch happens in LoadWorkspaceConnectionsAsync
        // after the workspace catalog is fetched and the saved workspace key is resolved.
        var defaultOption = WorkspaceConnectionOption.CreateDefault(_defaultMcpBaseUri, primaryWorkspaceRootPath: null, _defaultMcpApiKey);
        WorkspaceConnections = new ObservableCollection<WorkspaceConnectionOption>(new[] { defaultOption });
        _suppressWorkspaceSelectionChanged = true;
        try
        {
            SelectedWorkspaceConnection = defaultOption;
        }
        finally
        {
            _suppressWorkspaceSelectionChanged = false;
        }
    }

    private static string NormalizeMcpBaseUrl(string mcpBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(mcpBaseUrl))
            throw new InvalidOperationException("MCP base URL is required.");

        var trimmed = mcpBaseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("MCP base URL must be an absolute URI.");

        return uri.ToString().TrimEnd('/');
    }

    private void ApplyActiveMcpBaseUrl(string mcpBaseUrl, string? mcpApiKey = null, string? workspaceRootPath = null)
    {
        _activeMcpBaseUrl = NormalizeMcpBaseUrl(mcpBaseUrl);
        _activeMcpApiKey = string.IsNullOrWhiteSpace(mcpApiKey) ? null : mcpApiKey.Trim();

        _logger.LogInformation(
            "[Workspace Switch] ApplyActiveMcpBaseUrl: BaseUrl={BaseUrl}, Bearer={BearerPresent}, ApiKey={ApiKeyPresent}, WorkspacePath={WorkspacePath}",
            _activeMcpBaseUrl,
            !string.IsNullOrWhiteSpace(_activeBearerToken) ? "set" : "null",
            !string.IsNullOrWhiteSpace(_activeMcpApiKey) ? "set" : "null",
            workspaceRootPath ?? "(none)");

        // Update auth/workspace routing on the shared clients — no service recreation needed.
        _mcpClient.BearerToken = _activeBearerToken ?? string.Empty;
        _mcpPromptClient.BearerToken = _activeBearerToken ?? string.Empty;
        _mcpClient.ApiKey = _activeMcpApiKey ?? string.Empty;
        _mcpPromptClient.ApiKey = _activeMcpApiKey ?? string.Empty;

        var resolvedPath = workspaceRootPath ?? string.Empty;
        _mcpClient.WorkspacePath = resolvedPath;
        _mcpPromptClient.WorkspacePath = resolvedPath;
        _uiCoreRuntime.WorkspaceContext.ActiveWorkspacePath = resolvedPath;

        // Notify child VMs reactively — they self-refresh without imperative ordering.
        WorkspacePathChanged?.Invoke(resolvedPath);

        if (_agentEventListenerStarted)
            StartAgentEventListener(restart: true);
    }

    private async Task<string?> ResolveActiveConnectionApiKeyAsync(WorkspaceConnectionOption option, string baseUrl)
    {
        // JWT and API keys are mutually exclusive. When a Bearer token is available,
        // the server ignores API keys entirely — skip the resolution overhead.
        if (!string.IsNullOrWhiteSpace(_activeBearerToken))
        {
            _logger.LogDebug("[Workspace Switch] Bearer token active — skipping API key resolution");
            return null;
        }

        var normalizedTargetBaseUrl = NormalizeMcpBaseUrl(baseUrl);

        // Try the workspace option's API key first.
        if (!string.IsNullOrWhiteSpace(option.ApiKey))
        {
            _logger.LogDebug("[Workspace Switch] Using workspace option API key for {BaseUrl}", normalizedTargetBaseUrl);
            return option.ApiKey;
        }

        // Prefer marker file full-access token before /api-key default token.
        // /api-key tokens are workspace-default and can 401 when paired with
        // explicit X-Workspace-Path headers on workspace-scoped endpoints.
        if (!string.IsNullOrWhiteSpace(option.WorkspaceRootPath))
        {
            var markerKey = McpServerRestClientFactory.TryResolveApiKeyForWorkspaceRoot(option.WorkspaceRootPath, normalizedTargetBaseUrl);
            if (!string.IsNullOrWhiteSpace(markerKey))
            {
                _logger.LogDebug("[Workspace Switch] Using marker file API key for {BaseUrl}", normalizedTargetBaseUrl);
                return markerKey;
            }
        }

        // Last resort: fetch current default key from /api-key.
        var fetchedDefaultKey = await McpServerRestClientFactory
            .TryFetchDefaultApiKeyAsync(normalizedTargetBaseUrl)
            .ConfigureAwait(true);

        if (!string.IsNullOrWhiteSpace(fetchedDefaultKey))
        {
            _logger.LogDebug("[Workspace Switch] Using /api-key default token for {BaseUrl}", normalizedTargetBaseUrl);
            return fetchedDefaultKey;
        }

        _logger.LogDebug("[Workspace Switch] No API key resolved for {BaseUrl}; proceeding without explicit key", normalizedTargetBaseUrl);
        return null;
    }

    /// <summary>Command for tree item tap (handles directory expand/collapse and MCP node refresh).</summary>
    protected void TreeItemTapped(FileNode? node)
        => _ = _dispatcher.SendAsync(new Commands.TreeItemTappedCommand(node));

    internal void TreeItemTappedInternal(FileNode? node)
    {
        if (node == null) return;
        if (IsMcpVirtualNode(node) && SelectedNode == node)
        {
            RefreshCurrentMcpNode();
            return;
        }
        if (node.IsDirectory)
        {
            // Root "All JSON" node: select it to load/refresh aggregated JSON
            if (Nodes.Contains(node))
            {
                if (SelectedNode == node)
                    GenerateAndNavigateInternal(node);
                else
                    SelectedNode = node;
                return;
            }

            bool expanding = !node.IsExpanded;
            node.IsExpanded = expanding;

            // Accordion: collapse siblings when expanding a directory
            if (expanding)
            {
                foreach (var topNode in Nodes)
                {
                    foreach (var sibling in topNode.Children)
                    {
                        if (sibling != node && sibling.IsDirectory)
                            sibling.IsExpanded = false;
                    }
                }
            }
            return;
        }
        SelectedNode = node;
    }

    /// <summary>Command for JSON tree node double-tap (navigate to details or copy text).</summary>
    protected void JsonNodeDoubleTapped(JsonTreeNode? node)
        => _ = _dispatcher.SendAsync(new Commands.JsonNodeDoubleTappedCommand(node));

    internal void JsonNodeDoubleTappedInternal(JsonTreeNode? node)
    {
        if (node == null) return;
        if (!string.IsNullOrEmpty(node.SourcePath))
            TryNavigateToDetailsForSourcePath(node.SourcePath);
    }

    /// <summary>Command for search row single-tap (select entry).</summary>
    protected void SearchRowTapped(SearchableEntry? entry)
        => _ = _dispatcher.SendAsync(new Commands.SearchRowTappedCommand(entry));

    internal void SearchRowTappedInternal(SearchableEntry? entry)
    {
        if (entry != null)
            SelectedSearchEntry = entry;
    }

    /// <summary>Command for search row double-tap (open request details).</summary>
    protected void SearchRowDoubleTapped(SearchableEntry? entry)
        => _ = _dispatcher.SendAsync(new Commands.SearchRowDoubleTappedCommand(entry));

    internal void SearchRowDoubleTappedInternal(SearchableEntry? entry)
    {
        if (entry?.UnifiedEntry != null)
            ShowRequestDetailsInternal(entry);
    }

    private static string ResolveAppVersion()
    {
        var assembly = typeof(MainWindowViewModel).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString()
            : informationalVersion;

        if (string.IsNullOrWhiteSpace(version))
            return "unknown";

        var markerIndex = version.IndexOf(".Sha", StringComparison.OrdinalIgnoreCase);
        return markerIndex > 0 ? version[..markerIndex] : version;
    }

    /// <summary>Called by MainWindow when it has opened. Builds the file tree off the UI thread and applies on UI; starts the watcher.</summary>
    public void InitializeAfterWindowShown()
    {
        _ = _dispatcher.SendAsync(new Commands.InitializeFromMcpCommand());
        _ = LoadWorkspaceConnectionsAsync();
        StartAgentEventListener();
    }

    private void StartAgentEventListener(bool restart = false)
    {
        if (restart)
        {
            StopAgentEventListener();
        }

        if (_agentEventListenerStarted)
            return;

        _agentEventListenerCts = new CancellationTokenSource();
        _agentEventListenerStarted = true;
        _ = Task.Run(() => RunAgentEventListenerLoopAsync(_agentEventListenerCts.Token));
    }

    private void StopAgentEventListener()
    {
        _agentEventListenerCts?.Cancel();
        _agentEventListenerCts?.Dispose();
        _agentEventListenerCts = null;
        _agentEventListenerStarted = false;
    }

    private async Task RunAgentEventListenerLoopAsync(CancellationToken cancellationToken)
    {
        var hasReportedFailure = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var changeEvent in _agentEventStreamService
                                   .StreamEventsAsync(cancellationToken: cancellationToken)
                                   )
                {
                    if (!IsActionableAgentEvent(changeEvent))
                        continue;

                    var message = BuildActionableAgentEventMessage(changeEvent);
                    await DispatchToUiAsync(() => StatusMessage = message);
                    await _systemNotificationService
                        .NotifyAgentEventAsync(changeEvent, message, cancellationToken)
                        ;
                }

                hasReportedFailure = false;

                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("[Agent Events] Stream ended; reconnecting.");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Agent Events] Listener failed; reconnecting.");
                if (!hasReportedFailure)
                {
                    await DispatchToUiAsync(() => StatusMessage = $"Agent event listener unavailable: {ex.Message}");
                    hasReportedFailure = true;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }

    private static bool IsActionableAgentEvent(McpIncomingChangeEvent changeEvent)
    {
        if (!IsAgentScopedEvent(changeEvent))
            return false;

        return MatchesActionableAgentState(changeEvent.Action)
            || MatchesActionableAgentState(changeEvent.EventType)
            || MatchesActionableAgentState(changeEvent.Status)
            || MatchesActionableAgentState(TryGetExtensionString(changeEvent, "action"))
            || MatchesActionableAgentState(TryGetExtensionString(changeEvent, "eventType"))
            || MatchesActionableAgentState(TryGetExtensionString(changeEvent, "status"))
            || MatchesActionableAgentState(TryGetExtensionString(changeEvent, "state"));
    }

    private static bool IsAgentScopedEvent(McpIncomingChangeEvent changeEvent)
    {
        if (!string.IsNullOrWhiteSpace(changeEvent.AgentId))
            return true;

        if (!string.IsNullOrWhiteSpace(TryGetExtensionString(changeEvent, "agentId")))
            return true;

        if (!string.IsNullOrWhiteSpace(changeEvent.Category) &&
            changeEvent.Category.Contains("agent", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(changeEvent.ResourceUri) &&
            changeEvent.ResourceUri.Contains("/agent", StringComparison.OrdinalIgnoreCase))
            return true;

        var extensionResourceUri = TryGetExtensionString(changeEvent, "resourceUri");
        return !string.IsNullOrWhiteSpace(extensionResourceUri) &&
               extensionResourceUri.Contains("/agent", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesActionableAgentState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();
        return normalized.Equals("launch", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("launched", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("completed", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("failed", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("blocked", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildActionableAgentEventMessage(McpIncomingChangeEvent changeEvent)
    {
        var action = FirstNonEmpty(
                changeEvent.Action,
                changeEvent.EventType,
                changeEvent.Status,
                TryGetExtensionString(changeEvent, "status"),
                TryGetExtensionString(changeEvent, "state"),
                TryGetExtensionString(changeEvent, "action"),
                TryGetExtensionString(changeEvent, "eventType"))
            ?? "updated";

        var normalizedAction = action.Trim().ToLowerInvariant() switch
        {
            "launched" => "launch",
            _ => action.Trim()
        };

        var agentId = FirstNonEmpty(
            changeEvent.AgentId,
            TryGetExtensionString(changeEvent, "agentId"),
            changeEvent.EntityId,
            TryGetExtensionString(changeEvent, "entityId"));

        return string.IsNullOrWhiteSpace(agentId)
            ? $"Agent event: {normalizedAction}"
            : $"Agent {agentId}: {normalizedAction}";
    }

    private static string? TryGetExtensionString(McpIncomingChangeEvent changeEvent, string key)
    {
        if (changeEvent.ExtensionData is null ||
            !changeEvent.ExtensionData.TryGetValue(key, out var extensionValue))
            return null;

        return extensionValue.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.String => extensionValue.GetString(),
            _ => extensionValue.ToString()
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    /// <summary>Platform callback to persist the selected workspace key.</summary>
    public Action<string?>? SaveWorkspaceKey { get; set; }

    /// <summary>Platform callback to load the previously selected workspace key.</summary>
    public Func<string?>? LoadWorkspaceKey { get; set; }

    partial void OnSelectedWorkspaceConnectionChanged(WorkspaceConnectionOption? value)
    {
        if (value == null)
        {
            UpdateAgentsReadmeWatcherForSelection(null);
            StopWorkspaceHealthRefresh();
            UpdateWorkspaceHealthIndicator(null, "Select a workspace");
            return;
        }

        UpdateAgentsReadmeWatcherForSelection(value);

        StartWorkspaceHealthRefresh();
        _ = RefreshSelectedWorkspaceHealthAsync();

        // Persist the selection for next startup
        SaveWorkspaceKey?.Invoke(value.Key);

        if (_suppressWorkspaceSelectionChanged)
            return;

        _ = SwitchWorkspaceConnectionAsync(value);
    }

    private async Task RefreshSelectedWorkspaceHealthAsync()
    {
        var selected = SelectedWorkspaceConnection;
        if (selected == null)
        {
            UpdateWorkspaceHealthIndicator(null, "Select a workspace");
            return;
        }

        if (_isWorkspaceHealthCheckRunning)
        {
            _pendingWorkspaceHealthRefresh = true;
            return;
        }

        _pendingWorkspaceHealthRefresh = false;
        _isWorkspaceHealthCheckRunning = true;
        var baseUrl = NormalizeMcpBaseUrl(selected.BaseUrl);
        var displayName = selected.DisplayName;

        try
        {
            var health = await McpWorkspaceService.ProbeHealthAsync(baseUrl).ConfigureAwait(true);

            var current = SelectedWorkspaceConnection;
            if (current == null ||
                !string.Equals(NormalizeMcpBaseUrl(current.BaseUrl), baseUrl, StringComparison.OrdinalIgnoreCase))
                return;

            UpdateWorkspaceHealthIndicator(health.Success, FormatWorkspaceHealthTooltip(displayName, health));
        }
        catch (Exception ex)
        {
            var current = SelectedWorkspaceConnection;
            if (current == null ||
                !string.Equals(NormalizeMcpBaseUrl(current.BaseUrl), baseUrl, StringComparison.OrdinalIgnoreCase))
                return;

            UpdateWorkspaceHealthIndicator(false, $"Unhealthy: {displayName} ({ex.Message})");
        }
        finally
        {
            _isWorkspaceHealthCheckRunning = false;
            if (_pendingWorkspaceHealthRefresh)
            {
                _pendingWorkspaceHealthRefresh = false;
                DispatchToUi(() => _ = RefreshSelectedWorkspaceHealthAsync());
            }
        }
    }

    private void StartWorkspaceHealthRefresh()
    {
        StopWorkspaceHealthRefresh();
        var interval = TimeSpan.FromMinutes(1);
        _workspaceHealthTimer = _timerService.CreateRecurring(interval, async ct =>
        {
            DispatchToUi(() => _ = RefreshSelectedWorkspaceHealthAsync());
            await Task.CompletedTask;
        });
    }

    private void StopWorkspaceHealthRefresh()
    {
        _workspaceHealthTimer?.Dispose();
        _workspaceHealthTimer = null;
    }

    private void UpdateWorkspaceHealthIndicator(bool? isHealthy, string tooltip)
    {
        WorkspaceHealthIndicatorBrush = isHealthy switch
        {
            true => Brushes.LimeGreen,
            false => Brushes.IndianRed,
            _ => Brushes.Gray
        };
        WorkspaceHealthIndicatorTooltip = tooltip;
    }

    private void UpdateAgentsReadmeWatcherForSelection(WorkspaceConnectionOption? selection)
    {
        var workspaceRootPath = selection?.WorkspaceRootPath;
        if (string.IsNullOrWhiteSpace(workspaceRootPath))
        {
            ReplaceAgentsReadmeWatcher(null);
            DispatchToUi(() =>
            {
                AgentsReadmeFilePath = "";
                AgentsReadmeFileTimestampText = "";
                AgentsReadmeLoadedAtLocalText = "";
                AgentsReadmeContent = "";
                AgentsReadmeStatusText = selection == null
                    ? "Waiting for workspace selection..."
                    : $"AGENTS file unavailable: no workspace root path for {selection.DisplayName}.";
            });
            return;
        }

        var filePath = _fs.CombinePath(workspaceRootPath.Trim(), AgentsReadmeFileName);
        ReplaceAgentsReadmeWatcher(filePath);
        DispatchToUi(() => AgentsReadmeFilePath = filePath);
        QueueAgentsReadmeReload(filePath);
    }

    private void ReplaceAgentsReadmeWatcher(string? filePath)
    {
        if (string.Equals(_agentsReadmeWatchedFilePath, filePath, StringComparison.OrdinalIgnoreCase))
            return;

        if (_agentsReadmeWatcher != null)
        {
            try
            {
                _agentsReadmeWatcher.Stop();
                _agentsReadmeWatcher.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing AGENTS file watcher");
            }
            finally
            {
                _agentsReadmeWatcher = null;
            }
        }

        _agentsReadmeWatchedFilePath = filePath;
        Interlocked.Increment(ref _agentsReadmeReloadVersion);

        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var directory = _fs.GetDirectoryName(filePath);
        var filter = Path.GetFileName(filePath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(filter) || !_fs.DirectoryExists(directory))
            return;

        try
        {
            _agentsReadmeWatcher = _serviceFactory.WatchFileSystem(directory, filter, changedPath =>
            {
                var watched = _agentsReadmeWatchedFilePath;
                if (!string.IsNullOrWhiteSpace(watched))
                    QueueAgentsReadmeReload(watched);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start AGENTS file watcher for {FilePath}", filePath);
            DispatchToUi(() => AgentsReadmeStatusText = $"AGENTS watcher failed: {ex.Message}");
        }
    }

    private void QueueAgentsReadmeReload(string filePath)
    {
        var version = Interlocked.Increment(ref _agentsReadmeReloadVersion);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150);
                if (version != Volatile.Read(ref _agentsReadmeReloadVersion))
                    return;

                await LoadAgentsReadmeFileAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AGENTS file reload scheduling failed");
            }
        });
    }

    private async Task LoadAgentsReadmeFileAsync(string filePath)
    {
        var loadedAtLocal = DateTimeOffset.Now;
        string content = "";
        string status;
        string fileTimestampText = "";

        try
        {
            if (!_fs.FileExists(filePath))
            {
                status = $"AGENTS file not found: {filePath}";
            }
            else
            {
                content = await ReadTextFileWithEncodingAsync(filePath);
                var lastWriteUtc = _fs.GetLastWriteTimeUtc(filePath);
                if (lastWriteUtc != DateTime.MinValue)
                    fileTimestampText = lastWriteUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");

                status = "AGENTS file loaded.";
            }
        }
        catch (Exception ex)
        {
            status = $"AGENTS file load failed: {ex.Message}";
        }

        await DispatchToUiAsync(() =>
        {
            if (!string.Equals(_agentsReadmeWatchedFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                return;

            AgentsReadmeFilePath = filePath;
            AgentsReadmeFileTimestampText = fileTimestampText;
            AgentsReadmeLoadedAtLocalText = loadedAtLocal.ToString("yyyy-MM-dd HH:mm:ss zzz");
            AgentsReadmeContent = content;
            AgentsReadmeStatusText = status;
        }).ConfigureAwait(true);
    }

    private static string FormatWorkspaceHealthTooltip(string displayName, McpWorkspaceHealthResult health)
    {
        var status = health.StatusCode > 0 ? $"HTTP {health.StatusCode}" : "HTTP n/a";
        var endpoint = string.IsNullOrWhiteSpace(health.Url) ? "" : $" @ {health.Url}";
        var error = string.IsNullOrWhiteSpace(health.Error) ? "" : $" ({health.Error})";
        return $"{(health.Success ? "Healthy" : "Unhealthy")}: {displayName} - {status}{endpoint}{error}";
    }

    protected async Task LoadWorkspaceConnectionsAsync()
    {
        var preferredSelection = SelectedWorkspaceConnection;
        var preferredBaseUrl = preferredSelection?.BaseUrl ?? _activeMcpBaseUrl;

        try
        {
            await LoadWorkspaceConnectionsAsync(preferredSelection, preferredBaseUrl, suppressStatusFailure: false)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LoadWorkspaceConnectionsAsync failed");
        }
    }

    private async Task LoadWorkspaceConnectionsAsync(
        WorkspaceConnectionOption? preferredSelection,
        string preferredBaseUrl,
        bool suppressStatusFailure)
    {
        preferredBaseUrl = string.IsNullOrWhiteSpace(preferredBaseUrl)
            ? _activeMcpBaseUrl
            : preferredBaseUrl;

        try
        {
            var query = await _mcpWorkspaceService.QueryAsync().ConfigureAwait(true);
            var options = BuildWorkspaceConnectionOptions(query.Items);
            DispatchToUi(() => ApplyWorkspaceConnectionOptions(options, preferredSelection, preferredBaseUrl));
        }
        catch (Exception ex)
        {
            if (!suppressStatusFailure)
                DispatchToUi(() => StatusMessage = $"Workspace list failed: {ex.Message}");
            throw;
        }
    }

    private List<WorkspaceConnectionOption> BuildWorkspaceConnectionOptions(List<McpWorkspaceItem>? workspaces)
    {
        var options = new List<WorkspaceConnectionOption>();

        if (workspaces != null)
        {
            foreach (var workspace in workspaces)
                options.Add(WorkspaceConnectionOption.FromWorkspace(_defaultMcpBaseUri, workspace));
        }

        // Only add a synthetic Default entry when no real workspaces exist
        if (options.Count == 0)
        {
            options.Add(WorkspaceConnectionOption.CreateDefault(_defaultMcpBaseUri, primaryWorkspaceRootPath: null, _defaultMcpApiKey));
        }
        else if (!options.Any(o => o.IsPrimary))
        {
            // No workspace marked as primary — promote the first one
            var first = options[0];
            options[0] = new WorkspaceConnectionOption
            {
                Key = first.Key,
                WorkspaceKey = first.WorkspaceKey,
                WorkspaceRootPath = first.WorkspaceRootPath,
                ApiKey = first.ApiKey,
                DisplayName = first.DisplayName,
                BaseUrl = first.BaseUrl,
                IsPrimary = true,
                IsEnabled = first.IsEnabled
            };
        }

        return options;
    }

    private void ApplyWorkspaceConnectionOptions(
        IEnumerable<WorkspaceConnectionOption> options,
        WorkspaceConnectionOption? preferredSelection,
        string preferredBaseUrl)
    {
        var list = options.ToList();
        if (list.Count == 0)
            list.Add(WorkspaceConnectionOption.CreateDefault(_defaultMcpBaseUri, primaryWorkspaceRootPath: null, apiKey: _defaultMcpApiKey));

        WorkspaceConnections = new ObservableCollection<WorkspaceConnectionOption>(list);

        // Try saved workspace key from platform persistence
        var savedKey = LoadWorkspaceKey?.Invoke();

        // Priority: exact preferred match by workspace key → saved workspace key → preferred base URL → primary → first.
        // Saved key is checked before base URL because all workspaces share the same base URL in single-port mode.
        var selected = FindWorkspaceConnectionOption(preferredSelection)
            ?? (!string.IsNullOrWhiteSpace(savedKey)
                ? WorkspaceConnections.FirstOrDefault(item =>
                    string.Equals(item.Key, savedKey, StringComparison.OrdinalIgnoreCase))
                : null)
            ?? WorkspaceConnections.FirstOrDefault(item => item.IsPrimary && item.IsEnabled)
            ?? WorkspaceConnections[0];

        _suppressWorkspaceSelectionChanged = true;
        try
        {
            SelectedWorkspaceConnection = selected;
        }
        finally
        {
            _suppressWorkspaceSelectionChanged = false;
        }

        // Initial picker population suppresses selection-changed switching to avoid duplicate work,
        // but we still need one real connection switch to set workspace path and refresh views.
        var selectedBaseUrl = NormalizeMcpBaseUrl(selected.BaseUrl);
        var activeBaseUrl = string.IsNullOrWhiteSpace(_activeMcpBaseUrl) ? "" : NormalizeMcpBaseUrl(_activeMcpBaseUrl);
        var activeWorkspacePath = _mcpClient.WorkspacePath ?? string.Empty;
        var selectedWorkspacePath = selected.WorkspaceRootPath ?? string.Empty;
        var needsSwitch = !_hasCompletedInitialSwitch
            || !string.Equals(activeBaseUrl, selectedBaseUrl, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(activeWorkspacePath, selectedWorkspacePath, StringComparison.OrdinalIgnoreCase);
        if (needsSwitch && !IsSwitchingWorkspace)
            _ = SwitchWorkspaceConnectionAsync(selected);
    }

    protected async Task RefreshWorkspacePickerAfterCatalogChangeAsync(UiCoreWorkspaceCatalogChangeEvent change)
    {
        var preferredSelection = SelectedWorkspaceConnection;
        var preferredBaseUrl = preferredSelection?.BaseUrl ?? _activeMcpBaseUrl;

        try
        {
            await LoadWorkspaceConnectionsAsync(preferredSelection, preferredBaseUrl, suppressStatusFailure: true)
                .ConfigureAwait(true);

            if (change.ChangeKind == UiCoreWorkspaceCatalogChangeKind.Deleted)
                return;

            await DispatchToUiAsync(() =>
            {
                var present = WorkspaceConnections.Any(item =>
                    !string.IsNullOrWhiteSpace(item.WorkspaceKey) &&
                    string.Equals(item.WorkspaceKey, change.WorkspaceKey, StringComparison.OrdinalIgnoreCase));

                if (present)
                    return;

                StatusMessage = $"Saved {change.WorkspaceKey}, but workspace picker did not include it after refresh.";
            }).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            DispatchToUi(() =>
            {
                StatusMessage = $"Workspace picker refresh failed after {change.ChangeKind.ToString().ToLowerInvariant()} {change.WorkspaceKey}: {ex.Message}";
            });
        }
    }

    private async Task SwitchWorkspaceConnectionAsync(WorkspaceConnectionOption option)
    {
        _logger.LogInformation($"[Workspace Switch] Switching to '{option.DisplayName}' (BaseUrl={option.BaseUrl})");
        var previousBaseUrl = _activeMcpBaseUrl;
        var previousApiKey = _activeMcpApiKey;
        var previousWorkspacePath = _mcpClient.WorkspacePath;
        var previousSelection = FindWorkspaceConnectionOption(
            WorkspaceConnections.FirstOrDefault(c =>
                !string.IsNullOrWhiteSpace(c.WorkspaceKey) &&
                string.Equals(c.WorkspaceKey, SelectedWorkspaceConnection?.WorkspaceKey, StringComparison.OrdinalIgnoreCase))
            ?? FindWorkspaceConnectionOptionByBaseUrl(previousBaseUrl));
        var selectedBaseUrl = NormalizeMcpBaseUrl(option.BaseUrl);
        IsSwitchingWorkspace = true;

        try
        {
            var preflight = await ProbeWorkspaceConnectionHealthAsync(selectedBaseUrl).ConfigureAwait(true);
            if (!preflight.Success)
            {
                throw new InvalidOperationException($"Health check failed ({FormatHealthFailure(preflight)})");
            }

            var selectedApiKey = await ResolveActiveConnectionApiKeyAsync(option, selectedBaseUrl).ConfigureAwait(true);
            _logger.LogInformation(
                "[Workspace Switch] Resolved for '{DisplayName}': BaseUrl={BaseUrl}, Bearer={Bearer}, WorkspacePath={WorkspacePath}",
                option.DisplayName, selectedBaseUrl,
                !string.IsNullOrWhiteSpace(_activeBearerToken) ? "set" : "null",
                option.WorkspaceRootPath ?? "(none)");
            ApplyActiveMcpBaseUrl(selectedBaseUrl, selectedApiKey ?? string.Empty, option.WorkspaceRootPath);
            await RefreshAllViewsForConnectionChangeAsync().ConfigureAwait(true);
            _hasCompletedInitialSwitch = true;
            _logger.LogInformation($"[Workspace Switch] Successfully connected to '{option.DisplayName}'");
            DispatchToUi(() => StatusMessage = $"Connected: {option.DisplayName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[Workspace Switch] Failed switching to '{option.DisplayName}'");
            ApplyActiveMcpBaseUrl(previousBaseUrl, previousApiKey ?? string.Empty, previousWorkspacePath);
            var revertedDisplayName = previousSelection?.DisplayName ?? previousBaseUrl;
            await DispatchToUiAsync(() =>
            {
                RestoreWorkspaceSelection(previousSelection, previousBaseUrl);
                StatusMessage = $"Workspace switch failed: {option.DisplayName} ({ex.Message}). Reverted to {revertedDisplayName}.";
            }).ConfigureAwait(true);
        }
        finally
        {
            IsSwitchingWorkspace = false;
        }
    }

    private async Task RefreshAllViewsForConnectionChangeAsync()
    {
        // Session logs are owned by MainWindowViewModel — refresh directly.
        _logger.LogDebug("[Workspace Switch] Refreshing session logs...");
        try
        {
            await ReloadFromMcpAsyncInternal().ConfigureAwait(true);
            _logger.LogDebug("[Workspace Switch] Session logs refreshed OK");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Workspace Switch] Session log refresh failed ({ExType}: {ExMsg}); continuing",
                ex.GetType().Name, ex.Message);
        }

        // Child VMs (Todo, Workspace, Voice) are refreshed reactively via
        // WorkspacePathChanged event — no imperative calls needed here.
        _logger.LogDebug("[Workspace Switch] All views refreshed.");
    }

    private async Task<McpWorkspaceHealthResult> ProbeWorkspaceConnectionHealthAsync(string baseUrl)
    {
        _logger.LogInformation("[Workspace Switch] Probing health at {BaseUrl}...", baseUrl);
        var result = await McpWorkspaceService.ProbeHealthAsync(baseUrl).ConfigureAwait(true);
        _logger.LogInformation("[Workspace Switch] Health probe for {BaseUrl}: Success={Success}, StatusCode={StatusCode}, Error={Error}",
            baseUrl, result.Success, result.StatusCode, result.Error ?? "(none)");
        return result;
    }

    private static string FormatHealthFailure(McpWorkspaceHealthResult health)
    {
        var status = health.StatusCode > 0 ? $"HTTP {health.StatusCode}" : "HTTP n/a";
        var target = string.IsNullOrWhiteSpace(health.Url) ? "" : $" @ {health.Url}";
        var error = string.IsNullOrWhiteSpace(health.Error) ? "" : $" - {health.Error}";
        return $"{status}{target}{error}";
    }

    private WorkspaceConnectionOption? FindWorkspaceConnectionOptionByBaseUrl(string baseUrl)
    {
        var normalized = NormalizeMcpBaseUrl(baseUrl);
        return WorkspaceConnections.FirstOrDefault(item =>
            string.Equals(NormalizeMcpBaseUrl(item.BaseUrl), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private WorkspaceConnectionOption? FindWorkspaceConnectionOption(WorkspaceConnectionOption? preferredSelection)
    {
        if (preferredSelection == null)
            return null;

        if (!string.IsNullOrWhiteSpace(preferredSelection.WorkspaceKey))
        {
            return WorkspaceConnections.FirstOrDefault(item =>
                string.Equals(item.WorkspaceKey, preferredSelection.WorkspaceKey, StringComparison.OrdinalIgnoreCase));
        }

        // No workspace key on preferred selection — can't match precisely in single-port mode
        return null;
    }

    private void RestoreWorkspaceSelection(WorkspaceConnectionOption? preferredSelection, string baseUrl)
    {
        var match = FindWorkspaceConnectionOption(preferredSelection)
            ?? FindWorkspaceConnectionOptionByBaseUrl(baseUrl);

        match ??= WorkspaceConnections.FirstOrDefault();
        if (match == null)
            return;

        _suppressWorkspaceSelectionChanged = true;
        try
        {
            SelectedWorkspaceConnection = match;
        }
        finally
        {
            _suppressWorkspaceSelectionChanged = false;
        }

        _ = RefreshSelectedWorkspaceHealthAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        UpdateFilteredSearchEntriesInternal();
    }

    partial void OnRequestIdFilterChanged(string value) => UpdateFilteredSearchEntriesInternal();
    partial void OnDisplayFilterChanged(string value) => UpdateFilteredSearchEntriesInternal();
    partial void OnModelFilterChanged(string value) => UpdateFilteredSearchEntriesInternal();
    partial void OnTimestampFilterChanged(string value) => UpdateFilteredSearchEntriesInternal();
    partial void OnAgentFilterChanged(string value) => UpdateFilteredSearchEntriesInternal();

    partial void OnSelectedSearchEntryChanged(SearchableEntry? value)
    {
        if (value == null) return;
        var node = FindJsonNodeBySourcePath(JsonTree, value.SourcePath);
        if (node != null)
        {
            SelectedJsonNode = node;
            ExpandToJsonNode(JsonTree, node);
        }
    }

    internal void UpdateFilteredSearchEntriesInternal()
    {
        var shouldRestoreDetailView = IsRequestDetailsVisible || _restoreRequestDetailsAfterRefresh;
        var detailRequestId = _pendingDetailRequestId ?? SelectedUnifiedRequest?.RequestId;
        var detailSourcePath = _pendingDetailSourcePath ?? SelectedSearchEntry?.SourcePath;

        if (string.IsNullOrWhiteSpace(detailSourcePath) && !string.IsNullOrWhiteSpace(detailRequestId))
        {
            detailSourcePath = SearchableEntries
                .FirstOrDefault(e => string.Equals(e.RequestId, detailRequestId, StringComparison.OrdinalIgnoreCase))
                ?.SourcePath;
        }

        var q = (SearchQuery ?? "").Trim();
        var rid = (RequestIdFilter ?? "").Trim().ToLowerInvariant();
        var disp = (DisplayFilter ?? "").Trim().ToLowerInvariant();
        var mod = (ModelFilter ?? "").Trim().ToLowerInvariant();
        var ts = (TimestampFilter ?? "").Trim().ToLowerInvariant();

        IEnumerable<SearchableEntry> filtered = SearchableEntries;

        if (!string.IsNullOrEmpty(q))
        {
            var matcher = Services.BooleanSearchParser.Parse(q);
            filtered = filtered.Where(e => matcher(e.SearchText ?? "") ||
                                          matcher(e.RequestId ?? "") ||
                                          matcher(e.DisplayText ?? "") ||
                                          matcher(e.Model ?? "") ||
                                          matcher(e.Agent ?? "") ||
                                          matcher(e.Timestamp ?? ""));
        }
        if (!string.IsNullOrEmpty(rid))
            filtered = filtered.Where(e => string.Equals(e.RequestId ?? "", rid, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(disp))
            filtered = filtered.Where(e => string.Equals(e.DisplayText ?? "", disp, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(mod))
            filtered = filtered.Where(e => string.Equals(e.Model ?? "", mod, StringComparison.OrdinalIgnoreCase));
        var agent = (AgentFilter ?? "").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(agent))
            filtered = filtered.Where(e => string.Equals(e.Agent ?? "", agent, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(ts))
            filtered = filtered.Where(e => string.Equals(e.TimestampDisplay ?? "", ts, StringComparison.OrdinalIgnoreCase) ||
                                            string.Equals(e.Timestamp ?? "", ts, StringComparison.OrdinalIgnoreCase));

        var sorted = filtered.OrderByDescending(e => e.SortableTimestamp ?? DateTime.MinValue).ToList();
        FilteredSearchEntries = new ObservableCollection<SearchableEntry>(sorted);

        if (shouldRestoreDetailView)
        {
            var restoredEntry = FindDetailEntryForRestore(FilteredSearchEntries, detailSourcePath, detailRequestId);
            if (restoredEntry?.UnifiedEntry != null)
            {
                SelectedSearchEntry = restoredEntry;
                ShowRequestDetailsInternal(restoredEntry);
            }
            NotifyRequestNavigationCanExecuteChanged();

            _restoreRequestDetailsAfterRefresh = false;
            _pendingDetailRequestId = null;
            _pendingDetailSourcePath = null;
        }

        NotifyContextConsumer();
    }

    private static SearchableEntry? FindDetailEntryForRestore(
        IEnumerable<SearchableEntry> entries,
        string? sourcePath,
        string? requestId)
    {
        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            var bySourcePath = entries.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.SourcePath) &&
                string.Equals(e.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase));
            if (bySourcePath != null)
                return bySourcePath;
        }

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            var byRequestId = entries.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.RequestId) &&
                string.Equals(e.RequestId, requestId, StringComparison.OrdinalIgnoreCase));
            if (byRequestId != null)
                return byRequestId;
        }

        return null;
    }

    protected void NavigateBack() => _ = _dispatcher.SendAsync(new Commands.NavigateBackCommand());

    public void NavigateBackInternal()
    {
        if (_backStack.Count > 0 && SelectedNode != null)
        {
            _isNavigatingHistory = true;
            _forwardStack.Push(SelectedNode);
            SelectedNode = _backStack.Pop();
            _isNavigatingHistory = false;
            NotifyHistoryNavigationCanExecuteChanged();
        }
    }

    protected bool CanNavigateBack() => _backStack.Count > 0;

    protected void NavigateForward() => _ = _dispatcher.SendAsync(new Commands.NavigateForwardCommand());

    public void NavigateForwardInternal()
    {
        if (_forwardStack.Count > 0 && SelectedNode != null)
        {
            _isNavigatingHistory = true;
            _backStack.Push(SelectedNode);
            SelectedNode = _forwardStack.Pop();
            _isNavigatingHistory = false;
            NotifyHistoryNavigationCanExecuteChanged();
        }
    }

    protected bool CanNavigateForward() => _forwardStack.Count > 0;

    protected async Task RefreshAsync()
    {
        _isRefreshing = true;
        NotifyRefreshCanExecuteChanged();
        try
        {
            await _dispatcher.SendAsync(new Commands.RefreshViewCommand());
        }
        finally
        {
            _isRefreshing = false;
            NotifyRefreshCanExecuteChanged();
        }
    }

    protected bool CanRefresh() => !_isRefreshing;

    public async Task RefreshInternalAsync()
    {
        if (SelectedNode != null)
        {
            if (SelectedNode.Path == "ALL_JSON_VIRTUAL_NODE" ||
                IsMcpSessionNode(SelectedNode) ||
                SelectedNode.Path.StartsWith("MCP_", StringComparison.OrdinalIgnoreCase))
            {
                await ReloadFromMcpAsyncInternal();
                return;
            }

            // Force regenerate
             string hash = SelectedNode.Path.GetHashCode().ToString("X");
             string tempFileName = $"{Path.GetFileNameWithoutExtension(SelectedNode.Path)}_{hash}.html";
             string tempDir = GetHtmlCacheDir();
             string tempPath = _fs.CombinePath(tempDir, tempFileName);

             if (_fs.FileExists(tempPath)) _fs.DeleteFile(tempPath);

             GenerateAndNavigateInternal(SelectedNode);
        }
    }

    partial void OnSelectedUnifiedRequestChanged(UnifiedRequestEntry? value)
    {
        LogUnifiedEntryActionsForGrid(value);
    }

    /// <summary>
    /// Detailed logging when the unified entry (and its Actions) are bound by the details grid.
    /// </summary>
    private static void LogUnifiedEntryActionsForGrid(UnifiedRequestEntry? entry)
    {
        const string prefix = "[Unified Grid Actions]";
        if (entry == null)
        {
            _logger.LogDebug($"{prefix} Bound entry is null; grid will show no actions.");
            return;
        }
        string requestId = entry.RequestId ?? "(null)";
        _logger.LogDebug($"{prefix} Binding entry for details grid RequestId={requestId} HasActions={entry.HasActions}");
        var actions = entry.Actions;
        if (actions == null || actions.Count == 0)
        {
            _logger.LogDebug($"{prefix}   Actions count=0 (grid will be empty/hidden).");
            return;
        }
        _logger.LogDebug($"{prefix}   Actions count={actions.Count}");
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            string desc = string.IsNullOrEmpty(a.Description) ? "" : (a.Description.Length <= 80 ? a.Description : a.Description.Substring(0, 77) + "...");
            _logger.LogDebug($"{prefix}   [{i + 1}] Order={a.Order} Type={a.Type} Status={a.Status} FilePath={a.FilePath} Description={desc}");
        }
    }

    protected void ShowRequestDetails(SearchableEntry entry) => _ = _dispatcher.SendAsync(new Commands.ShowRequestDetailsCommand(entry));

    public void ShowRequestDetailsInternal(SearchableEntry entry)
    {
        if (entry != null && entry.UnifiedEntry != null)
        {
            SelectedUnifiedRequest = entry.UnifiedEntry;
            IsMarkdownVisible = false;
            IsJsonVisible = false;
            IsRequestDetailsVisible = true;
            NotifyArchiveCanExecuteChanged();
            NotifyRequestNavigationCanExecuteChanged();
            NotifyContextConsumer();
        }
    }

    /// <summary>Navigates to the request details view for the currently selected search entry (e.g. on double-click).</summary>
    public void TryNavigateToSelectedSearchEntry()
    {
        if (SelectedSearchEntry is { } e && e.UnifiedEntry != null)
            ShowRequestDetails(e);
    }

    /// <summary>Called when the chat window is open; we push current context so the agent stays in sync with navigation.</summary>
    private Action<string>? _contextConsumer;

    /// <summary>Raised when the VM wants the platform to open the chat window (desktop-only).</summary>
    public event EventHandler? OpenChatWindowRequested;

    /// <summary>Raised when the user taps/clicks Logout. The platform host should clear tokens and navigate to the connection screen.</summary>
    public event EventHandler? LogoutRequested;

    protected void OpenChatWindow() => OpenChatWindowRequested?.Invoke(this, EventArgs.Empty);

    protected void Logout()
    {
        StopAgentEventListener();
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Register or clear the context consumer (chat window). When set, we call it when the user navigates to JSON or details.</summary>
    public void SetContextConsumer(Action<string>? consumer) => _contextConsumer = consumer;

    /// <summary>Called when the chat window is open; we set the AI model based on tree selection (e.g. codellama for source files, llama3 otherwise).</summary>
    private Action<string>? _modelConsumer;

    /// <summary>Register or clear the model consumer (chat window). When set, we call it with the desired model name when the selected node changes.</summary>
    public void SetModelConsumer(Action<string>? consumer) => _modelConsumer = consumer;

    /// <summary>Applies the model for the currently selected node (codellama for source files, llama3 for other files). Directory nodes are ignored. Call when the chat window is opened so the model matches the current selection.</summary>
    public void ApplyModelForCurrentSelection()
    {
        if (SelectedNode == null || SelectedNode.IsDirectory) return;
        bool isSourceFile = !string.IsNullOrEmpty(Path.GetExtension(SelectedNode.Path)) && IsCodeFile(Path.GetExtension(SelectedNode.Path));
        NotifyModelConsumer(isSourceFile ? "codellama:latest" : "llama3:latest");
    }

    private void NotifyModelConsumer(string model)
    {
        if (_modelConsumer == null) return;
        try
        {
            _modelConsumer(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyModelConsumer");
        }
    }

    private void NotifyContextConsumer()
    {
        if (_contextConsumer == null) return;
        try
        {
            var ctx = GetLogContextForAgent();
            _contextConsumer(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NotifyContextConsumer");
        }
    }

    /// <summary>Builds a short summary of the current log view for the AI assistant (filtered entries and selected request). Includes agent config, docs folder, src folder, and current log view.</summary>
    public string GetLogContextForAgent()
    {
        var entries = FilteredSearchEntries ?? SearchableEntries;
        var sb = new StringBuilder();
        string? resolvedPath = null;
        try { resolvedPath = GetResolvedTargetPath(); } catch (Exception ex) { _logger.LogDebug(ex, "[Path] GetResolvedTargetPath failed"); }

        var agentConfig = AgentConfigIo.ReadContent();
        if (!string.IsNullOrWhiteSpace(agentConfig))
        {
            sb.AppendLine("--- Agent instructions (from agent_config.md) ---");
            sb.AppendLine(agentConfig.Trim());
            sb.AppendLine();
        }

        if (resolvedPath != null)
        {
            AppendDocsContext(sb, resolvedPath);
            AppendSourceContext(sb, resolvedPath);
        }

        // Navigation context: what the user is currently viewing
        if (IsRequestDetailsVisible)
            sb.AppendLine("Navigation: Request details view (one request selected).");
        else if (IsJsonVisible && SelectedNode != null)
            sb.AppendLine(SelectedNode.Path == "ALL_JSON_VIRTUAL_NODE"
                ? "Navigation: All JSON (aggregated log from MCP sessions)."
                : $"Navigation: JSON file: {SelectedNode.Path}");
        else if (IsMarkdownVisible && !string.IsNullOrEmpty(_currentMarkdownPath))
            sb.AppendLine($"Navigation: Markdown: {_currentMarkdownPath}");
        else
            sb.AppendLine("Navigation: (list or loading)");
        sb.AppendLine();

        if (entries == null || entries.Count == 0)
        {
            sb.AppendLine("(No log loaded or no entries in current view.)");
            return sb.ToString();
        }
        sb.AppendLine($"Current view: {entries.Count} request(s).");
        sb.AppendLine("Columns: RequestId | DisplayText | Model | Agent | Timestamp");
        int take = Math.Min(50, entries.Count);
        for (int i = 0; i < take; i++)
        {
            var e = entries[i];
            sb.AppendLine($"  {e.RequestId} | {TruncateForContext(e.DisplayText, 60)} | {e.Model} | {e.Agent} | {e.TimestampDisplay}");
        }
        if (entries.Count > take)
            sb.AppendLine($"  ... and {entries.Count - take} more.");
        if (SelectedUnifiedRequest != null)
        {
            sb.AppendLine();
            sb.AppendLine("Selected request:");
            var r = SelectedUnifiedRequest;
            sb.AppendLine($"  RequestId: {r.RequestId}; Model: {r.Model}; Agent: {r.Agent}; Status: {r.Status}");
            if (!string.IsNullOrWhiteSpace(r.QueryTitle)) sb.AppendLine($"  Title: {r.QueryTitle}");
            if (!string.IsNullOrWhiteSpace(r.QueryText)) sb.AppendLine($"  Query: {TruncateForContext(r.QueryText, 200)}");
        }
        return sb.ToString();
    }

    private const int MaxDocsContextChars = 12_000;
    private const int MaxSourceContextChars = 16_000;
    private const int MaxSourceFileLines = 400;

    private void AppendDocsContext(StringBuilder sb, string resolvedPath)
    {
        string? docsPath = _fs.GetDirectoryName(resolvedPath);
        if (string.IsNullOrEmpty(docsPath) || !_fs.DirectoryExists(docsPath))
            return;
        try
        {
            var files = _fs.EnumerateFiles(docsPath, "*.md", false)
                .Where(f => !IsArchivedName(f.Name))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0)
                return;
            sb.AppendLine("--- Documents (from docs folder) ---");
            int remaining = MaxDocsContextChars;
            foreach (var entry in files)
            {
                if (remaining <= 0) break;
                string content;
                try
                {
                    content = _fs.ReadAllText(entry.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[CodeFiles] Could not read {FileName}", entry.Name);
                    content = $"(could not read {entry.Name})";
                }
                string block = $"\n### {entry.Name}\n{content}\n";
                if (block.Length > remaining)
                    block = block.AsSpan(0, remaining).ToString() + "\n...(truncated)";
                sb.Append(block);
                remaining -= block.Length;
            }
            if (remaining <= 0)
                sb.AppendLine("\n...(docs truncated)");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"(Docs folder unavailable: {ex.Message})");
            sb.AppendLine();
        }
    }

    private void AppendSourceContext(StringBuilder sb, string resolvedPath)
    {
        string? sourcePath = GetSourcePath(resolvedPath);
        if (string.IsNullOrEmpty(sourcePath) || !_fs.DirectoryExists(sourcePath))
            return;
        try
        {
            var filePaths = new List<string>();
            CollectCodeFiles(sourcePath, filePaths, resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (filePaths.Count == 0)
                return;
            sb.AppendLine("--- Source code (from src folder) ---");
            int remaining = MaxSourceContextChars;
            foreach (var path in filePaths)
            {
                if (remaining <= 0) break;
                string fileName = Path.GetFileName(path);
                string content;
                try
                {
                    var lines = _fs.ReadAllLines(path);
                    if (lines.Length > MaxSourceFileLines)
                        lines = lines.Take(MaxSourceFileLines).ToArray();
                    content = string.Join(Environment.NewLine, lines);
                    if (content.Length > 2000)
                        content = content.AsSpan(0, 2000).ToString() + "\n... (truncated)";
                }
                catch
                {
                    content = $"(could not read {fileName})";
                }
                string block = $"\n### {fileName}\n```\n{content}\n```\n";
                if (block.Length > remaining)
                    block = block.AsSpan(0, remaining).ToString() + "\n...(truncated)";
                sb.Append(block);
                remaining -= block.Length;
            }
            if (remaining <= 0)
                sb.AppendLine("\n...(source truncated)");
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"(Source folder unavailable: {ex.Message})");
            sb.AppendLine();
        }
    }

    private void CollectCodeFiles(string dirPath, List<string> filePaths, string sessionsPathToIgnore)
    {
        try
        {
            foreach (var dir in _fs.EnumerateDirectories(dirPath, "*", false))
            {
                if (SourceDirectoriesToSkip.Contains(dir.Name))
                    continue;
                var childFull = dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(childFull, sessionsPathToIgnore, StringComparison.OrdinalIgnoreCase))
                    continue;
                CollectCodeFiles(dir.FullName, filePaths, sessionsPathToIgnore);
            }
            foreach (var entry in _fs.EnumerateFiles(dirPath, "*", false))
            {
                if (IsCodeFile(entry.Extension))
                    filePaths.Add(entry.FullName);
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "[CodeFiles] Error traversing directory"); }
    }

    private static string TruncateForContext(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= maxLen ? s : s.AsSpan(0, maxLen).ToString() + "...";
    }

    /// <summary>If the given path matches a request/entry (e.g. "entries[0]", "requests[1]"), navigates to that request's detail view. Returns true if navigation occurred.</summary>
    public bool TryNavigateToDetailsForSourcePath(string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) return false;
        var entries = FilteredSearchEntries ?? SearchableEntries;
        if (entries == null) return false;
        foreach (var entry in entries)
        {
            if (string.Equals(entry.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) && entry.UnifiedEntry != null)
            {
                SelectedSearchEntry = entry;
                ShowRequestDetails(entry);
                return true;
            }
        }
        return false;
    }

    protected void SelectSearchEntry(SearchableEntry entry) => _ = _dispatcher.SendAsync(new Commands.SelectSearchEntryCommand(entry));

    public void SelectSearchEntryInternal(SearchableEntry entry)
    {
        if (entry != null)
            SelectedSearchEntry = entry;
    }

    public void CloseRequestDetailsInternal()
    {
        IsRequestDetailsVisible = false;
        IsJsonVisible = true;
        IsMarkdownVisible = false;
        NotifyArchiveCanExecuteChanged();
        NotifyContextConsumer();
    }

    private int GetCurrentRequestIndexInFilteredList()
    {
        if (SelectedUnifiedRequest == null || FilteredSearchEntries == null || FilteredSearchEntries.Count == 0)
            return -1;
        for (int i = 0; i < FilteredSearchEntries.Count; i++)
        {
            var entry = FilteredSearchEntries[i].UnifiedEntry;
            if (entry == SelectedUnifiedRequest)
                return i;
            if (entry != null && !string.IsNullOrEmpty(entry.RequestId) &&
                string.Equals(entry.RequestId, SelectedUnifiedRequest.RequestId, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    protected bool CanNavigateToPreviousRequest()
    {
        return GetCurrentRequestIndexInFilteredList() > 0;
    }

    protected bool CanNavigateToNextRequest()
    {
        int i = GetCurrentRequestIndexInFilteredList();
        return i >= 0 && i < FilteredSearchEntries.Count - 1;
    }

    protected void NavigateToPreviousRequest() => _ = _dispatcher.SendAsync(new Commands.NavigateToPreviousRequestCommand());

    public void NavigateToPreviousRequestInternal()
    {
        int i = GetCurrentRequestIndexInFilteredList();
        if (i <= 0) return;
        var entry = FilteredSearchEntries[i - 1];
        if (entry?.UnifiedEntry == null) return;
        SelectedSearchEntry = entry;
        ShowRequestDetails(entry);
            NotifyRequestNavigationCanExecuteChanged();
    }

    protected void NavigateToNextRequest() => _ = _dispatcher.SendAsync(new Commands.NavigateToNextRequestCommand());

    public void NavigateToNextRequestInternal()
    {
        int i = GetCurrentRequestIndexInFilteredList();
        if (i < 0 || i >= FilteredSearchEntries.Count - 1) return;
        var entry = FilteredSearchEntries[i + 1];
        if (entry?.UnifiedEntry == null) return;
        SelectedSearchEntry = entry;
        ShowRequestDetails(entry);
            NotifyRequestNavigationCanExecuteChanged();
    }

    partial void OnSelectedNodeChanging(FileNode? value)
    {
        if (!_isNavigatingHistory && SelectedNode != null && value != null && SelectedNode != value)
        {
            _backStack.Push(SelectedNode);
            _forwardStack.Clear();
            NotifyHistoryNavigationCanExecuteChanged();
        }
    }

    partial void OnSelectedNodeChanged(FileNode? value)
    {
        _logger.LogInformation($"Selected Node Changed: {value?.Path}");

        if (_isRestoringTreeSelection && value == null)
            return;

        // Start or stop the auto-refresh timer based on whether an MCP node is selected.
        if (value != null && IsMcpVirtualNode(value))
            StartMcpAutoRefresh();
        else
            StopMcpAutoRefresh();

        // Avoid reloading content when selection was restored to same path (e.g. after tree rebuild / file watcher).
        // MCP nodes always reload so clicks refresh data from the server.
        if (value != null && string.Equals(value.Path, _lastNavigatedPath, StringComparison.OrdinalIgnoreCase)
            && !IsMcpVirtualNode(value))
        {
            ExpandToNode(Nodes, value);
            return;
        }
        GenerateAndNavigateInternal(value);
        if (value != null)
        {
             ExpandToNode(Nodes, value);
        }
    }

    protected async Task CopyText(string text) => await _dispatcher.SendAsync(new Commands.CopyTextCommand(text));

    public async Task CopyTextInternal(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            await _clipboardService.SetTextAsync(text);
            SetStatus($"Copied: {text}");
        }
    }

    protected async Task CopyOriginalJson(UnifiedRequestEntry? entry) => await _dispatcher.SendAsync(new Commands.CopyOriginalJsonCommand(entry));

    public async Task CopyOriginalJsonInternal(UnifiedRequestEntry? entry)
    {
        if (entry?.OriginalEntry == null)
        {
            SetStatus("No original JSON to copy.");
            return;
        }
        try
        {
            var json = JsonSerializer.Serialize(entry.OriginalEntry, new JsonSerializerOptions { WriteIndented = true });
            await _clipboardService.SetTextAsync(json);
            SetStatus("Copied original JSON to clipboard.");
        }
        catch (Exception ex)
        {
            SetStatus($"Copy failed: {ex.Message}");
        }
    }

    public void HandleNavigation(string path)
    {
        // Path might be the resolved path in the temp directory, e.g., C:\Users\...\Temp\McpServerManager_Cache\next.md
        // We need to resolve it relative to the _currentMarkdownPath

        if (_currentMarkdownPath == null) return;

        string fileName = Path.GetFileName(path);
        // Assuming relative links are just filenames or relative paths.
        // If the browser resolved it to the temp dir, we just want the relative part.
        // But extracting the relative part from the temp path is hard if we don't know the temp root.

        // Simple approach: Take the filename and look in the current markdown's folder
        // Better approach: If the path contains "McpServerManager_Cache", strip it and the prefix.

        // Actually, pandoc generates relative links.
        // If I am at "doc.html" in "temp/", and link is "sub/next.md", browser goes to "temp/sub/next.md".
        // I want "original_dir/sub/next.md".

        string? currentDir = _fs.GetDirectoryName(_currentMarkdownPath);
        if (currentDir == null) return;

        // Try to handle navigation relative to the current markdown file's directory
        // The path might be absolute (e.g. temp dir) or relative
        // e.g. C:\Users\...\Temp\McpServerManager_Cache\copilot\session-2026-02-02-073300\session-log.md
        // But we want to map it to the configured sessions root (e.g. <SessionsRootPath>\copilot\session-2026-02-02-073300\session-log.md)

        // If the path contains "McpServerManager_Cache", we should try to extract the relative part
        // But since we flatten the cache (or do we?), wait, let's check GenerateAndNavigate.
        // We generate into tempDir directly: Path.Combine(tempDir, tempFileName);
        // tempFileName is Name_Hash.html.
        // So all HTML files are in the root of McpServerManager_Cache.

        // However, pandoc generated links might be relative.
        // If README.md links to "copilot/session.md", browser tries to go to "McpServerManager_Cache/copilot/session.md"
        // In that case, 'path' will be ".../McpServerManager_Cache/copilot/session.md"

        string tempDir = GetHtmlCacheDir();
        if (path.StartsWith(tempDir, StringComparison.OrdinalIgnoreCase))
        {
             // It's inside our temp dir structure.
             // Get the relative path from tempDir
             string relativePath = Path.GetRelativePath(tempDir, path);

             // Now combine with the root target path? Or current markdown's directory?
             // Since we don't replicate directory structure in temp (we flatten or hash), this is tricky.
             // But wait, if pandoc generates relative links, and the browser resolves them against the base URL (the temp html file),
             // then "copilot/session.md" becomes "temp/copilot/session.md".

             // If we assume the link was relative to the markdown file, we should combine it with the markdown's folder

             string targetPath = _fs.CombinePath(currentDir, relativePath);

             // Normalize path
             targetPath = _fs.GetFullPath(targetPath);

             if (_fs.FileExists(targetPath))
             {
                 SelectNodeByPath(targetPath);
                 return;
             }
        }

        // Fallback: Try just the filename in current dir
        string targetPathSimple = _fs.CombinePath(currentDir, fileName);
        if (_fs.FileExists(targetPathSimple))
        {
            SelectNodeByPath(targetPathSimple);
        }
        else
        {
            _logger.LogWarning($"Could not resolve navigation: {path} -> {targetPathSimple}");
        }
    }

    private void SelectNodeByPath(string path)
    {
        // Recursive search for the node
        var node = FindNode(Nodes, path);
        if (node != null)
        {
            SelectedNode = node;
            ExpandToNode(Nodes, node);
        }
        else
        {
             _logger.LogWarning($"Node not found in tree for: {path}");
        }
    }

    private void ExpandToNode(ObservableCollection<FileNode> nodes, FileNode target)
    {
        foreach (var node in nodes)
        {
            if (node == target) return;

            if (ContainsNode(node, target))
            {
                node.IsExpanded = true;
                ExpandToNode(node.Children, target);
                return;
            }
        }
    }

    private bool ContainsNode(FileNode parent, FileNode target)
    {
        foreach (var child in parent.Children)
        {
            if (child == target) return true;
            if (ContainsNode(child, target)) return true;
        }
        return false;
    }

    private FileNode? FindNode(ObservableCollection<FileNode> nodes, string path)
    {
        foreach (var node in nodes)
        {
            if (node.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) return node;

            var found = FindNode(node.Children, path);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>After rebuilding the file tree, restore selection by path so we don't force "All JSON" every time (e.g. on watcher refresh).</summary>
    private void RestoreTreeSelection(string? previousPath, FileNode allJsonNode)
    {
        var toSelect = !string.IsNullOrEmpty(previousPath) ? FindNode(Nodes, previousPath) : null;
        SelectedNode = toSelect ?? allJsonNode;
    }

    private bool IsMcpSessionNode(FileNode? node) =>
        node != null &&
        !node.IsDirectory &&
        node.Path.StartsWith(McpSessionPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool IsMcpAgentNode(FileNode? node) =>
        node != null &&
        node.Path.StartsWith(McpAgentPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true for ALL_JSON, MCP agent, and MCP session virtual nodes.</summary>
    public static bool IsMcpVirtualNode(FileNode? node) =>
        node != null &&
        (node.Path == "ALL_JSON_VIRTUAL_NODE" || IsMcpAgentNode(node) ||
         node.Path.StartsWith(McpSessionPrefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>Re-navigates the currently selected MCP node, refreshing its data from the server. Called from code-behind when the user re-taps an already-selected node.</summary>
    public void RefreshCurrentMcpNode()
    {
        if (SelectedNode != null && IsMcpVirtualNode(SelectedNode))
            GenerateAndNavigateInternal(SelectedNode);
    }

    /// <summary>Starts an auto-refresh timer for MCP nodes (60s). Stops any existing timer first.</summary>
    private void StartMcpAutoRefresh()
    {
        StopMcpAutoRefresh();
        var interval = TimeSpan.FromMinutes(1);
        _mcpAutoRefreshTimer = _timerService.CreateRecurring(interval, async ct =>
        {
            DispatchToUi(() =>
            {
                if (!_isRefreshing && SelectedNode != null && IsMcpVirtualNode(SelectedNode))
                    _ = RefreshAsync();
            });
            await Task.CompletedTask;
        });
    }

    private void StopMcpAutoRefresh()
    {
        _mcpAutoRefreshTimer?.Dispose();
        _mcpAutoRefreshTimer = null;
    }

    private static string GetAgentNameFromVirtualPath(string virtualPath) =>
        virtualPath.StartsWith(McpAgentPrefix, StringComparison.OrdinalIgnoreCase)
            ? virtualPath.Substring(McpAgentPrefix.Length).Trim()
            : virtualPath.Trim();

    internal static string BuildMcpSessionPath(string sourceType, string sessionId) =>
        $"{McpSessionPrefix}{sourceType}/{sessionId}";

    internal static DateTime GetSessionSortTimestamp(UnifiedSessionLog s) =>
        s.LastUpdated ?? s.Started ?? DateTime.MinValue;

    internal static IEnumerable<UnifiedSessionLog> OrderSessionsNewestFirst(IEnumerable<UnifiedSessionLog> sessions) =>
        sessions
            .OrderByDescending(GetSessionSortTimestamp)
            .ThenByDescending(s => s.SessionId ?? "", StringComparer.OrdinalIgnoreCase);

    /// <summary>Builds a path-keyed dictionary of sessions, keeping the newest per path.</summary>
    internal Dictionary<string, UnifiedSessionLog> BuildSessionsByPathDict(IReadOnlyList<UnifiedSessionLog> sessions)
    {
        var byPath = new Dictionary<string, UnifiedSessionLog>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sessions)
        {
            var path = BuildMcpSessionPath(s.SourceType ?? "Unknown", s.SessionId ?? "");
            if (!byPath.TryGetValue(path, out var existing) || GetSessionSortTimestamp(s) >= GetSessionSortTimestamp(existing))
                byPath[path] = s;
        }
        return byPath;
    }

    /// <summary>Deduplicates and orders sessions newest-first.</summary>
    internal List<UnifiedSessionLog> OrderAndDeduplicateSessions(Dictionary<string, UnifiedSessionLog> byPath)
        => OrderSessionsNewestFirst(byPath.Values).ToList();

    /// <summary>Updates the cached MCP session state on the UI thread.</summary>
    internal void SetMcpSessionState(List<UnifiedSessionLog> sessions, Dictionary<string, UnifiedSessionLog> byPath)
    {
        _mcpSessions = sessions;
        _mcpSessionsByPath = byPath;
    }

    internal async Task ReloadFromMcpAsyncInternal()
    {
        var isInitialMcpLoad = _mcpSessions.Count == 0;
        if (IsRequestDetailsVisible)
        {
            _restoreRequestDetailsAfterRefresh = true;
            _pendingDetailRequestId = SelectedUnifiedRequest?.RequestId;
            _pendingDetailSourcePath = SelectedSearchEntry?.SourcePath;
        }

        var sessions = await McpSessionService.GetAllSessionsAsync(CancellationToken.None).ConfigureAwait(true);
        var byPath = new Dictionary<string, UnifiedSessionLog>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sessions)
        {
            var path = BuildMcpSessionPath(s.SourceType ?? "Unknown", s.SessionId ?? "");
            if (!byPath.TryGetValue(path, out var existing) || GetSessionSortTimestamp(s) >= GetSessionSortTimestamp(existing))
                byPath[path] = s;
        }

        var uniqueSessions = OrderSessionsNewestFirst(byPath.Values).ToList();
        var (allJsonNode, documentsDto, sourceDto) = BuildMcpTreeOffThread(uniqueSessions);
        // Await the UI dispatch so the tracked task stays alive until the tree selection
        // (and any subsequent navigation commands it triggers) has been dispatched.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _mcpSessions = uniqueSessions;
            _mcpSessionsByPath = byPath;

            var previousPath = SelectedNode?.Path;
            _isRestoringTreeSelection = true;
            try
            {
                Nodes.Clear();
                Nodes.Add(allJsonNode);
                if (documentsDto != null)
                {
                    var documentsNode = ApplyTreeDtoToNodes(documentsDto);
                    documentsNode.Name = "Documents";
                    Nodes.Add(documentsNode);
                }
                if (sourceDto != null)
                {
                    var sourceNode = ApplyTreeDtoToNodes(sourceDto);
                    sourceNode.Name = "Source";
                    Nodes.Add(sourceNode);
                }

                // Cache sessions so the auto-triggered ALL_JSON load doesn't re-fetch from MCP.
                _cachedSessionsForAutoLoad = uniqueSessions;
                if (OperatingSystem.IsAndroid() && isInitialMcpLoad &&
                    (string.IsNullOrEmpty(previousPath) ||
                     string.Equals(previousPath, "ALL_JSON_VIRTUAL_NODE", StringComparison.OrdinalIgnoreCase)))
                {
                    // Avoid startup ANR by deferring the heavy ALL_JSON load until explicit user selection.
                    SelectedNode = null;
                }
                else
                {
                    RestoreTreeSelection(previousPath, allJsonNode);
                }
            }
            finally
            {
                _isRestoringTreeSelection = false;
            }
            StatusMessage = $"Loaded {_mcpSessions.Count} session(s) from MCP.";
        });
    }

    private (FileNode allJsonNode, TreeDto? documentsDto, TreeDto? sourceDto) BuildMcpTreeOffThread(IReadOnlyList<UnifiedSessionLog> sessions)
    {
        var allJsonNode = new FileNode("ALL_JSON_VIRTUAL_NODE", true) { Name = "All JSON", IsExpanded = true };
        var byAgent = sessions
            .GroupBy(s => string.IsNullOrWhiteSpace(s.SourceType) ? "Unknown" : s.SourceType.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Max(GetSessionSortTimestamp))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in byAgent)
        {
            var agentNode = new FileNode($"{McpAgentPrefix}{group.Key}", true) { Name = group.Key };
            foreach (var session in OrderSessionsNewestFirst(group))
            {
                var sessionPath = BuildMcpSessionPath(group.Key, session.SessionId ?? "");
                var title = !string.IsNullOrWhiteSpace(session.Title) ? session.Title!.Trim() : session.SessionId ?? "(session)";
                var item = new FileNode(sessionPath, false)
                {
                    Name = $"{title} ({session.SessionId})"
                };
                agentNode.Children.Add(item);
            }
            allJsonNode.Children.Add(agentNode);
        }

        string? resolvedPath = null;
        try { resolvedPath = GetResolvedTargetPath(); } catch (Exception ex) { _logger.LogDebug(ex, "[Path] GetResolvedTargetPath failed"); }
        var documentsDto = resolvedPath != null ? BuildDocumentsDto(resolvedPath) : null;
        var sourceDto = resolvedPath != null ? BuildSourceDto(resolvedPath) : null;
        return (allJsonNode, documentsDto, sourceDto);
    }

    private static JsonTreeNode? FindJsonNodeBySourcePath(ObservableCollection<JsonTreeNode> nodes, string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath)) return null;
        foreach (var node in nodes)
        {
            if (string.Equals(node.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                return node;
            var found = FindJsonNodeBySourcePath(node.Children, sourcePath);
            if (found != null) return found;
        }
        return null;
    }

    private static void ExpandToJsonNode(ObservableCollection<JsonTreeNode> nodes, JsonTreeNode target)
    {
        foreach (var node in nodes)
        {
            if (node == target) return;
            if (ContainsJsonNode(node, target))
            {
                node.IsExpanded = true;
                ExpandToJsonNode(node.Children, target);
                return;
            }
        }
    }

    private static bool ContainsJsonNode(JsonTreeNode parent, JsonTreeNode target)
    {
        foreach (var child in parent.Children)
        {
            if (child == target) return true;
            if (ContainsJsonNode(child, target)) return true;
        }
        return false;
    }

    /// <summary>Dispatches an action to the UI thread. Use for any property/collection updates from background work.</summary>
    protected void DispatchToUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(() => action());
    }

    internal Task DispatchToUiAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    /// <summary>Reads a text file with encoding detection (BOM) and strips BOM so Markdown.Avalonia displays correctly.</summary>
    private async Task<string> ReadTextFileWithEncodingAsync(string filePath)
    {
        var fullPath = _fs.GetFullPath(filePath);
        if (!_fs.FileExists(fullPath))
            throw new FileNotFoundException("File not found.", fullPath);
        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync().ConfigureAwait(true);
        if (string.IsNullOrEmpty(text)) return text;
        // Strip Unicode BOM if present (can cause Markdown.Avalonia to show nothing)
        if (text[0] == '\uFEFF')
            text = text.Substring(1);
        return text;
    }

    internal void GenerateAndNavigateInternal(FileNode? node)
    {
         CancelMarkdownPreview();
         IsPreviewOpenedInBrowser = false;
         CurrentPreviewHtmlPath = null;
         CurrentPreviewMarkdownText = "";

         // Reset details view only when navigating to a different node (preserve on refresh)
         bool preserveDetailsView = node != null && node.Path == _lastNavigatedPath && IsRequestDetailsVisible;
         if (!preserveDetailsView)
             IsRequestDetailsVisible = false;

         if (node == null)
         {
             _lastNavigatedPath = null;
             return;
         }
         _lastNavigatedPath = node.Path;

         if (preserveDetailsView)
         {
            NotifyRequestNavigationCanExecuteChanged();
         }

         // Sync AI model with selection: codellama for source files, llama3 for other files; ignore directory nodes.
         if (!node.IsDirectory)
         {
             bool isSourceFile = !string.IsNullOrEmpty(Path.GetExtension(node.Path)) && IsCodeFile(Path.GetExtension(node.Path));
             NotifyModelConsumer(isSourceFile ? "codellama:latest" : "llama3:latest");
         }

         if (node.Path == "ALL_JSON_VIRTUAL_NODE")
         {
             IsMarkdownVisible = false;
             if (!preserveDetailsView) IsJsonVisible = true;
             NotifyArchiveCanExecuteChanged();
             var cached = _cachedSessionsForAutoLoad;
             _cachedSessionsForAutoLoad = null;
             _ = _dispatcher.SendAsync(new Commands.RefreshAndLoadAllJsonCommand() { CachedSessions = cached });
             return;
         }

         if (IsMcpAgentNode(node))
         {
             IsMarkdownVisible = false;
             if (!preserveDetailsView) IsJsonVisible = true;
             NotifyArchiveCanExecuteChanged();
             var agentName = GetAgentNameFromVirtualPath(node.Path);
             _ = _dispatcher.SendAsync(new Commands.RefreshAndLoadAgentJsonCommand(agentName));
             return;
         }

         if (IsMcpSessionNode(node))
         {
             IsMarkdownVisible = false;
             if (!preserveDetailsView) IsJsonVisible = true;
             NotifyArchiveCanExecuteChanged();
             var virtualPath = node.Path;
             _ = _dispatcher.SendAsync(new Commands.RefreshAndLoadSessionCommand(virtualPath));
             return;
         }

         if (node.IsDirectory) return;

         if (node.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
         {
             IsMarkdownVisible = false;
             if (!preserveDetailsView) IsJsonVisible = true;
             NotifyArchiveCanExecuteChanged();
             _ = _dispatcher.SendAsync(new Commands.LoadJsonFileCommand(node.Path));
             return;
         }

         if (node.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
         {
             _ = _dispatcher.SendAsync(new Commands.LoadMarkdownFileCommand(node));
             return;
         }

         // Source tree: display code/project files in the markdown previewer (as a code block)
         if (!node.IsDirectory)
         {
             var ext = Path.GetExtension(node.Path);
             if (!string.IsNullOrEmpty(ext) && IsCodeFile(ext))
             {
                 _ = _dispatcher.SendAsync(new Commands.LoadSourceFileCommand(node));
                 return;
             }
         }
    }

    internal void LoadMarkdownFileInternal(FileNode node)
    {
         IsMarkdownVisible = true;
         IsJsonVisible = false;
         ShowMarkdownAsRawText = false;
         _currentMarkdownPath = node.Path;
         CurrentPreviewHtmlPath = null;
         NotifyArchiveCanExecuteChanged();
         NotifyContextConsumer();

         string pathForThisPreview = _fs.GetFullPath(node.Path);

         _markdownPreviewCts?.Dispose();
         _markdownPreviewCts = new CancellationTokenSource();
         var token = _markdownPreviewCts.Token;

         _ = Task.Run(async () =>
         {
             string markdownText;
             try
             {
                 markdownText = await ReadTextFileWithEncodingAsync(pathForThisPreview);
             }
             catch (Exception ex)
             {
                 markdownText = "";
                 DispatchToUi(() =>
                 {
                     if (_currentMarkdownPath != node.Path) return;
                     CurrentPreviewMarkdownText = "Could not read file: " + ex.Message;
                     IsPreviewOpenedInBrowser = true;
                 });
                 return;
             }
             if (token.IsCancellationRequested) return;
             DispatchToUi(() =>
             {
                 if (token.IsCancellationRequested || _currentMarkdownPath != node.Path) return;
                 CurrentPreviewMarkdownText = "";
                 IsPreviewOpenedInBrowser = true;
                 StatusMessage = "Markdown loaded.";
             });
             if (token.IsCancellationRequested) return;
             DispatchToUi(() =>
             {
                 if (token.IsCancellationRequested || _currentMarkdownPath != node.Path) return;
                 CurrentPreviewMarkdownText = markdownText ?? "";
             });
         });
    }

    internal void LoadSourceFileInternal(FileNode node)
    {
         IsMarkdownVisible = true;
         IsJsonVisible = false;
         _currentMarkdownPath = node.Path;
         CurrentPreviewHtmlPath = null;
         NotifyArchiveCanExecuteChanged();
         NotifyContextConsumer();

         string pathForThisPreview = node.Path;
         _markdownPreviewCts?.Dispose();
         _markdownPreviewCts = new CancellationTokenSource();
         var token = _markdownPreviewCts.Token;

         _ = Task.Run(async () =>
         {
             string content;
             try
             {
                 content = await _fs.ReadAllTextAsync(pathForThisPreview);
             }
             catch (Exception ex)
             {
                 content = "";
                 DispatchToUi(() =>
                 {
                     if (_currentMarkdownPath != pathForThisPreview) return;
                     CurrentPreviewMarkdownText = "Could not read file: " + ex.Message;
                     IsPreviewOpenedInBrowser = true;
                 });
                 return;
             }
             if (token.IsCancellationRequested) return;
             var lang = GetCodeBlockLanguage(pathForThisPreview);
             var markdownText = $"```{lang}\n{content}\n```";
             DispatchToUi(() =>
             {
                 if (token.IsCancellationRequested || _currentMarkdownPath != pathForThisPreview) return;
                 CurrentPreviewMarkdownText = markdownText;
                 IsPreviewOpenedInBrowser = true;
                 StatusMessage = "Source file loaded.";
             });
         });
    }

    private static string GetCodeBlockLanguage(string filePath)
    {
         var ext = Path.GetExtension(filePath);
         if (string.IsNullOrEmpty(ext)) return "";
         ext = ext.TrimStart('.').ToLowerInvariant();
         if (ext == "csproj" || ext == "vbproj" || ext == "fsproj" || ext == "sln") return "xml";
         return ext;
    }

    /// <summary>Removes duplicate entries by RequestId (case-insensitive). Keeps the first occurrence when ordered by timestamp descending (newest wins). Entries with empty RequestId are not deduplicated.</summary>
    public static List<UnifiedRequestEntry> DeduplicateUnifiedEntries(IEnumerable<UnifiedRequestEntry> orderedByNewestFirst)
    {
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<UnifiedRequestEntry>();
        foreach (var e in orderedByNewestFirst)
        {
            if (!string.IsNullOrWhiteSpace(e.RequestId))
            {
                if (!seenIds.Add(e.RequestId.Trim()))
                    continue;
            }
            result.Add(e);
        }
        return result;
    }

    /// <summary>Opens an HTML file in the system default browser.</summary>
    private void OpenHtmlInDefaultBrowser(string htmlFilePath)
    {
        if (!_fs.FileExists(htmlFilePath)) return;
        try
        {
            var path = _fs.GetFullPath(htmlFilePath);
            _processLauncher.OpenWithDefaultApp(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not open in browser");
        }
    }

    public void OpenPreviewInBrowserInternal()
    {
        if (!string.IsNullOrEmpty(CurrentPreviewHtmlPath))
        {
            OpenHtmlInDefaultBrowser(CurrentPreviewHtmlPath);
            return;
        }
        SetStatus("HTML preview is disabled.");
    }

    public void ToggleShowRawMarkdownInternal()
    {
        ShowMarkdownAsRawText = !ShowMarkdownAsRawText;
    }

    public void OpenAgentConfigInternal()
    {
        AgentConfigIo.EnsureExists();
        OpenFileInDefaultEditor(AgentConfigIo.GetFilePath(), "config");
    }

    public void OpenPromptTemplatesInternal()
    {
        PromptTemplatesIo.EnsureExists();
        OpenFileInDefaultEditor(PromptTemplatesIo.GetFilePath(), "prompts");
    }

    private void OpenFileInDefaultEditor(string path, string label)
    {
        if (!_fs.FileExists(path))
        {
            SetStatus($"Could not open {label}: file not found.");
            return;
        }
        try
        {
            var fullPath = _fs.GetFullPath(path);
            _processLauncher.OpenWithDefaultApp(fullPath);
            SetStatus($"Opened {label}: {path}");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open {label}: {ex.Message}");
        }
    }

    protected bool CanArchive() =>
        _currentMarkdownPath != null &&
        _fs.FileExists(_currentMarkdownPath) &&
        IsMarkdownVisible &&
        !_currentMarkdownPath.StartsWith(McpSessionPrefix, StringComparison.OrdinalIgnoreCase) &&
        !IsArchivedName(Path.GetFileName(_currentMarkdownPath));

    protected void Archive() => _ = _dispatcher.SendAsync(new Commands.ArchiveCurrentCommand());

    public void ArchiveInternal()
    {
        string? path = _currentMarkdownPath;
        if (string.IsNullOrEmpty(path) || !_fs.FileExists(path)) return;
        string? dir = _fs.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) return;
        string name = Path.GetFileName(path);
        string newName = "Archived-" + name;
        string newPath = _fs.CombinePath(dir, newName);
        Task.Run(() =>
        {
            try
            {
                _fs.MoveFile(path, newPath);
                DispatchToUi(() =>
                {
                    _currentMarkdownPath = null;
                    RebuildFileTree();
                    NotifyArchiveCanExecuteChanged();
                    StatusMessage = $"Archived: {newName}";
                });
            }
            catch (Exception ex)
            {
                DispatchToUi(() =>
                {
                    StatusMessage = $"Archive failed: {ex.Message}";
                    NotifyArchiveCanExecuteChanged();
                });
            }
        });
    }

    /// <summary>Opens (navigates to) the tree node. Used by tree context menu.</summary>
    protected void OpenTreeItem(FileNode? node) => _ = _dispatcher.SendAsync(new Commands.OpenTreeItemCommand(node));

    public void OpenTreeItemInternal(FileNode? node)
    {
        if (node == null) return;
        SelectedNode = node;
        ExpandToNode(Nodes, node);
    }

    /// <summary>Archives the file represented by the tree node by renaming to archive-&lt;name&gt;. Used by tree context menu. Not available for All JSON or directories.</summary>
    protected void ArchiveTreeItem(FileNode? node) => _ = _dispatcher.SendAsync(new Commands.ArchiveTreeItemCommand(node));

    public void ArchiveTreeItemInternal(FileNode? node)
    {
        if (node == null || node.Path == "ALL_JSON_VIRTUAL_NODE" || node.IsDirectory || node.Path.StartsWith(McpSessionPrefix, StringComparison.OrdinalIgnoreCase)) return;
        string path = node.Path;
        if (!_fs.FileExists(path)) return;
        string? dir = _fs.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) return;
        string name = Path.GetFileName(path);
        if (IsArchivedName(name)) return;
        string newName = "archive-" + name;
        string newPath = _fs.CombinePath(dir, newName);
        Task.Run(() =>
        {
            try
            {
                _fs.MoveFile(path, newPath);
                DispatchToUi(() =>
                {
                    if (SelectedNode == node)
                        _currentMarkdownPath = null;
                    RebuildFileTree();
                    StatusMessage = $"Archived: {newName}";
                });
            }
            catch (Exception ex)
            {
                DispatchToUi(() => StatusMessage = $"Archive failed: {ex.Message}");
            }
        });
    }

    protected static bool CanArchiveTreeItem(FileNode? node) =>
        node != null &&
        node.Path != "ALL_JSON_VIRTUAL_NODE" &&
        !node.Path.StartsWith(McpSessionPrefix, StringComparison.OrdinalIgnoreCase) &&
        !node.IsDirectory &&
        !IsArchivedName(Path.GetFileName(node.Path));

    /// <summary>Rebuilds the file tree on a background thread and applies on UI; selects All JSON.</summary>
    private void RebuildFileTree()
    {
        _ = ReloadFromMcpAsyncInternal();
    }

    private string? ResolveCssPath()
    {
        string? configuredPath = AppSettings.ResolveCssFallbackPath();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (_fs.FileExists(configuredPath)) return configuredPath;
            _logger.LogWarning($"Configured CSS path not found: {configuredPath}");
        }

        string cssPath = _fs.CombinePath(AppContext.BaseDirectory, "Assets", "styles.css");
        if (_fs.FileExists(cssPath)) return cssPath;
        string sourceCss = _fs.CombinePath(AppContext.BaseDirectory, "..", "..", "..", "McpServerManager", "Assets", "styles.css");
        if (_fs.FileExists(sourceCss)) return _fs.GetFullPath(sourceCss);
        return null;
    }

    private static bool? _isPandocAvailable;

    private async Task<bool> IsPandocAvailableAsync()
    {
        if (_isPandocAvailable.HasValue) return _isPandocAvailable.Value;
        try
        {
            var result = await _processLauncher.RunAsync("pandoc", "--version");
            _isPandocAvailable = result.ExitCode == 0;
        }
        catch
        {
            _isPandocAvailable = false;
        }
        if (!_isPandocAvailable.Value)
            _logger.LogWarning("Pandoc is not installed or not on PATH. Markdown preview will be limited.");
        return _isPandocAvailable.Value;
    }

    private async Task<bool> ConvertMarkdownToHtmlViaPandocAsync(string srcPath, string destPath)
    {
        try
        {
            if (!await IsPandocAvailableAsync()) return false;
            string? cssPath = ResolveCssPath();
            if (cssPath == null) return false;

            var result = await _processLauncher.RunAsync(
                "pandoc",
                $"\"{srcPath}\" -f markdown -t html -s --css \"{cssPath}\" --metadata title=\"Request Tracker\"");

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                _logger.LogError("Pandoc failed: ExitCode={ExitCode}. stderr: {Stderr}", result.ExitCode, result.StandardError);
                return false;
            }
            _fs.WriteAllText(destPath, result.StandardOutput);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting markdown to HTML");
            return false;
        }
    }

    private async Task<bool> ConvertMarkdownToHtmlAsync(string srcPath, string destPath)
    {
        try
        {
            if (!await IsPandocAvailableAsync()) return false;
            string? cssPath = ResolveCssPath();
            if (cssPath == null)
            {
                _logger.LogWarning("ConvertMarkdownToHtmlAsync: CSS path not found");
                return false;
            }

            var result = await _processLauncher.RunAsync(
                "pandoc",
                $"\"{srcPath}\" -f markdown -t html -s --css \"{cssPath}\" --metadata title=\"Request Tracker\"");

            if (result.ExitCode != 0)
            {
                _logger.LogError("Pandoc failed: ExitCode={ExitCode}. stderr: {Stderr}", result.ExitCode, result.StandardError);
                return false;
            }
            if (string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                _logger.LogError("Pandoc produced no output. stderr: {Stderr}", result.StandardError);
                return false;
            }
            await _fs.WriteAllTextAsync(destPath, result.StandardOutput);
            _logger.LogInformation("Pandoc completed: wrote {Length} chars to {DestPath}", result.StandardOutput.Length, destPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting markdown to HTML");
            return false;
        }
    }

    private void SetFallbackHtmlSource(string message)
    {
        CurrentPreviewMarkdownText = message;
        IsPreviewOpenedInBrowser = true;
    }

    internal void LoadJsonInternal(string path)
    {
        DispatchToUi(() => StatusMessage = "Loading JSON...");

        Task.Run(() =>
        {
            try
            {
                string jsonContent = _fs.ReadAllText(path);
                var treeResult = _jsonParser.ParseToTree(jsonContent);
                var jsonNode = treeResult.RootNode as JsonNode;
                string schemaType = "Unknown";
                var summary = new JsonLogSummary();
                UnifiedSessionLog? unifiedLog = null;

                if (jsonNode is JsonObject obj)
                {
                    if (obj.ContainsKey("sessionId") && obj.ContainsKey("statistics"))
                    {
                        schemaType = "Copilot Session Log";
                        var model = JsonSerializer.Deserialize<CopilotSessionLog>(jsonContent, CopilotJsonOptions);
                        if (model != null)
                        {
                            BuildCopilotSummaryAndIndex(model, summary);
                            unifiedLog = UnifiedLogFactory.Create(model);
                        }
                    }
                    else if (HasKeyCI(obj, "entries") && HasKeyCI(obj, "session"))
                    {
                        schemaType = "Cursor Request Log";
                        var model = JsonSerializer.Deserialize<CursorRequestLog>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (model != null)
                        {
                            BuildCursorSummaryAndIndex(model, summary);
                            unifiedLog = UnifiedLogFactory.Create(model);
                            if (unifiedLog != null)
                                FillActionsFromRawJson(jsonContent, unifiedLog);
                        }
                    }
                    else if (IsSingleCursorRequest(obj))
                    {
                        schemaType = "Cursor Request (single)";
                        var singleEntry = JsonSerializer.Deserialize<CursorRequestEntry>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (singleEntry != null)
                        {
                            var syntheticLog = new CursorRequestLog
                            {
                                Session = "Single Request",
                                Description = singleEntry.RequestId ?? "Request",
                                Entries = new List<CursorRequestEntry> { singleEntry }
                            };
                            unifiedLog = UnifiedLogFactory.Create(syntheticLog);
                            if (unifiedLog != null && unifiedLog.Entries.Count > 0 && unifiedLog.Entries[0].Actions.Count == 0)
                                FillActionsFromSingleEntryJson(jsonContent, unifiedLog);
                        }
                    }
                    else if (HasKeyCI(obj, "entries") && HasKeyCI(obj, "sourceType"))
                    {
                        schemaType = "Unified Session Log";
                        var unified = JsonSerializer.Deserialize<UnifiedSessionLog>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (unified?.Entries != null)
                        {
                            UnifiedLogFactory.EnsureOriginalEntriesSet(unified);
                            unifiedLog = unified;
                        }
                    }
                }

                DispatchToUi(() => ApplyLoadedJsonToUi(path, jsonNode, schemaType, summary, unifiedLog));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON");
                var msg = ex.Message;
                DispatchToUi(() =>
                {
                    JsonTree.Clear();
                    SearchableEntries.Clear();
                    FilteredSearchEntries.Clear();
                    UpdateDistinctFilterValues();
                    JsonLogSummary = new JsonLogSummary();
                    JsonTree.Add(new JsonTreeNode("Error", msg, "Error"));
                    StatusMessage = $"Error loading JSON: {msg}";
                });
            }
        });
    }

    private void ApplyLoadedJsonToUi(string path, JsonNode? jsonNode, string schemaType, JsonLogSummary summary, UnifiedSessionLog? unifiedLog)
    {
        JsonTree.Clear();
        SearchableEntries.Clear();
        UpdateDistinctFilterValues();
        JsonLogSummary = new JsonLogSummary();

        if (unifiedLog != null)
        {
            schemaType = $"{unifiedLog.SourceType} (Unified)";
            BuildUnifiedSummaryAndIndexInternal(unifiedLog, summary);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
            var unifiedNode = JsonSerializer.SerializeToNode(unifiedLog, options);
            summary.SummaryLines.Clear();
            summary.SummaryLines.Add($"Type: {unifiedLog.SourceType}");
            summary.SummaryLines.Add($"Session: {unifiedLog.SessionId}");
            summary.SummaryLines.Add($"Entries: {unifiedLog.EntryCount}");
            if (!string.IsNullOrEmpty(unifiedLog.Model))
                summary.SummaryLines.Add($"Model: {unifiedLog.Model}");
            if (unifiedLog.LastUpdated.HasValue)
                summary.SummaryLines.Add($"Last Updated: {unifiedLog.LastUpdated}");
            JsonLogSummary = summary;
            var root = new JsonTreeNode("Root", schemaType, "Object");
            root.IsExpanded = true;
            BuildJsonTreeInternal(unifiedNode, root, null);
            JsonTree.Add(root);
        }
        else
        {
            summary.SchemaType = schemaType;
            summary.SummaryLines.Clear();
            summary.SummaryLines.Add($"Schema: {schemaType}");
            summary.SummaryLines.Add($"Total: {summary.TotalCount}");
            if (!string.IsNullOrEmpty(summary.StatsByModel)) summary.SummaryLines.Add(summary.StatsByModel);
            if (!string.IsNullOrEmpty(summary.StatsBySuccess)) summary.SummaryLines.Add(summary.StatsBySuccess);
            if (!string.IsNullOrEmpty(summary.StatsCostOrTokens)) summary.SummaryLines.Add(summary.StatsCostOrTokens);
            JsonLogSummary = summary;
            var root = new JsonTreeNode("Root", schemaType, "Object");
            root.IsExpanded = true;
            BuildJsonTreeInternal(jsonNode, root, null);
            JsonTree.Add(root);
        }

        UpdateFilteredSearchEntriesInternal();
        StatusMessage = $"Loaded {Path.GetFileName(path)}";
    }

    /// <summary>
    /// Fills Actions on unified entries from raw JSON when the deserialized Cursor entry didn't have them
    /// (e.g. "Actions" vs "actions" or different structure). Ensures req-001-logging-system and others show actions.
    /// </summary>
    private void FillActionsFromRawJson(string jsonContent, UnifiedSessionLog unifiedLog)
    {
        if (unifiedLog?.Entries == null || unifiedLog.Entries.Count == 0) return;
        try
        {
            var treeResult = _jsonParser.ParseToTree(jsonContent);
            if (treeResult.RootNode is not JsonObject rootObj) return;

            JsonArray? entriesArray = null;
            foreach (var kvp in rootObj)
            {
                if (string.Equals(kvp.Key, "entries", StringComparison.OrdinalIgnoreCase) && kvp.Value is JsonArray arr)
                {
                    entriesArray = arr;
                    break;
                }
            }
            if (entriesArray == null) return;

            int i = 0;
            foreach (var entryNode in entriesArray)
            {
                if (i >= unifiedLog.Entries.Count) break;
                if (unifiedLog.Entries[i].Actions.Count == 0 && entryNode != null)
                {
                    var entryElement = entryNode.Deserialize<JsonElement>();
                    var actions = UnifiedLogFactory.ParseActionsFromEntryElement(entryElement);
                    if (actions.Count > 0)
                        unifiedLog.Entries[i].Actions = new ObservableCollection<UnifiedAction>(actions);
                }
                i++;
            }
        }
        catch
        {
            // Ignore; we already have whatever the deserializer gave us
        }
    }

    private static bool TryGetPropertyCI(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object) return false;
        if (element.TryGetProperty(propertyName, out value)) return true;
        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        return false;
    }

    private static bool HasKeyCI(JsonObject obj, string key)
    {
        foreach (var kv in obj)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool IsSingleCursorRequest(JsonObject obj)
    {
        if (!HasKeyCI(obj, "requestId")) return false;
        return HasKeyCI(obj, "exactRequest") || HasKeyCI(obj, "exactRequestNote") ||
               HasKeyCI(obj, "actions");
    }

    /// <summary>Parses a JSON file into a unified log using key-based format detection (matches single-file logic).
    /// Returns (unifiedLog, entryCount) or (null, -1) if not recognized.</summary>
    private (UnifiedSessionLog? log, int entryCount) TryParseFileToUnifiedLog(string text)
    {
        try
        {
            var treeResult = _jsonParser.ParseToTree(text);
            if (treeResult.RootNode is not JsonObject obj) return (null, -1);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1) Unified (entries + sourceType)
            if (HasKeyCI(obj, "entries") && HasKeyCI(obj, "sourceType"))
            {
                var unifiedLog = JsonSerializer.Deserialize<UnifiedSessionLog>(text, options);
                if (unifiedLog?.Entries != null)
                {
                    UnifiedLogFactory.EnsureOriginalEntriesSet(unifiedLog);
                    return (unifiedLog, unifiedLog.EntryCount);
                }
            }

            // 2) Cursor log (entries + session)
            if (HasKeyCI(obj, "entries") && HasKeyCI(obj, "session"))
            {
                var cursorLog = JsonSerializer.Deserialize<CursorRequestLog>(text, options);
                if (cursorLog?.Entries != null)
                {
                    var unified = UnifiedLogFactory.Create(cursorLog);
                    if (unified != null) return (unified, unified.EntryCount);
                }
            }

            // 3) Copilot (sessionId + statistics) — require non-empty Requests so Cursor files aren't mis-detected
            if (obj.ContainsKey("sessionId") && obj.ContainsKey("statistics"))
            {
                var copilotLog = JsonSerializer.Deserialize<CopilotSessionLog>(text, CopilotJsonOptions);
                if (copilotLog?.Requests != null && copilotLog.Requests.Count > 0)
                {
                    var unified = UnifiedLogFactory.Create(copilotLog);
                    if (unified != null) return (unified, unified.EntryCount);
                }
            }

            // 4) Single Cursor request
            if (IsSingleCursorRequest(obj))
            {
                var singleEntry = JsonSerializer.Deserialize<CursorRequestEntry>(text, options);
                if (singleEntry != null)
                {
                    var syntheticLog = new CursorRequestLog
                    {
                        Session = "Single Request",
                        Description = singleEntry.RequestId ?? "Request",
                        Entries = new List<CursorRequestEntry> { singleEntry }
                    };
                    var unified = UnifiedLogFactory.Create(syntheticLog);
                    if (unified != null) return (unified, unified.EntryCount);
                }
            }

            // 5) Older non-unified: entries array only (e.g. Cursor without "session")
            if (HasKeyCI(obj, "entries"))
            {
                var cursorLog = JsonSerializer.Deserialize<CursorRequestLog>(text, options);
                if (cursorLog?.Entries != null && cursorLog.Entries.Count > 0)
                {
                    var unified = UnifiedLogFactory.Create(cursorLog);
                    if (unified != null) return (unified, unified.EntryCount);
                }
            }

            return (null, -1);
        }
        catch
        {
            return (null, -1);
        }
    }

    private void FillActionsFromSingleEntryJson(string jsonContent, UnifiedSessionLog unifiedLog)
    {
        if (unifiedLog?.Entries == null || unifiedLog.Entries.Count == 0) return;
        try
        {
            var treeResult = _jsonParser.ParseToTree(jsonContent);
            if (treeResult.RootNode is JsonNode rootNode)
            {
                var rootElement = rootNode.Deserialize<JsonElement>();
                var actions = UnifiedLogFactory.ParseActionsFromEntryElement(rootElement);
                if (actions.Count > 0)
                    unifiedLog.Entries[0].Actions = new ObservableCollection<UnifiedAction>(actions);
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "[Actions] Failed to parse actions from JSON entry"); }
    }

    private void BuildCopilotSummaryAndIndex(CopilotSessionLog log, JsonLogSummary summary)
    {
        var requests = log.Requests ?? new List<CopilotRequestEntry>();
        summary.TotalCount = requests.Count;
        summary.SearchIndex = new List<SearchableEntry>();

        var byModel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < requests.Count; i++)
        {
            var r = requests[i];
            var model = r.Model ?? "(unknown)";
            byModel[model] = byModel.GetValueOrDefault(model) + 1;

            var display = (r.Slug ?? r.Title ?? r.UserRequest ?? "").Trim();
            if (display.Length > 60) display = display.Substring(0, 57) + "...";
            var searchText = string.Join(" ", r.RequestId ?? "", r.Slug ?? "", r.Title ?? "", r.UserRequest ?? "", r.Model ?? "", r.Timestamp?.ToString() ?? "");
            summary.SearchIndex.Add(new SearchableEntry
            {
                RequestId = r.RequestId ?? "",
                DisplayText = string.IsNullOrEmpty(display) ? $"Request {i + 1}" : display,
                Timestamp = r.Timestamp?.ToString("o") ?? "",
                Model = r.Model ?? "",
                EntryIndex = i,
                SourcePath = $"requests[{i}]",
                SearchText = searchText
            });
        }
        summary.StatsByModel = "By model: " + string.Join(", ", byModel.Select(kv => $"{kv.Key}: {kv.Value}"));
        if (log.Statistics != null)
        {
            var s = log.Statistics;
            summary.StatsBySuccess = $"Completed: {s.CompletedCount}, In progress: {s.InProgressCount}, Failed: {s.FailedCount}";
            if (s.AverageSuccessScore.HasValue) summary.StatsBySuccess += $", Avg score: {s.AverageSuccessScore:F1}";
            if (s.TotalNetTokens.HasValue) summary.StatsCostOrTokens = $"Total tokens: {s.TotalNetTokens:N0}";
            if (s.TotalNetPremiumRequests.HasValue) summary.StatsCostOrTokens += $", Premium requests: {s.TotalNetPremiumRequests:N0}";
        }
    }

    private void BuildCursorSummaryAndIndex(CursorRequestLog log, JsonLogSummary summary)
    {
        var entries = log.Entries ?? new List<CursorRequestEntry>();
        summary.TotalCount = entries.Count;
        summary.SearchIndex = new List<SearchableEntry>();

        var byModel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var successScores = new List<int>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var model = e.Model ?? "(unknown)";
            byModel[model] = byModel.GetValueOrDefault(model) + 1;
            if (e.Successfulness?.Score.HasValue == true) successScores.Add(e.Successfulness.Score!.Value);

            var display = (e.ExactRequest ?? e.ExactRequestNote ?? "").Trim();
            if (display.Length > 60) display = display.Substring(0, 57) + "...";
            var searchText = string.Join(" ", e.RequestId ?? "", e.ExactRequest ?? "", e.ExactRequestNote ?? "", e.Model ?? "", e.Timestamp ?? "",
                e.Successfulness?.Score?.ToString() ?? "", e.ActionsTaken ?? new List<string>());
            summary.SearchIndex.Add(new SearchableEntry
            {
                RequestId = e.RequestId ?? "",
                DisplayText = string.IsNullOrEmpty(display) ? $"Entry {i + 1}" : display,
                Timestamp = e.Timestamp ?? "",
                Model = e.Model ?? "",
                EntryIndex = i,
                SourcePath = $"entries[{i}]",
                SearchText = searchText
            });
        }
        summary.StatsByModel = "By model: " + string.Join(", ", byModel.Select(kv => $"{kv.Key}: {kv.Value}"));
        if (successScores.Count > 0)
        {
            summary.StatsBySuccess = $"Success scores: min {successScores.Min()}, max {successScores.Max()}, avg {successScores.Average():F1}";
        }
    }

    internal void BuildUnifiedSummaryAndIndex(UnifiedSessionLog log)
    {
         var summary = new JsonLogSummary();
         BuildUnifiedSummaryAndIndexInternal(log, summary);

         // Update Summary Header for Aggregated view
         summary.SummaryLines.Clear();
         summary.SummaryLines.Add($"Type: {log.SourceType}");
         summary.SummaryLines.Add($"Total Entries: {log.EntryCount}");
         summary.SummaryLines.Add($"Total Tokens: {log.TotalTokens:N0}");
         summary.SummaryLines.Add($"Aggregated at: {log.Started}");

         JsonLogSummary = summary;

         // Build Tree
         JsonTree.Clear();
         var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false };
         var unifiedNode = JsonSerializer.SerializeToNode(log, options);
         var root = new JsonTreeNode("Root", "Aggregated Unified Log", "Object");
         root.IsExpanded = true;
         BuildJsonTreeInternal(unifiedNode, root, null);
         JsonTree.Add(root);

         UpdateFilteredSearchEntriesInternal();
    }

    internal void BuildUnifiedSummaryAndIndexInternal(UnifiedSessionLog log, JsonLogSummary summary)
    {
        summary.SearchIndex = new List<SearchableEntry>();
        var entries = log.Entries;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var display = (e.QueryText ?? "").Trim(); // Updated field name
            if (display.Length > 60) display = display.Substring(0, 57) + "...";

            var searchText = string.Join(" ",
                e.RequestId ?? "",
                e.QueryText ?? "",  // Updated field name
                e.QueryTitle ?? "", // Updated field name
                e.Model ?? "",
                e.Agent ?? "",
                e.Timestamp?.ToString() ?? "",
                e.Status ?? "");

            summary.SearchIndex.Add(new SearchableEntry
            {
                RequestId = e.RequestId ?? "",
                DisplayText = string.IsNullOrEmpty(display) ? $"Entry {i + 1}" : display,
                Timestamp = e.Timestamp?.ToString("o") ?? "",
                Model = e.Model ?? "",
                Agent = e.Agent ?? "",
                EntryIndex = i,
                SourcePath = $"entries[{i}]", // Matches Unified Model structure
                SearchText = searchText,
                UnifiedEntry = e
            });
        }
        var sorted = summary.SearchIndex.OrderByDescending(e => e.SortableTimestamp ?? DateTime.MinValue).ToList();
        SearchableEntries = new ObservableCollection<SearchableEntry>(sorted);
        UpdateDistinctFilterValues();
    }

    private void UpdateDistinctFilterValues()
    {
        var entries = SearchableEntries ?? new ObservableCollection<SearchableEntry>();
        var requestIds = new List<string> { "" };
        requestIds.AddRange(entries.Select(e => e.RequestId ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        var displayTexts = new List<string> { "" };
        displayTexts.AddRange(entries.Select(e => e.DisplayText ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        var models = new List<string> { "" };
        models.AddRange(entries.Select(e => e.Model ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        var agents = new List<string> { "" };
        agents.AddRange(entries.Select(e => e.Agent ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        var timestamps = new List<string> { "" };
        timestamps.AddRange(entries.Select(e => e.TimestampDisplay ?? "").Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase));

        DistinctRequestIds = new ObservableCollection<string>(requestIds);
        DistinctDisplayTexts = new ObservableCollection<string>(displayTexts);
        DistinctModels = new ObservableCollection<string>(models);
        DistinctAgents = new ObservableCollection<string>(agents);
        DistinctTimestamps = new ObservableCollection<string>(timestamps);
    }

    internal void BuildJsonTreeInternal(JsonNode? node, JsonTreeNode parent, string? pathPrefix)
    {
        if (node == null)
        {
            parent.Children.Add(new JsonTreeNode("null", "null", "null"));
            return;
        }

        if (node is JsonObject jsonObj)
        {
            foreach (var property in jsonObj)
            {
                string type = property.Value?.GetType().Name ?? "null";
                if (property.Value is JsonArray) type = "Array";
                else if (property.Value is JsonObject) type = "Object";
                else if (property.Value is JsonValue) type = "Value";

                var childKey = property.Key;
                var childPath = string.IsNullOrEmpty(pathPrefix) ? childKey : pathPrefix + "." + childKey;
                var child = new JsonTreeNode(childKey, "", type);
                if (parent.Name == "Root") child.IsExpanded = true;

                BuildJsonTreeInternal(property.Value, child, childPath);

                if (property.Value is JsonValue val)
                {
                    child.Value = val.ToString();
                }

                parent.Children.Add(child);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            IEnumerable<JsonNode?> items = jsonArray;

            // Sort requests/entries by timestamp descending if applicable
            if (parent.Name.Equals("requests", StringComparison.OrdinalIgnoreCase) ||
                parent.Name.Equals("entries", StringComparison.OrdinalIgnoreCase))
            {
                items = jsonArray.OrderByDescending(n => {
                     if (n is JsonObject obj)
                     {
                         if (obj.TryGetPropertyValue("timestamp", out var tsNode) && tsNode is JsonValue tsVal)
                         {
                             if (tsVal.TryGetValue(out DateTime dt)) return dt;
                             if (tsVal.TryGetValue(out string? s) && DateTime.TryParse(s, out var dt2)) return dt2;

                             // Try numeric timestamp (Unix seconds or milliseconds)
                             if (tsVal.TryGetValue(out long tsLong))
                             {
                                 // Assume milliseconds if large, seconds if small
                                 // 2020-01-01 is ~1.5 billion seconds or 1.5 trillion ms
                                 if (tsLong > 1000000000000) return DateTimeOffset.FromUnixTimeMilliseconds(tsLong).UtcDateTime;
                                 return DateTimeOffset.FromUnixTimeSeconds(tsLong).UtcDateTime;
                             }
                             // Try parsing string as long
                             if (tsVal.TryGetValue(out string? sLong) && long.TryParse(sLong, out var tsLong2))
                             {
                                 if (tsLong2 > 1000000000000) return DateTimeOffset.FromUnixTimeMilliseconds(tsLong2).UtcDateTime;
                                 return DateTimeOffset.FromUnixTimeSeconds(tsLong2).UtcDateTime;
                             }
                         }
                     }
                     return DateTime.MinValue;
                }).ToList();
            }

            int index = 0;
            foreach (var item in items)
            {
                var itemPath = pathPrefix + "[" + index + "]";
                var child = (pathPrefix == "requests" || pathPrefix == "entries")
                    ? new JsonTreeNode($"[{index}]", "", "Item", itemPath)
                    : new JsonTreeNode($"[{index}]", "", "Item");

                string timestampPrefix = "";
                if (item is JsonObject objTS && objTS.TryGetPropertyValue("timestamp", out var tsNode))
                {
                    // Format timestamp nicely if possible
                    string tsStr = tsNode?.ToString() ?? "";
                    if (tsNode is JsonValue tsVal)
                    {
                         if (tsVal.TryGetValue(out DateTime dt))
                         {
                             if (dt.Kind == DateTimeKind.Utc) dt = dt.ToLocalTime();
                             else if (dt.Kind == DateTimeKind.Unspecified) dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
                             tsStr = dt.ToString("MM/dd HH:mm");
                         }
                         else if (tsVal.TryGetValue(out long l))
                         {
                             // Try to format unix time
                             try {
                                 if (l > 1000000000000) tsStr = DateTimeOffset.FromUnixTimeMilliseconds(l).LocalDateTime.ToString("MM/dd HH:mm");
                                 else tsStr = DateTimeOffset.FromUnixTimeSeconds(l).LocalDateTime.ToString("MM/dd HH:mm");
                             } catch (Exception ex) { _logger.LogDebug(ex, "[Timestamp] Failed to parse unix time {Value}", l); }
                         }
                    }
                    timestampPrefix = $"[{tsStr}] ";
                }

                if (item is JsonObject objItem)
                {
                    foreach (var prop in objItem)
                    {
                        if (prop.Value is JsonValue propVal && propVal.TryGetValue(out string? text) && !string.IsNullOrEmpty(text))
                        {
                            if (text.Length > 50) text = text.Substring(0, 47) + "...";
                            child.Value = timestampPrefix + text;
                            break;
                        }
                    }
                }

                // If no text found but we have timestamp, show it
                if (string.IsNullOrEmpty(child.Value) && !string.IsNullOrEmpty(timestampPrefix))
                {
                    child.Value = timestampPrefix.Trim();
                }

                BuildJsonTreeInternal(item, child, itemPath);
                if (item is JsonValue val)
                {
                    child.Value = val.ToString();
                }
                parent.Children.Add(child);
                index++;
            }
        }
    }

    /// <summary>DTO for building the file tree on a background thread (no ObservableCollection).</summary>
    private sealed class TreeDto
    {
        public string Path { get; set; } = "";
        public bool IsDirectory { get; set; }
        public List<TreeDto> Children { get; } = new();
    }

    /// <summary>Builds the file tree on a background thread; returns allJson node, root DTO, Documents DTO, and Source DTO (code files from ../../src).</summary>
    private (FileNode allJsonNode, TreeDto? rootDto, TreeDto? documentsDto, TreeDto? sourceDto) BuildTreeOffThread(string resolvedPath)
    {
        var allJsonNode = new FileNode("ALL_JSON_VIRTUAL_NODE", false) { Name = "All JSON" };
        TreeDto? rootDto = null;
        if (_fs.DirectoryExists(resolvedPath))
        {
            rootDto = new TreeDto { Path = resolvedPath, IsDirectory = true };
            LoadChildrenDto(rootDto);
        }
        TreeDto? documentsDto = BuildDocumentsDto(resolvedPath);
        TreeDto? sourceDto = BuildSourceDto(resolvedPath);
        return (allJsonNode, rootDto, documentsDto, sourceDto);
    }

    /// <summary>Builds a Source tree DTO: ../../src with code files only, recursing into subdirs but ignoring session log folders.</summary>
    private TreeDto? BuildSourceDto(string resolvedPath)
    {
        string? sourcePath = GetSourcePath(resolvedPath);
        if (string.IsNullOrEmpty(sourcePath))
            return null;
        var sourceDto = new TreeDto { Path = sourcePath, IsDirectory = true };
        try
        {
            LoadSourceChildrenDto(sourceDto, resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building Source tree");
        }
        return sourceDto;
    }

    /// <summary>Recursively adds code files and subdirs to the node, skipping session log folder(s).</summary>
    private void LoadSourceChildrenDto(TreeDto node, string sessionsPathToIgnore)
    {
        foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
        {
            if (SourceDirectoriesToSkip.Contains(dir.Name))
                continue;
            var childFull = dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(childFull, sessionsPathToIgnore, StringComparison.OrdinalIgnoreCase))
                continue;
            var child = new TreeDto { Path = dir.FullName, IsDirectory = true };
            LoadSourceChildrenDto(child, sessionsPathToIgnore);
            if (child.Children.Count > 0)
                node.Children.Add(child);
        }
        foreach (var entry in _fs.EnumerateFiles(node.Path, "*", false))
        {
            if (IsCodeFile(entry.Extension))
                node.Children.Add(new TreeDto { Path = entry.FullName, IsDirectory = false });
        }
    }

    /// <summary>Builds a Documents tree DTO: parent of resolvedPath with .md files, recursing into subfolders (ignoring session log subfolders).</summary>
    private TreeDto? BuildDocumentsDto(string resolvedPath)
    {
        string? parentPath = _fs.GetDirectoryName(resolvedPath);
        if (string.IsNullOrEmpty(parentPath) || !_fs.DirectoryExists(parentPath))
            return null;
        var documentsDto = new TreeDto { Path = parentPath, IsDirectory = true };
        try
        {
            string sessionsPathToIgnore = resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            LoadDocumentsChildrenDto(documentsDto, sessionsPathToIgnore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building Documents tree");
        }
        return documentsDto;
    }

    private void LoadDocumentsChildrenDto(TreeDto node, string sessionsPathToIgnore)
    {
        foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
        {
            var childFull = dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(childFull, sessionsPathToIgnore, StringComparison.OrdinalIgnoreCase))
                continue;
            var child = new TreeDto { Path = dir.FullName, IsDirectory = true };
            LoadDocumentsChildrenDto(child, sessionsPathToIgnore);
            if (child.Children.Count > 0)
                node.Children.Add(child);
        }
        foreach (var entry in _fs.EnumerateFiles(node.Path, "*.md", false))
        {
            if (IsArchivedName(entry.Name))
                continue;
            node.Children.Add(new TreeDto { Path = entry.FullName, IsDirectory = false });
        }
    }

    private static bool IsArchivedName(string name) =>
        name.StartsWith("archived-", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("archive-", StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> CodeFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".py", ".rs", ".go", ".java", ".kt", ".kts", ".vb", ".fs", ".fsx",
        ".cpp", ".c", ".h", ".hpp", ".m", ".mm", ".swift", ".r", ".rb", ".php", ".scala", ".lua", ".sql", ".sh", ".ps1",
        ".csproj", ".vbproj", ".fsproj", ".sln"
    };

    private static readonly HashSet<string> SourceDirectoriesToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "obj", "bin"
    };

    private static bool IsCodeFile(string extension) =>
        !string.IsNullOrEmpty(extension) && CodeFileExtensions.Contains(extension);

    /// <summary>Resolves ../../src relative to the sessions path.</summary>
    private string? GetSourcePath(string resolvedPath)
    {
        try
        {
            var full = _fs.GetFullPath(_fs.CombinePath(resolvedPath, "..", "..", "src"));
            return _fs.DirectoryExists(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }

    private void LoadChildrenDto(TreeDto node)
    {
        try
        {
            foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
            {
                var child = new TreeDto { Path = dir.FullName, IsDirectory = true };
                LoadChildrenDto(child);
                node.Children.Add(child);
            }
            foreach (var entry in _fs.EnumerateFiles(node.Path, "*", false))
            {
                if (IsArchivedName(entry.Name))
                    continue;
                if (entry.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                    entry.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                    node.Children.Add(new TreeDto { Path = entry.FullName, IsDirectory = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error accessing {node.Path}");
        }
    }

    /// <summary>Converts DTO tree to FileNode tree on the UI thread (ObservableCollection safe).</summary>
    private static FileNode ApplyTreeDtoToNodes(TreeDto dto)
    {
        var node = new FileNode(dto.Path, dto.IsDirectory);
        foreach (var childDto in dto.Children)
            node.Children.Add(ApplyTreeDtoToNodes(childDto));
        return node;
    }

    private void InitializeTree()
    {
        Nodes.Clear();
        string? resolvedPath = null;
        try { resolvedPath = GetResolvedTargetPath(); } catch (Exception ex) { _logger.LogDebug(ex, "[Path] GetResolvedTargetPath failed"); }
        var allJsonNode = new FileNode("ALL_JSON_VIRTUAL_NODE", false) { Name = "All JSON" };
        Nodes.Add(allJsonNode);
        if (resolvedPath != null)
        {
            var documentsNode = BuildDocumentsNode(resolvedPath);
            if (documentsNode != null)
            {
                documentsNode.Name = "Documents";
                Nodes.Add(documentsNode);
            }
            var sourceNode = BuildSourceNode(resolvedPath);
            if (sourceNode != null)
            {
                sourceNode.Name = "Source";
                Nodes.Add(sourceNode);
            }
            if (!_fs.DirectoryExists(resolvedPath))
            {
                SetStatus($"Directory not found: {resolvedPath}");
                Nodes.Add(new FileNode(resolvedPath, true) { Name = "Directory not found" });
                SelectedNode = allJsonNode;
                return;
            }
            SetStatus($"Loaded: {resolvedPath}");
            var root = new FileNode(resolvedPath, true);
            LoadChildren(root);
            Nodes.Add(root);
        }
        SelectedNode = allJsonNode;
    }

    /// <summary>Builds the Documents FileNode (md files from parent of resolvedPath, recursing into subfolders). Call on UI thread.</summary>
    private FileNode? BuildDocumentsNode(string resolvedPath)
    {
        string? parentPath = _fs.GetDirectoryName(resolvedPath);
        if (string.IsNullOrEmpty(parentPath) || !_fs.DirectoryExists(parentPath))
            return null;
        var documentsNode = new FileNode(parentPath, true);
        try
        {
            string sessionsPathToIgnore = resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            LoadDocumentsChildren(documentsNode, sessionsPathToIgnore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building Documents node");
        }
        return documentsNode;
    }

    private void LoadDocumentsChildren(FileNode node, string sessionsPathToIgnore)
    {
        foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
        {
            var childFull = dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(childFull, sessionsPathToIgnore, StringComparison.OrdinalIgnoreCase))
                continue;
            var childNode = new FileNode(dir.FullName, true);
            LoadDocumentsChildren(childNode, sessionsPathToIgnore);
            if (childNode.Children.Count > 0)
                node.Children.Add(childNode);
        }
        foreach (var entry in _fs.EnumerateFiles(node.Path, "*.md", false))
        {
            if (IsArchivedName(entry.Name))
                continue;
            node.Children.Add(new FileNode(entry.FullName, false));
        }
    }

    /// <summary>Builds the Source FileNode (code files from ../../src, recursing; skipping session log folders). Call on UI thread.</summary>
    private FileNode? BuildSourceNode(string resolvedPath)
    {
        string? sourcePath = GetSourcePath(resolvedPath);
        if (string.IsNullOrEmpty(sourcePath))
            return null;
        var sourceNode = new FileNode(sourcePath, true);
        try
        {
            LoadSourceChildren(sourceNode, resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building Source node");
        }
        return sourceNode;
    }

    private void LoadSourceChildren(FileNode node, string sessionsPathToIgnore)
    {
        foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
        {
            if (SourceDirectoriesToSkip.Contains(dir.Name))
                continue;
            var childFull = dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(childFull, sessionsPathToIgnore, StringComparison.OrdinalIgnoreCase))
                continue;
            var childNode = new FileNode(dir.FullName, true);
            LoadSourceChildren(childNode, sessionsPathToIgnore);
            if (childNode.Children.Count > 0)
                node.Children.Add(childNode);
        }
        foreach (var entry in _fs.EnumerateFiles(node.Path, "*", false))
        {
            if (IsCodeFile(entry.Extension))
                node.Children.Add(new FileNode(entry.FullName, false));
        }
    }

    private void LoadChildren(FileNode node)
    {
        try
        {
            foreach (var dir in _fs.EnumerateDirectories(node.Path, "*", false))
            {
                var childNode = new FileNode(dir.FullName, true);
                LoadChildren(childNode);
                node.Children.Add(childNode);
            }

            foreach (var entry in _fs.EnumerateFiles(node.Path, "*", false))
            {
                if (IsArchivedName(entry.Name))
                    continue;
                if (entry.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                    entry.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    node.Children.Add(new FileNode(entry.FullName, false));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error accessing {node.Path}");
        }
    }

    private void SetupWatcher()
    {
        // Session logs now come from the MCP server; no local session-file watcher is needed.
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }

    private void OnTreeChanged(object sender, FileSystemEventArgs e)
    {
        string? resolvedPath = null;
        try { resolvedPath = GetResolvedTargetPath(); } catch (Exception ex) { _logger.LogDebug(ex, "[Path] GetResolvedTargetPath failed"); }
        if (resolvedPath == null) return;
        Task.Run(() =>
        {
            try
            {
                var (allJsonNode, rootDto, documentsDto, sourceDto) = BuildTreeOffThread(resolvedPath);
                DispatchToUi(() =>
                {
                    try
                    {
                        var previousPath = SelectedNode?.Path;
                        _isRestoringTreeSelection = true;
                        try
                        {
                            Nodes.Clear();
                            Nodes.Add(allJsonNode);
                            if (documentsDto != null)
                            {
                                var documentsNode = ApplyTreeDtoToNodes(documentsDto);
                                documentsNode.Name = "Documents";
                                Nodes.Add(documentsNode);
                            }
                            if (sourceDto != null)
                            {
                                var sourceNode = ApplyTreeDtoToNodes(sourceDto);
                                sourceNode.Name = "Source";
                                Nodes.Add(sourceNode);
                            }
                            if (rootDto != null)
                            {
                                var root = ApplyTreeDtoToNodes(rootDto);
                                Nodes.Add(root);
                            }
                            else
                            {
                                Nodes.Add(new FileNode(resolvedPath, true) { Name = "Directory not found" });
                            }
                            RestoreTreeSelection(previousPath, allJsonNode);
                        }
                        finally
                        {
                            _isRestoringTreeSelection = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OnTreeChanged apply failed");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnTreeChanged build failed");
            }
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            // If the changed file is the current one, regenerate
            if (_currentMarkdownPath != null && e.FullPath.Equals(_currentMarkdownPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Detected change in current file: {e.FullPath}");

                // Log the change
                ChangeLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Rebuilt: {Path.GetFileName(e.FullPath)}");

                // Invalidate cache by forcing generation (the date check in GenerateAndNavigate will handle it since file date is newer)
                // But we need to call GenerateAndNavigate with a node.
                var node = new FileNode(e.FullPath, false);
                GenerateAndNavigateInternal(node);
            }
        });
    }

    // ── ICommandTarget explicit implementation ──────────────────────────
    // Bridges command handlers to the ViewModel's existing Internal methods.
    // Each method is implemented against the granular sub-interface that declares it.

    void INavigationTarget.NavigateBack() => NavigateBackInternal();
    void INavigationTarget.NavigateForward() => NavigateForwardInternal();
    Task INavigationTarget.RefreshAsync() => RefreshInternalAsync();
    void INavigationTarget.PhoneNavigateSection(string? sectionKey) => PhoneNavigateSectionInternal(sectionKey);
    void INavigationTarget.GenerateAndNavigate(FileNode? node) => GenerateAndNavigateInternal(node);
    void INavigationTarget.OpenTreeItem(FileNode? node) => OpenTreeItemInternal(node);
    void INavigationTarget.TreeItemTapped(FileNode? node) => TreeItemTappedInternal(node);
    void INavigationTarget.JsonNodeDoubleTapped(JsonTreeNode? node) => JsonNodeDoubleTappedInternal(node);
    void IRequestDetailsTarget.ShowRequestDetails(SearchableEntry entry) => ShowRequestDetailsInternal(entry);
    void IRequestDetailsTarget.CloseRequestDetails() => CloseRequestDetailsInternal();
    void IRequestDetailsTarget.NavigateToPreviousRequest() => NavigateToPreviousRequestInternal();
    void IRequestDetailsTarget.NavigateToNextRequest() => NavigateToNextRequestInternal();
    void IRequestDetailsTarget.SelectSearchEntry(SearchableEntry entry) => SelectSearchEntryInternal(entry);
    void IRequestDetailsTarget.SearchRowTapped(SearchableEntry? entry) => SearchRowTappedInternal(entry);
    void IRequestDetailsTarget.SearchRowDoubleTapped(SearchableEntry? entry) => SearchRowDoubleTappedInternal(entry);
    void IPreviewTarget.OpenPreviewInBrowser() => OpenPreviewInBrowserInternal();
    void IPreviewTarget.ToggleShowRawMarkdown() => ToggleShowRawMarkdownInternal();
    void IArchiveTarget.Archive() => ArchiveInternal();
    void IArchiveTarget.ArchiveTreeItem(FileNode? node) => ArchiveTreeItemInternal(node);
    Task IClipboardTarget.CopyText(string text) => CopyTextInternal(text);
    Task IClipboardTarget.CopyOriginalJson(UnifiedRequestEntry? entry) => CopyOriginalJsonInternal(entry);
    void IConfigTarget.OpenAgentConfig() => OpenAgentConfigInternal();
    void IConfigTarget.OpenPromptTemplates() => OpenPromptTemplatesInternal();
    Task ISessionDataTarget.ReloadFromMcpAsync() => ReloadFromMcpAsyncInternal();
    void ISessionDataTarget.BuildUnifiedSummaryAndIndex(UnifiedSessionLog session, JsonLogSummary summary) => BuildUnifiedSummaryAndIndexInternal(session, summary);
    void ISessionDataTarget.BuildJsonTree(JsonNode? node, JsonTreeNode root, string? pathPrefix) => BuildJsonTreeInternal(node, root, pathPrefix);
    void ISessionDataTarget.LoadJson(string filePath) => LoadJsonInternal(filePath);
    void ISessionDataTarget.LoadMarkdownFile(FileNode node) => LoadMarkdownFileInternal(node);
    void ISessionDataTarget.LoadSourceFile(FileNode node) => LoadSourceFileInternal(node);
    void ISessionDataTarget.UpdateFilteredSearchEntries() => UpdateFilteredSearchEntriesInternal();
    private readonly object _busyLock = new();
    private readonly List<Task> _outstandingTasks = new();

    void IUiDispatchTarget.TrackBackgroundWork(Task task)
    {
        if (task == null || task.IsCompleted) return;
        bool wasBusy;
        lock (_busyLock)
        {
            wasBusy = _outstandingTasks.Count > 0;
            _outstandingTasks.Add(task);
        }
        if (!wasBusy) DispatchToUi(() => IsBusy = true);

        task.ContinueWith(t =>
        {
            bool nowIdle;
            lock (_busyLock)
            {
                _outstandingTasks.Remove(task);
                nowIdle = _outstandingTasks.Count == 0;
            }
            if (nowIdle) DispatchToUi(() => IsBusy = false);
        }, TaskScheduler.Default);
    }
    void IUiDispatchTarget.DispatchToUi(Action action) => DispatchToUi(action);

    string IUiDispatchTarget.StatusMessage
    {
        get => StatusMessage;
        set => StatusMessage = value;
    }

    McpSessionLogService ISessionDataTarget.McpSessionService => McpSessionService;

    JsonLogSummary ISessionDataTarget.JsonLogSummary
    {
        get => JsonLogSummary;
        set => JsonLogSummary = value;
    }

    ObservableCollection<JsonTreeNode> ISessionDataTarget.JsonTree => JsonTree;

    string ISessionDataTarget.AgentFilter
    {
        get => AgentFilter;
        set => AgentFilter = value;
    }

    Dictionary<string, UnifiedSessionLog> ISessionDataTarget.BuildSessionsByPathDict(IReadOnlyList<UnifiedSessionLog> sessions) => BuildSessionsByPathDict(sessions);
    List<UnifiedSessionLog> ISessionDataTarget.OrderAndDeduplicateSessions(Dictionary<string, UnifiedSessionLog> byPath) => OrderAndDeduplicateSessions(byPath);
    void ISessionDataTarget.SetMcpSessionState(List<UnifiedSessionLog> sessions, Dictionary<string, UnifiedSessionLog> byPath) => SetMcpSessionState(sessions, byPath);

    // --- ITodoCopilotTarget ---

    public virtual Task CopilotStatusAsync() => Task.CompletedTask;
    public virtual Task CopilotPlanAsync() => Task.CompletedTask;
    public virtual Task CopilotImplementAsync() => Task.CompletedTask;
}

public sealed class WorkspaceConnectionOption
{
    public string Key { get; init; } = "";
    public string? WorkspaceKey { get; init; }
    public string? WorkspaceRootPath { get; init; }
    public string? ApiKey { get; init; }
    public string DisplayName { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    public bool IsPrimary { get; init; }
    public bool IsEnabled { get; init; } = true;

    public static WorkspaceConnectionOption CreateDefault(Uri defaultBaseUri, string? primaryWorkspaceRootPath, string? apiKey = null)
    {
        var normalizedBaseUrl = defaultBaseUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        var resolvedApiKey = string.IsNullOrWhiteSpace(apiKey)
            ? McpServerRestClientFactory.TryResolveApiKeyForWorkspaceRoot(primaryWorkspaceRootPath, normalizedBaseUrl)
            : apiKey?.Trim();

        return new WorkspaceConnectionOption
        {
            Key = "__DEFAULT__",
            WorkspaceKey = null,
            WorkspaceRootPath = string.IsNullOrWhiteSpace(primaryWorkspaceRootPath) ? null : primaryWorkspaceRootPath.Trim(),
            ApiKey = string.IsNullOrWhiteSpace(resolvedApiKey) ? null : resolvedApiKey,
            DisplayName = $"Default ({defaultBaseUri.Host}:{defaultBaseUri.Port})",
            BaseUrl = normalizedBaseUrl,
            IsPrimary = true,
            IsEnabled = true
        };
    }

    public static WorkspaceConnectionOption FromWorkspace(Uri defaultBaseUri, McpWorkspaceItem workspace)
    {
        var isPrimary = workspace.IsPrimary ?? false;

        var key = string.IsNullOrWhiteSpace(workspace.WorkspacePath)
            ? (workspace.Name ?? "unknown")
            : workspace.WorkspacePath.Trim();

        var label = string.IsNullOrWhiteSpace(workspace.Name)
            ? key
            : workspace.Name.Trim();
        var isEnabled = workspace.IsEnabled ?? true;
        var flags = new List<string>();
        if (isPrimary)
            flags.Add("Primary");
        if (!isEnabled)
            flags.Add("Disabled");
        var flagsSuffix = flags.Count == 0 ? "" : $" [{string.Join(", ", flags)}]";
        var baseUrl = defaultBaseUri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        var workspaceRootPath = string.IsNullOrWhiteSpace(workspace.WorkspacePath) ? null : workspace.WorkspacePath.Trim();
        var apiKey = McpServerRestClientFactory.TryResolveApiKeyForWorkspaceRoot(workspaceRootPath, baseUrl);

        return new WorkspaceConnectionOption
        {
            Key = key,
            WorkspaceKey = key,
            WorkspaceRootPath = workspaceRootPath,
            ApiKey = apiKey,
            DisplayName = $"{label}{flagsSuffix}",
            BaseUrl = baseUrl,
            IsPrimary = isPrimary,
            IsEnabled = isEnabled
        };
    }
}

