using Microsoft.Extensions.Logging;
using McpServer.Repl.Core;
using Spectre.Console;

namespace McpServer.Repl.Host;

/// <summary>
/// Handles interactive REPL mode with a command-line interface.
/// Provides workspace selection, command execution, and status display.
/// </summary>
public class InteractiveHandler
{
    private readonly ILogger<InteractiveHandler> _logger;
    private readonly IReplProtocol _protocol;
    private readonly IWorkspaceSelector _workspaceSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="protocol">REPL protocol implementation.</param>
    /// <param name="workspaceSelector">Workspace selection service.</param>
    public InteractiveHandler(
        ILogger<InteractiveHandler> logger,
        IReplProtocol protocol,
        IWorkspaceSelector workspaceSelector)
    {
        _logger = logger;
        _protocol = protocol;
        _workspaceSelector = workspaceSelector;
    }

    /// <summary>
    /// Runs the interactive REPL loop, prompting for commands and displaying results.
    /// Continues until the user exits or a cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting interactive REPL mode");

        AnsiConsole.MarkupLine("[bold green]MCP Server REPL - Interactive Mode[/]");
        AnsiConsole.MarkupLine("[dim]Type 'help' for available commands, 'exit' to quit[/]");
        AnsiConsole.WriteLine();

        await Task.CompletedTask;

        AnsiConsole.MarkupLine("[yellow]Interactive mode is not yet implemented[/]");
    }
}
