using McpServer.McpAgent.PowerShellSessions;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IAgentSessionLogWorkflow = McpServer.McpAgent.SessionLog.ISessionLogWorkflow;
using IAgentTodoWorkflow = McpServer.McpAgent.Todo.ITodoWorkflow;
using IReplSessionLogWorkflow = McpServer.Repl.Core.ISessionLogWorkflow;

namespace McpServer.McpAgent.Hosting;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Hosted-agent wrapper that exposes the configured MCP transport
/// client plus the built-in workflow services and local PowerShell-session tool surface.
/// </summary>
public sealed class McpHostedAgent : IMcpHostedAgent
{
    private readonly ChatClientAgentOptions _agentOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpAgentOptions _options;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new scaffolded hosted agent wrapper around the configured MCP transport client.
    /// </summary>
    /// <param name="client">The transport client registered for the hosted agent scaffold.</param>
    /// <param name="identifiers">Canonical identifier helpers bound to the hosted agent source type.</param>
    /// <param name="agentOptions">The projected <c>Microsoft.Agents.AI</c> metadata for the hosted agent.</param>
    /// <param name="options">The configured scaffold options for the hosted agent.</param>
    /// <param name="sessionLog">The session-log workflow service bound to this agent instance.</param>
    /// <param name="todo">The TODO workflow service bound to this agent instance.</param>
    /// <param name="requirements">The REPL-backed requirements workflow for FR/TR/TEST operations.</param>
    /// <param name="clientPassthrough">The generic client passthrough for dynamic sub-client method invocation.</param>
    /// <param name="replSessionLog">The REPL-backed session-log workflow for history queries.</param>
    /// <param name="serviceProvider">The service provider used to create Agent Framework wrappers around the workflows.</param>
    public McpHostedAgent(
        McpServerClient client,
        IMcpSessionIdentifierFactory identifiers,
        ChatClientAgentOptions agentOptions,
        IOptions<McpAgentOptions> options,
        IAgentSessionLogWorkflow sessionLog,
        IAgentTodoWorkflow todo,
        IRequirementsWorkflow requirements,
        IGenericClientPassthrough clientPassthrough,
        IReplSessionLogWorkflow replSessionLog,
        IServiceProvider serviceProvider)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Identifiers = identifiers ?? throw new ArgumentNullException(nameof(identifiers));
        ArgumentNullException.ThrowIfNull(agentOptions);
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        SessionLog = sessionLog ?? throw new ArgumentNullException(nameof(sessionLog));
        Todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _agentOptions = agentOptions.Clone();
        _loggerFactory = ResolveLoggerFactory();
        PowerShellSessions = new HostedPowerShellSessionManager(_loggerFactory.CreateLogger<HostedPowerShellSessionManager>());

