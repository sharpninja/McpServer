using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    private const string ServiceExeName = "McpServer.Support.Mcp.exe";
    private const string LauncherExeName = "McpServer.Launcher.exe";
    private static readonly string[] DeployPreservePatterns = ["appsettings.yaml"];
    private static readonly string[] DeployPreserveDirs = ["logs", "tools"];

    [Parameter("Windows service name")]
    readonly string ServiceName = "McpServer";

    [Parameter("Service installation path")]
    readonly AbsolutePath InstallPath = (AbsolutePath)@"C:\ProgramData\McpServer";

    [Parameter("Service port")]
    readonly int ServicePort = 7147;

    /// <summary>
    /// Publish both McpServer.Support.Mcp and McpServer.Launcher (self-contained win-x64 single-file)
    /// then deploy in-place: stop service, backup, copy, restore config, register, start, health-check.
    /// Exactly replicates scripts/Update-McpService.ps1 (update pipeline).
    /// Requires an elevated (Administrator) shell.
    /// </summary>
    public Target DeployService => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            AssertElevated();

            var installPath      = (string)InstallPath;
            var timestamp        = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            var backupDir        = Path.Combine(Path.GetTempPath(), $"McpServer-update-backup-{timestamp}");
            var archiveDir       = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "McpServer-Backups");
            var archivePath      = Path.Combine(archiveDir, $"McpServer-backup-{timestamp}.zip");
            var stageDir         = Path.Combine(Path.GetTempPath(), $"McpServer-publish-stage-{timestamp}");
            var launcherStageDir = Path.Combine(Path.GetTempPath(), $"McpServer-launcher-stage-{timestamp}");

            // ── 1. Stop service ───────────────────────────────────────────────
            Log.Information("1/9  Stopping service '{ServiceName}'...", ServiceName);
            StopDeployedService(ServiceName, ServiceExeName.Replace(".exe", ""));

            // ── 2. Backup preserved state ─────────────────────────────────────
            Log.Information("2/9  Backing up config and data files...");
            BackupDeployedState(installPath, backupDir, archiveDir, archivePath);

            // ── 3. Publish main project ───────────────────────────────────────
            Log.Information("3/9  Publishing new build (self-contained win-x64)...");
            if (Directory.Exists(stageDir)) Directory.Delete(stageDir, recursive: true);

            DotNetPublish(_ => _
                .SetProject(SourceDirectory / "McpServer.Support.Mcp" / "McpServer.Support.Mcp.csproj")
                .SetConfiguration("Release")
                .EnableSelfContained()
                .SetRuntime("win-x64")
                .AddProperty("PublishSingleFile", "true")
                .AddProperty("IncludeNativeLibrariesForSelfExtract", "true")
                .SetOutput(stageDir));

            // ── 3b. Publish launcher sidecar ──────────────────────────────────
            if (Directory.Exists(launcherStageDir)) Directory.Delete(launcherStageDir, recursive: true);

            DotNetPublish(_ => _
                .SetProject(SourceDirectory / "McpServer.Launcher" / "McpServer.Launcher.csproj")
                .SetConfiguration("Release")
                .EnableSelfContained()
                .SetRuntime("win-x64")
                .AddProperty("PublishSingleFile", "true")
                .AddProperty("IncludeNativeLibrariesForSelfExtract", "true")
                .SetOutput(launcherStageDir));

            var launcherSrc = Path.Combine(launcherStageDir, LauncherExeName);
            if (File.Exists(launcherSrc))
                File.Copy(launcherSrc, Path.Combine(stageDir, LauncherExeName), overwrite: true);
            else
                Log.Warning("Launcher publish output missing {LauncherExeName} — desktop launch will not work", LauncherExeName);

            if (Directory.Exists(launcherStageDir)) Directory.Delete(launcherStageDir, recursive: true);

            // ── 3c. Remove stale files then copy stage → install ──────────────
            Directory.CreateDirectory(installPath);
            RemoveStaleDeployContent(installPath, stageDir, DeployPreservePatterns, DeployPreserveDirs);
            CopyDeployDirectory(stageDir, installPath);
            Directory.Delete(stageDir, recursive: true);

            if (!File.Exists(Path.Combine(installPath, LauncherExeName)))
                Assert.Fail($"Deployment is missing {LauncherExeName} under {installPath}.");
            Log.Information("  Launcher sidecar present: {Path}", Path.Combine(installPath, LauncherExeName));

            // ── 4. Restore preserved config + data ────────────────────────────
            Log.Information("4/9  Restoring config and data files...");
            RestoreDeployedState(backupDir, installPath);

            // Remove legacy appsettings.json if present
            var legacyJson = Path.Combine(installPath, "appsettings.json");
            if (File.Exists(legacyJson)) { File.Delete(legacyJson); Log.Information("  Removed legacy appsettings.json"); }
            var legacyProd = Path.Combine(installPath, "appsettings.Production.json");
            if (File.Exists(legacyProd)) { File.Delete(legacyProd); Log.Information("  Removed legacy appsettings.Production.json"); }

            // ── 5. Ensure service registration ────────────────────────────────
            Log.Information("5/9  Ensuring service registration...");
            EnsureServiceRegistration(ServiceName, installPath, ServiceExeName, ServicePort);
            var manifestPath = WriteDeploymentManifest(installPath, ServiceName, ServiceExeName, ServicePort, "update");
            Log.Information("  Deployment manifest: {Path}", manifestPath);

            // ── 6. Start service ──────────────────────────────────────────────
            Log.Information("6/9  Starting service '{ServiceName}'...", ServiceName);
            StartDeployedService(ServiceName);
            Thread.Sleep(3000);

            // ── 7. Health check ───────────────────────────────────────────────
            Log.Information("7/9  Verifying health on port {Port}...", ServicePort);
            var healthy = WaitForHealth(ServicePort, attempts: 10, timeoutSec: 3, delaySec: 2);
            if (healthy) Log.Information("  Health check passed.");
            else         Log.Warning("Service did not respond to health check after 20 s.");

            // ── 8. Workspace health checks ────────────────────────────────────
            Log.Information("8/9  Workspace health checks...");
            var deployedSettings = Path.Combine(installPath, "appsettings.yaml");
            if (!File.Exists(deployedSettings))
                Log.Warning("No deployed appsettings.yaml found; skipping workspace health checks.");
            else
                Log.Information("  (Workspace checks require running server; primary health = {H})", healthy ? "OK" : "FAILED");

            // ── 9. Cleanup ────────────────────────────────────────────────────
            Log.Information("9/9  Cleanup...");
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, recursive: true);

            Log.Information("=== Deploy complete ===");
            Log.Information("  Service : {Name}", ServiceName);
            Log.Information("  Path    : {Path}", installPath);
            Log.Information("  Health  : {H}", healthy ? "OK" : "FAILED");
            if (File.Exists(archivePath)) Log.Information("  Archive : {A}", archivePath);

            if (!healthy) Assert.Fail("Service health check failed after deployment.");
        });

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void AssertElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            Assert.Fail("DeployService must be run from an elevated (Administrator) shell. Use: gsudo ./build.ps1 DeployService");
    }

    private static bool QueryServiceExists(string name)
    {
        var result = ProcessTasks.StartProcess("sc.exe", $"query {name}", logOutput: false, logInvocation: false);
        result.WaitForExit();
        return result.ExitCode == 0;
    }

    private static bool QueryServiceRunning(string name)
    {
        var result = ProcessTasks.StartProcess("sc.exe", $"query {name}", logOutput: false, logInvocation: false);
        result.WaitForExit();
        return result.ExitCode == 0 &&
               result.Output.Any(l => l.Text.Contains("RUNNING", StringComparison.OrdinalIgnoreCase));
    }

    private static void StopDeployedService(string name, string processName)
    {
        if (!QueryServiceExists(name)) { Log.Information("  Service not installed yet."); return; }

        if (QueryServiceRunning(name))
        {
            ProcessTasks.StartProcess("sc.exe", $"stop {name}").AssertZeroExitCode();
            // Wait up to 30 s for the process to exit
            for (var i = 0; i < 30; i++)
            {
                if (!System.Diagnostics.Process.GetProcessesByName(processName).Any()) break;
                Thread.Sleep(1000);
            }
            foreach (var p in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                try { p.Kill(); } catch { /* ignore */ }
            }
            Log.Information("  Service stopped.");
        }
        else Log.Information("  Service was not running.");
    }

    private static void BackupDeployedState(string installPath, string backupDir, string archiveDir, string archivePath)
    {
        if (!Directory.Exists(installPath)) { Log.Information("  Install path does not exist; no backup needed."); return; }

        Directory.CreateDirectory(backupDir);

        foreach (var pattern in DeployPreservePatterns)
        {
            foreach (var f in Directory.GetFiles(installPath, pattern))
                File.Copy(f, Path.Combine(backupDir, Path.GetFileName(f)), overwrite: true);
        }

        var dataFolder = ResolveDataFolder(installPath);
        var dataBackupDir = Path.Combine(backupDir, "data");
        Directory.CreateDirectory(dataBackupDir);

        if (string.Equals(
            Path.GetFullPath(dataFolder).TrimEnd('\\', '/'),
            Path.GetFullPath(installPath).TrimEnd('\\', '/'),
            StringComparison.OrdinalIgnoreCase))
        {
            // Data folder IS install root — back up known runtime patterns only
            foreach (var pattern in new[] { "*.db", "*.db-shm", "*.db-wal" })
                foreach (var f in Directory.GetFiles(installPath, pattern))
                    File.Copy(f, Path.Combine(dataBackupDir, Path.GetFileName(f)), overwrite: true);
            foreach (var dir in new[] { "mcp-data", "templates", "tools", "logs" })
            {
                var src = Path.Combine(installPath, dir);
                if (Directory.Exists(src)) CopyDeployDirectory(src, Path.Combine(dataBackupDir, dir));
            }
        }
        else if (Directory.Exists(dataFolder))
        {
            CopyDeployDirectory(dataFolder, dataBackupDir);
        }

        // Create archive zip
        Directory.CreateDirectory(archiveDir);
        if (File.Exists(archivePath)) File.Delete(archivePath);
        ZipFile.CreateFromDirectory(backupDir, archivePath);
        Log.Information("  Archived to: {Archive}", archivePath);
    }

    private static void RestoreDeployedState(string backupDir, string installPath)
    {
        if (!Directory.Exists(backupDir)) return;

        foreach (var name in new[] { "appsettings.yaml" })
        {
            var src = Path.Combine(backupDir, name);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(installPath, name), overwrite: true);
                Log.Information("  Restored config: {Name}", name);
            }
        }

        var dataRestoreSrc = Path.Combine(backupDir, "data");
        if (Directory.Exists(dataRestoreSrc))
        {
            var dataFolder = ResolveDataFolder(installPath);
            Directory.CreateDirectory(dataFolder);
            CopyDeployDirectory(dataRestoreSrc, dataFolder);
            Log.Information("  Restored data folder: {Folder}", dataFolder);
        }
    }

    private static string ResolveDataFolder(string installPath)
    {
        var yamlPath = Path.Combine(installPath, "appsettings.yaml");
        if (File.Exists(yamlPath))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                File.ReadAllText(yamlPath),
                @"(?m)^\s*DataFolder\s*:\s*(.+?)\s*$");
            if (match.Success)
            {
                var raw = match.Groups[1].Value.Trim().Trim('\'', '"');
                if (!string.IsNullOrWhiteSpace(raw))
                    return Path.IsPathRooted(raw) ? raw : Path.GetFullPath(Path.Combine(installPath, raw));
            }
        }
        return installPath;
    }

    private static void RemoveStaleDeployContent(string installRoot, string publishRoot,
        string[] preserveFilePatterns, string[] preserveDirNames)
    {
        if (!Directory.Exists(installRoot) || !Directory.Exists(publishRoot)) return;

        var sourceFiles = new HashSet<string>(
            Directory.GetFiles(publishRoot, "*", SearchOption.AllDirectories)
                     .Select(f => Path.GetRelativePath(publishRoot, f)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(installRoot, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(installRoot, file);
            var topSegment = rel.Split(Path.DirectorySeparatorChar)[0];
            if (preserveDirNames.Contains(topSegment, StringComparer.OrdinalIgnoreCase)) continue;
            if (!rel.Contains(Path.DirectorySeparatorChar) &&
                preserveFilePatterns.Any(p => FileMatchesGlob(Path.GetFileName(rel), p))) continue;
            if (!sourceFiles.Contains(rel))
            {
                File.Delete(file);
                Log.Debug("  Removed stale file: {File}", rel);
            }
        }
    }

    private static bool FileMatchesGlob(string name, string pattern) =>
        System.Text.RegularExpressions.Regex.IsMatch(name,
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static void CopyDeployDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(src, dir)));
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(dest, Path.GetRelativePath(src, file)), overwrite: true);
    }

    private static void EnsureServiceRegistration(string name, string installRoot, string exeName, int port)
    {
        var exePath = Path.Combine(installRoot, exeName);
        if (!File.Exists(exePath)) Assert.Fail($"Deployment is missing {exeName} under {installRoot}.");

        // sc.exe binPath= quoting is finnicky — delegate to a temp PS1 to match the exact
        // quoting used by scripts/Update-McpService.ps1.
        var script = Path.ChangeExtension(Path.GetTempFileName(), ".ps1");
        try
        {
            File.WriteAllText(script, $@"
$ErrorActionPreference = 'Stop'
$exePath = '{exePath.Replace("'", "''")}'
$binPath = ""`""$exePath`"" --urls `""http://+:{port}`""""
$svc = Get-Service -Name '{name}' -ErrorAction SilentlyContinue
if ($svc) {{
    sc.exe config '{name}' binPath= $binPath start= auto
}} else {{
    sc.exe create '{name}' binPath= $binPath start= auto DisplayName= 'MCP Server'
}}
if ($LASTEXITCODE -ne 0) {{ exit $LASTEXITCODE }}
sc.exe description '{name}' 'MCP Model Context Protocol Server'
if ($LASTEXITCODE -ne 0) {{ exit $LASTEXITCODE }}
sc.exe failure '{name}' reset= 86400 actions= restart/60000/restart/60000/restart/60000
if ($LASTEXITCODE -ne 0) {{ exit $LASTEXITCODE }}
");
            ProcessTasks.StartProcess("powershell.exe",
                $"-ExecutionPolicy Bypass -File \"{script}\"").AssertZeroExitCode();
        }
        finally
        {
            if (File.Exists(script)) File.Delete(script);
        }

        Log.Information("  Service registration updated.");
    }

    private static string WriteDeploymentManifest(string installRoot, string serviceName, string exeName, int port, string operation)
    {
        var hashes = Directory.GetFiles(installRoot, "*.exe")
            .OrderBy(f => Path.GetFileName(f))
            .Select(f => new { name = Path.GetFileName(f), sha256 = ComputeSha256(f) })
            .ToArray();

        var manifest = new
        {
            schemaVersion    = 1,
            generatedUtc     = DateTime.UtcNow.ToString("o"),
            generatedBy      = @"scripts\Update-McpService.ps1",
            operation,
            serviceName,
            executable       = exeName,
            port,
            executableHashes = hashes
        };

        var path = Path.Combine(installRoot, ".mcpservice-deployment.json");
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    private static void StartDeployedService(string name) =>
        ProcessTasks.StartProcess("sc.exe", $"start {name}").AssertZeroExitCode();

    private static bool WaitForHealth(int port, int attempts, int timeoutSec, int delaySec)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSec) };
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                var resp = client.GetAsync($"http://localhost:{port}/health").GetAwaiter().GetResult();
                if (resp.IsSuccessStatusCode)
                {
                    Log.Information("  Health: HTTP {Status}", (int)resp.StatusCode);
                    return true;
                }
            }
            catch { /* not ready yet */ }
            if (i < attempts - 1) Thread.Sleep(delaySec * 1000);
        }
        return false;
    }
}
