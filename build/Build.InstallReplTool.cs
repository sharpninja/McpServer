using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;
using System.IO;
using System.Security;

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

            var packageSource = SecurityElement.Escape(LocalPackagesDirectory.ToString());
            var nugetConfig = LocalPackagesDirectory / "NuGet.LocalTool.config";
            File.WriteAllText(
                nugetConfig,
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local-packages" value="{packageSource}" />
                  </packageSources>
                </configuration>
                """);

            var versionArgs = string.IsNullOrWhiteSpace(PackageVersion)
                ? string.Empty
                : $" --version {PackageVersion}";

            var args = UpdateTool
                ? $"tool update --global {packageId} --configfile \"{nugetConfig}\"{versionArgs}"
                : $"tool install --global {packageId} --configfile \"{nugetConfig}\"{versionArgs}";

            Log.Information("{Action} {Package}...", UpdateTool ? "Updating" : "Installing", packageId);
            ProcessTasks.StartProcess("dotnet", args).AssertZeroExitCode();

            // Verify installation
            Log.Information("Verifying installation...");
            ProcessTasks.StartProcess("mcpserver-repl", "--version").AssertZeroExitCode();
        });
}