        var toolAdapter = new McpHostedAgentToolAdapter(
            Client, SessionLog, Todo, PowerShellSessions,
            requirements ?? throw new ArgumentNullException(nameof(requirements)),
            clientPassthrough ?? throw new ArgumentNullException(nameof(clientPassthrough)),
            replSessionLog ?? throw new ArgumentNullException(nameof(replSessionLog)),
            _options);
        var functions = toolAdapter.CreateFunctions();
        Registration = new McpHostedAgentRegistration(
            _agentOptions,
            functions,
            chatClient => new ChatClientAgent(
                chatClient,
                _agentOptions.Clone(),
                _loggerFactory,
                _serviceProvider),
            CreateRunOptionsCore);
    }

    /// <inheritdoc />
    public McpHostedAgentRegistration Registration { get; }

    /// <inheritdoc />
    public string Name => _options.AgentName;

    /// <inheritdoc />
    public string SourceType => _options.SourceType;

    /// <inheritdoc />
    public McpAgentExecutionProfile ExecutionProfile => _options.ExecutionProfile;

    /// <inheritdoc />
    public IMcpSessionIdentifierFactory Identifiers { get; }

    /// <inheritdoc />
    public ChatClientAgentOptions AgentOptions => _agentOptions.Clone();

    /// <inheritdoc />
    public McpServerClient Client { get; }

    /// <inheritdoc />
    public IAgentSessionLogWorkflow SessionLog { get; }

    /// <inheritdoc />
    public IAgentTodoWorkflow Todo { get; }

    /// <inheritdoc />
    public IHostedPowerShellSessionManager PowerShellSessions { get; }

    /// <inheritdoc />
    public ChatClientAgent CreateChatClientAgent(IChatClient chatClient) =>
        Registration.CreateChatClientAgent(chatClient);

    /// <inheritdoc />
    public QBAgentRuntime CreateAcidTightlyCoupledRuntime(
        IChatClient chatClient,
        ChatClientAgentRunOptions? baseOptions = null)
    {
        if (_options.ExecutionProfile != McpAgentExecutionProfile.AcidTightlyCoupled)
        {
            throw new InvalidOperationException(
                "CreateAcidTightlyCoupledRuntime requires McpAgentOptions.UseAcidTightlyCoupledProfile().");
        }

        var agent = CreateChatClientAgent(chatClient);
        var runOptions = CreateRunOptions(baseOptions);
        var toolNames = runOptions.ChatOptions?.Tools?
            .Select(static tool => tool.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!)
            .ToArray() ?? [];

        return new QBAgentRuntime(
            agent,
            runOptions,
            QBAgentDefinition.Instance,
            toolNames,
            ExecuteQuadBrainCodingTaskAsync);
    }

    /// <inheritdoc />
    public ChatClientAgentRunOptions CreateRunOptions(ChatClientAgentRunOptions? baseOptions = null) =>
        Registration.CreateRunOptions(baseOptions);

    /// <inheritdoc />
    public Task<QuadBrainOrchestrationResponse> ExecuteQuadBrainCodingTaskAsync(
        McpQuadBrainCodingAgentRequest request,
        CancellationToken cancellationToken = default) =>
        McpQuadBrainCodingAgentRouter.ExecuteAsync(
            Client,
            _options,
            request ?? throw new ArgumentNullException(nameof(request)),
            cancellationToken);

    private ChatClientAgentRunOptions CreateRunOptionsCore(ChatClientAgentRunOptions? baseOptions)
    {
        var runOptions = CloneRunOptions(baseOptions);
        var existingFactory = runOptions.ChatClientFactory;
        var chatOptions = runOptions.ChatOptions?.Clone() ?? new ChatOptions();
        chatOptions.Tools = _options.ExecutionProfile == McpAgentExecutionProfile.AcidTightlyCoupled
            ? MergeAcidTools(chatOptions.Tools, Registration.Tools)
            : MergeTools(chatOptions.Tools, Registration.Tools);
        chatOptions.ToolMode ??= ChatToolMode.Auto;
        chatOptions.AllowMultipleToolCalls = _options.RequireSerializedToolInvocation
            ? false
            : chatOptions.AllowMultipleToolCalls ?? false;
        runOptions.ChatOptions = chatOptions;
        runOptions.ChatClientFactory = chatClient =>
        {
            var innerClient = existingFactory?.Invoke(chatClient) ?? chatClient;
            return new FunctionInvokingChatClient(innerClient, _loggerFactory, _serviceProvider)
            {
                AllowConcurrentInvocation = false,
            };
        };

        return runOptions;
    }

    private ILoggerFactory ResolveLoggerFactory() =>
        _serviceProvider.GetService(typeof(ILoggerFactory)) as ILoggerFactory ?? NullLoggerFactory.Instance;

    private static ChatClientAgentRunOptions CloneRunOptions(ChatClientAgentRunOptions? options) =>
        options is null
            ? new ChatClientAgentRunOptions()
            : new ChatClientAgentRunOptions
            {
                AdditionalProperties = options.AdditionalProperties,
                AllowBackgroundResponses = options.AllowBackgroundResponses,
                ChatClientFactory = options.ChatClientFactory,
                ChatOptions = options.ChatOptions?.Clone(),
                ResponseFormat = options.ResponseFormat,
            };

    private static IList<AITool> MergeTools(IList<AITool>? existingTools, IReadOnlyList<AITool> adapterTools)
    {
        var mergedTools = existingTools is null
            ? new List<AITool>()
            : new List<AITool>(existingTools);
        var knownNames = new HashSet<string>(
            mergedTools.Select(static tool => tool.Name),
            StringComparer.Ordinal);

        foreach (var tool in adapterTools)
        {
            if (knownNames.Add(tool.Name))
                mergedTools.Add(tool);
        }

        return mergedTools;
    }

    private IList<AITool> MergeAcidTools(IList<AITool>? existingTools, IReadOnlyList<AITool> adapterTools)
    {
        if (existingTools is { Count: > 0 } && !_options.AllowHostToolsInAcidProfile)
        {
            throw new InvalidOperationException(
                "ACID tightly coupled run options reject caller-supplied host tools by default. " +
                "Set AllowHostToolsInAcidProfile only after those tools have their own transaction and audit contract.");
        }

        var definition = QBAgentDefinition.Instance;
        var approvedAdapterTools = adapterTools
            .Where(tool => definition.IsToolAllowed(tool.Name))
            .ToArray();
        var hostTools = _options.AllowHostToolsInAcidProfile ? existingTools : null;

        return MergeTools(hostTools, approvedAdapterTools);
    }
}
