using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using McpServer.Client.Models;
using Terminal.Gui;

namespace McpServer.Director.Screens;

/// <summary>
/// Terminal chat surface for connecting to an agent session and streaming interactive turns.
/// </summary>
internal sealed class AgentChatScreen : View
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly DirectorMcpContext _directorContext;
    private readonly object _turnGate = new();

    private Label _statusLabel = null!;
    private ComboBox _agentNamePicker = null!;
    private ComboBox _sessionIdPicker = null!;
    private TextView _transcriptView = null!;
    private TextView _promptView = null!;
    private CancellationTokenSource? _activeTurnCts;
    private readonly ObservableCollection<string> _agentNameOptions = [];
    private readonly ObservableCollection<string> _sessionIdOptions = [];
    private readonly Dictionary<string, string> _agentToSession = new(StringComparer.OrdinalIgnoreCase);
    private bool _syncingPickers;

    public AgentChatScreen(DirectorMcpContext directorContext)
    {
        _directorContext = directorContext ?? throw new ArgumentNullException(nameof(directorContext));
        Title = "Chat";
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = true;
        BuildUi();
        Disposing += (_, _) => CancelActiveTurn();
    }

    public async Task LoadAsync()
    {
        await RefreshAgentAndSessionPickersAsync().ConfigureAwait(false);
        if (GetSessionIdOrNull() is null)
            SetStatus("Select an agent/session from the dropdowns or create/connect.");
        else
            await RefreshStatusAsync().ConfigureAwait(false);
    }

    private void BuildUi()
    {
        _statusLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Chat: Connect an agent session to begin.",
        };
        Add(_statusLabel);

        var agentLabel = new Label { X = 0, Y = 1, Text = "Agent:" };
        _agentNamePicker = new ComboBox
        {
            X = Pos.Right(agentLabel) + 1,
            Y = 1,
            Width = 22,
            Height = 1,
            Text = string.Empty,
            HideDropdownListOnClick = true,
        };
        _agentNamePicker.SetSource(_agentNameOptions);
        _agentNamePicker.SelectedItemChanged += (_, e) => OnAgentSelectionChanged(e.Value?.ToString());
        _agentNamePicker.OpenSelectedItem += (_, e) => OnAgentSelectionChanged(e.Value?.ToString());

        var sessionLabel = new Label { X = Pos.Right(_agentNamePicker) + 2, Y = 1, Text = "Session:" };
        _sessionIdPicker = new ComboBox
        {
            X = Pos.Right(sessionLabel) + 1,
            Y = 1,
            Width = 30,
            Height = 1,
            Text = string.Empty,
            HideDropdownListOnClick = true,
        };
        _sessionIdPicker.SetSource(_sessionIdOptions);
        _sessionIdPicker.SelectedItemChanged += (_, e) => OnSessionSelectionChanged(e.Value?.ToString());
        _sessionIdPicker.OpenSelectedItem += (_, e) => OnSessionSelectionChanged(e.Value?.ToString());

        var connectBtn = new Button { X = Pos.Right(_sessionIdPicker) + 2, Y = 1, Text = "Connect Agent" };
        connectBtn.Accepting += (_, _) => _ = Task.Run(ConnectAgentAsync);

        var newSessionBtn = new Button { X = Pos.Right(connectBtn) + 1, Y = 1, Text = "New Session" };
        newSessionBtn.Accepting += (_, _) => _ = Task.Run(CreateVoiceSessionAsync);

        var statusBtn = new Button { X = Pos.Right(newSessionBtn) + 1, Y = 1, Text = "Session Status" };
        statusBtn.Accepting += (_, _) => _ = Task.Run(RefreshPickersAndStatusAsync);

        Add(agentLabel, _agentNamePicker, sessionLabel, _sessionIdPicker, connectBtn, newSessionBtn, statusBtn);

        var transcriptLabel = new Label { X = 0, Y = 2, Text = "Transcript" };
        Add(transcriptLabel);

        _transcriptView = new TextView
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(8),
            ReadOnly = true,
            WordWrap = true,
            Text = string.Empty,
        };
        Add(_transcriptView);

        var promptLabel = new Label { X = 0, Y = Pos.Bottom(_transcriptView), Text = "Message" };
        Add(promptLabel);

        _promptView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(promptLabel),
            Width = Dim.Fill(),
            Height = 3,
            WordWrap = true,
            Text = string.Empty,
        };
        Add(_promptView);

        var sendBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Send" };
        sendBtn.Accepting += (_, _) => _ = Task.Run(SendTurnAsync);

        var interruptBtn = new Button { X = Pos.Right(sendBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Interrupt" };
        interruptBtn.Accepting += (_, _) => _ = Task.Run(InterruptAsync);

        var escapeBtn = new Button { X = Pos.Right(interruptBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Send ESC" };
        escapeBtn.Accepting += (_, _) => _ = Task.Run(SendEscapeAsync);

        var clearBtn = new Button { X = Pos.Right(escapeBtn) + 1, Y = Pos.AnchorEnd(1), Text = "Clear" };
        clearBtn.Accepting += (_, _) => ClearTranscript();

        Add(sendBtn, interruptBtn, escapeBtn, clearBtn);
    }

    private async Task ConnectAgentAsync()
    {
        SetStatus("Connecting to pooled agent...");
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var agentName = GetAgentNameOrNull();
            AgentPoolConnectResult result = string.IsNullOrWhiteSpace(agentName)
                ? await api.AgentPool.ConnectDefaultAsync().ConfigureAwait(false)
                : await api.AgentPool.ConnectAsync(agentName).ConfigureAwait(false);

            if (!result.Success || string.IsNullOrWhiteSpace(result.SessionId))
            {
                SetStatus(result.Error ?? "Agent connect failed.");
                return;
            }

            SetAgentName(result.AgentName);
            SetSessionId(result.SessionId);
            await RefreshAgentAndSessionPickersAsync(result.AgentName, result.SessionId).ConfigureAwait(false);
            AppendTranscriptLine($"[system] Connected '{result.AgentName ?? "default"}' session {result.SessionId}.");
            SetStatus($"Connected session {result.SessionId}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Connect failed: {ex.Message}");
        }
    }

    private async Task CreateVoiceSessionAsync()
    {
        SetStatus("Creating voice session...");
        try
        {
            var client = _directorContext.GetRequiredActiveWorkspaceHttpClient();
            var response = await client.PostAsync<VoiceSessionCreateResponseDto>(
                "/mcpserver/voice/session",
                new VoiceSessionCreateRequestDto
                {
                    AgentName = GetAgentNameOrNull(),
                    ClientName = "McpServer.Director",
                    DeviceId = Environment.MachineName,
                    WorkspacePath = _directorContext.ActiveWorkspacePath,
                }).ConfigureAwait(false);

            if (response is null || string.IsNullOrWhiteSpace(response.SessionId))
            {
                SetStatus("Create session failed: empty response.");
                return;
            }

            SetAgentName(GetAgentNameOrNull());
            SetSessionId(response.SessionId);
            await RefreshAgentAndSessionPickersAsync(GetAgentNameOrNull(), response.SessionId).ConfigureAwait(false);
            AppendTranscriptLine($"[system] Created session {response.SessionId}.");
            SetStatus($"Session {response.SessionId} ready ({response.Status}).");
        }
        catch (Exception ex)
        {
            SetStatus($"Create session failed: {ex.Message}");
        }
    }

    private async Task RefreshStatusAsync()
    {
        var sessionId = GetSessionIdOrNull();
        if (sessionId is null)
        {
            SetStatus("No session selected.");
            return;
        }

        try
        {
            var client = _directorContext.GetRequiredActiveWorkspaceHttpClient();
            var status = await client.GetAsync<VoiceSessionStatusDto>(
                $"/mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}").ConfigureAwait(false);

            if (status is null)
            {
                SetStatus("Session status unavailable.");
                return;
            }

            SetStatus($"Session {status.SessionId} | {status.Status} | turns={status.TurnCounter}");
        }
        catch (Exception ex)
        {
            SetStatus($"Status failed: {ex.Message}");
        }
    }

    private async Task SendTurnAsync()
    {
        var sessionId = GetSessionIdOrNull();
        if (sessionId is null)
        {
            SetStatus("Connect or create a session first.");
            return;
        }

        var prompt = (_promptView.Text?.ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            SetStatus("Enter a message first.");
            return;
        }

        CancellationToken token;
        if (!TryBeginTurn(out token))
        {
            SetStatus("A turn is already in progress.");
            return;
        }

        Application.Invoke(() => _promptView.Text = string.Empty);
        AppendTranscriptLine($"You: {prompt}");
        SetStatus("Streaming response...");

        var hasAgentText = false;
        try
        {
            var client = _directorContext.GetRequiredActiveWorkspaceHttpClient();
            await foreach (var payload in client.PostSseAsync(
                $"/mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/turn/stream",
                new VoiceTurnRequestDto { UserTranscriptText = prompt },
                token).ConfigureAwait(false))
            {
                VoiceTurnStreamEventDto? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<VoiceTurnStreamEventDto>(payload, s_jsonOptions);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (evt is null)
                    continue;

                var eventType = evt.Type?.Trim().ToLowerInvariant() ?? string.Empty;
                switch (eventType)
                {
                    case "chunk":
                        if (!hasAgentText)
                        {
                            AppendTranscript("Agent: ");
                            hasAgentText = true;
                        }

                        if (!string.IsNullOrEmpty(evt.Text))
                            AppendTranscript(evt.Text);
                        break;

                    case "tool_status":
                        if (hasAgentText)
                        {
                            AppendTranscript(Environment.NewLine);
                            hasAgentText = false;
                        }

                        var toolMessage = !string.IsNullOrWhiteSpace(evt.Message) ? evt.Message : evt.Summary;
                        AppendTranscriptLine($"[tool:{evt.ToolName ?? "unknown"}] {toolMessage ?? "running"}");
                        break;

                    case "done":
                        if (hasAgentText)
                        {
                            AppendTranscript(Environment.NewLine);
                            hasAgentText = false;
                        }

                        var latencySuffix = evt.LatencyMs.HasValue ? $" ({evt.LatencyMs.Value}ms)" : string.Empty;
                        AppendTranscriptLine($"[done] {evt.Status ?? "completed"}{latencySuffix}");
                        SetStatus($"Turn complete: {evt.Status ?? "completed"}{latencySuffix}");
                        break;

                    case "error":
                        if (hasAgentText)
                        {
                            AppendTranscript(Environment.NewLine);
                            hasAgentText = false;
                        }

                        AppendTranscriptLine($"[error] {evt.Message ?? "Voice turn processing failed."}");
                        SetStatus(evt.Message ?? "Voice turn processing failed.");
                        break;
                }
            }

            if (hasAgentText)
                AppendTranscript(Environment.NewLine);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Turn canceled.");
        }
        catch (Exception ex)
        {
            SetStatus($"Send failed: {ex.Message}");
        }
        finally
        {
            EndTurn();
        }
    }

    private async Task InterruptAsync()
    {
        var sessionId = GetSessionIdOrNull();
        if (sessionId is null)
        {
            SetStatus("No session selected.");
            return;
        }

        try
        {
            var client = _directorContext.GetRequiredActiveWorkspaceHttpClient();
            await client.PostAsync<VoiceInterruptResponseDto>(
                $"/mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/interrupt",
                null).ConfigureAwait(false);
            AppendTranscriptLine("[system] Interrupt signaled.");
            SetStatus("Interrupt sent.");
        }
        catch (Exception ex)
        {
            SetStatus($"Interrupt failed: {ex.Message}");
        }
    }

    private async Task SendEscapeAsync()
    {
        var sessionId = GetSessionIdOrNull();
        if (sessionId is null)
        {
            SetStatus("No session selected.");
            return;
        }

        try
        {
            var client = _directorContext.GetRequiredActiveWorkspaceHttpClient();
            var response = await client.PostAsync<VoiceEscapeResponseDto>(
                $"/mcpserver/voice/session/{Uri.EscapeDataString(sessionId)}/escape",
                null).ConfigureAwait(false);
            var sent = response?.Sent == true ? "sent" : "not sent";
            AppendTranscriptLine($"[system] ESC {sent}.");
            SetStatus($"ESC {sent}.");
        }
        catch (Exception ex)
        {
            SetStatus($"ESC failed: {ex.Message}");
        }
    }

    private async Task RefreshPickersAndStatusAsync()
    {
        await RefreshAgentAndSessionPickersAsync().ConfigureAwait(false);
        await RefreshStatusAsync().ConfigureAwait(false);
    }

    private async Task RefreshAgentAndSessionPickersAsync(string? preferredAgent = null, string? preferredSessionId = null)
    {
        try
        {
            var api = await _directorContext.GetRequiredActiveWorkspaceApiClientAsync().ConfigureAwait(false);
            var agents = await api.AgentPool.GetAgentsAsync().ConfigureAwait(false);

            var selectedAgent = string.IsNullOrWhiteSpace(preferredAgent)
                ? GetAgentNameOrNull()
                : preferredAgent.Trim();
            var selectedSession = string.IsNullOrWhiteSpace(preferredSessionId)
                ? GetSessionIdOrNull()
                : preferredSessionId.Trim();

            var agentToSession = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var agentNames = new List<string>();
            var sessionIds = new List<string>();
            foreach (var agent in agents)
            {
                if (!string.IsNullOrWhiteSpace(agent.AgentName) && !agentNames.Contains(agent.AgentName, StringComparer.OrdinalIgnoreCase))
                    agentNames.Add(agent.AgentName);

                if (!string.IsNullOrWhiteSpace(agent.SessionId))
                {
                    if (!sessionIds.Contains(agent.SessionId, StringComparer.OrdinalIgnoreCase))
                        sessionIds.Add(agent.SessionId);
                    if (!string.IsNullOrWhiteSpace(agent.AgentName) && !agentToSession.ContainsKey(agent.AgentName))
                        agentToSession[agent.AgentName] = agent.SessionId;
                }
            }

            agentNames.Sort(StringComparer.OrdinalIgnoreCase);
            sessionIds.Sort(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(selectedAgent)
                && string.IsNullOrWhiteSpace(selectedSession)
                && agentToSession.TryGetValue(selectedAgent, out var mappedSession))
            {
                selectedSession = mappedSession;
            }

            Application.Invoke(() =>
            {
                _syncingPickers = true;
                _agentToSession.Clear();
                foreach (var entry in agentToSession)
                    _agentToSession[entry.Key] = entry.Value;

                _agentNameOptions.Clear();
                foreach (var name in agentNames)
                    _agentNameOptions.Add(name);
                _agentNamePicker.SetSource(_agentNameOptions);

                _sessionIdOptions.Clear();
                foreach (var id in sessionIds)
                    _sessionIdOptions.Add(id);
                _sessionIdPicker.SetSource(_sessionIdOptions);

                if (!string.IsNullOrWhiteSpace(selectedAgent))
                {
                    var agentIndex = agentNames.FindIndex(x => string.Equals(x, selectedAgent, StringComparison.OrdinalIgnoreCase));
                    if (agentIndex >= 0)
                        _agentNamePicker.SelectedItem = agentIndex;
                    _agentNamePicker.Text = selectedAgent;
                }

                if (!string.IsNullOrWhiteSpace(selectedSession))
                {
                    var sessionIndex = sessionIds.FindIndex(x => string.Equals(x, selectedSession, StringComparison.OrdinalIgnoreCase));
                    if (sessionIndex >= 0)
                        _sessionIdPicker.SelectedItem = sessionIndex;
                    _sessionIdPicker.Text = selectedSession;
                }

                _syncingPickers = false;
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Picker refresh failed: {ex.Message}");
        }
    }

    private void OnAgentSelectionChanged(string? selectedValue)
    {
        if (_syncingPickers)
            return;

        var selectedAgent = string.IsNullOrWhiteSpace(selectedValue)
            ? GetAgentNameOrNull()
            : selectedValue.Trim();
        if (string.IsNullOrWhiteSpace(selectedAgent))
            return;

        SetAgentName(selectedAgent);
        if (_agentToSession.TryGetValue(selectedAgent, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            SetSessionId(sessionId);
    }

    private void OnSessionSelectionChanged(string? selectedValue)
    {
        if (_syncingPickers)
            return;

        var sessionId = string.IsNullOrWhiteSpace(selectedValue)
            ? GetSessionIdOrNull()
            : selectedValue.Trim();
        if (!string.IsNullOrWhiteSpace(sessionId))
            SetSessionId(sessionId);
    }

    private string? GetAgentNameOrNull()
    {
        var value = (_agentNamePicker.Text ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private string? GetSessionIdOrNull()
    {
        var value = (_sessionIdPicker.Text ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void SetAgentName(string? agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
            return;

        var trimmed = agentName.Trim();
        Application.Invoke(() => _agentNamePicker.Text = trimmed);
    }

    private void SetSessionId(string sessionId)
    {
        Application.Invoke(() => _sessionIdPicker.Text = sessionId);
    }

    private bool TryBeginTurn(out CancellationToken token)
    {
        lock (_turnGate)
        {
            if (_activeTurnCts is not null)
            {
                token = default;
                return false;
            }

            _activeTurnCts = new CancellationTokenSource();
            token = _activeTurnCts.Token;
            return true;
        }
    }

    private void EndTurn()
    {
        CancellationTokenSource? cts;
        lock (_turnGate)
        {
            cts = _activeTurnCts;
            _activeTurnCts = null;
        }

        cts?.Dispose();
    }

    private void CancelActiveTurn()
    {
        CancellationTokenSource? cts;
        lock (_turnGate)
        {
            cts = _activeTurnCts;
            _activeTurnCts = null;
        }

        if (cts is null)
            return;

        cts.Cancel();
        cts.Dispose();
    }

    private void ClearTranscript()
    {
        Application.Invoke(() => _transcriptView.Text = string.Empty);
        SetStatus("Transcript cleared.");
    }

    private void AppendTranscriptLine(string line)
        => AppendTranscript(line + Environment.NewLine);

    private void AppendTranscript(string text)
    {
        Application.Invoke(() =>
        {
            var current = _transcriptView.Text?.ToString() ?? string.Empty;
            _transcriptView.Text = current + text;
        });
    }

    private void SetStatus(string text)
        => Application.Invoke(() => _statusLabel.Text = $"Chat: {text}");

    private sealed record VoiceSessionCreateRequestDto
    {
        public string? DeviceId { get; init; }
        public string? ClientName { get; init; }
        public string? WorkspacePath { get; init; }
        public string? AgentName { get; init; }
    }

    private sealed record VoiceSessionCreateResponseDto
    {
        public string SessionId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private sealed record VoiceSessionStatusDto
    {
        public string SessionId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int TurnCounter { get; init; }
    }

    private sealed record VoiceTurnRequestDto
    {
        public string UserTranscriptText { get; init; } = string.Empty;
    }

    private sealed record VoiceTurnStreamEventDto
    {
        public string? Type { get; init; }
        public string? Text { get; init; }
        public string? Status { get; init; }
        public string? Message { get; init; }
        public string? ToolName { get; init; }
        public string? Summary { get; init; }
        public int? LatencyMs { get; init; }
    }

    private sealed record VoiceInterruptResponseDto
    {
        public bool Interrupted { get; init; }
    }

    private sealed record VoiceEscapeResponseDto
    {
        public bool Sent { get; init; }
    }
}
