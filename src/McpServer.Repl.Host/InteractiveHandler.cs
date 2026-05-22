// FR-MCP-REPL-002: REPL Lifecycle Management - Interactive command loop handler
// TR-MCP-REPL-003: Command Loop Lifecycle - Interactive STDIO processing
// TEST-MCP-REPL-013: REPL host terminates gracefully on EOF or exit command

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
    private readonly LoginHandler _loginHandler;
    private string? _currentWorkspace;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <param name="client">MCP server client.</param>
    /// <param name="loginHandler">Login handler for OIDC authentication.</param>
    public InteractiveHandler(
        ILogger<InteractiveHandler> logger,
        McpServerClient client,
        LoginHandler loginHandler)
    {
        _logger = logger;
        _client = client;
        _loginHandler = loginHandler;
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

        // Attempt login before workspace selection if no valid token is cached
        if (!_loginHandler.IsLoggedIn)
        {
            await _loginHandler.LoginAsync(cancellationToken);
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Authenticated as [bold]{Markup.Escape(_loginHandler.CurrentUser ?? "cached")}[/][/]");
            AnsiConsole.WriteLine();
        }

        await SelectWorkspaceAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Refresh token if expired before showing the menu
                if (_loginHandler.IsLoggedIn || !string.IsNullOrWhiteSpace(_loginHandler.CurrentUser))
                    await _loginHandler.EnsureAuthenticatedAsync(cancellationToken);

                var timeRemaining = _loginHandler.TokenTimeRemaining;
                var tokenInfo = timeRemaining.HasValue ? $" [dim]({timeRemaining.Value.Minutes}m {timeRemaining.Value.Seconds}s)[/]" : "";
                var authStatus = _loginHandler.IsLoggedIn
                    ? $"[cyan]{Markup.Escape(_loginHandler.CurrentUser ?? "authenticated")}[/]{tokenInfo}"
                    : "[dim]not logged in[/]";

                var menuChoices = new List<string>
                {
                    "Bootstrap Session",
                    "Begin Turn",
                    "List TODOs",
                    "Create TODO",
                    "Update TODO",
                    "Ingest Requirements",
                    "List Requirements",
                    "Federation Status",
                    "Federation Push",
                    "Federation Pull",
                    "Switch Workspace",
                };

                menuChoices.Add(_loginHandler.IsLoggedIn ? "Logout" : "Login");
                menuChoices.Add("Exit");

                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"[green]Workspace:[/] [yellow]{_currentWorkspace ?? "none"}[/] | [green]User:[/] {authStatus}\n[green]Select an action:[/]")
                        .PageSize(10)
                        .AddChoices(menuChoices));

                switch (action)
                {
                    case "Bootstrap Session":
                        await BootstrapSessionAsync(cancellationToken);
                        break;
                    case "Begin Turn":
                        await BeginTurnAsync(cancellationToken);
                        break;
                    case "List TODOs":
                        await ListTodosAsync(cancellationToken);
                        break;
                    case "Create TODO":
                        await CreateTodoAsync(cancellationToken);
                        break;
                    case "Update TODO":
                        await UpdateTodoAsync(cancellationToken);
                        break;
                    case "Ingest Requirements":
                        await IngestRequirementsAsync(cancellationToken);
                        break;
                    case "List Requirements":
                        await ListRequirementsAsync(cancellationToken);
                        break;
                    case "Federation Status":
                        await FederationStatusAsync(cancellationToken);
                        break;
                    case "Federation Push":
                        await FederationPushAsync(cancellationToken);
                        break;
                    case "Federation Pull":
                        await FederationPullAsync(cancellationToken);
                        break;
                    case "Switch Workspace":
                        await SelectWorkspaceAsync(cancellationToken);
                        break;
                    case "Login":
                        await _loginHandler.ManualLoginMenuAsync(null, cancellationToken);
                        break;
                    case "Logout":
                        _loginHandler.Logout();
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
        var suffix = AnsiConsole.Ask<string>("Session suffix (e.g., feature-auth):", "dev-session");
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");
        var sessionId = $"{agent}-{timestamp}-{suffix}";

        AnsiConsole.MarkupLine($"[dim]Session ID: {Markup.Escape(sessionId)}[/]");

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
                    RequestId = $"req-{now:yyyyMMddTHHmmssZ}-bootstrap-001",
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
        var turnSlug = AnsiConsole.Ask<string>("Turn slug (e.g., implement-auth):", "turn-001");
        var requestId = $"req-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}-{turnSlug}";

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

    private async Task ListTodosAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        var filterAction = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Filter TODOs:[/]")
                .AddChoices("All", "Not Done", "Done", "By Priority", "By Keyword"));

        string? keyword = null, priority = null;
        bool? done = null;

        switch (filterAction)
        {
            case "Not Done":
                done = false;
                break;
            case "Done":
                done = true;
                break;
            case "By Priority":
                priority = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Priority:")
                        .AddChoices("P0-Critical", "P1-High", "P2-Medium", "P3-Low"));
                break;
            case "By Keyword":
                keyword = AnsiConsole.Ask<string>("Search keyword:");
                break;
        }

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Fetching TODOs...", async ctx =>
                {
                    var result = await _client.Todo.QueryAsync(
                        keyword: keyword, priority: priority, done: done,
                        cancellationToken: cancellationToken);

                    if (result?.Items == null || result.Items.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No TODOs found matching the filter.[/]");
                        return;
                    }

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("[green]ID[/]");
                    table.AddColumn("[green]Title[/]");
                    table.AddColumn("[green]Priority[/]");
                    table.AddColumn("[green]Section[/]");
                    table.AddColumn("[green]Done[/]");

                    foreach (var item in result.Items)
                    {
                        var doneText = item.Done ? "[green]Yes[/]" : "[dim]No[/]";
                        var priorityColor = item.Priority switch
                        {
                            "P0-Critical" => "red",
                            "P1-High" => "yellow",
                            "P2-Medium" => "blue",
                            _ => "dim"
                        };

                        table.AddRow(
                            Markup.Escape(item.Id),
                            Markup.Escape(item.Title),
                            $"[{priorityColor}]{Markup.Escape(item.Priority)}[/]",
                            Markup.Escape(item.Section),
                            doneText);
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"\n[dim]Total: {result.Items.Count} TODO(s)[/]");
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list TODOs");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task UpdateTodoAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        // Fetch all TODOs so the user can pick one
        TodoQueryResult? queryResult = null;
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Fetching TODOs...", async ctx =>
                {
                    queryResult = await _client.Todo.QueryAsync(cancellationToken: cancellationToken);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch TODOs");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return;
        }

        if (queryResult?.Items == null || queryResult.Items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No TODOs found.[/]");
            return;
        }

        var selectedDisplay = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Select a TODO to update:[/]")
                .PageSize(15)
                .AddChoices(queryResult.Items.Select(i =>
                    $"{i.Id} — {i.Title} [{(i.Done ? "Done" : i.Priority)}]")));

        var selectedId = selectedDisplay.Split(" — ")[0];
        var selectedItem = queryResult.Items.FirstOrDefault(i => i.Id == selectedId);
        if (selectedItem is null)
        {
            AnsiConsole.MarkupLine("[red]Could not find selected TODO.[/]");
            return;
        }

        // Show current state
        AnsiConsole.MarkupLine($"[bold blue]Updating:[/] {Markup.Escape(selectedItem.Id)} — {Markup.Escape(selectedItem.Title)}");
        AnsiConsole.MarkupLine($"  [dim]Priority:[/] {Markup.Escape(selectedItem.Priority)}  [dim]Section:[/] {Markup.Escape(selectedItem.Section)}  [dim]Done:[/] {selectedItem.Done}");
        AnsiConsole.WriteLine();

        var fieldToUpdate = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What to update:[/]")
                .AddChoices("Toggle Done", "Change Priority", "Change Title", "Change Section", "Add Note", "Set Estimate", "Cancel"));

        if (fieldToUpdate == "Cancel")
            return;

        var request = new TodoUpdateRequest();

        switch (fieldToUpdate)
        {
            case "Toggle Done":
                request.Done = !selectedItem.Done;
                if (request.Done == true)
                {
                    var summary = AnsiConsole.Ask("Completion summary (optional):", "");
                    if (!string.IsNullOrWhiteSpace(summary))
                        request.DoneSummary = summary;
                    request.CompletedDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                }
                break;
            case "Change Priority":
                request.Priority = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Current: [yellow]{Markup.Escape(selectedItem.Priority)}[/] → New priority:")
                        .AddChoices("P0-Critical", "P1-High", "P2-Medium", "P3-Low"));
                break;
            case "Change Title":
                request.Title = AnsiConsole.Ask("New title:", selectedItem.Title);
                break;
            case "Change Section":
                request.Section = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"Current: [yellow]{Markup.Escape(selectedItem.Section)}[/] → New section:")
                        .AddChoices("Planning", "In-Progress", "Done", "Blocked"));
                break;
            case "Add Note":
                request.Note = AnsiConsole.Ask<string>("Note:");
                break;
            case "Set Estimate":
                request.Estimate = AnsiConsole.Ask<string>("Estimate (e.g., 2h, 1d):");
                break;
        }

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Updating TODO...", async ctx =>
                {
                    var result = await _client.Todo.UpdateAsync(selectedId, request, cancellationToken);

                    if (result.Success && result.Item != null)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] Updated: {Markup.Escape(result.Item.Id)}");

                        var table = new Table();
                        table.AddColumn("Field");
                        table.AddColumn("Value");
                        table.AddRow("ID", Markup.Escape(result.Item.Id));
                        table.AddRow("Title", Markup.Escape(result.Item.Title));
                        table.AddRow("Section", Markup.Escape(result.Item.Section));
                        table.AddRow("Priority", Markup.Escape(result.Item.Priority));
                        table.AddRow("Done", result.Item.Done.ToString());
                        if (!string.IsNullOrWhiteSpace(result.Item.Note))
                            table.AddRow("Note", Markup.Escape(result.Item.Note));
                        if (!string.IsNullOrWhiteSpace(result.Item.Estimate))
                            table.AddRow("Estimate", Markup.Escape(result.Item.Estimate));

                        AnsiConsole.Write(table);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗[/] Failed to update TODO");
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update TODO {TodoId}", selectedId);
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

    private async Task IngestRequirementsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold blue]Ingest Requirements[/]");
        AnsiConsole.MarkupLine("[dim]Provide markdown file paths or paste content for each requirement type.[/]");
        AnsiConsole.MarkupLine("[dim]Leave blank to skip a type. The server parses markdown and upserts FR/TR/TEST/mapping entries.[/]");
        AnsiConsole.WriteLine();

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Ingest mode:[/]")
                .AddChoices("From Files", "From Workspace Defaults", "Paste Markdown", "Cancel"));

        if (mode == "Cancel")
            return;

        var request = new RequirementsIngestRequest();

        if (mode == "From Workspace Defaults")
        {
            var basePath = _currentWorkspace;
            var discovered = DiscoverRequirementsFiles(basePath);

            if (discovered.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No requirements files found in workspace.[/]");
                return;
            }

            // Group discovered files by type
            var frFiles = discovered.Where(d => d.Type == "functional").ToList();
            var trFiles = discovered.Where(d => d.Type == "technical").ToList();
            var testFiles = discovered.Where(d => d.Type == "testing").ToList();
            var mapFiles = discovered.Where(d => d.Type == "mapping").ToList();

            // Show all discovered files
            AnsiConsole.MarkupLine($"[bold]Discovered {discovered.Count} requirements file(s):[/]");
            foreach (var d in discovered)
            {
                var relPath = Path.GetRelativePath(basePath, d.FullPath);
                AnsiConsole.MarkupLine($"  [green]✓[/] [{d.TypeColor}]{Markup.Escape(d.TypeLabel)}[/] {Markup.Escape(relPath)}");
            }
            AnsiConsole.WriteLine();

            // Let user pick which files to ingest when there are multiple per type
            request.FunctionalMarkdown = await SelectAndConcatFilesAsync(frFiles, "Functional (FR)", cancellationToken);
            request.TechnicalMarkdown = await SelectAndConcatFilesAsync(trFiles, "Technical (TR)", cancellationToken);
            request.TestingMarkdown = await SelectAndConcatFilesAsync(testFiles, "Testing (TEST)", cancellationToken);
            request.MappingMarkdown = await SelectAndConcatFilesAsync(mapFiles, "Mapping", cancellationToken);
        }
        else if (mode == "From Files")
        {
            var frPath = AnsiConsole.Ask("Functional requirements file path (blank to skip):", "");
            var trPath = AnsiConsole.Ask("Technical requirements file path (blank to skip):", "");
            var testPath = AnsiConsole.Ask("Testing requirements file path (blank to skip):", "");
            var mapPath = AnsiConsole.Ask("FR-TR mapping file path (blank to skip):", "");

            if (!string.IsNullOrWhiteSpace(frPath))
            {
                if (!File.Exists(frPath)) { AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(frPath)}[/]"); return; }
                request.FunctionalMarkdown = await File.ReadAllTextAsync(frPath, cancellationToken);
            }
            if (!string.IsNullOrWhiteSpace(trPath))
            {
                if (!File.Exists(trPath)) { AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(trPath)}[/]"); return; }
                request.TechnicalMarkdown = await File.ReadAllTextAsync(trPath, cancellationToken);
            }
            if (!string.IsNullOrWhiteSpace(testPath))
            {
                if (!File.Exists(testPath)) { AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(testPath)}[/]"); return; }
                request.TestingMarkdown = await File.ReadAllTextAsync(testPath, cancellationToken);
            }
            if (!string.IsNullOrWhiteSpace(mapPath))
            {
                if (!File.Exists(mapPath)) { AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(mapPath)}[/]"); return; }
                request.MappingMarkdown = await File.ReadAllTextAsync(mapPath, cancellationToken);
            }
        }
        else // Paste Markdown
        {
            AnsiConsole.MarkupLine("[dim]Paste markdown for each type, then press Enter twice (blank line) to finish.[/]");
            AnsiConsole.WriteLine();

            request.FunctionalMarkdown = ReadMultiline("Functional Requirements (FR)");
            request.TechnicalMarkdown = ReadMultiline("Technical Requirements (TR)");
            request.TestingMarkdown = ReadMultiline("Testing Requirements (TEST)");
            request.MappingMarkdown = ReadMultiline("FR-TR Mapping");
        }

        // Check we have at least something to ingest
        if (string.IsNullOrWhiteSpace(request.FunctionalMarkdown)
            && string.IsNullOrWhiteSpace(request.TechnicalMarkdown)
            && string.IsNullOrWhiteSpace(request.TestingMarkdown)
            && string.IsNullOrWhiteSpace(request.MappingMarkdown))
        {
            AnsiConsole.MarkupLine("[yellow]No content provided. Nothing to ingest.[/]");
            return;
        }

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Ingesting requirements...", async ctx =>
                {
                    var result = await _client.Requirements.IngestAsync(request, cancellationToken);

                    AnsiConsole.MarkupLine("[green]✓ Requirements ingested successfully[/]");
                    AnsiConsole.WriteLine();

                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("[bold]Type[/]");
                    table.AddColumn("[bold]Parsed[/]");
                    table.AddColumn("[bold]Added[/]");
                    table.AddColumn("[bold]Updated[/]");

                    table.AddRow("Functional (FR)",
                        result.FunctionalParsed.ToString(),
                        $"[green]{result.FunctionalAdded}[/]",
                        $"[yellow]{result.FunctionalUpdated}[/]");
                    table.AddRow("Technical (TR)",
                        result.TechnicalParsed.ToString(),
                        $"[green]{result.TechnicalAdded}[/]",
                        $"[yellow]{result.TechnicalUpdated}[/]");
                    table.AddRow("Testing (TEST)",
                        result.TestingParsed.ToString(),
                        $"[green]{result.TestingAdded}[/]",
                        $"[yellow]{result.TestingUpdated}[/]");
                    table.AddRow("Mapping",
                        result.MappingParsed.ToString(),
                        $"[green]{result.MappingAdded}[/]",
                        $"[yellow]{result.MappingUpdated}[/]");

                    AnsiConsole.Write(table);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest requirements");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private static string? ReadMultiline(string label)
    {
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(label)}[/] [dim](blank line to finish, or just Enter to skip):[/]");
        var lines = new List<string>();
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null || line.Length == 0)
                break;
            lines.Add(line);
        }
        return lines.Count > 0 ? string.Join("\n", lines) : null;
    }

    private record DiscoveredRequirementsFile(string FullPath, string Type, string TypeLabel, string TypeColor);

    private static List<DiscoveredRequirementsFile> DiscoverRequirementsFiles(string workspacePath)
    {
        var results = new List<DiscoveredRequirementsFile>();

        // Search directories commonly used for requirements docs
        var searchDirs = new[]
        {
            "docs/Project", "docs/project",
            "docs/Requirements", "docs/requirements",
            "docs", "requirements", "specs",
        };

        // Filename patterns → type classification
        // Order matters: more specific patterns first
        var patterns = new (string[] FilePatterns, string Type, string Label, string Color)[]
        {
            (new[] { "Functional-Requirements", "functional-requirements", "FR.md", "functional.md", "Requirements-FR" },
                "functional", "FR", "green"),
            (new[] { "Technical-Requirements", "technical-requirements", "TR.md", "technical.md", "Requirements-TR" },
                "technical", "TR", "blue"),
            (new[] { "Testing-Requirements", "testing-requirements", "TEST.md", "testing.md", "Requirements-TEST" },
                "testing", "TEST", "yellow"),
            (new[] { "TR-per-FR-Mapping", "FR-TR-mapping", "mapping.md", "Requirements-Mapping", "traceability" },
                "mapping", "Mapping", "cyan"),
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in searchDirs)
        {
            var fullDir = Path.Combine(workspacePath, dir);
            if (!Directory.Exists(fullDir))
                continue;

            foreach (var file in Directory.EnumerateFiles(fullDir, "*.md"))
            {
                var fileName = Path.GetFileName(file);
                if (seen.Contains(file))
                    continue;

                // Check known patterns first
                var matched = false;
                foreach (var (filePatterns, type, label, color) in patterns)
                {
                    if (filePatterns.Any(p => fileName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new DiscoveredRequirementsFile(file, type, label, color));
                        seen.Add(file);
                        matched = true;
                        break;
                    }
                }

                // Also pick up domain-specific requirements files (e.g. Requirements-WebUI.md, Requirements-Director.md)
                if (!matched && fileName.StartsWith("Requirements-", StringComparison.OrdinalIgnoreCase))
                {
                    // Domain-specific requirements default to functional
                    results.Add(new DiscoveredRequirementsFile(file, "functional", "FR (domain)", "green"));
                    seen.Add(file);
                }

                // Also match REPL-Requirements-Summary.md style
                if (!matched && !seen.Contains(file)
                    && fileName.Contains("Requirements", StringComparison.OrdinalIgnoreCase)
                    && fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new DiscoveredRequirementsFile(file, "functional", "FR (misc)", "green"));
                    seen.Add(file);
                }
            }
        }

        return results;
    }

    private static async Task<string?> SelectAndConcatFilesAsync(
        List<DiscoveredRequirementsFile> files,
        string typeLabel,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
            return null;

        IEnumerable<DiscoveredRequirementsFile> selected;

        if (files.Count == 1)
        {
            selected = files;
        }
        else
        {
            // Let user multi-select which files to include
            var choices = files.Select(f => Path.GetFileName(f.FullPath)).ToList();
            var picked = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title($"[green]Select {Markup.Escape(typeLabel)} files to ingest:[/]")
                    .PageSize(10)
                    .AddChoices(choices)
                    .InstructionsText("[dim](Space to toggle, Enter to confirm)[/]"));

            selected = files.Where(f => picked.Contains(Path.GetFileName(f.FullPath)));
        }

        var parts = new List<string>();
        foreach (var file in selected)
        {
            var content = await File.ReadAllTextAsync(file.FullPath, cancellationToken);
            parts.Add(content);
        }

        return parts.Count > 0 ? string.Join("\n\n---\n\n", parts) : null;
    }

    private async Task FederationStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Fetching federation status...", async ctx =>
                {
                    var status = await _client.Federation.GetStatusAsync(cancellationToken);

                    AnsiConsole.MarkupLine($"[bold blue]Federation Status[/]");
                    AnsiConsole.MarkupLine($"  Enabled: {(status.Enabled ? "[green]Yes[/]" : "[dim]No[/]")}");
                    AnsiConsole.MarkupLine($"  Role: [cyan]{Markup.Escape(status.Role)}[/] (configured: {Markup.Escape(status.ConfiguredRole)})");
                    if (!string.IsNullOrWhiteSpace(status.HubBaseUrl))
                        AnsiConsole.MarkupLine($"  Hub: {Markup.Escape(status.HubBaseUrl)}");
                    if (!string.IsNullOrWhiteSpace(status.ProxyId))
                        AnsiConsole.MarkupLine($"  Proxy: {Markup.Escape(status.ProxyId)}");
                    AnsiConsole.MarkupLine($"  Enrolled proxies: {status.ProxyCount}");
                    AnsiConsole.MarkupLine($"  Hosted workspaces: {status.HostedWorkspaceCount}");
                    AnsiConsole.MarkupLine($"  Queue depth: {(status.QueueDepth == 0 ? "[green]0[/]" : $"[yellow]{status.QueueDepth}[/]")}");
                    AnsiConsole.MarkupLine($"  Fanout depth: {(status.FanoutDepth == 0 ? "[green]0[/]" : $"[yellow]{status.FanoutDepth}[/]")}");
                    AnsiConsole.MarkupLine($"  Conflicts: {(status.ConflictCount == 0 ? "[green]0[/]" : $"[red]{status.ConflictCount}[/]")}");
                    AnsiConsole.MarkupLine($"  Stale reads: {Markup.Escape(status.StaleReadStatus)}");
                    AnsiConsole.WriteLine();

                    if (status.Targets.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[dim]No federation targets registered.[/]");
                    }
                    else
                    {
                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn("[green]Name[/]");
                        table.AddColumn("[green]Base URL[/]");
                        table.AddColumn("[green]API Key[/]");
                        table.AddColumn("[green]Default[/]");

                        foreach (var t in status.Targets)
                        {
                            table.AddRow(
                                Markup.Escape(t.Name),
                                Markup.Escape(t.BaseUrl),
                                t.HasApiKey ? "[green]Yes[/]" : "[dim]No[/]",
                                t.IsDefault ? "[green]Yes[/]" : "[dim]No[/]");
                        }

                        AnsiConsole.Write(table);
                    }

                    if (status.WorkspaceRoutes.Count > 0)
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[bold]Workspace Routes:[/]");
                        var routeTable = new Table();
                        routeTable.Border(TableBorder.Rounded);
                        routeTable.AddColumn("[cyan]Workspace Path[/]");
                        routeTable.AddColumn("[cyan]Target[/]");

                        foreach (var r in status.WorkspaceRoutes)
                            routeTable.AddRow(Markup.Escape(r.WorkspacePath), Markup.Escape(r.TargetName));

                        AnsiConsole.Write(routeTable);
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get federation status");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task FederationPushAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("[bold blue]Federation Push[/]");
        AnsiConsole.WriteLine();

        var typeChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What to push:[/]")
                .AddChoices("All", "TODOs Only", "Session Logs Only", "Cancel"));

        if (typeChoice == "Cancel")
            return;

        List<string>? types = typeChoice switch
        {
            "TODOs Only" => ["todos"],
            "Session Logs Only" => ["sessionlogs"],
            _ => null
        };

        try
        {
            await AnsiConsole.Status()
                .StartAsync("Pushing data to federation target...", async ctx =>
                {
                    var result = await _client.Federation.PushAsync(types, cancellationToken);

                    if (result.Failed == 0 && result.Errors.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[green]Push complete:[/] {result.Succeeded} item(s) succeeded, {result.Failed} failed");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Push complete:[/] {result.Succeeded} succeeded, {result.Failed} failed");
                        foreach (var err in result.Errors)
                            AnsiConsole.MarkupLine($"  [red]Error:[/] {Markup.Escape(err)}");
                    }
                });
        }
        catch (McpConflictException)
        {
            AnsiConsole.MarkupLine("[red]Federation is disabled. Enable it first.[/]");
        }
        catch (McpNotFoundException)
        {
            AnsiConsole.MarkupLine("[red]No federation target resolved. Add a target and set it as default first.[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to push federation data");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }

    private async Task FederationPullAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentWorkspace))
        {
            AnsiConsole.MarkupLine("[red]No workspace selected[/]");
            return;
        }

        AnsiConsole.MarkupLine("[bold blue]Federation Pull[/]");
        AnsiConsole.MarkupLine("[dim]Queries local data with federation merge enabled — remote items are included automatically.[/]");
        AnsiConsole.WriteLine();

        var pullChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What to pull:[/]")
                .AddChoices("TODOs", "Session Logs", "Cancel"));

        if (pullChoice == "Cancel")
            return;

        try
        {
            if (pullChoice == "TODOs")
            {
                await AnsiConsole.Status()
                    .StartAsync("Querying TODOs (local + federated)...", async ctx =>
                    {
                        var result = await _client.Todo.QueryAsync(cancellationToken: cancellationToken);

                        if (result?.Items == null || result.Items.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No TODOs found.[/]");
                            return;
                        }

                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn("[green]ID[/]");
                        table.AddColumn("[green]Title[/]");
                        table.AddColumn("[green]Priority[/]");
                        table.AddColumn("[green]Done[/]");

                        foreach (var item in result.Items)
                        {
                            var priorityColor = item.Priority switch
                            {
                                "P0-Critical" => "red",
                                "P1-High" => "yellow",
                                "P2-Medium" => "blue",
                                _ => "dim"
                            };
                            table.AddRow(
                                Markup.Escape(item.Id),
                                Markup.Escape(item.Title),
                                $"[{priorityColor}]{Markup.Escape(item.Priority)}[/]",
                                item.Done ? "[green]Yes[/]" : "[dim]No[/]");
                        }

                        AnsiConsole.Write(table);
                        AnsiConsole.MarkupLine($"\n[dim]Total: {result.TotalCount} TODO(s) (local + federated)[/]");
                    });
            }
            else
            {
                await AnsiConsole.Status()
                    .StartAsync("Querying session logs (local + federated)...", async ctx =>
                    {
                        var result = await _client.SessionLog.QueryAsync(cancellationToken: cancellationToken);

                        if (result?.Items == null || result.Items.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[yellow]No session logs found.[/]");
                            return;
                        }

                        var table = new Table();
                        table.Border(TableBorder.Rounded);
                        table.AddColumn("[green]Session ID[/]");
                        table.AddColumn("[green]Agent[/]");
                        table.AddColumn("[green]Model[/]");
                        table.AddColumn("[green]Status[/]");
                        table.AddColumn("[green]Turns[/]");

                        foreach (var item in result.Items)
                        {
                            table.AddRow(
                                Markup.Escape(item.SessionId ?? ""),
                                Markup.Escape(item.SourceType ?? ""),
                                Markup.Escape(item.Model ?? ""),
                                Markup.Escape(item.Status ?? ""),
                                (item.Turns?.Count ?? 0).ToString());
                        }

                        AnsiConsole.Write(table);
                        AnsiConsole.MarkupLine($"\n[dim]Total: {result.TotalCount} session log(s) (local + federated)[/]");
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pull federation data");
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
                Markup.Escape(fr.Id ?? ""),
                Markup.Escape(fr.Title ?? ""),
                Markup.Escape(body.Length > 50 ? body.Substring(0, 50) + "..." : body));
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
                Markup.Escape(tr.Id ?? ""),
                Markup.Escape(tr.Title ?? ""),
                Markup.Escape(body.Length > 50 ? body.Substring(0, 50) + "..." : body));
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
                Markup.Escape(test.Id ?? ""),
                Markup.Escape(condition.Length > 80 ? condition.Substring(0, 80) + "..." : condition));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {tests.Count} testing requirements[/]");
    }
}
