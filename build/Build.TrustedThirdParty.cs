using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable CA1416 // Platform compatibility - trusted-party service deployment is Windows-only

partial class Build
{
    const string TrustedThirdPartyServiceDefaultName = "McpServerKeyServer";
    const string TrustedThirdPartyDefaultInstallPath = @"C:\ProgramData\McpServer-KeyServer";
    const int TrustedThirdPartyDefaultPort = 7146;
    const string TrustedThirdPartyExeName = "McpServer.KeyServer.exe";
    const string AgentExeName = "McpServer.McpAgent.SampleHost.exe";

    [Parameter("Trusted third-party Windows service name (default: McpServerKeyServer)")]
    readonly string TrustedThirdPartyServiceName = TrustedThirdPartyServiceDefaultName;

    [Parameter("Trusted third-party installation directory (default: C:\\ProgramData\\McpServer-KeyServer)")]
    readonly string TrustedThirdPartyInstallPath = TrustedThirdPartyDefaultInstallPath;

    [Parameter("Trusted third-party HTTP port (default: 7146)")]
    readonly int TrustedThirdPartyPort = TrustedThirdPartyDefaultPort;

    [Parameter("Path to pre-built trusted third-party publish output (used with --skip-build)")]
    readonly string TrustedThirdPartyPublishSource;

    /// <summary>Builds the trusted third-party transaction keyserver host.</summary>
    public Target BuildTrustedThirdParty => _ => _
        .DependsOn(Restore)
        .Description("Build the trusted third-party transaction keyserver host")
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(TrustedThirdPartyProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    /// <summary>Publishes the trusted third-party transaction keyserver host.</summary>
    public Target PublishTrustedThirdParty => _ => _
        .DependsOn(BuildTrustedThirdParty)
        .Description("Publish the trusted third-party transaction keyserver host")
        .Executes(() =>
        {
            PublishSingleFileProject(TrustedThirdPartyProject, ArtifactsDirectory / "trusted-third-party");
        });

    /// <summary>Builds the MCP Agent sample host used for agent-framework integration.</summary>
    public Target BuildAgent => _ => _
        .DependsOn(Restore)
        .Description("Build the MCP Agent host")
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(AgentProject)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    /// <summary>Publishes the MCP Agent sample host used for agent-framework integration.</summary>
    public Target PublishAgent => _ => _
        .DependsOn(BuildAgent)
        .Description("Publish the MCP Agent host")
        .Executes(() =>
        {
            PublishSingleFileProject(AgentProject, ArtifactsDirectory / "mcp-agent");
        });

    /// <summary>
    /// Deploys or updates the trusted third-party transaction keyserver as a Windows service.
    /// Requires elevation. Invoke via: gsudo ./build.ps1 UpdateTrustedThirdPartyService
    /// </summary>
    public Target UpdateTrustedThirdPartyService => _ => _
        .Description("Deploy trusted third-party transaction keyserver as a Windows service")
        .Executes(() =>
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            var backupDir = Path.Combine(Path.GetTempPath(), $"McpServer-keyserver-update-backup-{timestamp}");
            var archiveDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "McpServer-Backups");
            var archivePath = Path.Combine(archiveDir, $"McpServer-keyserver-backup-{timestamp}.zip");
            var serviceProcessName = TrustedThirdPartyExeName.Replace(".exe", "");
            string stageDir;
            var ownsStageDir = false;

            WindowsServiceHelper.AssertElevated(nameof(UpdateTrustedThirdPartyService));

            Log.Information(">> 1/{Total}  Stopping service '{ServiceName}' ...", 8, TrustedThirdPartyServiceName);
            WindowsServiceHelper.StopService(TrustedThirdPartyServiceName, serviceProcessName);

            Log.Information(">> 2/{Total}  Backing up trusted third-party config and data ...", 8);
            var backup = WindowsServiceHelper.BackupPreservedState(
                TrustedThirdPartyInstallPath,
                backupDir,
                archivePath);

            Log.Information(">> 3/{Total}  Publishing trusted third-party build ...", 8);
            if (SkipBuild)
            {
                if (string.IsNullOrWhiteSpace(TrustedThirdPartyPublishSource))
                    throw new InvalidOperationException("--trusted-third-party-publish-source is required when --skip-build is set.");
                if (!Directory.Exists(TrustedThirdPartyPublishSource))
                    throw new DirectoryNotFoundException($"Trusted third-party publish source not found: {TrustedThirdPartyPublishSource}");

                stageDir = TrustedThirdPartyPublishSource;
            }
            else
            {
                stageDir = Path.Combine(Path.GetTempPath(), "McpServer-keyserver-publish-stage");
                if (Directory.Exists(stageDir))
                    Directory.Delete(stageDir, true);

                PublishSingleFileProject(TrustedThirdPartyProject, stageDir);
                ownsStageDir = true;
            }

            Log.Information("  Cleaning stale trusted third-party files before copy ...");
            WindowsServiceHelper.RemoveStaleInstallContent(TrustedThirdPartyInstallPath, stageDir);
            WindowsServiceHelper.CopyDirectory(stageDir, TrustedThirdPartyInstallPath);
            Log.Information("  Trusted third-party publish complete.");

            if (ownsStageDir && Directory.Exists(stageDir))
                Directory.Delete(stageDir, true);

            Log.Information(">> 4/{Total}  Restoring trusted third-party config and data files ...", 8);
            WindowsServiceHelper.RestorePreservedState(backupDir, TrustedThirdPartyInstallPath);

            Log.Information(">> 5/{Total}  Ensuring trusted third-party service registration ...", 8);
            WindowsServiceHelper.EnsureServiceRegistration(
                TrustedThirdPartyServiceName,
                TrustedThirdPartyInstallPath,
                TrustedThirdPartyExeName,
                TrustedThirdPartyPort,
                "MCP Transaction Key Server",
                "MCP trusted third-party transaction keyserver");

            WindowsServiceHelper.WriteDeploymentManifest(
                TrustedThirdPartyInstallPath,
                TrustedThirdPartyServiceName,
                TrustedThirdPartyExeName,
                TrustedThirdPartyPort,
                "trusted-third-party-update");

            Log.Information(">> 6/{Total}  Starting service '{ServiceName}' ...", 8, TrustedThirdPartyServiceName);
            WindowsServiceHelper.StartService(TrustedThirdPartyServiceName);

            Log.Information(">> 7/{Total}  Verifying trusted third-party health on port {Port} ...", 8, TrustedThirdPartyPort);
            var health = WindowsServiceHelper.CheckHealth(TrustedThirdPartyPort);
            if (!health.Healthy)
                throw new InvalidOperationException($"Trusted third-party health check failed: {health.Error}");

            Log.Information(">> 8/{Total}  Cleanup ...", 8);
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
                Log.Information("  Backup directory removed.");
            }

