using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("MCP instance name from appsettings")]
    readonly string Instance;

    [Parameter("Skip build and run directly")]
    readonly bool NoBuild;

    /// <summary>Build and start the MCP server locally.</summary>
    public Target StartServer => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var project = SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj";

            Log.Information("Starting MCP server. Press Ctrl+C to stop.");

            var settings = new DotNetRunSettings()
                .SetProjectFile(project)
                .SetConfiguration(Configuration)
                .EnableNoBuild();

            if (!string.IsNullOrWhiteSpace(Instance))
            {
                Log.Information("Using MCP instance: {Instance}", Instance);
                settings = settings.SetApplicationArguments($"--instance {Instance}");
            }

            DotNetRun(_ => settings);
        });
}
