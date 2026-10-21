namespace QBrainAi.Support.Mcp.Tests.Services.AgentHelp;

internal static class AgentHelpTestPaths
{
    /// <summary>
    /// Resolves an Agent Help fixture copied to the test output (<c>Fixtures\AgentHelp\**</c>,
    /// CopyToOutputDirectory), never a file in the repository checkout (TR-MCP-USECASE-018 AC-006).
    /// </summary>
    public static string ResolveFixturePath(string relativePath)
    {
        var candidate = Path.Combine(AppContext.BaseDirectory, "Fixtures", "AgentHelp", relativePath);
        return File.Exists(candidate)
            ? candidate
            : throw new FileNotFoundException($"Agent Help fixture not found in the test output: {relativePath}", candidate);
    }

    public static string CreateTempWorkspaceRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcpserver-agenthelp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
