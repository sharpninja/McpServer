using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using McpServer.Client;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.Director.Commands;
using McpServer.Director.Auth;
using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace McpServer.Director;

/// <summary>
/// TR-MCP-DIR-001, TR-MCP-DIR-003: Director CLI entry point.
/// Provides <c>exec</c> command that resolves ViewModels from <see cref="IViewModelRegistry"/>,
/// populates properties from JSON input, executes the primary command, and renders results.
/// </summary>
internal static class Program
{
    /// <summary>Main entry point.</summary>
    /// <param name="args">CLI arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("McpServer Director — CLI management tool");

        // FR-MCP-030: Register all Director agent management commands
        DirectorCommands.Register(rootCommand);

        // FR-MCP-030: Register auth commands (login, logout, whoami)
        AuthCommands.Register(rootCommand);

        // Director CLI defaults (e.g., default base URL outside workspace markers)
        ConfigCommands.Register(rootCommand);

        // FR-MCP-030: Register interactive TUI command
        InteractiveCommand.Register(rootCommand);

        // exec <viewmodel> [--input <json>]
        var execCommand = BuildExecCommand();
        rootCommand.AddCommand(execCommand);

        // list-viewmodels
        var listCommand = BuildListCommand();
        rootCommand.AddCommand(listCommand);

