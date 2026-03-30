using Microsoft.Extensions.Logging;
using McpServer.Client;
using McpServer.Client.Models;
using Spectre.Console;

namespace McpServer.Repl.Host;

/// <summary>
/// Handles interactive REPL mode with a command-line interface.
/// Provides workspace selection, command execution, and status display.
/// </summary>
public class InteractiveHandler
{
    private readonly ILogger<InteractiveHandler> _logger;
    private readonly McpServerClient _client;
    private string? _currentWorkspace;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="client">MCP server client.</param>
    public InteractiveHandler(
        ILogger<InteractiveHandler> logger,
        McpServerClient client)
    {
        _logger = logger;
        _client = client;
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

        AnsiConsole.Write(new FigletText("MCP REPL")
            .LeftJustified()
            .Color(Color.Green));

        AnsiConsole.MarkupLine("[dim]Model Context Protocol - Interactive Mode[/]");
        AnsiConsole.WriteLine();

        await SelectWorkspaceAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[green]Workspace:[/] [yellow]{_currentWorkspace ?? "none"}[/]\n[green]Select an action:[/]")
                        .PageSize(10)
                        .AddChoices(new[]
                        {
                            "Bootstrap Session",
                            "Begin Turn",
                            "Create TODO",
                            "List Requirements",
                            "Switch Workspace",
                            "Exit"
                        }));

                switch (action)
                {
                    case "Bootstrap Session":
                        await BootstrapSessionAsync(cancellationToken);
                        break;
                    case "Begin Turn":
                        await BeginTurnAsync(cancellationToken);
                        break;
                    case "Create TODO":
                        await CreateTodoAsync(cancellationToken);
                        break;
                    case "List Requirements":
                        await ListRequirementsAsync(cancellationToken);
                        break;
                    case "Switch Workspace":
                        await SelectWorkspaceAsync(cancellationToken);
                        break;
                    case "Exit":
                        AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                        return;
                }

