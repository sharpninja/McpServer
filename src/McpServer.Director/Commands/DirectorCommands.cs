using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using static McpServer.Director.Commands.CommandHelpers;

namespace McpServer.Director.Commands;

/// <summary>
/// FR-MCP-030: All Director CLI commands for agent management in workspaces.
/// Commands: health, list, agents, add, ban, unban, delete, validate, init, todo, session-log.
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

    // ── health ──────────────────────────────────────────────────────────

    private static Command BuildHealthCommand()
    {
        var cmd = new Command("health", "Check MCP server health") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            try
            {
                var json = await client.GetStringAsync("/health").ConfigureAwait(false);
                Success($"Server healthy at {client.BaseUrl}");
                AnsiConsole.MarkupLine($"[dim]{Markup.Escape(json)}[/]");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error($"Server unreachable: {ex.Message}");
            }
        }, s_workspaceOption);
        return cmd;
    }

    // ── list (workspaces) ───────────────────────────────────────────────

    private static Command BuildListCommand()
    {
        var cmd = new Command("list", "List all registered workspaces") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            try
            {
                var result = await client.GetAsync<JsonElement>("/mcpserver/workspace").ConfigureAwait(false);
                var items = result.GetProperty("items");

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Path");
                table.AddColumn("Enabled");

                foreach (var item in items.EnumerateArray())
                {
                    table.AddRow(
                        Markup.Escape(item.GetProperty("name").GetString() ?? ""),
                        Markup.Escape(item.GetProperty("workspacePath").GetString() ?? ""),
                        item.TryGetProperty("isEnabled", out var en) ? (en.GetBoolean() ? "[green]Yes[/]" : "[red]No[/]") : "-");
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption);
        return cmd;
    }

    // ── agents ──────────────────────────────────────────────────────────

    private static Command BuildAgentsCommand()
    {
        var defCmd = new Command("definitions", "List all agent type definitions");
        defCmd.AddAlias("defs");
        defCmd.AddOption(s_workspaceOption);
        defCmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            try
            {
                var result = await client.GetAsync<JsonElement>("/mcpserver/agents/definitions").ConfigureAwait(false);
                var items = result.GetProperty("items");

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Display Name");
                table.AddColumn("Built-In");
                table.AddColumn("Default Models");

                foreach (var item in items.EnumerateArray())
                {
                    var models = item.TryGetProperty("defaultModels", out var m)
                        ? string.Join(", ", m.EnumerateArray().Select(x => x.GetString()))
                        : "";
                    table.AddRow(
                        Markup.Escape(item.GetProperty("id").GetString() ?? ""),
                        Markup.Escape(item.GetProperty("displayName").GetString() ?? ""),
                        item.TryGetProperty("isBuiltIn", out var bi) && bi.GetBoolean() ? "[green]Yes[/]" : "No",
                        Markup.Escape(models));
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption);

        var wsCmd = new Command("workspace", "List agents configured for this workspace");
        wsCmd.AddAlias("ws");
        wsCmd.AddOption(s_workspaceOption);
        wsCmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            var result = await client.GetAsync<JsonElement>($"/mcpserver/agents?workspace={path}").ConfigureAwait(false);
            var items = result.GetProperty("items");

            var table = new Table();
            table.AddColumn("Agent ID");
            table.AddColumn("Enabled");
            table.AddColumn("Banned");
            table.AddColumn("Isolation");
            table.AddColumn("Last Launched");

            foreach (var item in items.EnumerateArray())
            {
                var banned = item.TryGetProperty("banned", out var b) && b.GetBoolean();
                table.AddRow(
                    Markup.Escape(item.GetProperty("agentId").GetString() ?? ""),
                    item.TryGetProperty("enabled", out var en) && en.GetBoolean() ? "[green]Yes[/]" : "[red]No[/]",
                    banned ? $"[red]Yes[/] ({Markup.Escape(item.TryGetProperty("bannedReason", out var br) ? br.GetString() ?? "" : "")})" : "[green]No[/]",
                    Markup.Escape(item.TryGetProperty("agentIsolation", out var iso) ? iso.GetString() ?? "worktree" : "worktree"),
                    item.TryGetProperty("lastLaunchedAt", out var ll) && ll.ValueKind != JsonValueKind.Null ? ll.GetString() ?? "-" : "-");
            }

            AnsiConsole.Write(table);
        }, s_workspaceOption);

        var eventsCmd = new Command("events", "Show agent lifecycle events");
        var agentIdArg = new Argument<string>("agent-id", "Agent type ID");
        var limitOpt = new Option<int>("--limit", () => 20, "Max events to show");
        eventsCmd.AddArgument(agentIdArg);
        eventsCmd.AddOption(s_workspaceOption);
        eventsCmd.AddOption(limitOpt);
        eventsCmd.SetHandler(async (string agentId, string? workspace, int limit) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            var result = await client.GetAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}/events?workspace={path}&limit={limit}").ConfigureAwait(false);
            var items = result.GetProperty("items");

            var table = new Table();
            table.AddColumn("Timestamp");
            table.AddColumn("Event");
            table.AddColumn("User");
            table.AddColumn("Details");

            foreach (var item in items.EnumerateArray())
            {
                table.AddRow(
                    item.TryGetProperty("timestamp", out var ts) ? ts.GetString() ?? "" : "",
                    Markup.Escape(item.TryGetProperty("eventType", out var et) ? et.ToString() : ""),
                    Markup.Escape(item.TryGetProperty("userId", out var uid) ? uid.GetString() ?? "-" : "-"),
                    Markup.Escape(item.TryGetProperty("details", out var d) && d.ValueKind != JsonValueKind.Null ? d.GetString() ?? "" : ""));
            }

            AnsiConsole.Write(table);
        }, agentIdArg, s_workspaceOption, limitOpt);

        var agentsCmd = new Command("agents", "Manage agents (definitions, workspace configs, events)")
        {
            defCmd,
            wsCmd,
            eventsCmd,
        };
        return agentsCmd;
    }

    // ── add ─────────────────────────────────────────────────────────────

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
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                var body = new { agentId, enabled, agentIsolation = isolation };
                var result = await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}?workspace={path}", body).ConfigureAwait(false);
                var success = result.TryGetProperty("success", out var s) && s.GetBoolean();
                if (success)
                    Success($"Agent '{agentId}' added to workspace.");
                else
                    Error(result.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown error" : "Unknown error");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, agentIdArg, s_workspaceOption, isolationOpt, enabledOpt);

        return cmd;
    }

    // ── ban ──────────────────────────────────────────────────────────────

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
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                var body = new { reason, global, bannedUntilPr = untilPr };
                var result = await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}/ban?workspace={path}", body).ConfigureAwait(false);
                var success = result.TryGetProperty("success", out var s) && s.GetBoolean();
                if (success)
                    Success($"Agent '{agentId}' banned{(global ? " globally" : "")}.");
                else
                    Error(result.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown error" : "Unknown error");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, agentIdArg, s_workspaceOption, reasonOpt, globalOpt, prOpt);

        return cmd;
    }

    // ── unban ────────────────────────────────────────────────────────────

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
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                var result = await client.PostAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}/unban?workspace={path}&global={global}").ConfigureAwait(false);
                var success = result.TryGetProperty("success", out var s) && s.GetBoolean();
                if (success)
                    Success($"Agent '{agentId}' unbanned{(global ? " globally" : "")}.");
                else
                    Error(result.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown error" : "Unknown error");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, agentIdArg, s_workspaceOption, globalOpt);

        return cmd;
    }

    // ── delete ───────────────────────────────────────────────────────────

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
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                var result = await client.DeleteAsync<JsonElement>($"/mcpserver/agents/{Uri.EscapeDataString(agentId)}?workspace={path}").ConfigureAwait(false);
                var success = result.TryGetProperty("success", out var s) && s.GetBoolean();
                if (success)
                    Success($"Agent '{agentId}' removed from workspace.");
                else
                    Error(result.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown error" : "Unknown error");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, agentIdArg, s_workspaceOption);

        return cmd;
    }

    // ── validate ─────────────────────────────────────────────────────────

    private static Command BuildValidateCommand()
    {
        var cmd = new Command("validate", "Validate the agents.yaml file for a workspace") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                var result = await client.GetAsync<JsonElement>($"/mcpserver/agents/validate?workspace={path}").ConfigureAwait(false);
                var valid = result.TryGetProperty("valid", out var v) && v.GetBoolean();
                if (valid)
                    Success("agents.yaml is valid.");
                else
                {
                    Error("agents.yaml validation failed.");
                    if (result.TryGetProperty("error", out var e))
                        AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(e.GetString() ?? "")}[/]");
                }
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption);
        return cmd;
    }

    // ── init ─────────────────────────────────────────────────────────────

    private static Command BuildInitCommand()
    {
        var cmd = new Command("init", "Initialize the current workspace for agent management") { s_workspaceOption };
        cmd.SetHandler(async (string? workspace) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            var path = Uri.EscapeDataString(client.WorkspacePath);
            try
            {
                // Seed built-in agent definitions
                await client.PostRawAsync("/mcpserver/agents/definitions/seed").ConfigureAwait(false);
                Info("Built-in agent definitions seeded.");

                // Log init event
                // Server endpoint currently expects AgentEventType as a numeric enum value (Init = 7).
                var body = new { agentId = "system", eventType = 7, details = "Workspace initialized via Director CLI" };
                await client.PostAsync<JsonElement>($"/mcpserver/agents/system/events?workspace={path}", body).ConfigureAwait(false);

                Success("Workspace initialized for agent management.");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption);
        return cmd;
    }

    // ── todo ─────────────────────────────────────────────────────────────

    private static Command BuildTodoCommand()
    {
        var listCmd = new Command("list", "List TODO items") { s_workspaceOption };
        var sectionOpt = new Option<string?>("--section", "Filter by section");
        listCmd.AddOption(sectionOpt);
        listCmd.SetHandler(async (string? workspace, string? section) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            try
            {
                var url = "/mcpserver/todo" + (section is not null ? $"?section={Uri.EscapeDataString(section)}" : "");
                var result = await client.GetAsync<JsonElement>(url).ConfigureAwait(false);
                var items = result.GetProperty("items");

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Title");
                table.AddColumn("Section");
                table.AddColumn("Priority");
                table.AddColumn("Done");

                foreach (var item in items.EnumerateArray())
                {
                    var done = item.TryGetProperty("done", out var d) && d.GetBoolean();
                    table.AddRow(
                        Markup.Escape(item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : ""),
                        Markup.Escape(item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : ""),
                        Markup.Escape(item.TryGetProperty("section", out var s) ? s.GetString() ?? "" : ""),
                        Markup.Escape(item.TryGetProperty("priority", out var p) ? p.GetString() ?? "" : ""),
                        done ? "[green]✓[/]" : "○");
                }

                AnsiConsole.Write(table);
                Info($"{items.GetArrayLength()} items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption, sectionOpt);

        var todoCmd = new Command("todo", "Manage TODO items") { listCmd };
        return todoCmd;
    }

    // ── session-log ──────────────────────────────────────────────────────

    private static Command BuildSessionLogCommand()
    {
        var listCmd = new Command("list", "List recent session logs") { s_workspaceOption };
        var limitOpt = new Option<int>("--limit", () => 10, "Max logs to show");
        listCmd.AddOption(limitOpt);
        listCmd.SetHandler(async (string? workspace, int limit) =>
        {
            using var client = ResolveClient(workspace);
            if (client is null) return;

            try
            {
                var body = new { limit, sortBy = "lastUpdated", sortDirection = "desc" };
                var result = await client.PostAsync<JsonElement>("/mcpserver/sessionlog", body).ConfigureAwait(false);
                var items = result.GetProperty("items");

                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Source");
                table.AddColumn("Title");
                table.AddColumn("Status");
                table.AddColumn("Updated");

                foreach (var item in items.EnumerateArray())
                {
                    var status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
                    table.AddRow(
                        item.TryGetProperty("id", out var id) ? id.ToString() : "",
                        Markup.Escape(item.TryGetProperty("sourceType", out var src) ? src.GetString() ?? "" : ""),
                        Markup.Escape(item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : ""),
                        status == "completed" ? "[green]completed[/]" : $"[yellow]{Markup.Escape(status)}[/]",
                        Markup.Escape(item.TryGetProperty("lastUpdated", out var lu) ? lu.GetString() ?? "" : ""));
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                Error(ex.Message);
            }
        }, s_workspaceOption, limitOpt);

        var slCmd = new Command("session-log", "View session logs") { listCmd };
        slCmd.AddAlias("sl");
        return slCmd;
    }
}
