// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Host application entry point
// FR-MCP-REPL-002: REPL Lifecycle Management - Host startup and command routing
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service registration and composition root
// TR-MCP-REPL-003: Command Loop Lifecycle - Interactive and agent STDIO mode selection
// FR-MCP-REPL-007 / TR-MCP-REPL-008: --workspace-path / --marker-file CLI overrides
// FR-MCP-REPL-008 / TR-MCP-REPL-009: --agent CLI parameter for per-agent isolation + plugin enforcement
// TEST-MCP-REPL-001: REPL host processes well-formed YAML command envelopes
// TEST-MCP-REPL-013: REPL host terminates gracefully on EOF or exit command

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using McpServer.Repl.Host;
using McpServer.Client;
using McpServer.TransactionSecurity.Services;

var rootCommand = new RootCommand("MCP Server REPL Host");

var workspacePathOption = new Option<string?>("--workspace-path", "Override the workspace root used to discover AGENTS-README-FIRST.yaml.");
var markerFileOption = new Option<string?>("--marker-file", "Explicit path to AGENTS-README-FIRST.yaml; bypasses ancestor walking.");
var agentOption = new Option<string?>("--agent", "Agent identifier (e.g. Codex, ClaudeCode, Grok) for per-agent caching, session isolation, and trust state. Must be provided by calling plugins. // FR-MCP-REPL-008 TR-MCP-REPL-009");

var agentStdioCommand = new Command("--agent-stdio", "Run in agent STDIO mode for MCP protocol communication");
agentStdioCommand.AddOption(workspacePathOption);
agentStdioCommand.AddOption(markerFileOption);
agentStdioCommand.AddOption(agentOption);
agentStdioCommand.SetHandler(async (context) =>
{
    var workspacePath = context.ParseResult.GetValueForOption(workspacePathOption);
    var markerFile = context.ParseResult.GetValueForOption(markerFileOption);
    var agent = context.ParseResult.GetValueForOption(agentOption);
    var host = CreateHost(workspacePath, markerFile, agent, suppressConsoleLogging: true);
    var agentStdioHandler = host.Services.GetRequiredService<AgentStdioHandler>();
    await agentStdioHandler.RunAsync(context.GetCancellationToken());
});

var interactiveCommand = new Command("--interactive", "Run in interactive REPL mode");
interactiveCommand.AddOption(workspacePathOption);
interactiveCommand.AddOption(markerFileOption);
interactiveCommand.AddOption(agentOption);
interactiveCommand.SetHandler(async (context) =>
{
    var workspacePath = context.ParseResult.GetValueForOption(workspacePathOption);
    var markerFile = context.ParseResult.GetValueForOption(markerFileOption);
    var agent = context.ParseResult.GetValueForOption(agentOption);
    var host = CreateHost(workspacePath, markerFile, agent, suppressConsoleLogging: false);
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
    Console.WriteLine("Per-command options:");
    Console.WriteLine("  --workspace-path <dir>     Override workspace root used to discover AGENTS-README-FIRST.yaml");
    Console.WriteLine("  --marker-file <path>       Explicit path to AGENTS-README-FIRST.yaml");
    Console.WriteLine("  --agent <name>             Agent identifier (Codex, ClaudeCode, Grok, etc.) for per-agent cache and isolation");
    Console.WriteLine();
});

return await rootCommand.InvokeAsync(args);

static IHost CreateHost(string? workspacePathOverride, string? markerFileOverride, string? agentOverride, bool suppressConsoleLogging)
{
    // Set agent early so the marker resolver and per-agent cache (for Codex vs Claude etc.)
    // can key trust state correctly. This must be done before any resolution.
    if (!string.IsNullOrWhiteSpace(agentOverride))
    {
        MarkerFileClientOptionsResolver.AgentOverride = agentOverride;
    }

    return Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            if (suppressConsoleLogging)
            {
                logging.ClearProviders();
            }
        })
        .ConfigureServices((context, services) =>
        {
            services.AddInProcessTransactionSecurity(context.Configuration);
            services.AddReplCoreServices();

            services.AddSingleton(sp =>
            {
                var ok = MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(
                    workspacePathOverride,
                    markerFileOverride,
                    out var options,
                    out var error,
                    agentOverride);
                if (!ok || options is null)
                {
                    // Fall back to the legacy resolver so the CLI does not crash; the
                    // diagnostic message is forwarded into McpServerClient so any
                    // subsequent EnsureAuthenticated failure can surface the root cause.
                    var legacy = MarkerFileClientOptionsResolver.Resolve();
                    legacy.CredentialDiagnostic = error;
                    options = legacy;
                }
                var httpClient = new HttpClient();
                return new McpServerClient(httpClient, options);
            });

            services.AddTransient<AgentStdioHandler>();
            services.AddTransient<LoginHandler>();
            services.AddTransient<InteractiveHandler>();
        })
        .Build();
}
