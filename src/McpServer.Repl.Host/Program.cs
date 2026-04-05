// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Host application entry point
// FR-MCP-REPL-002: REPL Lifecycle Management - Host startup and command routing
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service registration and composition root
// TR-MCP-REPL-003: Command Loop Lifecycle - Interactive and agent STDIO mode selection
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes
// TEST-MCP-REPL-013: REPL host terminates gracefully on EOF or exit command

using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using McpServer.Repl.Host;
using McpServer.Client;

var rootCommand = new RootCommand("MCP Server REPL Host");

var agentStdioCommand = new Command("--agent-stdio", "Run in agent STDIO mode for MCP protocol communication");
agentStdioCommand.SetHandler(async (context) =>
{
    var host = CreateHost();
    var agentStdioHandler = host.Services.GetRequiredService<AgentStdioHandler>();
    await agentStdioHandler.RunAsync(context.GetCancellationToken());
});

var interactiveCommand = new Command("--interactive", "Run in interactive REPL mode");
interactiveCommand.SetHandler(async (context) =>
{
    var host = CreateHost();
    var interactiveHandler = host.Services.GetRequiredService<InteractiveHandler>();
    await interactiveHandler.RunAsync(context.GetCancellationToken());
});

rootCommand.AddCommand(agentStdioCommand);
rootCommand.AddCommand(interactiveCommand);

rootCommand.SetHandler(() =>
{
    Console.WriteLine("MCP Server REPL Host");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  mcpserver-repl [options]");
    Console.WriteLine("  mcpserver-repl [command]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --version              Show version information");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  --interactive          Run in interactive REPL mode");
    Console.WriteLine("  --agent-stdio          Run in agent STDIO mode for MCP protocol communication");
    Console.WriteLine();
});

return await rootCommand.InvokeAsync(args);

static IHost CreateHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddReplCoreServices();
            
            services.AddSingleton(sp =>
            {
                var serverUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL") ?? "http://localhost:7147";
                var options = new McpServerClientOptions
                {
                    BaseUrl = new Uri(serverUrl)
                };
                var httpClient = new HttpClient();
                return new McpServerClient(httpClient, options);
            });
            
            services.AddTransient<AgentStdioHandler>();
            services.AddTransient<LoginHandler>();
            services.AddTransient<InteractiveHandler>();
        })
        .Build();
}
