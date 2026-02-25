using Spectre.Console;

namespace McpServer.Director.Commands;

/// <summary>Shared helpers for Director CLI commands.</summary>
internal static class CommandHelpers
{
    /// <summary>Resolves an McpHttpClient from the marker file, or prints an error.</summary>
    public static McpHttpClient? ResolveClient(string? workspace)
    {
        var client = McpHttpClient.FromMarkerFile(workspace);
        if (client is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not find AGENTS-README-FIRST.yaml and no default MCP URL is configured.");
            AnsiConsole.MarkupLine("[dim]Run this command from a workspace directory, or pass --workspace <path>, or set a default URL with 'director config set-default-url <url>'.[/]");
            return null;
        }

        // Auto-attach cached Bearer token for JWT-protected mutation endpoints
        client.TrySetCachedBearerToken();
        return client;
    }

    /// <summary>Prints a success message.</summary>
    public static void Success(string message)
        => AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");

    /// <summary>Prints an error message.</summary>
    public static void Error(string message)
        => AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(message)}");

    /// <summary>Prints a warning message.</summary>
    public static void Warn(string message)
        => AnsiConsole.MarkupLine($"[yellow]![/] {Markup.Escape(message)}");

    /// <summary>Prints an info message.</summary>
    public static void Info(string message)
        => AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(message)}");
}