                AnsiConsole.WriteLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing interactive command");
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                AnsiConsole.WriteLine();
            }
        }
    }

    private async Task SelectWorkspaceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _client.Workspace.ListAsync(cancellationToken);

            if (result?.Items == null || result.Items.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No workspaces found. Please configure workspaces first.[/]");
                _currentWorkspace = null;
                return;
            }

            var workspacePath = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select a workspace:[/]")
                    .PageSize(10)
                    .AddChoices(result.Items.Select(w => w.WorkspacePath)));

            _currentWorkspace = workspacePath;
            _client.WorkspacePath = workspacePath;

            AnsiConsole.MarkupLine($"[green]Selected workspace:[/] {workspacePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select workspace");
            AnsiConsole.MarkupLine($"[red]Failed to list workspaces:[/] {ex.Message}");
        }
    }

    private async Task BootstrapSessionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold blue]Bootstrap Session[/]");
        AnsiConsole.WriteLine();

        var agent = AnsiConsole.Ask<string>("Agent name:", "Tonkotsu");
        var sessionId = AnsiConsole.Ask<string>("Session ID (leave empty for auto):", string.Empty);
        
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = $"session-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        }

        var model = AnsiConsole.Ask<string>("Model:", "claude-3-5-sonnet-20241022");
        var purpose = AnsiConsole.Ask<string>("Purpose:", "Development session");

        var now = DateTimeOffset.UtcNow;
        var sessionLog = new UnifiedSessionLogDto
        {
            SessionId = sessionId,
            SourceType = agent,
            Model = model,
            Started = now.ToString("o"),
            LastUpdated = now.ToString("o"),
            Status = "in_progress",
            Turns = new List<UnifiedRequestEntryDto>
            {
                new UnifiedRequestEntryDto
                {
                    RequestId = $"req-{now:yyyyMMdd-HHmmss}",
                    Timestamp = now.ToString("o"),
                    Interpretation = "Session bootstrap",
                    Response = purpose,
                    Status = "success",
                    Actions = new List<UnifiedActionDto>
                    {
                        new UnifiedActionDto
                        {
                            Type = "session_start",
                            Status = "success",
                            Description = "Session initialized"
                        }
                    }
                }
            }
        };

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Submitting session log...", async ctx =>
                {
                    var result = await _client.SessionLog.SubmitAsync(sessionLog, cancellationToken);
                    
                    if (result != null && result.Id > 0)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Session created: {sessionId}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to create session");
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap session");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task BeginTurnAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold blue]Begin Turn[/]");
        AnsiConsole.WriteLine();

        var agent = AnsiConsole.Ask<string>("Agent name:", "Tonkotsu");
        var sessionId = AnsiConsole.Ask<string>("Session ID:");
        var requestId = AnsiConsole.Ask<string>("Request ID (leave empty for auto):", string.Empty);
        
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = $"req-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        }

        var interpretation = AnsiConsole.Ask<string>("Interpretation:", "User request");
        var response = AnsiConsole.Ask<string>("Response:", "Processing...");

        var now = DateTimeOffset.UtcNow;
        var turn = new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Timestamp = now.ToString("o"),
            Interpretation = interpretation,
            Response = response,
            Status = "in_progress",
            Actions = new List<UnifiedActionDto>
            {
                new UnifiedActionDto
                {
                    Type = "turn_start",
                    Status = "success",
                    Description = "Turn started"
                }
            }
        };

        var sessionLog = new UnifiedSessionLogDto
        {
            SessionId = sessionId,
            SourceType = agent,
            Model = "claude-3-5-sonnet-20241022",
            Started = now.ToString("o"),
            LastUpdated = now.ToString("o"),
            Turns = new List<UnifiedRequestEntryDto> { turn }
        };

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Submitting turn...", async ctx =>
                {
                    var result = await _client.SessionLog.SubmitAsync(sessionLog, cancellationToken);
                    
                    if (result != null && result.Id > 0)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Turn created: {requestId}");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to create turn");
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin turn");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task CreateTodoAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold blue]Create TODO[/]");
        AnsiConsole.WriteLine();

        var id = AnsiConsole.Ask<string>("TODO ID (e.g., IMPL-MCP-001):");
        var title = AnsiConsole.Ask<string>("Title:");
        var section = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Section:")
                .AddChoices(new[] { "Planning", "In-Progress", "Done", "Blocked" }));
        
        var priority = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Priority:")
                .AddChoices(new[] { "P0-Critical", "P1-High", "P2-Medium", "P3-Low" }));

        var estimate = AnsiConsole.Ask<string>("Estimate (e.g., 2h, 1d):", "");
        var description = AnsiConsole.Ask<string>("Description:", "");

        var request = new TodoCreateRequest
        {
            Id = id,
            Title = title,
            Section = section,
            Priority = priority,
            Estimate = string.IsNullOrWhiteSpace(estimate) ? null : estimate,
            Description = string.IsNullOrWhiteSpace(description) ? null : new List<string> { description }
        };

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Creating TODO...", async ctx =>
                {
                    var result = await _client.Todo.CreateAsync(request, cancellationToken);
                    
                    if (result.Success && result.Item != null)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] TODO created: {result.Item.Id}");
                        
                        var table = new Table();
                        table.AddColumn("Field");
                        table.AddColumn("Value");
                        table.AddRow("ID", result.Item.Id ?? "");
                        table.AddRow("Title", result.Item.Title ?? "");
                        table.AddRow("Section", result.Item.Section ?? "");
                        table.AddRow("Priority", result.Item.Priority ?? "");
                        table.AddRow("Done", result.Item.Done.ToString());
                        
                        AnsiConsole.Write(table);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to create TODO");
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create TODO");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task ListRequirementsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        var requirementType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Select requirement type:[/]")
                .AddChoices(new[] { "Functional (FR)", "Technical (TR)", "Testing (TEST)" }));

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Fetching requirements...", async ctx =>
                {
                    if (requirementType.StartsWith("Functional"))
                    {
                        var frs = await _client.Requirements.ListFrAsync(cancellationToken);
                        DisplayFunctionalRequirements(frs);
                    }
                    else if (requirementType.StartsWith("Technical"))
                    {
                        var trs = await _client.Requirements.ListTrAsync(cancellationToken);
                        DisplayTechnicalRequirements(trs);
                    }
                    else
                    {
                        var tests = await _client.Requirements.ListTestAsync(cancellationToken);
                        DisplayTestingRequirements(tests);
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list requirements");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private void DisplayFunctionalRequirements(IReadOnlyList<FrEntry> frs)
    {
        if (frs == null || frs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No functional requirements found[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[green]ID[/]");
        table.AddColumn("[green]Title[/]");
        table.AddColumn("[green]Body[/]");

        foreach (var fr in frs)
        {
            var body = fr.Body ?? "";
            table.AddRow(
                fr.Id ?? "",
                fr.Title ?? "",
                body.Length > 50 ? body.Substring(0, 50) + "..." : body);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {frs.Count} functional requirements[/]");
    }

    private void DisplayTechnicalRequirements(IReadOnlyList<TrEntry> trs)
    {
        if (trs == null || trs.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No technical requirements found[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[blue]ID[/]");
        table.AddColumn("[blue]Title[/]");
        table.AddColumn("[blue]Body[/]");

        foreach (var tr in trs)
        {
            var body = tr.Body ?? "";
            table.AddRow(
                tr.Id ?? "",
                tr.Title ?? "",
                body.Length > 50 ? body.Substring(0, 50) + "..." : body);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {trs.Count} technical requirements[/]");
    }

    private void DisplayTestingRequirements(IReadOnlyList<TestEntry> tests)
    {
        if (tests == null || tests.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No testing requirements found[/]");
            return;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]ID[/]");
        table.AddColumn("[yellow]Condition[/]");

        foreach (var test in tests)
        {
            var condition = test.Condition ?? "";
            table.AddRow(
                test.Id ?? "",
                condition.Length > 80 ? condition.Substring(0, 80) + "..." : condition);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {tests.Count} testing requirements[/]");
    }
}
