using McpServer.McpAgent;
using McpServer.McpAgent.Hosting;
using McpServer.QBAgent;
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

    await using var provider = services.BuildServiceProvider();
    var agent = provider.GetRequiredService<IMcpHostedAgent>();

    // QuadBrain as the OpenAI model behind the Agent Framework loop; QBAgent executes the emitted tool calls.
    using var chatClient = QBAgentChatClientFactory.Create(bound);
    var chatAgent = agent.CreateChatClientAgent(chatClient);
    var runOptions = agent.CreateRunOptions();
    var session = await chatAgent.CreateSessionAsync().ConfigureAwait(false);

    await QBAgentRunLoop.RunAsync(
        async (prompt, cancellationToken) =>
        {
            var response = await chatAgent.RunAsync(
                [new ChatMessage(ChatRole.User, prompt)],
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
