using McpServer.Client;
using McpServer.Client.Models;
using McpServer.McpAgent.PowerShellSessions;
using McpServer.McpAgent.Hosting;
using McpServer.McpAgent.SessionLog;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using OpenAIChatClient = OpenAI.Chat.ChatClient;
using OpenAIClientOptions = OpenAI.OpenAIClientOptions;

namespace McpServer.McpAgent.SampleHost;

internal static class SampleHostAppFactory
{
    public static ServiceProvider BuildServiceProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = SampleHostSettings.Resolve(configuration);
        var services = new ServiceCollection();
        services.AddSingleton(settings);
        services.AddLogging();
        services.AddMcpServerMcpAgent(options =>
        {
            options.BaseUrl = settings.BaseUrl;
            options.ApiKey = settings.ApiKey;
            options.BearerToken = settings.BearerToken;
            options.RequireAuthentication = settings.RequireAuthentication;
            options.WorkspacePath = settings.WorkspacePath;
            options.AgentId = settings.AgentId;
            options.AgentName = settings.AgentName;
            options.Description = settings.AgentDescription;
            options.SourceType = settings.SourceType;
        });

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    public static SampleHostConsoleApplication CreateApplication(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var settings = serviceProvider.GetRequiredService<SampleHostSettings>();
        var factory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
        return new SampleHostConsoleApplication(factory.CreateHostedAgent(), settings);
    }
}

internal sealed class SampleHostConsoleApplication : IDisposable
{
    private readonly IMcpHostedAgent _hostedAgent;
    private readonly SampleHostSettings _settings;
    private readonly IChatClient _chatClient;
    private readonly ChatClientAgent _chatAgent;
    private readonly object _powerShellCommandSync = new();
    private readonly ChatClientAgentRunOptions _runOptions;
    private CancellationTokenSource? _activePowerShellCommandCancellationSource;
    private AgentSession? _agentSession;
    private string? _powerShellSessionId;
    private string _powerShellCurrentLocation;
    private string _verbosity;
    private bool _shouldInjectSystemPrompt = true;

