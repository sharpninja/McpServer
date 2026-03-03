using System.CommandLine;
using McpServer.Director.Auth;
using McpServer.Director.Helpers;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using static McpServer.Director.Commands.CommandHelpers;

namespace McpServer.Director.Commands;

/// <summary>
/// FR-MCP-030: Authentication commands for the Director CLI.
/// Commands: login, logout, whoami.
/// Uses Keycloak Device Authorization Flow for CLI authentication.
/// </summary>
internal static class AuthCommands
{
    /// <summary>Registers auth commands on the root command.</summary>
    public static void Register(RootCommand root)
    {
        root.AddCommand(BuildLoginCommand());
        root.AddCommand(BuildLogoutCommand());
        root.AddCommand(BuildWhoamiCommand());
    }

    // ── login ────────────────────────────────────────────────────────────

    private static Command BuildLoginCommand()
    {
        var authorityOpt = new Option<string?>("--authority", "Keycloak realm authority URL");
        var clientIdOpt = new Option<string>("--client-id", () => "mcp-director", "Keycloak client ID");

        var cmd = new Command("login", "Authenticate with Keycloak using Device Authorization Flow")
        {
            authorityOpt,
            clientIdOpt,
        };

        cmd.SetHandler(async (string? authority, string clientId) =>
        {
            // Resolve authority: CLI option → env var → server auto-discovery → marker file
            var resolvedAuthority = authority
                ?? Environment.GetEnvironmentVariable("MCP_AUTH_AUTHORITY");

            var options = new DirectorAuthOptions { ClientId = clientId };

            // Try auto-discovery from MCP server if no authority specified
            if (string.IsNullOrWhiteSpace(resolvedAuthority))
            {
                var serverConfig = await DiscoverAuthConfigAsync().ConfigureAwait(false);
                if (serverConfig is not null && serverConfig.Enabled)
                {
                    options.PopulateFrom(serverConfig);
                    AnsiConsole.MarkupLine("[dim]Auth config discovered from MCP server.[/]");
                }
                else
                {
                    resolvedAuthority = ResolveAuthorityFromMarker();
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedAuthority))
                options.Authority = resolvedAuthority;

            if (!options.IsConfigured)
            {
                Error("Keycloak authority not specified.");
                AnsiConsole.MarkupLine("[dim]Provide --authority, set MCP_AUTH_AUTHORITY env var, or ensure MCP server is running with Mcp:Auth configured.[/]");
                return;
            }

            using var authService = new OidcAuthService(options);

            // Resolve browser launcher from DI
            var services = new ServiceCollection();
            DirectorServiceRegistration.Configure(services);
            using var sp = DirectorServiceRegistration.BuildAndFinalize(services);
            var browserLauncher = sp.GetRequiredService<IBrowserLauncher>();

            AnsiConsole.MarkupLine($"[blue]Authenticating with:[/] {Markup.Escape(options.Authority)}");
            AnsiConsole.WriteLine();

            var result = await authService.LoginAsync((userCode, verificationUri, verificationUriComplete) =>
            {
                var targetUrl = verificationUriComplete ?? verificationUri;

                var panel = new Panel(
                    new Rows(
                        new Markup($"[bold yellow]User Code:[/] [bold white on blue] {Markup.Escape(userCode)} [/]"),
                        new Markup(""),
                        new Markup($"[blue]Go to:[/] [link]{Markup.Escape(targetUrl)}[/]"),
                        new Markup(""),
                        new Markup("[dim]Enter the code above in your browser to complete login.[/]"),
                        new Markup("[dim]Waiting for authentication...[/]")))
                {
                    Header = new PanelHeader("[bold]Device Authorization[/]"),
                    Border = BoxBorder.Rounded,
                    Padding = new Padding(2, 1),
                };

                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();

                if (browserLauncher.TryOpenUrl(targetUrl))
                    AnsiConsole.MarkupLine("[dim]Browser opened automatically.[/]");
            }).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                Success($"Logged in as {result.Username ?? "unknown"}");
                AnsiConsole.MarkupLine($"[dim]Token cached at {Markup.Escape(TokenCache.GetCachePath())}[/]");
            }
            else
            {
                Error(result.Error ?? "Login failed.");
            }
        }, authorityOpt, clientIdOpt);

        return cmd;
    }

    // ── logout ───────────────────────────────────────────────────────────

    private static Command BuildLogoutCommand()
    {
        var cmd = new Command("logout", "Clear cached authentication tokens");

        cmd.SetHandler(() =>
        {
            OidcAuthService.Logout();
            Success("Logged out. Token cache cleared.");
        });

        return cmd;
    }

    // ── whoami ───────────────────────────────────────────────────────────

    private static Command BuildWhoamiCommand()
    {
        var cmd = new Command("whoami", "Display current authenticated user");

        cmd.SetHandler(() =>
        {
            var user = OidcAuthService.GetCurrentUser();
            if (user is null)
            {
                Warn("Not logged in. Run 'director login' to authenticate.");
                return;
            }

            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");
            table.Border = TableBorder.Rounded;

            table.AddRow("Username", Markup.Escape(user.Username));
            table.AddRow("Subject", Markup.Escape(user.Subject));
            table.AddRow("Email", Markup.Escape(user.Email ?? "(not set)"));
            table.AddRow("Roles", user.Roles.Count > 0 ? Markup.Escape(string.Join(", ", user.Roles)) : "(none)");
            table.AddRow("Authority", Markup.Escape(user.Authority));
            table.AddRow("Expires", user.ExpiresAtUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            table.AddRow("Status", user.IsExpired ? "[red]Expired[/]" : "[green]Valid[/]");

            AnsiConsole.Write(table);
        });

        return cmd;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Discovers OIDC auth configuration from the MCP server's <c>/auth/config</c> endpoint.
    /// Uses the marker file to find the server base URL.
    /// </summary>
    private static async Task<AuthConfigResponse?> DiscoverAuthConfigAsync()
    {
        AuthConfigResponse? discovered = null;
        await RunWithDispatcherAsync(null, async (_, dispatcher, _) =>
        {
            var result = await dispatcher.QueryAsync(new GetAuthConfigQuery()).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
                return;

            discovered = new AuthConfigResponse
            {
                Enabled = result.Value.Enabled,
                Authority = result.Value.Authority ?? string.Empty,
                ClientId = result.Value.ClientId ?? string.Empty,
                Scopes = result.Value.Scopes ?? string.Empty,
                DeviceAuthorizationEndpoint = result.Value.DeviceAuthorizationEndpoint ?? string.Empty,
                TokenEndpoint = result.Value.TokenEndpoint ?? string.Empty,
            };
        }).ConfigureAwait(false);

        return discovered;
    }

    /// <summary>
    /// Tries to resolve the Keycloak authority from the AGENTS-README-FIRST.yaml marker file.
    /// Looks for an 'authority' key in the YAML.
    /// </summary>
    private static string? ResolveAuthorityFromMarker()
    {
        var markerPath = Path.Combine(Directory.GetCurrentDirectory(), "AGENTS-README-FIRST.yaml");
        if (!File.Exists(markerPath))
            return null;

        foreach (var line in File.ReadLines(markerPath))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("authority:", StringComparison.OrdinalIgnoreCase))
                return trimmed["authority:".Length..].Trim();
        }

        return null;
    }
}
