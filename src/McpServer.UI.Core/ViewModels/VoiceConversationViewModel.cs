using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// UI-state ViewModel for the Android voice conversation tab (manual transcript + MCP voice endpoint integration).
/// </summary>
public partial class VoiceConversationViewModel : ViewModelBase
{
    private readonly ILogger<VoiceConversationViewModel> _logger;
    private static readonly Regex AnsiEscapePattern = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    private readonly IVoiceConversationService _voiceService;
    private CancellationTokenSource? _activeTurnCts;

    [ObservableProperty] private string _sessionId = string.Empty;
    [ObservableProperty] private string _language = "en-US";
    [ObservableProperty] private string _statusText = "Voice ready";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _transcriptInput = string.Empty;
    [ObservableProperty] private string _assistantDisplayText = string.Empty;
    [ObservableProperty] private string _assistantSpeakText = string.Empty;
    [ObservableProperty] private string _lastTurnId = string.Empty;
    [ObservableProperty] private int _lastLatencyMs;
    [ObservableProperty] private bool _isSessionActive;

    /// <summary>When set, workspace path is resolved from the source of truth at read time.</summary>
    public Func<string?>? ResolveWorkspacePath { get; set; }

    /// <summary>The active workspace root path (e.g. "E:\github\FunWasHad"). Reads from <see cref="ResolveWorkspacePath"/> when set.</summary>
    public string WorkspacePath
    {
        get => ResolveWorkspacePath?.Invoke() ?? _workspacePath;
        set
        {
            _workspacePath = value ?? string.Empty;
            // Only push to service if the service doesn't have its own resolver
            if (_voiceService.ResolveWorkspacePath == null)
                _voiceService.WorkspacePath = _workspacePath;
        }
    }
    private string _workspacePath = string.Empty;

    /// <summary>Transcript items for the active session.</summary>
    public ObservableCollection<McpVoiceTranscriptEntry> TranscriptItems { get; } = [];

    /// <summary>Tool-call records from the most recent turn.</summary>
    public ObservableCollection<McpVoiceToolCallRecord> LastToolCalls { get; } = [];

    /// <summary>Raised when a status message should be displayed in the global status bar.</summary>
    public event Action<string>? GlobalStatusChanged;

    /// <summary>
    /// Creates a voice conversation ViewModel with a pre-configured voice service.
    /// </summary>
    public VoiceConversationViewModel(
        IVoiceConversationService service,
        ILogger<VoiceConversationViewModel>? logger = null)
    {
        _voiceService = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? NullLogger<VoiceConversationViewModel>.Instance;
    }

