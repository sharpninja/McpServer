using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using McpServer.Repl.Host;

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

return await rootCommand.InvokeAsync(args);

static IHost CreateHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddReplCoreServices();
            services.AddTransient<AgentStdioHandler>();
            services.AddTransient<InteractiveHandler>();
        })
        .Build();
}
