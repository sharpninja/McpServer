using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable CA1416 // Platform compatibility — UpdateService target is Windows-only

partial class Build
{
    const string DefaultServiceName = "McpServer";
    const string DefaultInstallPath = @"C:\ProgramData\McpServer";
    const int DefaultPort = 7147;
    const string MainExeName = "McpServer.Support.Mcp.exe";
    const string LauncherExeName = "McpServer.Launcher.exe";

    [Parameter("Windows service name (default: McpServer)")]
    readonly string ServiceName = DefaultServiceName;

    [Parameter("Service installation directory (default: C:\\ProgramData\\McpServer)")]
    readonly string InstallPath = DefaultInstallPath;

    [Parameter("HTTP port for the service (default: 7147)")]
    readonly int Port = DefaultPort;

    [Parameter("Skip build — use existing publish output from --publish-source")]
    readonly bool SkipBuild;

    [Parameter("Skip GitVersion.yml patch version bump")]
    readonly bool SkipVersionBump;

    [Parameter("Path to pre-built publish output (used with --skip-build)")]
    readonly string PublishSource;

    /// <summary>
    /// Deploy the MCP server as a Windows service: stop, backup, publish, restore config, register, start, and health check.
    /// Requires elevation (Administrator). Invoke via: sudo --chdir . pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService
    /// </summary>
    public Target UpdateService => _ => _
        .Description("Deploy MCP server as a Windows service (stop → backup → publish → restore → register → start → verify)")
        .Executes(() =>
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            var backupDir = Path.Combine(Path.GetTempPath(), $"McpServer-update-backup-{timestamp}");
            var archiveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "McpServer-Backups");
            var archivePath = Path.Combine(archiveDir, $"McpServer-backup-{timestamp}.zip");
            var serviceProcessName = MainExeName.Replace(".exe", "");

            // Step 0: Assert elevated
            WindowsServiceHelper.AssertElevated();

            // Step 1: Version bump (conditional)
            if (!SkipBuild && !SkipVersionBump)
            {
                Log.Information(">> 0/{Total}  Bumping GitVersion next-version patch ...", 8);
                var gitVersionPath = RootDirectory / "GitVersion.yml";
                var content = File.ReadAllText(gitVersionPath);
                var result = GitVersionBumper.BumpPatch(content)
                    ?? throw new InvalidOperationException("Could not parse next-version from GitVersion.yml.");
                File.WriteAllText(gitVersionPath, result.NewContent);
                Log.Information("  {Old} -> {New}", result.OldVersion, result.NewVersion);
                ProcessTasks.StartProcess("git", $"-C \"{RootDirectory}\" add GitVersion.yml")
                    .AssertZeroExitCode();
            }

            // Step 2: Stop service
            Log.Information(">> 1/{Total}  Stopping service '{ServiceName}' ...", 8, ServiceName);
            WindowsServiceHelper.StopService(ServiceName, serviceProcessName);

            var deploymentVersion = ResolveNuGetPackageVersion(PackageVersion, RootDirectory / "GitVersion.yml");
            Log.Information("  Deployment version: {Version}", deploymentVersion);

            // Step 3: Backup config and data
            Log.Information(">> 2/{Total}  Backing up config and data files ...", 8);
            var backup = WindowsServiceHelper.BackupPreservedState(InstallPath, backupDir, archivePath);

            // Step 4: Publish new build
            Log.Information(">> 3/{Total}  Publishing new build ...", 8);
            string stageDir;

