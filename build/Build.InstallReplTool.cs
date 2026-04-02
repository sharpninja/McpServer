using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;

partial class Build
{
    [Parameter("Update existing tool installation instead of fresh install")]
    readonly bool UpdateTool;

    [Parameter("Uninstall the global tool")]
    readonly bool UninstallTool;

    /// <summary>Install, update, or uninstall the mcpserver-repl global tool.</summary>
    public Target InstallReplTool => _ => _
        .DependsOn(PackReplTool)
        .Executes(() =>
        {
            const string packageId = "SharpNinja.McpServer.Repl";

            if (UninstallTool)
            {
                Log.Information("Uninstalling {Package}...", packageId);
                ProcessTasks.StartProcess("dotnet", $"tool uninstall --global {packageId}");
                return;
            }

            var args = UpdateTool
                ? $"tool update --global {packageId} --add-source \"{LocalPackagesDirectory}\""
                : $"tool install --global {packageId} --add-source \"{LocalPackagesDirectory}\"";

            Log.Information("{Action} {Package}...", UpdateTool ? "Updating" : "Installing", packageId);
            ProcessTasks.StartProcess("dotnet", args).AssertZeroExitCode();

            // Verify installation
            Log.Information("Verifying installation...");
            ProcessTasks.StartProcess("mcpserver-repl", "--version").AssertZeroExitCode();
        });
}
