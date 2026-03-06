using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for chat assistant window interactions.
/// </summary>
public partial class ChatWindowViewModel : ViewModelBase
{
    private static readonly Regex BareUriPattern = new(
        @"(?<!\(|\[)https?://[^\s)\]`""<>]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IChatWindowService _chatService;
    private readonly IUiDispatcherService _uiDispatcher;
    private readonly Func<string> _getContext;
    private readonly Action<string?>? _onModelChanged;
    private readonly string? _initialModelFromConfig;
    private CancellationTokenSource? _sendCts;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = [];

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<string> _availableModels = [];

    [ObservableProperty]
    private string? _selectedModel;

    [ObservableProperty]
    private ObservableCollection<PromptTemplate> _promptTemplates = [];

    /// <summary>
    /// Creates a chat window ViewModel.
    /// </summary>
    public ChatWindowViewModel(
        IChatWindowService chatService,
        Func<string>? getContext = null,
        string? initialModelFromConfig = null,
        Action<string?>? onModelChanged = null,
        IUiDispatcherService? uiDispatcher = null)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _uiDispatcher = uiDispatcher ?? new ImmediateUiDispatcherService();
        _getContext = getContext ?? (() => string.Empty);
        _initialModelFromConfig = initialModelFromConfig;
        _onModelChanged = onModelChanged;
    }

    /// <summary>
    /// Opens the agent config file in default editor.
    /// </summary>
    public Task<ChatFileOpenResult> OpenAgentConfigAsync(CancellationToken cancellationToken = default)
        => _chatService.OpenAgentConfigAsync(cancellationToken);

    /// <summary>
    /// Opens the prompt templates file in default editor.
    /// </summary>
    public Task<ChatFileOpenResult> OpenPromptTemplatesAsync(CancellationToken cancellationToken = default)
        => _chatService.OpenPromptTemplatesAsync(cancellationToken);

    /// <summary>
    /// Loads prompt templates from backing service.
    /// </summary>
    public void LoadPrompts() => _ = LoadPromptsAsync();

    /// <summary>
    /// Loads prompt templates from backing service.
    /// </summary>
    public async Task LoadPromptsAsync(CancellationToken cancellationToken = default)
    {
        var prompts = await _chatService.LoadPromptsAsync(cancellationToken).ConfigureAwait(true);
        DispatchToUi(() =>
        {
            PromptTemplates.Clear();
            foreach (var prompt in prompts)
                PromptTemplates.Add(prompt);
        });
    }

    partial void OnSelectedModelChanged(string? value)
    {
        _onModelChanged?.Invoke(value);
    }

    /// <summary>
    /// Loads available models and applies initial preferred model selection.
    /// </summary>
    public async Task LoadModelsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _chatService.LoadModelsAsync(_initialModelFromConfig, cancellationToken).ConfigureAwait(true);
        DispatchToUi(() =>
        {
            AvailableModels.Clear();
            if (!result.IsReachable)
            {
                AvailableModels.Add("(Ollama not reachable)");
                SelectedModel = null;
                return;
            }

            if (result.Models.Count == 0)
            {
                AvailableModels.Add("(No models - start Ollama)");
                SelectedModel = null;
                return;
            }

            foreach (var model in result.Models)
                AvailableModels.Add(model);

            SelectedModel = result.SelectedModel;
        });
    }

    /// <summary>
    /// Populates the input box from selected prompt template.
    /// </summary>
    protected async Task PopulatePrompt(PromptTemplate? prompt)
    {
        var promptText = await _chatService.PopulatePromptAsync(prompt).ConfigureAwait(true);
        if (string.IsNullOrEmpty(promptText))
            return;

        CurrentInput = promptText;
    }

    /// <summary>
    /// Submits selected prompt template as a chat message when appropriate.
    /// </summary>
    protected async Task SubmitPromptAsync(PromptTemplate? prompt)
    {
        var prepared = await _chatService.SubmitPromptAsync(prompt).ConfigureAwait(true);
        if (!prepared.ShouldSend)
            return;

        CurrentInput = prepared.PromptText;
        await SendCurrentInputAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Sends the current input to the assistant.
    /// </summary>
    protected async Task SendAsync()
        => await SendCurrentInputAsync().ConfigureAwait(true);

    /// <summary>
    /// Returns true when the send action can execute.
    /// </summary>
    protected bool CanSend()
        => !IsLoading;

    /// <summary>
    /// Cancels any in-flight send request.
    /// </summary>
    public void CancelSend()
    {
        _sendCts?.Cancel();
    }

    /// <summary>
    /// Called when surrounding context changes.
    /// </summary>
    public void NotifyContextChanged(string fullContext)
    {
        // Context is read from _getContext when sending.
    }

    private async Task SendCurrentInputAsync()
    {
        var text = (CurrentInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
            return;

        CurrentInput = string.Empty;
        var userMessage = new ChatMessage { Role = "user", Text = text };
        Messages.Add(userMessage);

        var context = _getContext();
        IsLoading = true;
        NotifySendCanExecuteChanged();

        _sendCts?.Cancel();
        _sendCts = new CancellationTokenSource();
        var token = _sendCts.Token;

        var assistantMessage = new ChatMessage { Role = "assistant", Text = string.Empty };
        DispatchToUi(() => Messages.Add(assistantMessage));

        IProgress<string>? progress = new Progress<string>(content =>
        {
            DispatchToUi(() =>
            {
                if (!token.IsCancellationRequested && Messages.Contains(assistantMessage))
                    assistantMessage.Text = content ?? string.Empty;
            });
        });

        try
        {
            var request = new ChatSendRequest(text, context, SelectedModel);
            var result = await _chatService
                .SendMessageAsync(request, progress, token)
                .ConfigureAwait(true);

            if (token.IsCancellationRequested && !result.WasCancelled)
                return;

            DispatchToUi(() =>
            {
                if (token.IsCancellationRequested && !result.WasCancelled)
                    return;

                assistantMessage.Text = result.WasCancelled
                    ? "[Cancelled]"
                    : (result.Success
                        ? ConvertBareUrisToMarkdownLinks(result.ReplyText ?? string.Empty)
                        : "Error: " + (result.ErrorMessage ?? "Unknown error"));

                IsLoading = false;
                NotifySendCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            DispatchToUi(() =>
            {
                assistantMessage.Text = "Error: " + ex.Message;
                IsLoading = false;
                NotifySendCanExecuteChanged();
            });
        }
    }

    private static string ConvertBareUrisToMarkdownLinks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return BareUriPattern.Replace(text, match => $"[{match.Value}]({match.Value})");
    }

    private void DispatchToUi(Action action)
        => _uiDispatcher.Post(action);

    /// <summary>
    /// Notifies command infrastructure that send availability changed.
    /// </summary>
    protected virtual void NotifySendCanExecuteChanged()
    {
    }
}
