using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Shared helpers for unauthenticated marker/server timestamp diagnostics endpoints.
/// </summary>
internal static class MarkerDiagnosticsEndpointHelper
{
    public static IResult GetServerStartupResult(ServerRuntimeInfo runtimeInfo, string? workspace = null, int? port = null)
    {
        var startedAtUtc = runtimeInfo.StartedAtUtc;
        return Results.Ok(new
        {
            serverStartedAtUtc = startedAtUtc.ToString("o", CultureInfo.InvariantCulture),
            nowUtc = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            processId = Environment.ProcessId,
            workspace,
            port,
        });
    }

    public static IResult GetMarkerFileTimestampResult(
        string? repoPath,
        IConfiguration configuration,
        string contentRootPath,
        bool restrictToCurrentRepoRoot = false)
    {
        if (string.IsNullOrWhiteSpace(repoPath))
        {
            return Results.BadRequest(new
            {
                error = "The 'repoPath' query parameter is required."
            });
        }

        if (!TryNormalizeRepoPath(repoPath, contentRootPath, out var normalizedRepoPath, out var normalizationError))
        {
            return Results.BadRequest(new
            {
                error = normalizationError
            });
        }

        var allowedPaths = GetAllowedRepoPaths(configuration, contentRootPath, restrictToCurrentRepoRoot);
        if (allowedPaths.Count > 0 && !allowedPaths.Contains(normalizedRepoPath))
        {
            return Results.NotFound(new
            {
                error = "Repository path is not a configured workspace.",
                repoPath = normalizedRepoPath,
            });
        }

        var markerPath = Path.Combine(normalizedRepoPath, MarkerFileService.MarkerFileName);
        if (!File.Exists(markerPath))
        {
            return Results.Ok(new
            {
                repoPath = normalizedRepoPath,
                markerPath,
                exists = false,
                lastWriteTimeUtc = (string?)null,
                creationTimeUtc = (string?)null,
            });
        }

        var fileInfo = new FileInfo(markerPath);
        fileInfo.Refresh();

        return Results.Ok(new
        {
            repoPath = normalizedRepoPath,
            markerPath,
            exists = true,
            lastWriteTimeUtc = fileInfo.LastWriteTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            creationTimeUtc = fileInfo.CreationTimeUtc.ToString("o", CultureInfo.InvariantCulture),
            length = fileInfo.Length,
        });
    }

    private static bool TryNormalizeRepoPath(string repoPath, string contentRootPath, out string normalizedPath, out string? error)
    {
        normalizedPath = string.Empty;
        error = null;

        try
        {
            var trimmed = repoPath.Trim();
            var absolute = Path.IsPathRooted(trimmed)
                ? Path.GetFullPath(trimmed)
                : Path.GetFullPath(Path.Combine(contentRootPath, trimmed));

            normalizedPath = absolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            error = $"Invalid repoPath '{repoPath}'.";
            return false;
        }
    }

    private static HashSet<string> GetAllowedRepoPaths(IConfiguration configuration, string contentRootPath, bool restrictToCurrentRepoRoot)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!restrictToCurrentRepoRoot)
        {
            var workspaces = configuration.GetSection("Mcp:Workspaces").Get<List<WorkspaceConfigEntry>>() ?? [];
            foreach (var workspace in workspaces)
            {
                if (string.IsNullOrWhiteSpace(workspace.WorkspacePath))
                    continue;

                if (TryNormalizeRepoPath(workspace.WorkspacePath, contentRootPath, out var normalizedWorkspacePath, out _))
                    allowed.Add(normalizedWorkspacePath);
            }
        }

        var currentRepoRoot = configuration["Mcp:RepoRoot"];
        if (!string.IsNullOrWhiteSpace(currentRepoRoot)
            && TryNormalizeRepoPath(currentRepoRoot, contentRootPath, out var normalizedRepoRoot, out _))
        {
            allowed.Add(normalizedRepoRoot);
        }

        return allowed;
    }
}