        return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    }

    private static ServiceProvider BuildServiceProvider(string? workspace = null)
    {
        var services = new ServiceCollection();
        services.AddDirectorLogging();
        services.AddCqrs(typeof(Program).Assembly);
        services.AddUiCore();
        services.RemoveAll<IRoleContext>();
        services.RemoveAll<IAuthorizationPolicyService>();
        services.AddSingleton<IRoleContext, DirectorRoleContext>();
        services.AddSingleton<IAuthorizationPolicyService, DirectorAuthorizationPolicyService>();

        var activeWorkspaceClient = McpHttpClient.FromMarkerOnly(workspace);
        activeWorkspaceClient?.TrySetCachedBearerToken();

        var controlClient = McpHttpClient.FromDefaultUrlOrMarker(workspace);
        controlClient?.TrySetCachedBearerToken();

        var directorContext = new DirectorMcpContext(controlClient, activeWorkspaceClient);
        services.AddSingleton(directorContext);
        if (controlClient is not null)
            services.AddSingleton(controlClient);

        services.AddSingleton<IHealthApiClient>(_ => new HealthApiClientAdapter(directorContext.ControlClient));
        services.AddSingleton<ISessionLogApiClient>(_ => new SessionLogApiClientAdapter(directorContext));
        services.AddSingleton<IWorkspaceApiClient>(_ => new WorkspaceApiClientAdapter(directorContext));
        services.AddSingleton<ISyncApiClient>(_ => new SyncApiClientAdapter(directorContext));
        services.AddSingleton<IRepoApiClient>(_ => new RepoApiClientAdapter(directorContext));
        services.AddSingleton<IContextApiClient>(_ => new ContextApiClientAdapter(directorContext));
        services.AddSingleton<IAuthConfigApiClient>(_ => new AuthConfigApiClientAdapter(directorContext));
        services.AddSingleton<IDiagnosticApiClient>(_ => new DiagnosticApiClientAdapter(directorContext));
        services.AddSingleton<ITodoApiClient>(_ => new TodoApiClientAdapter(directorContext));
        services.AddSingleton<ITunnelApiClient>(_ => new TunnelApiClientAdapter(directorContext));

        return services.BuildServiceProvider();
    }

    private static Command BuildExecCommand()
    {
        var viewModelArg = new Argument<string>("viewmodel",
            "ViewModel class name or CLI alias (see 'director list-viewmodels').");
        var inputOption = new Option<string?>("--input",
            "JSON object used to populate writable ViewModel properties before execution.");
        inputOption.AddAlias("-i");

        var execCommand = new Command("exec", "Execute a ViewModel command by name or alias")
        {
            viewModelArg,
            inputOption,
        };
        execCommand.Description =
            "Execute a ViewModel primary command through the MVVM/CQRS layer. " +
            "Use 'director list-viewmodels' to discover aliases and descriptions. " +
            "Examples: director exec list-todos | director exec get-todo -i '{\"TodoId\":\"MVP-SUPPORT-019\"}' | " +
            "director exec todo-prompt-status -i '{\"TodoId\":\"MVP-SUPPORT-019\"}' | " +
            "director exec todo-requirements -i '{\"TodoId\":\"MVP-SUPPORT-019\"}'";

        execCommand.SetHandler(async (string viewModelName, string? input) =>
        {
            using var sp = BuildServiceProvider();
            var registry = sp.GetRequiredService<IViewModelRegistry>();

            try
            {
                // Resolve ViewModel
                var vm = registry.Resolve(viewModelName);
                AnsiConsole.MarkupLine($"[green]Resolved:[/] {vm.GetType().Name}");

                // Set properties from JSON input
                if (!string.IsNullOrWhiteSpace(input))
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(input);
                    registry.SetProperties(vm, jsonElement);
                    AnsiConsole.MarkupLine("[dim]Properties set from input.[/]");
                }

                // Execute primary command
                var command = registry.GetPrimaryCommand(vm);
                AnsiConsole.MarkupLine("[yellow]Executing...[/]");

                await command.ExecuteAsync(null).ConfigureAwait(false);

                // Get and display result
                var result = registry.GetResult(vm);
                if (result is not null)
                {
                    var json = JsonSerializer.Serialize(ToSerializableResult(result), new JsonSerializerOptions { WriteIndented = true });
                    AnsiConsole.MarkupLine("[green]Result:[/]");
                    AnsiConsole.WriteLine(json);
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]Command completed (no result).[/]");
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Trace.TraceWarning(ex.ToString());
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                AnsiConsole.WriteException(ex);
            }
        }, viewModelArg, inputOption);

        return execCommand;
    }

    private static Command BuildListCommand()
    {
        var listCommand = new Command("list-viewmodels", "List registered ViewModels, CLI aliases, and descriptions for 'director exec'");
        var filterOption = new Option<string?>("--filter", "Optional substring filter for alias/name/type/description");
        filterOption.AddAlias("-f");
        listCommand.AddOption(filterOption);

        listCommand.SetHandler((string? filter) =>
        {
            using var sp = BuildServiceProvider();
            var registry = sp.GetRequiredService<IViewModelRegistry>();

            var table = new Table();
            table.AddColumn("Alias / Name");
            table.AddColumn("Type");
            table.AddColumn("Description");

            foreach (var kvp in registry.ViewModels.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var attr = kvp.Value.GetCustomAttribute<ViewModelCommandAttribute>();
                var description = attr?.Description ?? string.Empty;
                var rowText = $"{kvp.Key} {kvp.Value.FullName} {description}";
                if (!string.IsNullOrWhiteSpace(filter)
                    && rowText.Contains(filter, StringComparison.OrdinalIgnoreCase) is false)
                    continue;

                table.AddRow(
                    Markup.Escape(kvp.Key),
                    Markup.Escape(kvp.Value.FullName ?? kvp.Value.Name),
                    Markup.Escape(description));
            }

            AnsiConsole.Write(table);
        }, filterOption);

        return listCommand;
    }

    private static object ToSerializableResult(object result)
    {
        var type = result.GetType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            return new
            {
                IsSuccess = (bool)(type.GetProperty(nameof(Result<int>.IsSuccess))?.GetValue(result) ?? false),
                Error = type.GetProperty(nameof(Result<int>.Error))?.GetValue(result),
                Value = type.GetProperty(nameof(Result<int>.Value))?.GetValue(result),
            };
        }

        if (type == typeof(Result))
        {
            return new
            {
                IsSuccess = ((Result)result).IsSuccess,
                Error = ((Result)result).Error,
            };
        }

        return result;
    }
}