            Log.Information("");
            Log.Information("=== Trusted third-party update complete ===");
            Log.Information("  Service : {Name} (Running)", TrustedThirdPartyServiceName);
            Log.Information("  Path    : {Path}", TrustedThirdPartyInstallPath);
            Log.Information("  Health  : OK");
            Log.Information("  Config  : {Restored} restored, {BackedUp} backed up",
                backup.BackedUpConfig.Length,
                backup.BackedUpConfig.Length);
            Log.Information("  Data    : {Restored} restored item(s), {BackedUp} backed up item(s)",
                backup.BackedUpData.Length,
                backup.BackedUpData.Length);
            if (backup.ArchivePath != null)
                Log.Information("  Archive : {Path}", backup.ArchivePath);
        });

    AbsolutePath TrustedThirdPartyProject => SourceDirectory / "McpServer.KeyServer" / "McpServer.KeyServer.csproj";

    AbsolutePath AgentProject => SourceDirectory / "McpServer.McpAgent.SampleHost" / "McpServer.McpAgent.SampleHost.csproj";

    void PublishSingleFileProject(AbsolutePath project, AbsolutePath output)
    {
        DotNetPublish(_ => _
            .SetProject(project)
            .SetConfiguration("Release")
            .EnableSelfContained()
            .SetRuntime("win-x64")
            .SetProperty("PublishSingleFile", "true")
            .SetProperty("IncludeNativeLibrariesForSelfExtract", "true")
            .SetOutput(output));
    }
}
