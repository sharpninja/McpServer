using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using McpServer.Repl.Host;
using McpServer.Client;

var versionOption = new Option<bool>("--version", "Display version information");

var rootCommand = new RootCommand("MCP Server REPL Host");
rootCommand.AddOption(versionOption);

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

rootCommand.SetHandler((bool showVersion) =>
{
    if (showVersion)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "6.0.0";
        Console.WriteLine($"mcpserver-repl version {version}");
        return;
    }
    
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
}, versionOption);

return await rootCommand.InvokeAsync(args);

static IHost CreateHost()
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            services.AddReplCoreServices();
            
            services.AddSingleton(sp =>
            {
                var serverUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL") ?? "http://localhost:5000";
                var options = new McpServerClientOptions
                {
                    BaseUrl = new Uri(serverUrl)
                };
                var httpClient = new HttpClient();
                return new McpServerClient(httpClient, options);
            });
            
            services.AddTransient<AgentStdioHandler>();
            services.AddTransient<InteractiveHandler>();
        })
        .Build();
}
