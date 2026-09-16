using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.QBAgent;
using McpServer.QBAgent.Skills;
using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

// FR-MCP-QBAGENT-001 / FR-MCP-QBOPENAI-001: QBAgent starts in a folder, reads the AGENTS-README-FIRST.yaml
// marker there, binds to QuadBrain (as an OpenAI-compatible model), and runs the Microsoft Agent Framework
// tool loop - executing the tool calls QuadBrain emits. With no marker present it exits gracefully.
var cli = QBAgentCliOptions.Parse(args);
if (cli.ShowVersion)
{
    var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                  ?? typeof(Program).Assembly.GetName().Version?.ToString()
                  ?? "unknown";
    Console.WriteLine(version);
    return 0;
}

var startDirectory = !string.IsNullOrWhiteSpace(cli.StartDirectory)
    ? cli.StartDirectory
    : Directory.GetCurrentDirectory();

var result = QBAgentBootstrapper.Bootstrap(startDirectory);

if (result.Status == QBAgentBootstrapStatus.NoMarker)
{
    Console.WriteLine(result.Message);
    return 0; // graceful exit: no marker, nothing to do, no endpoint contacted
}

if (result.Status == QBAgentBootstrapStatus.InvalidMarker)
{
    Console.Error.WriteLine(result.Message);
    return 2;
}

Console.WriteLine(result.Message);
var bound = result.Options!;

QBAgentSessionLog sessionLog;
try
{
    var logRoot = QBAgentSessionLog.DefaultRoot();
    sessionLog = cli.ResumeSessionId is null
        ? QBAgentSessionLog.CreateNew(logRoot, workspace: bound.WorkspacePath)
        : string.Equals(cli.ResumeSessionId, QBAgentCliOptions.ResumeLatest, StringComparison.OrdinalIgnoreCase)
            ? QBAgentSessionLog.OpenLatest(logRoot)
            : QBAgentSessionLog.Open(cli.ResumeSessionId, logRoot);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

var resumedMessages = sessionLog.ToChatMessages();
var resuming = resumedMessages.Count > 0;

try
{
    var workspacePath = string.IsNullOrWhiteSpace(bound.WorkspacePath) ? startDirectory : bound.WorkspacePath!;
    var skillsRoot = Path.Combine(workspacePath, "skills");

    var services = new ServiceCollection();
    services.AddMcpServerMcpAgent(options =>
    {
        options.BaseUrl = bound.BaseUrl;
        options.ApiKey = bound.ApiKey;
        options.WorkspacePath = bound.WorkspacePath;
        options.AgentId = bound.AgentId;
        options.AgentName = bound.AgentName;
        options.SourceType = bound.SourceType;
        options.Description = bound.Description;
        options.RequireAuthentication = bound.RequireAuthentication;
    });
    services.AddQBAgentTools();
    services.AddQBAgentSkills(skillsRoot);

    await using var provider = services.BuildServiceProvider();
    var agent = provider.GetRequiredService<IMcpHostedAgent>();
    var processRunner = provider.GetRequiredService<IProcessRunner>();
    var skillRegistry = provider.GetRequiredService<ISkillRegistry>();

    // FR-MCP-QBTOOLS-007: register the agent-side external tools (file/powershell/bash/git) plus the skill tools
    // (list_skills/load_skill) so the Agent Framework loop can execute them; inject the skill discovery list.
    using var toolSet = QBAgentExternalToolSurface.Create(
        agent.Client, agent.PowerShellSessions, processRunner, workspacePath, bound.AllowGitPush);
    var tools = new List<AITool>(toolSet.Tools);
    tools.AddRange(new SkillTool(skillRegistry).CreateTools());

    var discovery = string.Join(
        Environment.NewLine,
        skillRegistry.Discover().Select(static s => $"- {s.Name}: {s.Description}"));
    var skillPreamble = discovery.Length == 0
        ? null
        : $"Available skills (call load_skill with the name to load full instructions before acting):{Environment.NewLine}{discovery}";

    // QuadBrain as the OpenAI model behind the Agent Framework loop; QBAgent executes the emitted tool calls.
    Action<string> progress = message =>
    {
        var stamp = QBAgentRunLoop.FormatLocalTimestamp(TimeProvider.System);
        Console.Out.WriteLine($"[{stamp}] {message}");
        Console.Out.Flush();
    };
    var sessionLogBox = new QBAgentSessionLogBox(sessionLog);
    using var chatClient = new QBAgentProgressChatClient(
        new QBAgentSessionLoggingChatClient(
            QBAgentChatClientFactory.Create(bound, httpClient: null, progress, cli.ShowIntent),
            sessionLogBox),
        progress);
    var chatAgent = agent.CreateChatClientAgent(chatClient);
    var runOptions = agent.CreateRunOptions(new ChatClientAgentRunOptions
    {
        ChatOptions = new ChatOptions { Tools = tools },
    });
    var session = await chatAgent.CreateSessionAsync().ConfigureAwait(false);

    var firstTurn = true;
    var loopOptions = new QBAgentRunLoopOptions
    {
        ShowIntent = cli.ShowIntent,
        SessionLog = sessionLog,
        ListOpenTodos = async cancellationToken =>
        {
            var todoResult = await agent.Todo.QueryAsync(done: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return QBAgentOpenTodoList.Format(todoResult, bound.WorkspacePath);
        },
    };
    await QBAgentRunLoop.RunAsync(
        async (prompt, cancellationToken) =>
        {
            var messages = new List<ChatMessage>();
            if (firstTurn)
            {
                if (resuming)
                    messages.AddRange(resumedMessages);
                else if (skillPreamble is not null)
                    messages.Add(new ChatMessage(ChatRole.System, skillPreamble));
                firstTurn = false;
            }

            messages.Add(new ChatMessage(ChatRole.User, prompt));

            var response = await chatAgent.RunAsync(
                messages,
                session,
                runOptions,
                cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(response.Text))
                return response.Text;

            return string.Join(
                Environment.NewLine,
                response.Messages
                    .Select(static message => message.Text)
                    .Where(static text => !string.IsNullOrWhiteSpace(text)));
        },
        Console.In,
        Console.Out,
        resetSession: async cancellationToken =>
        {
            session = await chatAgent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
            firstTurn = true;
            resuming = false;
            resumedMessages = [];
            sessionLog = QBAgentSessionLog.CreateNew(QBAgentSessionLog.DefaultRoot(), workspace: bound.WorkspacePath);
            sessionLogBox.Current = sessionLog;
            loopOptions.SessionLog = sessionLog;
            return $"Started a new session.{Environment.NewLine}Session {sessionLog.SessionId}";
        },
        options: loopOptions).ConfigureAwait(false);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"QBAgent failed to bind to the QuadBrain service: {ex.Message}");
    return 3;
}