            if (SkipBuild)
            {
                if (string.IsNullOrWhiteSpace(PublishSource))
                    throw new InvalidOperationException("--publish-source is required when --skip-build is set.");
                if (!Directory.Exists(PublishSource))
                    throw new DirectoryNotFoundException($"Publish source not found: {PublishSource}");
                stageDir = PublishSource;
            }
            else
            {
                stageDir = Path.Combine(Path.GetTempPath(), "McpServer-publish-stage");
                if (Directory.Exists(stageDir))
                    Directory.Delete(stageDir, true);

                // Publish main server
                var project = SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj";
                DotNetPublish(_ => _
                    .SetProject(project)
                    .SetConfiguration("Release")
                    .EnableSelfContained()
                    .SetRuntime("win-x64")
                    .SetProperty("PublishSingleFile", "true")
                    .SetProperty("IncludeNativeLibrariesForSelfExtract", "true")
                    .SetProperty("PackageVersion", deploymentVersion)
                    .SetProperty("Version", deploymentVersion)
                    .SetProperty("InformationalVersion", deploymentVersion)
                    .SetOutput(stageDir));

                // Publish launcher sidecar
                var launcherProject = SourceDirectory / "McpServer.Launcher" / "McpServer.Launcher.csproj";
                if (File.Exists(launcherProject))
                {
                    var launcherStage = Path.Combine(Path.GetTempPath(), "McpServer-launcher-stage");
                    if (Directory.Exists(launcherStage))
                        Directory.Delete(launcherStage, true);

                    DotNetPublish(_ => _
                        .SetProject(launcherProject)
                        .SetConfiguration("Release")
                        .EnableSelfContained()
                        .SetRuntime("win-x64")
                        .SetProperty("PublishSingleFile", "true")
                        .SetProperty("IncludeNativeLibrariesForSelfExtract", "true")
                        .SetProperty("PackageVersion", deploymentVersion)
                        .SetProperty("Version", deploymentVersion)
                        .SetProperty("InformationalVersion", deploymentVersion)
                        .SetOutput(launcherStage));

                    var launcherExe = Path.Combine(launcherStage, LauncherExeName);
                    if (File.Exists(launcherExe))
                        File.Copy(launcherExe, Path.Combine(stageDir, LauncherExeName), true);

                    Directory.Delete(launcherStage, true);
                }
            }

            // Remove stale files and copy
            Log.Information("  Cleaning stale files before copy ...");
            WindowsServiceHelper.RemoveStaleInstallContent(InstallPath, stageDir);
            WindowsServiceHelper.CopyDirectory(stageDir, InstallPath);
            CopyBrainSlotRuntimeConfig(RootDirectory, InstallPath);

            // Verify launcher sidecar
            var launcherPath = Path.Combine(InstallPath, LauncherExeName);
            if (File.Exists(launcherPath))
                Log.Information("  Launcher sidecar present: {Path}", launcherPath);

            Log.Information("  Publish complete.");

            // Clean up staging dir (only if we created it)
            if (!SkipBuild && Directory.Exists(stageDir))
                Directory.Delete(stageDir, true);

            // Step 5: Restore config and data
            Log.Information(">> 4/{Total}  Restoring config and data files ...", 8);
            WindowsServiceHelper.RestorePreservedState(backupDir, InstallPath);

            // Step 6: Ensure service registration
            Log.Information(">> 5/{Total}  Ensuring service registration ...", 9);
            WindowsServiceHelper.EnsureServiceRegistration(ServiceName, InstallPath, MainExeName, Port);

            // Step 7: Write deployment manifest
            WindowsServiceHelper.WriteDeploymentManifest(InstallPath, ServiceName, MainExeName, Port, "update");

            // Step 8: Start service and verify health
            Log.Information(">> 6/{Total}  Starting service '{ServiceName}' ...", 9, ServiceName);
            WindowsServiceHelper.StartService(ServiceName);

            Log.Information(">> 7/{Total}  Verifying health on port {Port} ...", 9, Port);
            var health = WindowsServiceHelper.CheckHealth(Port);
            if (!health.Healthy)
                throw new InvalidOperationException($"Health check failed: {health.Error}");

            // Step 9: Workspace health
            Log.Information(">> 8/{Total}  Verifying workspace health checks from deployed config ...", 9);
            var wsHealth = WindowsServiceHelper.CheckWorkspaceHealth(InstallPath, Port);

            // Step 10: Cleanup
            Log.Information(">> 9/{Total}  Cleanup ...", 9);
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
                Log.Information("  Backup directory removed.");
            }

            // Summary
            Log.Information("");
            Log.Information("=== Update complete ===");
            Log.Information("  Service : {Name} (Running)", ServiceName);
            Log.Information("  Path    : {Path}", InstallPath);
            Log.Information("  Health  : OK");
            Log.Information("  WSHealth: OK ({Healthy}/{Checked})", wsHealth.Healthy, wsHealth.Checked);
            Log.Information("  Config  : {Restored} restored, {BackedUp} backed up",
                backup.BackedUpConfig.Length, backup.BackedUpConfig.Length);
            Log.Information("  Data    : {Restored} restored item(s), {BackedUp} backed up item(s)",
                backup.BackedUpData.Length, backup.BackedUpData.Length);
            if (backup.ArchivePath != null)
                Log.Information("  Archive : {Path}", backup.ArchivePath);
        });
}
