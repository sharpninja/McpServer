using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.QBAgent;
using McpServer.QBAgent.Skills;
using McpServer.QBAgent.Tools;
using McpServer.Support.Mcp.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

// FR-MCP-QBAGENT-001 / FR-MCP-QBOPENAI-001: QBAgent starts in a folder, reads the AGENTS-README-FIRST.yaml
// marker there, binds to QuadBrain (as an OpenAI-compatible model), and runs the Microsoft Agent Framework
// tool loop - executing the tool calls QuadBrain emits. With no marker present it exits gracefully.
var startDirectory = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
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
    using var chatClient = QBAgentChatClientFactory.Create(bound);
    var chatAgent = agent.CreateChatClientAgent(chatClient);
    var runOptions = agent.CreateRunOptions(new ChatClientAgentRunOptions
    {
        ChatOptions = new ChatOptions { Tools = tools },
    });
    var session = await chatAgent.CreateSessionAsync().ConfigureAwait(false);

    var firstTurn = true;
    await QBAgentRunLoop.RunAsync(
        async (prompt, cancellationToken) =>
        {
            var messages = new List<ChatMessage>();
            if (firstTurn && skillPreamble is not null)
            {
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
        Console.Out).ConfigureAwait(false);

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"QBAgent failed to bind to the QuadBrain service: {ex.Message}");
    return 3;
}