    public SampleHostConsoleApplication(IMcpHostedAgent hostedAgent, SampleHostSettings settings)
    {
        _hostedAgent = hostedAgent ?? throw new ArgumentNullException(nameof(hostedAgent));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _chatClient = CreateChatClient(settings);
        _chatAgent = hostedAgent.CreateChatClientAgent(_chatClient);
        _runOptions = hostedAgent.CreateRunOptions();
        _powerShellCurrentLocation = settings.WorkspacePath;
        _verbosity = settings.Verbosity;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        WriteBanner();
        Console.WriteLine("Type /help for commands. Prefix a line with ! to run it directly in the local PowerShell session.");
        Console.WriteLine();

        EnsurePowerShellSession();
        await StartNewConversationAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (args.Length > 0)
            {
                var commandText = string.Join(' ', args).Trim();
                if (!string.IsNullOrWhiteSpace(commandText))
                    await TryDispatchAsync(commandText, cancellationToken).ConfigureAwait(false);

                return 0;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var input = ReadConsoleLine(BuildConsolePrompt(), cancellationToken);
                if (input is null)
                    break;

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (await TryDispatchAsync(input, cancellationToken).ConfigureAwait(false))
                    break;
            }

            return 0;
        }
        finally
        {
            ClosePowerShellSession();
            await TryCompleteSessionAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> TryDispatchAsync(string input, CancellationToken cancellationToken)
    {
        try
        {
            return await DispatchAsync(input, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (McpUnauthorizedException exception)
        {
            WriteExpiredAuthenticationMessage("processing the request", exception.Message);
            return false;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error> {exception.Message}");
            Console.Error.WriteLine();
            return false;
        }
    }

    public void Dispose()
    {
        ClosePowerShellSession();
        _hostedAgent.PowerShellSessions.Dispose();
        _chatClient.Dispose();
    }

    private async Task<bool> DispatchAsync(string input, CancellationToken cancellationToken)
    {
        if (input.StartsWith("! ", StringComparison.Ordinal) && !TryGetDirectPowerShellCommand(input, out _))
        {
            Console.Error.WriteLine("error> Enter a PowerShell command after '! '.");
            Console.Error.WriteLine();
            return false;
        }

        if (TryGetDirectPowerShellCommand(input, out var powerShellCommand))
        {
            await ExecuteDirectPowerShellAsync(powerShellCommand, cancellationToken).ConfigureAwait(false);
            return false;
        }

        if (TryHandleCommand(input, out var shouldExit, out var commandAction))
        {
            await commandAction(cancellationToken).ConfigureAwait(false);
            return shouldExit;
        }

        await ExecutePromptAsync(input.Trim(), cancellationToken).ConfigureAwait(false);
        return false;
    }

    public bool TryCancelActivePowerShellCommand()
    {
        CancellationTokenSource? activeCancellation;
        lock (_powerShellCommandSync)
            activeCancellation = _activePowerShellCommandCancellationSource;

        if (activeCancellation is null)
            return false;

        activeCancellation.Cancel();
        return true;
    }

    private async Task ExecuteDirectPowerShellAsync(string command, CancellationToken cancellationToken)
    {
        EnsurePowerShellSession();
        using var directCommandCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_powerShellCommandSync)
            _activePowerShellCommandCancellationSource = directCommandCancellationSource;

        try
        {
            var result = await _hostedAgent.PowerShellSessions.ExecuteInteractiveCommandAsync(
                    _powerShellSessionId!,
                    command,
                    static token => ReadConsoleLine(string.Empty, token),
                    Console.Out,
                    Console.Error,
                    directCommandCancellationSource.Token)
                .ConfigureAwait(false);

            _powerShellCurrentLocation = result.CurrentLocation ?? _powerShellCurrentLocation;
            WritePowerShellResult(result);
            DrainPendingConsoleInput();
        }
        finally
        {
            lock (_powerShellCommandSync)
                _activePowerShellCommandCancellationSource = null;
        }
    }

    private bool TryHandleCommand(
        string input,
        out bool shouldExit,
        out Func<CancellationToken, Task> commandAction)
    {
        shouldExit = false;
        commandAction = static _ => Task.CompletedTask;
        if (!input.StartsWith("/", StringComparison.Ordinal))
            return false;

        var command = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        switch (command)
        {
            case "/help":
                commandAction = _ =>
                {
                    WriteHelp();
                    return Task.CompletedTask;
                };
                return true;
            case "/exit":
            case "/quit":
                shouldExit = true;
                return true;
            case "/new":
            case "/reset":
                commandAction = StartNewConversationAsync;
                return true;
            case "/tools":
                commandAction = _ =>
                {
                    WriteToolList();
                    return Task.CompletedTask;
                };
                return true;
            case "/session":
                commandAction = _ =>
                {
                    WriteSessionSummary();
                    return Task.CompletedTask;
                };
                return true;
            case "/v":
                if (!TryParseVerbosityLevel(input, out var verbosity, out var errorMessage))
                {
                    commandAction = _ =>
                    {
                        Console.Error.WriteLine($"error> {errorMessage}");
                        Console.Error.WriteLine();
                        return Task.CompletedTask;
                    };
                    return true;
                }

                commandAction = _ =>
                {
                    _verbosity = verbosity;
                    _shouldInjectSystemPrompt = true;
                    Console.WriteLine($"Verbosity set to {_verbosity}.");
                    Console.WriteLine();
                    return Task.CompletedTask;
                };
                return true;
            default:
                commandAction = _ =>
                {
                    Console.WriteLine($"Unknown command '{command}'. Type /help for the supported commands.");
                    Console.WriteLine();
                    return Task.CompletedTask;
                };
                return true;
        }
    }

    private async Task ExecutePromptAsync(string input, CancellationToken cancellationToken)
    {
        SessionLogTurnContext? turn;
        try
        {
            turn = await _hostedAgent.SessionLog.BeginTurnAsync(
                new SessionLogTurnCreateRequest
                {
                    QueryText = input,
                    QueryTitle = BuildTurnTitle(input),
                    Interpretation = "User prompt submitted through the interactive MCP Agent sample host.",
                    Model = _settings.ModelId,
                    ModelProvider = "openai",
                    Status = "in_progress",
                    Tags = ["sample-host", "cli-chat"],
                    ContextList = [_settings.WorkspacePath],
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (McpUnauthorizedException exception)
        {
            WriteExpiredAuthenticationMessage("starting the session-log turn", exception.Message);
            return;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"warning> Failed to start the session-log turn: {exception.Message}");
            Console.Error.WriteLine();
            return;
        }

        try
        {
            var response = await _chatAgent.RunAsync(
                CreateInputMessages(input),
                _agentSession,
                _runOptions,
                cancellationToken).ConfigureAwait(false);

            var responseText = ExtractResponseText(response);
            var tokenCount = ConvertTokenCount(response.Usage?.TotalTokenCount);

            Console.WriteLine("assistant>");
            Console.WriteLine(responseText);
            Console.WriteLine();

            _shouldInjectSystemPrompt = false;
            await TryCompleteTurnAsync(turn.RequestId, responseText, tokenCount, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await TryFailTurnAsync(
                turn.RequestId,
                "The chat turn was canceled.",
                "The interactive MCP Agent sample host canceled the active turn.",
                "The chat turn was canceled.",
                ["The active request was canceled before a response was returned."],
                CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error> {exception.Message}");
            Console.Error.WriteLine();

            await TryFailTurnAsync(
                turn.RequestId,
                exception.Message,
                "The interactive MCP Agent sample host failed to complete the chat turn.",
                exception.Message,
                [exception.Message],
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task TryCompleteTurnAsync(
        string requestId,
        string responseText,
        int? tokenCount,
        CancellationToken cancellationToken)
    {
        if (!CanMutateTurn(requestId))
            return;

        try
        {
            await _hostedAgent.SessionLog.CompleteTurnAsync(
                new SessionLogTurnCompleteRequest
                {
                    RequestId = requestId,
                    Response = responseText,
                    Interpretation = "Processed through the interactive MCP Agent sample host.",
                    Model = _settings.ModelId,
                    ModelProvider = "openai",
                    TokenCount = tokenCount,
                    Tags = ["sample-host", "cli-chat"],
                    ContextList = [_settings.WorkspacePath],
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception) when (WasTurnReplaced(exception, requestId))
        {
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"warning> Failed to record session-log completion: {exception.Message}");
            Console.Error.WriteLine();
        }
    }

    private async Task TryFailTurnAsync(
        string requestId,
        string response,
        string interpretation,
        string failureNote,
        IReadOnlyList<string> blockers,
        CancellationToken cancellationToken)
    {
        if (!CanMutateTurn(requestId))
            return;

        try
        {
            await _hostedAgent.SessionLog.FailTurnAsync(
                new SessionLogTurnFailureRequest
                {
                    RequestId = requestId,
                    Response = response,
                    Interpretation = interpretation,
                    Model = _settings.ModelId,
                    ModelProvider = "openai",
                    FailureNote = failureNote,
                    Tags = ["sample-host", "cli-chat"],
                    ContextList = [_settings.WorkspacePath],
                    Blockers = [.. blockers],
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception) when (WasTurnReplaced(exception, requestId))
        {
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"warning> Failed to record session-log failure: {exception.Message}");
            Console.Error.WriteLine();
        }
    }

    private bool CanMutateTurn(string requestId) =>
        _hostedAgent.SessionLog.Context?.FindTurn(requestId) is not null;

    private static bool WasTurnReplaced(InvalidOperationException exception, string requestId) =>
        exception.Message.Contains(requestId, StringComparison.Ordinal)
        && exception.Message.Contains("was not found in the current session-log workflow context", StringComparison.Ordinal);

    private async Task StartNewConversationAsync(CancellationToken cancellationToken)
    {
        if (_hostedAgent.SessionLog.Context is not null)
        {
            try
            {
                await _hostedAgent.SessionLog.UpdateSessionAsync(
                    new SessionLogSessionUpdateRequest
                    {
                        Status = "completed",
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (McpUnauthorizedException exception)
            {
                WriteExpiredAuthenticationMessage("closing the previous session log", exception.Message);
            }
        }

        _agentSession = await _chatAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        _shouldInjectSystemPrompt = true;
        try
        {
            var context = await _hostedAgent.SessionLog.BootstrapAsync(
                new SessionLogBootstrapRequest
                {
                    Model = _settings.ModelId,
                    SessionIdSuffix = "cli-chat",
                    Status = "in_progress",
                    Title = _settings.SessionTitle,
                    Workspace = CreateWorkspaceInfo(),
                },
                cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Started session {context.SessionId}.");
            Console.WriteLine();
        }
        catch (McpUnauthorizedException exception)
        {
            WriteExpiredAuthenticationMessage("starting the session log", exception.Message);
        }
    }

    private async Task TryCompleteSessionAsync()
    {
        if (_hostedAgent.SessionLog.Context is null)
            return;

        try
        {
            await _hostedAgent.SessionLog.UpdateSessionAsync(
                new SessionLogSessionUpdateRequest
                {
                    Status = "completed",
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (McpUnauthorizedException exception)
        {
            WriteExpiredAuthenticationMessage("closing the session log cleanly", exception.Message);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"warning> Failed to close the session log cleanly: {exception.Message}");
            Console.Error.WriteLine();
        }
    }

    private WorkspaceInfoDto CreateWorkspaceInfo()
    {
        var workspaceDirectory = new DirectoryInfo(_settings.WorkspacePath);
        return new WorkspaceInfoDto
        {
            Project = string.IsNullOrWhiteSpace(workspaceDirectory.Name)
                ? _settings.WorkspacePath
                : workspaceDirectory.Name,
            TargetFramework = "net9.0",
        };
    }

    private IReadOnlyList<ChatMessage> CreateInputMessages(string input)
    {
        if (_shouldInjectSystemPrompt && !string.IsNullOrWhiteSpace(_settings.SystemPrompt))
        {
            return
            [
                new ChatMessage(ChatRole.System, SampleHostSettings.BuildSystemPrompt(_settings.SystemPrompt, _verbosity)),
                new ChatMessage(ChatRole.User, input),
            ];
        }

        return [new ChatMessage(ChatRole.User, input)];
    }

    private static IChatClient CreateChatClient(SampleHostSettings settings)
    {
        var options = new OpenAIClientOptions
        {
            NetworkTimeout = settings.ModelNetworkTimeout,
            RetryPolicy = new ClientRetryPolicy(settings.ModelMaxRetries),
        };
        var chatClient = new OpenAIChatClient(
            settings.ModelId,
            new ApiKeyCredential(settings.ModelApiKey),
            options);
        return chatClient.AsIChatClient();
    }

    private static string BuildTurnTitle(string input)
    {
        const int maximumLength = 80;
        var singleLine = input.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= maximumLength
            ? singleLine
            : $"{singleLine[..maximumLength].TrimEnd()}...";
    }

    private static int? ConvertTokenCount(long? tokenCount) =>
        tokenCount is > int.MaxValue or < int.MinValue
            ? null
            : tokenCount is null
                ? null
                : (int)tokenCount.Value;

    private static string ExtractResponseText(AgentResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!string.IsNullOrWhiteSpace(response.Text))
            return response.Text.Trim();

        var messageText = string.Join(
            Environment.NewLine + Environment.NewLine,
            response.Messages
                .Select(static message => message.Text?.Trim())
                .Where(static text => !string.IsNullOrWhiteSpace(text)));

        return string.IsNullOrWhiteSpace(messageText)
            ? "(The model returned no text response.)"
            : messageText;
    }

    private string BuildConsolePrompt() => $"{BuildConsolePromptLabel()} {_powerShellCurrentLocation}> ";

    private string BuildConsolePromptLabel() => $"{BuildConsolePromptAgentName()} [{_verbosity}]";

    private string BuildConsolePromptAgentName()
    {
        if (!string.IsNullOrWhiteSpace(_settings.AgentName))
            return _settings.AgentName.Trim();

        if (!string.IsNullOrWhiteSpace(_settings.ModelId))
            return _settings.ModelId.Trim();

        return "PS";
    }

    private void ClosePowerShellSession()
    {
        if (string.IsNullOrWhiteSpace(_powerShellSessionId))
            return;

        _hostedAgent.PowerShellSessions.CloseSession(_powerShellSessionId);
        _powerShellSessionId = null;
        _powerShellCurrentLocation = _settings.WorkspacePath;
    }

    private static void DrainPendingConsoleInput()
    {
        if (Console.IsInputRedirected)
            return;

        while (Console.KeyAvailable)
            _ = Console.ReadKey(intercept: true);
    }

    private void EnsurePowerShellSession()
    {
        if (!string.IsNullOrWhiteSpace(_powerShellSessionId))
            return;

        var session = _hostedAgent.PowerShellSessions.CreateSession(_settings.WorkspacePath);
        if (!session.Success || string.IsNullOrWhiteSpace(session.SessionId))
        {
            throw new InvalidOperationException(
                $"Failed to create the sample host PowerShell session: {session.ErrorMessage ?? "Unknown error."}");
        }

        _powerShellSessionId = session.SessionId;
        _powerShellCurrentLocation = session.CurrentLocation ?? _settings.WorkspacePath;
    }

    private static string? ReadConsoleLine(string prompt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        Console.Write(prompt);
        if (Console.IsInputRedirected)
            return Console.ReadLine();

        var buffer = new StringBuilder();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return buffer.ToString();

                case ConsoleKey.Backspace:
                    if (buffer.Length == 0)
                        continue;

                    buffer.Length--;
                    Console.Write("\b \b");
                    continue;

                default:
                    if (char.IsControl(key.KeyChar))
                        continue;

                    buffer.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                    continue;
            }
        }
    }

    private static bool TryGetDirectPowerShellCommand(string input, out string command)
    {
        if (input.StartsWith("! ", StringComparison.Ordinal))
        {
            command = input[2..];
            return !string.IsNullOrWhiteSpace(command);
        }

        command = string.Empty;
        return false;
    }

    private static void WritePowerShellResult(PowerShellSessionCommandResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var wroteAnyOutput = false;
        wroteAnyOutput |= WriteConsoleBlock(result.Output, Console.Out);
        wroteAnyOutput |= WriteConsoleBlock(result.InformationOutput, Console.Out);
        wroteAnyOutput |= WriteConsoleBlock(result.WarningOutput, Console.Out);
        wroteAnyOutput |= WriteConsoleBlock(result.VerboseOutput, Console.Out);
        wroteAnyOutput |= WriteConsoleBlock(result.DebugOutput, Console.Out);
        wroteAnyOutput |= WriteConsoleBlock(result.ErrorOutput, Console.Error);

        if (wroteAnyOutput)
            Console.WriteLine();
    }

    private static bool WriteConsoleBlock(string? value, TextWriter writer)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        writer.WriteLine(value.TrimEnd());
        return true;
    }

    private void WriteBanner()
    {
        Console.WriteLine("MCP Agent sample host");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine($"MCP server : {_settings.BaseUrl}");
        Console.WriteLine($"Workspace  : {_settings.WorkspacePath}");
        Console.WriteLine($"Model      : {_settings.ModelId}");
        Console.WriteLine($"SourceType : {_settings.SourceType}");
        Console.WriteLine($"Tools      : {string.Join(", ", _hostedAgent.Registration.Tools.Select(static tool => tool.Name))}");
        Console.WriteLine();
    }

    private void WriteHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  /help    Show this help text.");
        Console.WriteLine("  /tools   List the MCP-backed tools attached to the hosted agent.");
        Console.WriteLine("  /session Show the current MCP session-log identifier.");
        Console.WriteLine("  /v N     Set verbosity level (1=concise, 2=balanced, 3=detailed).");
        Console.WriteLine("  /new     Start a fresh conversation and session log.");
        Console.WriteLine("  /exit    Exit the sample host.");
        Console.WriteLine();
        Console.WriteLine("Prompt behavior:");
        Console.WriteLine("  - The prompt shows AgentName [verbosity] <location>>");
        Console.WriteLine("  - Prefix a line with ! to run it directly in the local PowerShell session.");
        Console.WriteLine("  - Any line without ! is sent to the hosted agent as a normal chat prompt.");
        Console.WriteLine();
        Console.WriteLine("You can also execute a single prompt non-interactively:");
        Console.WriteLine(@"  dotnet run --project src\McpServer.McpAgent.SampleHost -c Debug -- ""List open TODO items""");
        Console.WriteLine(@"  dotnet run --project src\McpServer.McpAgent.SampleHost -c Debug -- ""! Get-Location""");
        Console.WriteLine();
    }

    private void WriteToolList()
    {
        Console.WriteLine("Attached MCP tools:");
        foreach (var tool in _hostedAgent.Registration.Tools)
            Console.WriteLine($"  - {tool.Name}");
        Console.WriteLine();
    }

    private void WriteSessionSummary()
    {
        var sessionId = _hostedAgent.SessionLog.Context?.SessionId ?? "<not started>";
        Console.WriteLine($"Session log : {sessionId}");
        Console.WriteLine($"Agent name  : {_hostedAgent.Name}");
        Console.WriteLine($"Source type : {_hostedAgent.SourceType}");
        Console.WriteLine();
    }

    private static bool TryParseVerbosityLevel(string input, out string verbosity, out string errorMessage)
    {
        verbosity = string.Empty;
        errorMessage = string.Empty;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            errorMessage = "Usage: /v N where N is 1, 2, or 3.";
            return false;
        }

        verbosity = parts[1] switch
        {
            "1" => "concise",
            "2" => "balanced",
            "3" => "detailed",
            _ => string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(verbosity))
            return true;

        errorMessage = $"Unsupported verbosity level '{parts[1]}'. Use 1, 2, or 3.";
        return false;
    }

    private static void WriteExpiredAuthenticationMessage(string operation, string? serverMessage = null)
    {
        Console.Error.WriteLine(
            $"warning> MCP authentication expired while {operation}. Re-read AGENTS-README-FIRST.yaml in the workspace root and restart the sample host to refresh the API key.");
        if (!string.IsNullOrWhiteSpace(serverMessage))
            Console.Error.WriteLine($"warning> Server detail: {serverMessage}");
        Console.Error.WriteLine();
    }
}

internal sealed record SampleHostSettings(
    Uri BaseUrl,
    string? ApiKey,
    string? BearerToken,
    bool RequireAuthentication,
    string WorkspacePath,
    string AgentId,
    string AgentName,
    string AgentDescription,
    string SourceType,
    string ModelId,
    string ModelApiKey,
    TimeSpan ModelNetworkTimeout,
    int ModelMaxRetries,
    string Verbosity,
    string SessionTitle,
    string SystemPrompt)
{
    private const string ApiKeyEnvironmentVariable = "MCP_SERVER_API_KEY";
    private const string BearerTokenEnvironmentVariable = "MCP_SERVER_BEARER_TOKEN";
    private const string BaseUrlEnvironmentVariable = "MCP_SERVER_BASE_URL";
    private const string WorkspacePathEnvironmentVariable = "MCP_SERVER_WORKSPACE_PATH";
    private const string AgentIdEnvironmentVariable = "MCP_AGENT_ID";
    private const string AgentNameEnvironmentVariable = "MCP_AGENT_NAME";
    private const string AgentDescriptionEnvironmentVariable = "MCP_AGENT_DESCRIPTION";
    private const string SourceTypeEnvironmentVariable = "MCP_AGENT_SOURCE_TYPE";
    private const string ModelIdEnvironmentVariable = "OPENAI_MODEL";
    private const string ModelApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    private const string ModelNetworkTimeoutSecondsEnvironmentVariable = "OPENAI_NETWORK_TIMEOUT_SECONDS";
    private const string ModelMaxRetriesEnvironmentVariable = "OPENAI_MAX_RETRIES";
    private const string AlternateModelIdEnvironmentVariable = "MCP_AGENT_MODEL";
    private const string VerbosityEnvironmentVariable = "MCP_AGENT_VERBOSITY";
    private const string SystemPromptEnvironmentVariable = "MCP_AGENT_SYSTEM_PROMPT";

    public static SampleHostSettings Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var marker = MarkerSettings.TryLoad();
        var baseUrl = ResolveBaseUrl(configuration, marker);
        var apiKey = FirstNonEmpty(
            ReadEnvironmentVariable(ApiKeyEnvironmentVariable),
            configuration["McpServer:AgentFramework:ApiKey"],
            marker?.ApiKey);
        var bearerToken = FirstNonEmpty(
            ReadEnvironmentVariable(BearerTokenEnvironmentVariable),
            configuration["McpServer:AgentFramework:BearerToken"]);
        var requireAuthentication = ResolveRequireAuthentication(configuration, apiKey, bearerToken);
        var workspacePath = ResolveWorkspacePath(configuration, marker);
        var modelId = FirstNonEmpty(
            ReadEnvironmentVariable(ModelIdEnvironmentVariable),
            ReadEnvironmentVariable(AlternateModelIdEnvironmentVariable),
            configuration["SampleHost:Model:Id"],
            configuration["VoiceConversation:CopilotModel"],
            "gpt-5.4")!;
        var modelApiKey = ResolveModelApiKey(configuration);
        var modelNetworkTimeout = ResolvePositiveTimeSpanSeconds(
            ReadEnvironmentVariable(ModelNetworkTimeoutSecondsEnvironmentVariable),
            configuration["SampleHost:Model:NetworkTimeoutSeconds"],
            100,
            "OpenAI network timeout");
        var modelMaxRetries = ResolveNonNegativeInt(
            ReadEnvironmentVariable(ModelMaxRetriesEnvironmentVariable),
            configuration["SampleHost:Model:MaxRetries"],
            4,
            "OpenAI max retries");

        if (string.IsNullOrWhiteSpace(modelApiKey))
        {
            throw new InvalidOperationException(
                "Configure the model API key through OPENAI_API_KEY, SampleHost:Model:ApiKey, or VoiceConversation:ModelApiKey before running the sample host.");
        }

        var verbosity = ResolveVerbosity(configuration);

        return new SampleHostSettings(
            baseUrl,
            apiKey,
            bearerToken,
            requireAuthentication,
            workspacePath,
            FirstNonEmpty(
                ReadEnvironmentVariable(AgentIdEnvironmentVariable),
                configuration["McpServer:AgentFramework:AgentId"],
                McpHostedAgentDefaults.DefaultAgentId)!,
            FirstNonEmpty(
                ReadEnvironmentVariable(AgentNameEnvironmentVariable),
                configuration["McpServer:AgentFramework:AgentName"],
                McpHostedAgentDefaults.DefaultAgentName)!,
            FirstNonEmpty(
                ReadEnvironmentVariable(AgentDescriptionEnvironmentVariable),
                configuration["McpServer:AgentFramework:Description"],
                "Interactive console chat host for McpServer.McpAgent.")!,
            FirstNonEmpty(
                ReadEnvironmentVariable(SourceTypeEnvironmentVariable),
                configuration["McpServer:AgentFramework:SourceType"],
                McpHostedAgentDefaults.DefaultSourceType)!,
            modelId,
            modelApiKey,
            modelNetworkTimeout,
            modelMaxRetries,
            verbosity,
            FirstNonEmpty(
                configuration["SampleHost:SessionTitle"],
                "McpServer.McpAgent sample host chat")!,
            FirstNonEmpty(
                ReadEnvironmentVariable(SystemPromptEnvironmentVariable),
                configuration["SampleHost:SystemPrompt"],
                "You are an interactive command-line assistant running inside the McpServer.McpAgent sample host. Be helpful and use the available MCP workflow tools whenever they help you inspect TODOs, session logs, repository context, or other MCP-backed information.")!);
    }

    private static Uri ResolveBaseUrl(IConfiguration configuration, MarkerSettings? marker)
    {
        var configuredBaseUrl = FirstNonEmpty(
            ReadEnvironmentVariable(BaseUrlEnvironmentVariable),
            configuration["McpServer:AgentFramework:BaseUrl"],
            marker?.BaseUrl,
            "http://localhost:7147");

        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUrl))
            return baseUrl;

        throw new InvalidOperationException(
            $"The configured MCP base URL '{configuredBaseUrl}' is not a valid absolute URI.");
    }

    private static bool ResolveRequireAuthentication(
        IConfiguration configuration,
        string? apiKey,
        string? bearerToken)
    {
        if (bool.TryParse(configuration["McpServer:AgentFramework:RequireAuthentication"], out var parsed))
            return parsed;

        return !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace(bearerToken);
    }

    private static string ResolveWorkspacePath(IConfiguration configuration, MarkerSettings? marker)
    {
        return FirstNonEmpty(
            ReadEnvironmentVariable(WorkspacePathEnvironmentVariable),
            configuration["McpServer:AgentFramework:WorkspacePath"],
            marker?.WorkspacePath,
            TryFindWorkspaceRoot(),
            Path.GetFullPath(Environment.CurrentDirectory))!;
    }

    private static string? ResolveModelApiKey(IConfiguration configuration)
    {
        var configuredEnvironmentVariableName = FirstNonEmpty(
            configuration["SampleHost:Model:ApiKeyEnvironmentVariableName"],
            configuration["VoiceConversation:ModelApiKeyEnvironmentVariableName"]);

        var namedEnvironmentApiKey = configuredEnvironmentVariableName is null
            ? null
            : ReadEnvironmentVariable(configuredEnvironmentVariableName);

        return FirstNonEmpty(
            ReadEnvironmentVariable(ModelApiKeyEnvironmentVariable),
            configuration["SampleHost:Model:ApiKey"],
            configuration["VoiceConversation:ModelApiKey"],
            namedEnvironmentApiKey);
    }

    private static string ResolveVerbosity(IConfiguration configuration)
    {
        var configured = FirstNonEmpty(
            ReadEnvironmentVariable(VerbosityEnvironmentVariable),
            configuration["SampleHost:Verbosity"],
            "concise")!;

        return configured.Trim().ToLowerInvariant() switch
        {
            "concise" => "concise",
            "balanced" => "balanced",
            "detailed" => "detailed",
            _ => throw new InvalidOperationException(
                $"Unsupported sample host verbosity '{configured}'. Use concise, balanced, or detailed."),
        };
    }

    private static TimeSpan ResolvePositiveTimeSpanSeconds(
        string? environmentValue,
        string? configurationValue,
        int defaultSeconds,
        string settingName)
    {
        var configured = FirstNonEmpty(environmentValue, configurationValue);
        if (configured is null)
            return TimeSpan.FromSeconds(defaultSeconds);

        if (!int.TryParse(configured, out var seconds) || seconds <= 0)
        {
            throw new InvalidOperationException(
                $"{settingName} must be a positive integer number of seconds, but was '{configured}'.");
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static int ResolveNonNegativeInt(
        string? environmentValue,
        string? configurationValue,
        int defaultValue,
        string settingName)
    {
        var configured = FirstNonEmpty(environmentValue, configurationValue);
        if (configured is null)
            return defaultValue;

        if (!int.TryParse(configured, out var value) || value < 0)
        {
            throw new InvalidOperationException(
                $"{settingName} must be a non-negative integer, but was '{configured}'.");
        }

        return value;
    }

    internal static string BuildSystemPrompt(string basePrompt, string verbosity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePrompt);

        var sb = new StringBuilder(basePrompt.Trim());
        sb.AppendLine();
        sb.AppendLine();
        sb.Append("Response verbosity: ");
        sb.Append(verbosity);
        sb.AppendLine(".");
        sb.AppendLine(verbosity switch
        {
            "concise" => "Keep answers short by default. Prefer brief paragraphs or bullets and avoid extra detail unless the user asks for it.",
            "balanced" => "Give enough detail to be useful while staying focused. Include key reasoning and next steps without over-explaining.",
            "detailed" => "Be thorough and explicit. Include important context, reasoning, and follow-up details when they help the user.",
            _ => throw new InvalidOperationException($"Unsupported sample host verbosity '{verbosity}'."),
        });

        return sb.ToString();
    }

    private static string? TryFindWorkspaceRoot()
    {
        var marker = MarkerSettings.TryLoad();
        if (!string.IsNullOrWhiteSpace(marker?.WorkspacePath))
            return marker.WorkspacePath;

        foreach (var root in EnumerateSearchRoots())
        {
            var current = root;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "McpServer.sln")))
                    return current;

                var parent = Directory.GetParent(current);
                if (parent is null)
                    break;

                current = parent.FullName;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        yield return Path.GetFullPath(Environment.CurrentDirectory);
        yield return Path.GetFullPath(AppContext.BaseDirectory);
    }

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string? ReadEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal sealed record MarkerSettings(string MarkerPath, string? BaseUrl, string? ApiKey, string? WorkspacePath)
{
    public static MarkerSettings? TryLoad()
    {
        foreach (var root in new[]
                 {
                     Path.GetFullPath(Environment.CurrentDirectory),
                     Path.GetFullPath(AppContext.BaseDirectory),
                 })
        {
            var current = root;
            while (!string.IsNullOrWhiteSpace(current))
            {
                var candidate = Path.Combine(current, "AGENTS-README-FIRST.yaml");
                if (File.Exists(candidate))
                    return Parse(candidate);

                var parent = Directory.GetParent(current);
                if (parent is null)
                    break;

                current = parent.FullName;
            }
        }

        return null;
    }

    private static MarkerSettings Parse(string path)
    {
        string? baseUrl = null;
        string? apiKey = null;
        string? workspacePath = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (TryReadScalar(line, "baseUrl:", out var baseUrlValue))
                baseUrl = baseUrlValue;
            else if (TryReadScalar(line, "apiKey:", out var apiKeyValue))
                apiKey = apiKeyValue;
            else if (TryReadScalar(line, "workspacePath:", out var workspacePathValue))
                workspacePath = workspacePathValue;
        }

        return new MarkerSettings(path, baseUrl, apiKey, workspacePath);
    }

    private static bool TryReadScalar(string line, string prefix, out string? value)
    {
        value = null;
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        value = line[prefix.Length..].Trim();
        return true;
    }
}

