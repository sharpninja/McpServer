using System.CommandLine;
using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using Spectre.Console;
using static McpServer.Director.Commands.CommandHelpers;

namespace McpServer.Director.Commands;

/// <summary>
/// FR-MCP-030: Director CLI commands for health, workspace/agent operations, TODO, and session logs.
/// All server interactions dispatch through UI.Core CQRS handlers.
/// </summary>
internal static class DirectorCommands
{
    private static readonly Option<string?> s_workspaceOption = new("--workspace", "Workspace path (defaults to current directory)");

    /// <summary>Registers all Director commands on the root command.</summary>
    public static void Register(RootCommand root)
    {
        s_workspaceOption.AddAlias("-w");

        root.AddCommand(BuildHealthCommand());
        root.AddCommand(BuildListCommand());
        root.AddCommand(BuildAgentsCommand());
        root.AddCommand(BuildAddCommand());
        root.AddCommand(BuildBanCommand());
        root.AddCommand(BuildUnbanCommand());
        root.AddCommand(BuildDeleteCommand());
        root.AddCommand(BuildValidateCommand());
        root.AddCommand(BuildInitCommand());
        root.AddCommand(BuildTodoCommand());
        root.AddCommand(BuildSessionLogCommand());
    }

    private static Command BuildHealthCommand()
    {
        var cmd = new Command("health", "Check MCP server health") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                try
                {
                    var result = await dispatcher.QueryAsync(new CheckHealthQuery()).ConfigureAwait(true);
                    if (!result.IsSuccess || result.Value is null)
                    {
                        Error(result.Error ?? "Health check failed.");
                        return;
                    }

                    var snapshot = result.Value;
                    var server = snapshot.ServerBaseUrl
                        ?? context.ControlClient?.BaseUrl
                        ?? context.ActiveWorkspaceClient?.BaseUrl
                        ?? "(unknown)";
                    Success($"Server healthy at {server}");
                    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(snapshot.RawPayload)}[/]");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                    Error($"Server unreachable: {ex.Message}");
                }
            }).ConfigureAwait(true);
        }, s_workspaceOption);
        return cmd;
    }

    private static Command BuildListCommand()
    {
        var cmd = new Command("list", "List all registered workspaces") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, _) =>
            {
                try
                {
                    var result = await dispatcher.QueryAsync(new ListWorkspacesQuery()).ConfigureAwait(true);
                    if (!result.IsSuccess || result.Value is null)
                    {
                        Error(result.Error ?? "Workspace list failed.");
                        return;
                    }

                    var table = new Table();
                    table.AddColumn("Name");
                    table.AddColumn("Path");
                    table.AddColumn("Enabled");

                    foreach (var item in result.Value.Items)
                    {
                        table.AddRow(
                            Markup.Escape(item.Name),
                            Markup.Escape(item.WorkspacePath),
                            item.IsEnabled ? "[green]Yes[/]" : "[red]No[/]");
                    }

                    AnsiConsole.Write(table);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                    Error(ex.Message);
                }
            }).ConfigureAwait(true);
        }, s_workspaceOption);
        return cmd;
    }

    private static Command BuildAgentsCommand()
    {
        var defCmd = new Command("definitions", "List all agent type definitions");
        defCmd.AddAlias("defs");
        defCmd.AddOption(s_workspaceOption);
        defCmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, _) =>
            {
                var result = await dispatcher.QueryAsync(new ListAgentDefinitionsQuery()).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Failed to load agent definitions.");
                    return;
                }

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Display Name");
                table.AddColumn("Built-In");

                foreach (var item in result.Value.Items)
                {
                    table.AddRow(
                        Markup.Escape(item.Id),
                        Markup.Escape(item.DisplayName),
                        item.IsBuiltIn ? "[green]Yes[/]" : "No");
                }

                AnsiConsole.Write(table);
            }).ConfigureAwait(true);
        }, s_workspaceOption);

        var wsCmd = new Command("workspace", "List agents configured for this workspace");
        wsCmd.AddAlias("ws");
        wsCmd.AddOption(s_workspaceOption);
        wsCmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.QueryAsync(new ListWorkspaceAgentsQuery(workspacePath)).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Failed to load workspace agents.");
                    return;
                }

                var table = new Table();
                table.AddColumn("Agent ID");
                table.AddColumn("Enabled");
                table.AddColumn("Banned");
                table.AddColumn("Isolation");
                table.AddColumn("Last Launched");

                foreach (var item in result.Value.Items)
                {
                    table.AddRow(
                        Markup.Escape(item.AgentId),
                        item.Enabled ? "[green]Yes[/]" : "[red]No[/]",
                        item.Banned ? $"[red]Yes[/] ({Markup.Escape(item.BannedReason ?? "")})" : "[green]No[/]",
                        Markup.Escape(item.AgentIsolation),
                        item.LastLaunchedAt?.ToString("O") ?? "-");
                }

                AnsiConsole.Write(table);
            }).ConfigureAwait(true);
        }, s_workspaceOption);

        var eventsCmd = new Command("events", "Show agent lifecycle events");
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID");
        var limitOpt = new Option<int>("--limit", () => 20, "Max events to show");
        eventsCmd.AddArgument(agentIdArg);
        eventsCmd.AddOption(s_workspaceOption);
        eventsCmd.AddOption(limitOpt);
        eventsCmd.SetHandler(async (string agentId, string? workspace, int limit) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.QueryAsync(new GetAgentEventsQuery(agentId, workspacePath, limit)).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Failed to load events.");
                    return;
                }

                var table = new Table();
                table.AddColumn("Timestamp");
                table.AddColumn("Event");
                table.AddColumn("User");
                table.AddColumn("Details");

                foreach (var item in result.Value.Items)
                {
                    table.AddRow(
                        item.Timestamp.ToString("O"),
                        Markup.Escape(item.EventType.ToString()),
                        Markup.Escape(item.UserId ?? "-"),
                        Markup.Escape(item.Details ?? ""));
                }

                AnsiConsole.Write(table);
            }).ConfigureAwait(true);
        }, agentIdArg, s_workspaceOption, limitOpt);

        var agentsCmd = new Command("agents", "Manage agents (definitions, workspace configs, events)")
        {
            defCmd,
            wsCmd,
            eventsCmd,
        };
        return agentsCmd;
    }

    private static Command BuildAddCommand()
    {
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID to add");
        var isolationOpt = new Option<string>("--isolation", () => "worktree", "Isolation strategy: worktree or clone");
        var enabledOpt = new Option<bool>("--enabled", () => true, "Whether the agent is enabled");

        var cmd = new Command("add", "Add an agent to the current workspace")
        {
            agentIdArg,
            s_workspaceOption,
            isolationOpt,
            enabledOpt,
        };

        cmd.SetHandler(async (string agentId, string? workspace, string isolation, bool enabled) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.SendAsync(new AssignWorkspaceAgentCommand
                {
                    AgentId = agentId,
                    WorkspacePath = workspacePath,
                    Enabled = enabled,
                    AgentIsolation = isolation,
                }).ConfigureAwait(true);

                PrintAgentMutationOutcome(result, $"Agent '{agentId}' added to workspace.");
            }).ConfigureAwait(true);
        }, agentIdArg, s_workspaceOption, isolationOpt, enabledOpt);

        return cmd;
    }

    private static Command BuildBanCommand()
    {
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID to ban");
        var reasonOpt = new Option<string?>("--reason", "Reason for banning");
        var globalOpt = new Option<bool>("--global", () => false, "Ban globally across all workspaces");
        var prOpt = new Option<int?>("--until-pr", "PR number that must close before unbanning");

        var cmd = new Command("ban", "Ban an agent from a workspace (or globally)")
        {
            agentIdArg,
            s_workspaceOption,
            reasonOpt,
            globalOpt,
            prOpt,
        };

        cmd.SetHandler(async (string agentId, string? workspace, string? reason, bool global, int? untilPr) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = global ? null : ResolveWorkspacePathOrError(context);
                if (!global && workspacePath is null)
                    return;

                var result = await dispatcher.SendAsync(new BanAgentCommand
                {
                    AgentId = agentId,
                    Reason = reason,
                    Global = global,
                    BannedUntilPr = untilPr,
                    WorkspacePath = workspacePath,
                }).ConfigureAwait(true);

                PrintAgentMutationOutcome(result, $"Agent '{agentId}' banned{(global ? " globally" : "")}.");
            }).ConfigureAwait(true);
        }, agentIdArg, s_workspaceOption, reasonOpt, globalOpt, prOpt);

        return cmd;
    }

    private static Command BuildUnbanCommand()
    {
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID to unban");
        var globalOpt = new Option<bool>("--global", () => false, "Unban globally across all workspaces");

        var cmd = new Command("unban", "Unban an agent")
        {
            agentIdArg,
            s_workspaceOption,
            globalOpt,
        };

        cmd.SetHandler(async (string agentId, string? workspace, bool global) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = global ? null : ResolveWorkspacePathOrError(context);
                if (!global && workspacePath is null)
                    return;

                var result = await dispatcher.SendAsync(new UnbanAgentCommand(agentId, workspacePath, global)).ConfigureAwait(true);
                PrintAgentMutationOutcome(result, $"Agent '{agentId}' unbanned{(global ? " globally" : "")}.");
            }).ConfigureAwait(true);
        }, agentIdArg, s_workspaceOption, globalOpt);

        return cmd;
    }

    private static Command BuildDeleteCommand()
    {
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID to remove");

        var cmd = new Command("delete", "Remove an agent from the current workspace")
        {
            agentIdArg,
            s_workspaceOption,
        };

        cmd.SetHandler(async (string agentId, string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.SendAsync(new DeleteWorkspaceAgentCommand(agentId, workspacePath)).ConfigureAwait(true);
                PrintAgentMutationOutcome(result, $"Agent '{agentId}' removed from workspace.");
            }).ConfigureAwait(true);
        }, agentIdArg, s_workspaceOption);

        return cmd;
    }

    private static Command BuildValidateCommand()
    {
        var cmd = new Command("validate", "Validate the agents.yaml file for a workspace") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.QueryAsync(new ValidateAgentQuery(workspacePath)).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Validation failed.");
                    return;
                }

                if (result.Value.Valid)
                    Success("agents.yaml is valid.");
                else
                {
                    Error("agents.yaml validation failed.");
                    if (!string.IsNullOrWhiteSpace(result.Value.Error))
                        AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(result.Value.Error)}[/]");
                }
            }).ConfigureAwait(true);
        }, s_workspaceOption);
        return cmd;
    }

    private static Command BuildInitCommand()
    {
        var cmd = new Command("init", "Initialize the current workspace for agent management") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, context) =>
            {
                var workspacePath = ResolveWorkspacePathOrError(context);
                if (workspacePath is null)
                    return;

                var result = await dispatcher.SendAsync(new InitWorkspaceCommand(workspacePath)).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Workspace init failed.");
                    return;
                }

                var seededText = result.Value.SeededDefinitions is int seeded ? $" (seeded {seeded})" : "";
                Success($"Workspace initialized for agent management{seededText}.");
            }).ConfigureAwait(true);
        }, s_workspaceOption);
        return cmd;
    }

    private static Command BuildTodoCommand()
    {
        var listCmd = new Command("list", "List TODO items") { s_workspaceOption };
        var sectionOpt = new Option<string?>("--section", "Filter by section");
        listCmd.AddOption(sectionOpt);
        listCmd.SetHandler(async (string? workspace, string? section) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, _) =>
            {
                var result = await dispatcher.QueryAsync(new ListTodosQuery { Section = section }).ConfigureAwait(true);
                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "TODO list failed.");
                    return;
                }

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Title");
                table.AddColumn("Section");
                table.AddColumn("Priority");
                table.AddColumn("Done");

                foreach (var item in result.Value.Items)
                {
                    table.AddRow(
                        Markup.Escape(item.Id),
                        Markup.Escape(item.Title),
                        Markup.Escape(item.Section),
                        Markup.Escape(item.Priority),
                        item.Done ? "[green]✓[/]" : "○");
                }

                AnsiConsole.Write(table);
                Info($"{result.Value.Items.Count} items");
            }).ConfigureAwait(true);
        }, s_workspaceOption, sectionOpt);

        var todoCmd = new Command("todo", "Manage TODO items") { listCmd };
        return todoCmd;
    }

    private static Command BuildSessionLogCommand()
    {
        var listCmd = new Command("list", "List recent session logs") { s_workspaceOption };
        var limitOpt = new Option<int>("--limit", () => 10, "Max logs to show");
        listCmd.AddOption(limitOpt);
        listCmd.SetHandler(async (string? workspace, int limit) =>
        {
            await RunWithDispatcherAsync(workspace, async (_, dispatcher, _) =>
            {
                var result = await dispatcher.QueryAsync(new ListSessionLogsQuery
                {
                    Limit = limit,
                    Offset = 0
                }).ConfigureAwait(true);

                if (!result.IsSuccess || result.Value is null)
                {
                    Error(result.Error ?? "Session log query failed.");
                    return;
                }

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Source");
                table.AddColumn("Title");
                table.AddColumn("Status");
                table.AddColumn("Updated");

                foreach (var item in result.Value.Items)
                {
                    table.AddRow(
                        Markup.Escape(item.SessionId),
                        Markup.Escape(item.SourceType),
                        Markup.Escape(item.Title),
                        string.Equals(item.Status, "completed", StringComparison.OrdinalIgnoreCase)
                            ? "[green]completed[/]"
                            : $"[yellow]{Markup.Escape(item.Status)}[/]",
                        Markup.Escape(item.LastUpdated ?? string.Empty));
                }

                AnsiConsole.Write(table);
            }).ConfigureAwait(true);
        }, s_workspaceOption, limitOpt);

        var slCmd = new Command("session-log", "View session logs") { listCmd };
        slCmd.AddAlias("sl");
        return slCmd;
    }

    private static void PrintAgentMutationOutcome(Result<AgentMutationOutcome> result, string successMessage)
    {
        if (!result.IsSuccess || result.Value is null)
        {
            Error(result.Error ?? "Operation failed.");
            return;
        }

        if (result.Value.Success)
        {
            Success(successMessage);
            return;
        }

        Error(result.Value.Error ?? "Operation failed.");
    }
}