    /// <summary>
    /// Refreshes for a connection change by clearing the prior remote session state.
    /// </summary>
    public Task RefreshForConnectionChangeAsync()
    {
        _activeTurnCts?.Cancel();
        ClearSessionState();
        StatusText = "Voice session reset after connection change.";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates or resumes a voice session.
    /// </summary>
    protected async Task CreateSessionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var deviceId = Environment.MachineName;

            // Try to reconnect to an existing session for this device
            GlobalStatusChanged?.Invoke("Looking for existing voice session...");
            StatusText = "Looking for existing voice session...";
            try
            {
                var existing = await _voiceService.FindExistingSessionAsync(deviceId).ConfigureAwait(true);
                if (existing is not null)
                {
                    SessionId = existing.SessionId;
                    Language = string.IsNullOrWhiteSpace(existing.Language) ? Language : existing.Language;
                    IsSessionActive = true;
                    LastTurnId = existing.LastTurnId ?? string.Empty;
                    StatusText = $"Resumed session {SessionId} (turn {existing.TurnCounter})";
                    GlobalStatusChanged?.Invoke(StatusText);
                    _logger.LogInformation("Resumed existing voice session {SessionId} (turns={TurnCounter}, transcripts={TranscriptCount})",
                        SessionId, existing.TurnCounter, existing.TranscriptCount);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device session lookup failed, falling back to create");
            }

            // No existing session — create a new one
            GlobalStatusChanged?.Invoke("Creating voice session...");
            StatusText = "Creating voice session...";
            var response = await _voiceService.CreateSessionAsync(new McpVoiceSessionCreateRequest
            {
                Language = Language,
                ClientName = "RequestTracker.Android",
                DeviceId = deviceId
            }).ConfigureAwait(true);

            SessionId = response.SessionId;
            Language = string.IsNullOrWhiteSpace(response.Language) ? Language : response.Language;
            IsSessionActive = true;
            TranscriptItems.Clear();
            LastToolCalls.Clear();
            AssistantDisplayText = string.Empty;
            AssistantSpeakText = string.Empty;
            LastTurnId = string.Empty;
            LastLatencyMs = 0;
            StatusText = $"Voice session created: {SessionId}";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create voice session failed");
            StatusText = $"Create session failed: {ex.Message}";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Submits the current transcript input as a voice turn.
    /// </summary>
    protected async Task SubmitTurnAsync()
    {
        if (IsBusy) return;

        var text = (TranscriptInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText = "Enter transcript text first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SessionId))
            await CreateSessionAsync().ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(SessionId))
            return;

        IsBusy = true;
        _activeTurnCts?.Cancel();
        _activeTurnCts = new CancellationTokenSource();
        var ct = _activeTurnCts.Token;

        try
        {
            GlobalStatusChanged?.Invoke("Submitting voice turn...");
            StatusText = "Submitting voice turn...";
            var response = await _voiceService.SubmitTurnAsync(
                SessionId,
                new McpVoiceTurnRequest
                {
                    UserTranscriptText = text,
                    Language = Language,
                    ClientTimestampUtc = DateTimeOffset.UtcNow.ToString("O")
                },
                ct).ConfigureAwait(true);

            LastTurnId = response.TurnId ?? string.Empty;
            LastLatencyMs = response.LatencyMs;
            AssistantDisplayText = response.AssistantDisplayText ?? string.Empty;
            AssistantSpeakText = response.AssistantSpeakText ?? string.Empty;

            LastToolCalls.Clear();
            if (response.ToolCalls != null)
            {
                foreach (var toolCall in response.ToolCalls)
                    LastToolCalls.Add(toolCall);
            }

            await RefreshTranscriptAsyncInternal(ct).ConfigureAwait(true);

            StatusText = string.Equals(response.Status, "completed", StringComparison.OrdinalIgnoreCase)
                ? $"Voice turn completed ({response.LatencyMs} ms)"
                : $"Voice turn {response.Status}: {response.Error ?? "no details"}";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Voice turn canceled.";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit voice turn failed");
            StatusText = $"Voice turn failed: {ex.Message}";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Submits a turn via SSE streaming. Yields text chunks as they arrive from Copilot.
    /// Updates <see cref="AssistantDisplayText"/> incrementally.
    /// </summary>
    public async IAsyncEnumerable<McpVoiceTurnStreamEvent> SubmitTurnStreamingAsync(
        string text,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        if (string.IsNullOrWhiteSpace(SessionId))
            await CreateSessionAsync().ConfigureAwait(true);

        if (string.IsNullOrWhiteSpace(SessionId))
            yield break;

        IsBusy = true;
        _activeTurnCts?.Cancel();
        _activeTurnCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_activeTurnCts.Token, cancellationToken);

        AssistantDisplayText = string.Empty;
        AssistantSpeakText = string.Empty;
        StatusText = "Streaming response...";
        GlobalStatusChanged?.Invoke(StatusText);

        var channel = System.Threading.Channels.Channel.CreateUnbounded<McpVoiceTurnStreamEvent>();
        var accumulated = new System.Text.StringBuilder();

        // Producer: reads from service stream, writes to channel
        _ = Task.Run(async () =>
        {
            McpVoiceTurnStreamEvent? lastEvent = null;
            try
            {
                await foreach (var evt in _voiceService.SubmitTurnStreamingAsync(
                    SessionId,
                    new McpVoiceTurnRequest
                    {
                        UserTranscriptText = text,
                        Language = Language,
                        ClientTimestampUtc = DateTimeOffset.UtcNow.ToString("O")
                    },
                    linkedCts.Token))
                {
                    lastEvent = evt;
                    if (evt.Type == "chunk" && evt.Text is not null)
                    {
                        accumulated.Append(AnsiEscapePattern.Replace(evt.Text, ""));
                        AssistantDisplayText = accumulated.ToString();
                    }
                    await channel.Writer.WriteAsync(evt, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                StatusText = "Voice turn canceled.";
                GlobalStatusChanged?.Invoke(StatusText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Streaming voice turn failed");
                StatusText = $"Voice turn failed: {ex.Message}";
                GlobalStatusChanged?.Invoke(StatusText);
                await channel.Writer.WriteAsync(
                    new McpVoiceTurnStreamEvent { Type = "error", Message = ex.Message },
                    CancellationToken.None);
            }
            finally
            {
                // Finalize state
                var finalText = accumulated.ToString().Trim();
                AssistantDisplayText = finalText;
                AssistantSpeakText = finalText;

                if (lastEvent?.Type == "done")
                {
                    LastTurnId = lastEvent.TurnId ?? string.Empty;
                    LastLatencyMs = lastEvent.LatencyMs ?? 0;
                    LastToolCalls.Clear();
                    if (lastEvent.ToolCalls is not null)
                        foreach (var tc in lastEvent.ToolCalls)
                            LastToolCalls.Add(tc);
                    StatusText = $"Voice turn completed ({LastLatencyMs} ms)";
                }
                else if (lastEvent?.Type == "error")
                {
                    StatusText = $"Voice turn error: {lastEvent.Message}";
                }

                GlobalStatusChanged?.Invoke(StatusText);
                IsBusy = false;
                linkedCts.Dispose();
                channel.Writer.Complete();
            }
        }, linkedCts.Token);

        // Consumer: yield from channel
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(true))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Refreshes transcript history from the service.
    /// </summary>
    protected async Task RefreshTranscriptAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await RefreshTranscriptAsyncInternal(CancellationToken.None).ConfigureAwait(true);
            StatusText = $"Loaded {TranscriptItems.Count} transcript item(s).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh transcript failed");
            StatusText = $"Refresh transcript failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Refreshes session status from the service.
    /// </summary>
    protected async Task RefreshStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            StatusText = "No active session.";
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var status = await _voiceService.GetStatusAsync(SessionId).ConfigureAwait(true);
            IsSessionActive = true;
            LastTurnId = status.LastTurnId ?? LastTurnId;
            StatusText = $"{status.Status} (turn active: {status.IsTurnActive})";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh voice status failed");
            StatusText = $"Refresh status failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Sends an interrupt request for the current turn.
    /// </summary>
    protected async Task InterruptAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            StatusText = "No active session.";
            return;
        }

        try
        {
            _activeTurnCts?.Cancel();
            var result = await _voiceService.InterruptAsync(SessionId).ConfigureAwait(true);
            StatusText = result.Interrupted ? "Interrupt sent." : "No active turn to interrupt.";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interrupt voice turn failed");
            StatusText = $"Interrupt failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Sends ESC to the underlying interactive session when available.
    /// </summary>
    protected async Task SendEscapeAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            StatusText = "No active session.";
            return;
        }

        try
        {
            var sent = await _voiceService.SendEscapeAsync(SessionId).ConfigureAwait(true);
            StatusText = sent ? "ESC sent to Copilot." : "No active interactive session.";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send escape failed");
            StatusText = $"Send ESC failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Ends and deletes the active session.
    /// </summary>
    protected async Task EndSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(SessionId))
        {
            ClearSessionState();
            return;
        }

        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _activeTurnCts?.Cancel();
            await _voiceService.DeleteSessionAsync(SessionId).ConfigureAwait(true);
            ClearSessionState();
            StatusText = "Voice session ended.";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "End voice session failed");
            StatusText = $"End session failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Clears transcript input text.
    /// </summary>
    protected void ClearTurnInput()
    {
        TranscriptInput = string.Empty;
    }

    private async Task RefreshTranscriptAsyncInternal(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SessionId))
            return;

        var response = await _voiceService.GetTranscriptAsync(SessionId, cancellationToken).ConfigureAwait(true);
        TranscriptItems.Clear();
        foreach (var item in response.Items.OrderBy(i => i.TimestampUtc, StringComparer.Ordinal))
            TranscriptItems.Add(item);
    }

    private void ClearSessionState()
    {
        SessionId = string.Empty;
        IsSessionActive = false;
        TranscriptItems.Clear();
        LastToolCalls.Clear();
        AssistantDisplayText = string.Empty;
        AssistantSpeakText = string.Empty;
        LastTurnId = string.Empty;
        LastLatencyMs = 0;
    }
}
