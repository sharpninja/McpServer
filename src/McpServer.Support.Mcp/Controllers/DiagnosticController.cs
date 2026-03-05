using Microsoft.AspNetCore.Mvc;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Diagnostic endpoints for inspecting the running server's filesystem context.
/// Only registered in Debug builds and Staging environments — excluded from Release/Production.
/// </summary>
[ApiController]
[Route("mcpserver/diagnostic")]
public sealed class DiagnosticController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    /// <summary>Initializes a new instance of the <see cref="DiagnosticController"/> class.</summary>
    /// <param name="env">Host environment providing content root and environment name.</param>
    public DiagnosticController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>Returns the execution path of the running MCP server process.</summary>
    /// <returns>
    /// An object containing <c>processPath</c> (the full path of the executable, if available)
    /// and <c>baseDirectory</c> (the directory from which the application is running).
    /// </returns>
    [HttpGet("execution-path")]
    public ActionResult<object> GetExecutionPath()
    {
        return Ok(new
        {
            processPath = Environment.ProcessPath,
            baseDirectory = AppContext.BaseDirectory,
        });
    }

    /// <summary>Returns the resolved paths of all appsettings files for the running server.</summary>
    /// <returns>
    /// An object containing <c>environmentName</c>, <c>contentRootPath</c>, and <c>files</c> —
    /// an ordered list of appsettings file paths indicating whether each file exists on disk.
    /// Files are listed in ASP.NET Core load order (base → environment-specific → secrets).
    /// </returns>
    [HttpGet("appsettings-path")]
    public ActionResult<object> GetAppSettingsPath()
    {
        var root = _env.ContentRootPath;
        var envName = _env.EnvironmentName;

        var candidates = new[]
        {
            Path.Combine(root, "appsettings.json"),
            Path.Combine(root, $"appsettings.{envName}.json"),
        };

        var files = candidates.Select(p => new
        {
            path = p,
            exists = System.IO.File.Exists(p),
        }).ToArray();

        return Ok(new
        {
            environmentName = envName,
            contentRootPath = root,
            files,
        });
    }
}
