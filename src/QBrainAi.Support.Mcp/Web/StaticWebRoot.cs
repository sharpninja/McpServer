using Microsoft.AspNetCore.StaticFiles;

namespace QBrainAi.Support.Mcp.Web;

/// <summary>
/// TR-MCP-USECASE-007 AC-1: options for the static file middleware that serves the web root (/, /usecases/, /memory/).
/// Markdown files under wwwroot are folder documentation for maintainers, never UI assets, so the content type map
/// leaves out .md and unknown file types are not served. A request for a Markdown file then falls through and its
/// content is never returned, whatever content root the server runs from (a source-tree run reads wwwroot from disk,
/// where the folder README.md files exist).
/// </summary>
public static class StaticWebRoot
{
    /// <summary>TR-MCP-USECASE-007 AC-1: creates the static file options for the web root.</summary>
    /// <returns>Options whose content type map has every default type except .md.</returns>
    public static StaticFileOptions CreateOptions()
    {
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings.Remove(".md");
        return new StaticFileOptions { ContentTypeProvider = contentTypes, ServeUnknownFileTypes = false };
    }
}
