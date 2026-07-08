namespace McpServer.Support.Mcp.Tests.Services.AgentHelp;

internal static class AgentHelpTestPaths
{
    public static string ResolveFixturePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Fixtures", "AgentHelp", relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Agent Help fixture not found: {relativePath}");
    }

    public static string CreateTempWorkspaceRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcpserver-agenthelp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}