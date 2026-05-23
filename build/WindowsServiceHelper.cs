using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nuke.Common.Tooling;
using Serilog;

#pragma warning disable CA1416 // Platform compatibility — this helper is Windows-only by design

/// <summary>
/// Provides utilities for Windows service deployment: stop/start, backup/restore,
/// service registration, health checking, and stale file cleanup.
/// </summary>
static partial class WindowsServiceHelper
{
    private static readonly string[] PreservePatterns = ["appsettings.yaml"];
    private static readonly string[] PreserveDirectories = ["logs", "tools"];
    private static readonly string[] LegacyDataDirectories = ["mcp-data", "templates", "tools", "logs"];
    private static readonly string[] LegacyDataGlobs = ["*.db", "*.db-shm", "*.db-wal"];

    /// <summary>Asserts that the current process is running elevated (Administrator).</summary>
    public static void AssertElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException(
                "This target must be run elevated. Use: gsudo ./build.ps1 UpdateService");
    }

    /// <summary>Checks whether a Windows service with the given name exists.</summary>
    public static bool ServiceExists(string serviceName)
    {
        var process = ProcessTasks.StartProcess("sc.exe", $"query {serviceName}",
            logOutput: false, logInvocation: false);
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    /// <summary>Stops the Windows service and waits for the process to exit.</summary>
    public static bool StopService(string serviceName, string processName, int timeoutSeconds = 30)
    {
        if (!ServiceExists(serviceName))
        {
            Log.Information("  Service is not installed yet.");
            return false;
        }

        // Check if running via sc.exe query
        var query = ProcessTasks.StartProcess("sc.exe", $"query {serviceName}",
            logOutput: false, logInvocation: false);
        query.WaitForExit();
        var output = string.Join('\n', query.Output.Select(o => o.Text));
        var isRunning = output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase);

        if (!isRunning)
        {
            Log.Information("  Service was not running.");
            return false;
        }

        // Stop the service — exit code 1062 means already stopped
        var stop = ProcessTasks.StartProcess("sc.exe", $"stop {serviceName}",
            logOutput: false, logInvocation: false);
        stop.WaitForExit();
        if (stop.ExitCode != 0 && stop.ExitCode != 1062)
            Log.Warning("sc.exe stop exited with code {ExitCode}", stop.ExitCode);

        // Wait for process to exit
        if (!WaitForProcessExit(processName, timeoutSeconds))
        {
            Log.Warning("Process did not exit within {Timeout}s — forcing termination", timeoutSeconds);
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                try { proc.Kill(); }
                catch { /* best effort */ }
                finally { proc.Dispose(); }
            }

            Thread.Sleep(2000);
        }

        Log.Information("  Service stopped.");
        return true;
    }

    /// <summary>Starts the Windows service and waits for it to initialize.</summary>
    public static void StartService(string serviceName, int waitSeconds = 3)
    {
        ProcessTasks.StartProcess("sc.exe", $"start {serviceName}",
            logOutput: false, logInvocation: false).WaitForExit();
        Thread.Sleep(waitSeconds * 1000);

        var query = ProcessTasks.StartProcess("sc.exe", $"query {serviceName}",
            logOutput: false, logInvocation: false);
        query.WaitForExit();
        var output = string.Join('\n', query.Output.Select(o => o.Text));
        var status = output.Contains("RUNNING", StringComparison.OrdinalIgnoreCase) ? "Running" : "Unknown";
        Log.Information("  Service status: {Status}", status);
    }

    /// <summary>Creates or updates the Windows service registration.</summary>
    public static void EnsureServiceRegistration(string serviceName, string installRoot, string exeName, int port)
    {
        var exePath = Path.Combine(installRoot, exeName);
        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Deployment is missing {exeName} under {installRoot}.");

        var binPath = GetServiceImagePath(installRoot, exeName, port);
        const string displayName = "MCP Server";
        const string description = "MCP Model Context Protocol Server";

        // sc.exe requires binPath= value where the value is a single argument.
        // When the value itself contains quotes/spaces, wrap the entire value in an outer set of quotes.
        var quotedBinPath = $"\"{binPath}\"";

        if (ServiceExists(serviceName))
        {
            RunScRaw($"config {serviceName} binPath= {quotedBinPath} start= auto");
        }
        else
        {
            RunScRaw($"create {serviceName} binPath= {quotedBinPath} start= auto DisplayName= \"{displayName}\"");
        }

        RunScRaw($"description {serviceName} \"{description}\"");
        RunScRaw($"failure {serviceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");
    }

    /// <summary>Backs up preserved configuration and data from the install root.</summary>
    public static BackupResult BackupPreservedState(string installRoot, string backupDir, string archivePath)
    {
        if (Directory.Exists(backupDir))
            Directory.Delete(backupDir, true);
        Directory.CreateDirectory(backupDir);

        var dataFolder = GetConfiguredDataFolder(installRoot);
        var dataBackupDir = Path.Combine(backupDir, "data");
        Directory.CreateDirectory(dataBackupDir);

        // Backup config files
        var backedUpConfig = new List<string>();
        foreach (var pattern in PreservePatterns)
        {
            var filePath = Path.Combine(installRoot, pattern);
            if (File.Exists(filePath))
            {
                File.Copy(filePath, Path.Combine(backupDir, pattern), true);
                backedUpConfig.Add(pattern);
            }
        }

        // Backup data
        var backedUpData = BackupDataFolderContents(dataFolder, installRoot, dataBackupDir);

        // Create archive
        string? archiveResult = null;
        if (backedUpConfig.Count > 0 || backedUpData.Count > 0)
        {
            var archiveDir = Path.GetDirectoryName(archivePath)!;
            if (!Directory.Exists(archiveDir))
                Directory.CreateDirectory(archiveDir);
            if (File.Exists(archivePath))
                File.Delete(archivePath);

            ZipFile.CreateFromDirectory(backupDir, archivePath);
            archiveResult = archivePath;
        }

        Log.Information("  Data folder: {DataFolder}", dataFolder);
        if (backedUpData.Count > 0)
            Log.Information("  Backed up data items: {Items}", string.Join(", ", backedUpData));
        if (backedUpConfig.Count > 0)
            Log.Information("  Backed up config files: {Items}", string.Join(", ", backedUpConfig));
        if (archiveResult != null)
            Log.Information("  Archived to: {Path}", archiveResult);

        return new BackupResult(dataFolder, [.. backedUpConfig], [.. backedUpData], archiveResult);
    }

    /// <summary>Restores preserved configuration and data to the install root.</summary>
    public static RestoreResult RestorePreservedState(string restoreSource, string installRoot)
    {
        if (!Directory.Exists(restoreSource))
            throw new DirectoryNotFoundException($"Restore source path not found: {restoreSource}");

        // Restore config files
        var restored = new List<string>();
        foreach (var name in PreservePatterns)
        {
            var sourcePath = Path.Combine(restoreSource, name);
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, Path.Combine(installRoot, name), true);
                restored.Add(name);
            }
        }

        // Restore data
        var restoredDataFolder = GetConfiguredDataFolder(installRoot);
        var dataRestoreSource = Path.Combine(restoreSource, "data");
        var restoredData = RestoreDataFolderContents(dataRestoreSource, restoredDataFolder);

        // Remove legacy configs
        var legacyRemoved = new List<string>();
        foreach (var name in new[] { "appsettings.json", "appsettings.Production.json" })
        {
            var path = Path.Combine(installRoot, name);
            if (File.Exists(path))
            {
                File.Delete(path);
                legacyRemoved.Add(name);
            }
        }

        if (restored.Count > 0)
            Log.Information("  Restored config files: {Items}", string.Join(", ", restored));
        Log.Information("  Restored data folder: {Path}", restoredDataFolder);
        if (restoredData.Count > 0)
            Log.Information("  Restored data items: {Items}", string.Join(", ", restoredData));

        return new RestoreResult([.. restored], restoredDataFolder, [.. restoredData], [.. legacyRemoved]);
    }

    /// <summary>Removes files from the install directory that are not present in the publish output.</summary>
    public static (int FilesRemoved, int DirsRemoved) RemoveStaleInstallContent(
        string installRoot, string publishRoot)
    {
        if (!Directory.Exists(installRoot) || !Directory.Exists(publishRoot))
            return (0, 0);

        var comparer = StringComparer.OrdinalIgnoreCase;
        var sourceFiles = new HashSet<string>(comparer);
        var sourceDirs = new HashSet<string>(comparer);

        foreach (var entry in Directory.EnumerateFileSystemEntries(publishRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(publishRoot, entry);
            if (Directory.Exists(entry))
                sourceDirs.Add(relative);
            else
                sourceFiles.Add(relative);
        }

        var preserveDirSet = new HashSet<string>(PreserveDirectories.Concat(LegacyDataDirectories), comparer);

        int filesRemoved = 0;
        foreach (var file in Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(installRoot, file);
            if (IsPreserved(relative, preserveDirSet))
                continue;
            if (!sourceFiles.Contains(relative))
            {
                File.Delete(file);
                filesRemoved++;
            }
        }

        int dirsRemoved = 0;
        var dirs = Directory.EnumerateDirectories(installRoot, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length)
            .ToList();
        foreach (var dir in dirs)
        {
            var relative = Path.GetRelativePath(installRoot, dir);
            var topSegment = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            if (preserveDirSet.Contains(topSegment))
                continue;
            if (!sourceDirs.Contains(relative) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
                dirsRemoved++;
            }
        }

        Log.Information("  Removed stale items: {Files} file(s), {Dirs} directories", filesRemoved, dirsRemoved);
        return (filesRemoved, dirsRemoved);
    }

    /// <summary>Recursively copies all files from source to destination.</summary>
    public static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destPath = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, true);
        }
    }

    /// <summary>Checks the health endpoint with retries.</summary>
    public static HealthResult CheckHealth(int port, int attempts = 10, int timeoutSeconds = 3, int delaySeconds = 2)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
        string? lastError = null;

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var response = client.GetAsync($"http://localhost:{port}/health").GetAwaiter().GetResult();
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("  Health: HTTP {StatusCode} - {Content}", (int)response.StatusCode, content);
                    return new HealthResult(true, (int)response.StatusCode, content, null);
                }

                lastError = $"HTTP {(int)response.StatusCode}: {content}";
            }
            catch (Exception ex)
            {
                lastError = ex.InnerException?.Message ?? ex.Message;
            }

            if (attempt < attempts)
                Thread.Sleep(delaySeconds * 1000);
        }

        Log.Error("  Health check failed after {Attempts} attempts: {Error}", attempts, lastError);
        return new HealthResult(false, null, null, lastError);
    }

    /// <summary>Parses workspace definitions from deployed config and checks each workspace's health.</summary>
    public static WorkspaceHealthResult CheckWorkspaceHealth(string installRoot, int port)
    {
        var configPath = Path.Combine(installRoot, "appsettings.yaml");
        if (!File.Exists(configPath))
        {
            Log.Warning("No deployed appsettings.yaml found at {Path}; skipping workspace health checks.", installRoot);
            return new WorkspaceHealthResult(0, 0, 0);
        }

        var content = File.ReadAllText(configPath);
        var workspaces = ParseWorkspaceNames(content);

        if (workspaces.Count == 0)
        {
            Log.Information("  No workspaces defined in deployed configuration.");
            return new WorkspaceHealthResult(0, 0, 0);
        }

        int healthy = 0, failed = 0;
        foreach (var name in workspaces)
        {
            var probe = CheckHealth(port, attempts: 1, timeoutSeconds: 2, delaySeconds: 1);
            if (probe.Healthy)
            {
                healthy++;
                Log.Information("  OK {Name} health OK on port {Port}", name, port);
            }
            else
            {
                failed++;
                Log.Warning("  Workspace health check failed: {Name}; port={Port}; error={Error}",
                    name, port, probe.Error);
            }
        }

        return new WorkspaceHealthResult(workspaces.Count, healthy, failed);
    }

    /// <summary>Writes a deployment manifest JSON file to the install root.</summary>
    public static string WriteDeploymentManifest(
        string installRoot, string serviceName, string exeName, int port, string operation)
    {
        var manifestPath = Path.Combine(installRoot, ".mcpservice-deployment.json");

        var exeHashes = new List<object>();
        foreach (var file in Directory.EnumerateFiles(installRoot, "*.exe").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var hash = ComputeSha256(file);
            exeHashes.Add(new { name = Path.GetFileName(file), sha256 = hash });
        }

        var manifest = new
        {
            schemaVersion = 1,
            generatedUtc = DateTime.UtcNow.ToString("o"),
            generatedBy = "build/Build.UpdateService.cs",
            operation,
            serviceName,
            executable = exeName,
            port,
            executableHashes = exeHashes
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestPath, json);

        Log.Information("  Deployment manifest: {Path}", manifestPath);
        return manifestPath;
    }

    /// <summary>Builds the Windows service ImagePath (binPath) argument string.</summary>
    public static string GetServiceImagePath(string installRoot, string exeName, int port)
    {
        var exePath = Path.Combine(installRoot, exeName);
        return $"\"{exePath}\" --urls \"http://+:{port}\"";
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private static bool WaitForProcessExit(string processName, int timeoutSeconds)
    {
        for (int elapsed = 0; elapsed < timeoutSeconds; elapsed++)
        {
            var procs = Process.GetProcessesByName(processName);
            var any = procs.Length > 0;
            foreach (var p in procs) p.Dispose();
            if (!any) return true;
            Thread.Sleep(1000);
        }

        return false;
    }

    private static void RunSc(string arguments)
    {
        var process = ProcessTasks.StartProcess("sc.exe", arguments,
            logOutput: false, logInvocation: false);
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"sc.exe {arguments.Split(' ')[0]} failed with exit code {process.ExitCode}");
    }

    /// <summary>
    /// Runs sc.exe with raw argument string to avoid shell escaping issues with quoted binPath values.
    /// </summary>
    private static void RunScRaw(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var process = Process.Start(psi)!;
        process.WaitForExit(30_000);
        if (process.ExitCode != 0)
        {
            var verb = arguments.Split(' ')[0];
            throw new InvalidOperationException($"sc.exe {verb} failed with exit code {process.ExitCode}");
        }
    }

    private static string GetConfiguredDataFolder(string installRoot)
    {
        var yamlPath = Path.Combine(installRoot, "appsettings.yaml");
        string? configured = null;

        if (File.Exists(yamlPath))
        {
            try
            {
                var yamlContent = File.ReadAllText(yamlPath);
                var match = Regex.Match(yamlContent, @"(?m)^\s*DataFolder\s*:\s*(.+?)\s*$");
                if (match.Success)
                    configured = match.Groups[1].Value.Trim().Trim('\'', '"');
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to parse DataFolder from appsettings.yaml: {Error}", ex.Message);
            }
        }

        if (string.IsNullOrWhiteSpace(configured))
            configured = ".";

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(installRoot, configured));
    }

    private static List<string> BackupDataFolderContents(string dataFolder, string installRoot, string destRoot)
    {
        Directory.CreateDirectory(destRoot);
        var copied = new List<string>();

        var normalizedData = Path.GetFullPath(dataFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedInstall = Path.GetFullPath(installRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedData, normalizedInstall, StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("Configured DataFolder resolves to install root; backing up legacy runtime data patterns only.");

            // Legacy glob patterns
            foreach (var pattern in LegacyDataGlobs)
            {
                foreach (var file in Directory.EnumerateFiles(installRoot, pattern))
                {
                    File.Copy(file, Path.Combine(destRoot, Path.GetFileName(file)), true);
                    copied.Add(Path.GetFileName(file));
                }
            }

            // Legacy directories
            foreach (var dirName in LegacyDataDirectories)
            {
                var sourceDir = Path.Combine(installRoot, dirName);
                if (Directory.Exists(sourceDir))
                {
                    CopyDirectory(sourceDir, Path.Combine(destRoot, dirName));
                    copied.Add(dirName);
                }
            }
        }
        else if (Directory.Exists(dataFolder))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(dataFolder))
            {
                var name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                    CopyDirectory(entry, Path.Combine(destRoot, name));
                else
                    File.Copy(entry, Path.Combine(destRoot, name), true);
                copied.Add(name);
            }
        }
        else
        {
            Log.Warning("Configured data folder not found: {Path}", dataFolder);
        }

        return copied;
    }

    private static List<string> RestoreDataFolderContents(string sourceRoot, string destRoot)
    {
        if (!Directory.Exists(sourceRoot))
            return [];

        Directory.CreateDirectory(destRoot);
        var restored = new List<string>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(sourceRoot))
        {
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry))
                CopyDirectory(entry, Path.Combine(destRoot, name));
            else
                File.Copy(entry, Path.Combine(destRoot, name), true);
            restored.Add(name);
        }

        return restored;
    }

    private static bool IsPreserved(string relativePath, HashSet<string> preserveDirSet)
    {
        var topSegment = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        if (preserveDirSet.Contains(topSegment))
            return true;

        // Root-level file matching preserve patterns
        if (!relativePath.Contains(Path.DirectorySeparatorChar) && !relativePath.Contains(Path.AltDirectorySeparatorChar))
        {
            var fileName = Path.GetFileName(relativePath);
            foreach (var pattern in PreservePatterns)
            {
                if (string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"(?m)^\s*-\s*Name\s*:\s*(.+?)\s*$")]
    private static partial Regex WorkspaceNameRegex();

    private static List<string> ParseWorkspaceNames(string yamlContent)
    {
        // Simple regex approach: find Name: values under the Workspaces section
        var names = new List<string>();
        var inWorkspaces = false;

        foreach (var line in yamlContent.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.TrimStart().StartsWith("Workspaces:", StringComparison.Ordinal))
            {
                inWorkspaces = true;
                continue;
            }

            // Exit workspace section when we hit a non-indented, non-empty line that isn't a list item
            if (inWorkspaces && trimmed.Length > 0 && !char.IsWhiteSpace(trimmed[0]) && !trimmed.TrimStart().StartsWith("-"))
            {
                inWorkspaces = false;
                continue;
            }

            if (inWorkspaces)
            {
                var match = WorkspaceNameRegex().Match(trimmed);
                if (match.Success)
                {
                    var name = match.Groups[1].Value.Trim().Trim('\'', '"');
                    if (!string.IsNullOrWhiteSpace(name))
                        names.Add(name);
                }
            }
        }

        return names;
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Result of a backup operation.</summary>
    public sealed record BackupResult(
        string ConfiguredDataFolder,
        string[] BackedUpConfig,
        string[] BackedUpData,
        string? ArchivePath);

    /// <summary>Result of a restore operation.</summary>
    public sealed record RestoreResult(
        string[] RestoredConfig,
        string RestoredDataFolder,
        string[] RestoredData,
        string[] LegacyRemoved);

    /// <summary>Result of a health check.</summary>
    public sealed record HealthResult(bool Healthy, int? StatusCode, string? Content, string? Error);

    /// <summary>Result of workspace health checks.</summary>
    public sealed record WorkspaceHealthResult(int Checked, int Healthy, int Failed);
}
