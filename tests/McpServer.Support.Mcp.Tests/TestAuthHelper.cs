using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests;

/// <summary>Shared helper for adding workspace auth headers to test HttpClient instances.</summary>
internal static class TestAuthHelper
{
    /// <summary>
    /// Resolves the workspace API key from <see cref="WorkspaceTokenService"/> and adds
    /// the <c>X-Api-Key</c> header to <paramref name="client"/>.
    /// </summary>
    public static void AddAuthHeader(HttpClient client, IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var repoRoot = config["Mcp:RepoRoot"] ?? ".";
        var workspacePath = Path.IsPathRooted(repoRoot)
            ? Path.GetFullPath(repoRoot)
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, repoRoot));
        workspacePath = workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var token = tokenService.GetToken(workspacePath);
        if (token is not null)
            client.DefaultRequestHeaders.Add("X-Api-Key", token);
    }
}
