using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;
using System.IO;
using System.Security;

partial class Build
{
    /// <summary>Pack and force-install the qbagent .NET global tool from the local package source.</summary>
    public Target DeployQBAgentTool => _ => _
        .DependsOn(PackQBAgentTool)
        .Executes(() =>
        {
            const string packageId = "SharpNinja.McpServer.QBAgent";
            const string commandName = "qbagent";
            var packageVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            var packageSource = SecurityElement.Escape(LocalPackagesDirectory.ToString());
            var nugetConfig = LocalPackagesDirectory / "NuGet.QBAgentTool.config";
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
            if (installedTools.Contains(packageId, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Uninstalling existing {Package} global tool before redeploy...", packageId);
                ProcessTasks.StartProcess("dotnet", $"tool uninstall --global {packageId}").AssertZeroExitCode();
            }
            else
            {
                Log.Information("{Package} is not installed; skipping uninstall.", packageId);
            }

            Log.Information("Installing {Package} {Version} from local packages...", packageId, packageVersion);
            ProcessTasks.StartProcess(
                    "dotnet",
                    $"tool install --global {packageId} --configfile \"{nugetConfig}\" --version {packageVersion}")
                .AssertZeroExitCode();

            Log.Information("Verifying {Command} installation...", commandName);
            ProcessTasks.StartProcess(commandName, "--version").AssertZeroExitCode();
        });
}
