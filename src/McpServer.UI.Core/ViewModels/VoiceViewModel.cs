using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for voice-session lifecycle and turn workflows.
/// </summary>
[ViewModelCommand("voice-session", Description = "Create/use/delete voice sessions and turns")]
public sealed partial class VoiceViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<VoiceViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public VoiceViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<VoiceViewModel> logger)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
        StatusMessage = "Idle.";
    }

    /// <summary>Logical area represented by this ViewModel.</summary>
    public McpArea Area => McpArea.Voice;

    /// <summary>Current session identifier.</summary>
    [ObservableProperty]
    private string? _sessionId;

    /// <summary>Preferred transcript language (for example "en").</summary>
    [ObservableProperty]
    private string? _language = "en";

    /// <summary>Optional client device identifier.</summary>
    [ObservableProperty]
    private string? _deviceId;

    /// <summary>Optional client display name.</summary>
    [ObservableProperty]
    private string? _clientName;

    /// <summary>Optional agent routing name override.</summary>
    [ObservableProperty]
    private string? _agentName;

    /// <summary>Optional agent model override.</summary>
    [ObservableProperty]
    private string? _agentModel;

    /// <summary>User transcript text for the next turn submission.</summary>
    [ObservableProperty]
    private string? _userTranscriptText;

    /// <summary>Whether an operation is currently running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Latest operation error message.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Latest operation status text.</summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Latest created session details.</summary>
    [ObservableProperty]
    private VoiceSessionInfo? _lastSession;

    /// <summary>Latest submitted turn details.</summary>
    [ObservableProperty]
    private VoiceTurnInfo? _lastTurn;

    /// <summary>Latest fetched session status snapshot.</summary>
    [ObservableProperty]
    private VoiceSessionStatusInfo? _lastStatus;

    /// <summary>Latest interrupt outcome.</summary>
    [ObservableProperty]
    private VoiceInterruptInfo? _lastInterrupt;

    /// <summary>Most recently retrieved transcript entries.</summary>
    public ObservableCollection<VoiceTranscriptEntryInfo> TranscriptItems { get; } = [];

    /// <summary>Most recently retrieved tool calls from the last turn.</summary>
    public ObservableCollection<VoiceToolCallInfo> LastTurnToolCalls { get; } = [];

    /// <summary>
    /// Creates a voice session using current ViewModel inputs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created session on success; otherwise null.</returns>
    public async Task<VoiceSessionInfo?> CreateSessionAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Creating voice session...";

        try
        {
            var result = await _dispatcher.SendAsync(
                new CreateVoiceSessionCommand
                {
                    Language = Normalize(Language),
                    DeviceId = Normalize(DeviceId),
                    ClientName = Normalize(ClientName),
                    WorkspacePath = _workspaceContext.ActiveWorkspacePath,
                    AgentName = Normalize(AgentName),
                    AgentModel = Normalize(AgentModel)
                },
                ct);

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Create voice session failed.";
                StatusMessage = "Create voice session failed.";
                return null;
            }

            LastSession = result.Value;
            SessionId = result.Value.SessionId;
            LastStatus = null;
            LastTurn = null;
            LastInterrupt = null;
            TranscriptItems.Clear();
            LastTurnToolCalls.Clear();
            StatusMessage = $"Voice session '{result.Value.SessionId}' created.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Create voice session failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Submits one user turn to the active voice session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The turn result on success; otherwise null.</returns>
    public async Task<VoiceTurnInfo?> SubmitTurnAsync(CancellationToken ct = default)
    {
        var currentSessionId = Normalize(SessionId);
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            StatusMessage = "Set a session id first.";
            return null;
        }

        var text = Normalize(UserTranscriptText);
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusMessage = "Enter transcript text first.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Submitting turn to '{currentSessionId}'...";

        try
        {
            var result = await _dispatcher.SendAsync(
                new SubmitVoiceTurnCommand
                {
                    SessionId = currentSessionId,
                    UserTranscriptText = text
                },
                ct);

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Submit turn failed.";
                StatusMessage = "Submit turn failed.";
                return null;
            }

            LastTurn = result.Value;
            UserTranscriptText = string.Empty;
            ReplaceCollection(LastTurnToolCalls, result.Value.ToolCalls);
            StatusMessage = $"Turn '{result.Value.TurnId}' submitted.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Submit turn failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Interrupts the active voice session turn.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The interrupt result on success; otherwise null.</returns>
    public async Task<VoiceInterruptInfo?> InterruptAsync(CancellationToken ct = default)
    {
        var currentSessionId = Normalize(SessionId);
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            StatusMessage = "Set a session id first.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Interrupting '{currentSessionId}'...";
        try
        {
            var result = await _dispatcher.SendAsync(new InterruptVoiceCommand(currentSessionId), ct);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Interrupt failed.";
                StatusMessage = "Interrupt failed.";
                return null;
            }

            LastInterrupt = result.Value;
            StatusMessage = result.Value.Interrupted
                ? $"Session '{currentSessionId}' interrupted."
                : $"Session '{currentSessionId}' was not active.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Interrupt failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads current status for the active voice session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Status snapshot on success; otherwise null.</returns>
    public async Task<VoiceSessionStatusInfo?> LoadStatusAsync(CancellationToken ct = default)
    {
        var currentSessionId = Normalize(SessionId);
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            StatusMessage = "Set a session id first.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading status for '{currentSessionId}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetVoiceStatusQuery(currentSessionId), ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Status load failed.";
                StatusMessage = "Status load failed.";
                return null;
            }

            LastStatus = result.Value;
            if (result.Value is null)
            {
                StatusMessage = $"Session '{currentSessionId}' not found.";
                return null;
            }

            StatusMessage = $"Session '{currentSessionId}' status: {result.Value.Status}.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Status load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads transcript entries for the active voice session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transcript payload on success; otherwise null.</returns>
    public async Task<VoiceTranscriptInfo?> LoadTranscriptAsync(CancellationToken ct = default)
    {
        var currentSessionId = Normalize(SessionId);
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            StatusMessage = "Set a session id first.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading transcript for '{currentSessionId}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetVoiceTranscriptQuery(currentSessionId), ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Transcript load failed.";
                StatusMessage = "Transcript load failed.";
                return null;
            }

            if (result.Value is null)
            {
                TranscriptItems.Clear();
                StatusMessage = $"Session '{currentSessionId}' not found.";
                return null;
            }

            ReplaceCollection(TranscriptItems, result.Value.Items);
            StatusMessage = $"Loaded {TranscriptItems.Count} transcript entries.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Transcript load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Deletes the active voice session and clears retained state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True when deletion succeeds.</returns>
    public async Task<bool> DeleteSessionAsync(CancellationToken ct = default)
    {
        var currentSessionId = Normalize(SessionId);
        if (string.IsNullOrWhiteSpace(currentSessionId))
        {
            StatusMessage = "Set a session id first.";
            return false;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Deleting session '{currentSessionId}'...";
        try
        {
            var result = await _dispatcher.SendAsync(new DeleteVoiceSessionCommand(currentSessionId), ct);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Delete session failed.";
                StatusMessage = "Delete session failed.";
                return false;
            }

            if (!result.Value)
            {
                StatusMessage = $"Session '{currentSessionId}' was not deleted.";
                return false;
            }

            ClearSessionState();
            StatusMessage = $"Session '{currentSessionId}' deleted.";
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Delete session failed.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears all retained voice session state.
    /// </summary>
    public void ClearSessionState()
    {
        SessionId = null;
        LastSession = null;
        LastTurn = null;
        LastStatus = null;
        LastInterrupt = null;
        UserTranscriptText = null;
        TranscriptItems.Clear();
        LastTurnToolCalls.Clear();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
