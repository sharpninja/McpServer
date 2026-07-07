using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;
using System.IO;
using System.Security;

partial class Build
{
    [Parameter("Update existing tool installation instead of fresh install")]
    readonly bool UpdateTool = false;

    [Parameter("Uninstall the global tool")]
    readonly bool UninstallTool = false;

    /// <summary>Install, update, or uninstall the mcpserver-repl global tool.</summary>
    public Target InstallReplTool => _ => _
        .DependsOn(PackReplTool)
        .Executes(() =>
        {
            const string packageId = "SharpNinja.McpServer.Repl";
            var packageVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");

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

            var toolList = ProcessTasks.StartProcess("dotnet", "tool list --global", logOutput: false, logInvocation: false);
            toolList.WaitForExit();
            toolList.AssertZeroExitCode();
            var installedTools = string.Join('\n', toolList.Output.Select(static output => output.Text));
            var installedVersion = GetInstalledGlobalToolVersion(installedTools, packageId);
            var shouldInstall = true;

            if (!string.IsNullOrWhiteSpace(installedVersion))
            {
                if (UpdateTool || !string.Equals(installedVersion, packageVersion, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information(
                        "Uninstalling existing {Package} {InstalledVersion} before installing {PackageVersion}...",
                        packageId,
                        installedVersion,
                        packageVersion);
                    ProcessTasks.StartProcess("dotnet", $"tool uninstall --global {packageId}").AssertZeroExitCode();
                }
                else
                {
                    Log.Information("{Package} {Version} is already installed.", packageId, installedVersion);
                    shouldInstall = false;
                }
            }

            if (shouldInstall)
            {
                Log.Information("Installing {Package} {Version}...", packageId, packageVersion);
                ProcessTasks.StartProcess(
                        "dotnet",
                        $"tool install --global {packageId} --configfile \"{nugetConfig}\" --version {packageVersion}")
                    .AssertZeroExitCode();
            }

            // Verify installation
            Log.Information("Verifying installation...");
            ProcessTasks.StartProcess("mcpserver-repl", "--version").AssertZeroExitCode();
        });

    internal static string? GetInstalledGlobalToolVersion(string toolListOutput, string packageId)
    {
        ArgumentNullException.ThrowIfNull(toolListOutput);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        foreach (var line in toolListOutput.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
        {
            var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length >= 2 && columns[0].Equals(packageId, StringComparison.OrdinalIgnoreCase))
                return columns[1];
        }

        return null;
    }
}
