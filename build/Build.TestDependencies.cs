using System.Diagnostics;
using System.Net.Http;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

#pragma warning disable CA1416 // Platform compatibility - integration-test tooling targets Windows dev machines

partial class Build
{
    const string SqlLocalDbMsiUrl =
        "https://download.microsoft.com/download/3/8/d/38de7036-2433-4207-8eae-06e247e17b25/SqlLocalDB.msi";

    const string PostgresBinariesZipUrl =
        "https://get.enterprisedb.com/postgresql/postgresql-17.5-1-windows-x64-binaries.zip";

    const string OllamaTagsEndpoint = "http://localhost:11434/api/tags";

    /// <summary>
    /// Model pulled for the QuadBrain Ollama tests. The default must be strong enough that the
    /// ArbiterOfTruth slot reliably accepts simple prompts; 1b-class models reject them as
    /// ambiguous and fail the content assertions.
    /// </summary>
    static string RequiredOllamaModel =>
        (Environment.GetEnvironmentVariable("QBRAINAI_QUADBRAIN_OLLAMA_MODEL")
            ?? Environment.GetEnvironmentVariable("MCP_QUADBRAIN_OLLAMA_MODEL")) is { Length: > 0 } configured
            ? configured.Trim()
            : "gemma4:e4b";

    static AbsolutePath TestToolsDirectory =>
        (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "QBrainAi" / "test-tools";

    /// <summary>
    /// Idempotent installer for the provider integration-test dependencies:
    /// SQL Server LocalDB (elevated MSI install when missing) and PostgreSQL server binaries
    /// (existing installation preferred; otherwise the portable EDB binaries zip is downloaded
    /// to a user-scoped tools directory, no elevation required). Re-running when everything is
    /// already present is a no-op.
    /// </summary>
    public Target InstallTestDependencies => _ => _
        .Description("Ensure SQL Server LocalDB and PostgreSQL binaries exist for provider integration tests (idempotent)")
        .Executes(() =>
        {
            EnsureSqlLocalDb();
            var pgBin = EnsurePostgresBinaries();
            Log.Information("Test dependencies ready. LocalDB: OK; PostgreSQL binaries: {PgBin}", pgBin);
        });

    /// <summary>
    /// Runs the <c>Category=Integration</c> tests living inside the unit-test projects, including
    /// the provider migration tests (SQLite in-memory, PostgreSQL, SQL Server LocalDB). The
    /// PostgreSQL tests boot their own ephemeral cluster with generated credentials on a free port
    /// via their xUnit class fixture (or honor <c>MCP_TEST_POSTGRES_CONNECTION</c>); SQL Server
    /// tests use an ad-hoc LocalDB database (auto-started). Integration tests run only through
    /// this target: the default <c>Test</c> target excludes the category. The dedicated
    /// <c>*.IntegrationTests</c> projects remain separate (their classes carry the same trait).
    /// </summary>
    public Target MigrationIntegrationTests => _ => _
        .Description("Run Category=Integration tests (provider migrations, process spawns) with provisioned dependencies")
        .DependsOn(InstallTestDependencies)
        .Executes(() =>
        {
            // Only projects that contain Category=Integration tests: an empty filter match
            // makes the test runner fail the target.
            var projectsWithIntegrationTests = new[]
            {
                TestsDirectory / "QBrainAi.Support.Mcp.Tests" / "QBrainAi.Support.Mcp.Tests.csproj",
                TestsDirectory / "Build.Tests" / "Build.Tests.csproj",
            };

            var runId = SessionLifeUnitGateReports.NormalizeRunId(TestRunId);
            if (runId is not null)
            {
                SessionLifeUnitGateReports.WriteInventory(
                    RootDirectory,
                    runId,
                    "provider",
                    projectsWithIntegrationTests.Select(project => (
                        Path.GetFileNameWithoutExtension(project.ToString()),
                        Path.GetRelativePath(RootDirectory, project).Replace('\\', '/'))));
            }

            var failures = new List<string>();
            foreach (var project in projectsWithIntegrationTests)
            {
                var projectName = Path.GetFileNameWithoutExtension(project.ToString());
                try
                {
                    if (runId is null)
                    {
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetFilter("Category=Integration")
                            .SetVerbosity(DotNetVerbosity.minimal));
                    }
                    else
                    {
                        var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, "provider", projectName);
                        Directory.CreateDirectory(directory);
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetFilter("Category=Integration")
                            .SetVerbosity(DotNetVerbosity.minimal)
                            .SetResultsDirectory(directory)
                            .SetLoggers($"trx;LogFileName={projectName}.trx"));
                        SessionLifeUnitGateReports.EnsureCanonicalTrx(directory, $"{projectName}.trx");
                    }
                }
                catch (Exception ex)
                {
                    if (runId is null)
                        throw;

                    failures.Add(projectName);
                    Log.Error(ex, "Session-life provider project {Project} failed.", projectName);
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Session-life provider test projects failed: " + string.Join(", ", failures));
            }
        });

    /// <summary>
    /// TR-MCP-NUKE-016: idempotent installer for the local Ollama service used by the QuadBrain Ollama integration tests
    /// (TEST-MCP-QBOLLAMA-001), on Windows and Linux. <see cref="OllamaInstallPlan.Choose"/> decides from what the machine shows: an
    /// installed ollama (on PATH, or on Windows in LocalAppData/Programs/Ollama) or a previous portable install is used, otherwise the
    /// operating system's portable release is downloaded into the test-tools folder; the server is started detached on localhost:11434 when
    /// it does not answer, and the required model (default gemma4:e4b, override with QBRAINAI_QUADBRAIN_OLLAMA_MODEL or
    /// MCP_QUADBRAIN_OLLAMA_MODEL) is pulled when it is absent. Re-running when everything is in place changes nothing.
    /// </summary>
    public Target InstallOllama => _ => _
        .Description("Ensure Ollama is installed, running on localhost:11434, and has the required model (idempotent; Windows and Linux)")
        .Executes(() =>
        {
            var model = RequiredOllamaModel;
            var isWindows = OperatingSystem.IsWindows();
            var serverAnswers = TryGetOllamaTags(out var tags);
            var plan = OllamaInstallPlan.Choose(new OllamaInstallFacts(
                isWindows,
                serverAnswers,
                serverAnswers && tags.Contains($"\"{model}\"", StringComparison.OrdinalIgnoreCase),
                FindInstalledOllama(isWindows),
                File.Exists(OllamaInstallPlan.PortableExecutable(isWindows, TestToolsDirectory)),
                TestToolsDirectory,
                model));
            Log.Information(
                "Ollama plan: source {Source}, executable {Executable}, start {Start}, pull {Pull} ({Model}).",
                plan.Source, plan.Executable, plan.StartServer, plan.PullModel, model);
            if (plan.Source == OllamaExecutableSource.None)
            {
                Log.Information("Ollama is running and model {Model} is installed.", model);
                return;
            }

            if (plan.Source == OllamaExecutableSource.Download)
                DownloadPortableOllama(plan, isWindows);
            if (plan.StartServer)
                EnsureOllamaServerRunning(plan.Executable!);
            if (!TryGetOllamaTags(out tags))
                throw new InvalidOperationException("Ollama server did not become reachable on localhost:11434.");
            if (plan.PullModel && !tags.Contains($"\"{model}\"", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Pulling Ollama model {Model} (this can take a while) ...", model);
                RunTool(plan.Executable!, $"pull {model}");
            }

            Log.Information("Ollama ready with model {Model}.", model);
        });

    // TR-MCP-NUKE-016: an installed ollama is the first one on PATH or, on Windows, the one in Ollama's install folder.
    static string? FindInstalledOllama(bool isWindows)
    {
        var name = isWindows ? "ollama.exe" : "ollama";
        var candidates = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory.Trim('"'), name))
            .ToList();
        if (isWindows)
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"));
        return candidates.FirstOrDefault(File.Exists);
    }

    // TR-MCP-NUKE-016: downloads the operating system's portable release into the test-tools folder and unpacks it under test-tools/ollama
    // (the zip with ZipFile on Windows; the .tar.zst with GNU tar's --zstd on Linux, which keeps bin/ollama executable).
    static void DownloadPortableOllama(OllamaInstallPlan plan, bool isWindows)
    {
        var targetDirectory = TestToolsDirectory / "ollama";
        targetDirectory.CreateDirectory();
        var archive = TestToolsDirectory / Path.GetFileName(new Uri(plan.DownloadUrl!).AbsolutePath);
        Log.Information("Ollama missing; downloading the portable release {Url} ...", plan.DownloadUrl);
        DownloadFile(plan.DownloadUrl!, archive);
        if (isWindows)
            System.IO.Compression.ZipFile.ExtractToDirectory(archive, targetDirectory, overwriteFiles: true);
        else
            RunTool("tar", $"--zstd -xf \"{archive}\" -C \"{targetDirectory}\"");
        if (!File.Exists(plan.Executable))
            throw new InvalidOperationException($"{plan.Executable} not found after unpacking {archive}.");
        Log.Information("Portable Ollama ready: {Path}", plan.Executable);
    }

    static void EnsureOllamaServerRunning(string ollamaExe)
    {
        if (TryGetOllamaTags(out _))
            return;

        Log.Information("Starting Ollama server (detached) ...");
        if (OperatingSystem.IsWindows())
        {
            // UseShellExecute detaches the child from this process's console pipes; with handle
            // inheritance the long-lived server would keep the build's stdout pipe open and hang
            // any caller that captures build output. The server intentionally outlives the build.
            var started = Process.Start(new ProcessStartInfo
            {
                FileName = ollamaExe,
                Arguments = "serve",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }) ?? throw new InvalidOperationException("Failed to start the Ollama server process.");
            _ = started;
        }
        else
        {
            // TR-MCP-NUKE-016 on Linux: a child started here inherits this build's output pipes whatever UseShellExecute says, so the
            // systemd user manager owns the long-lived server instead; it outlives the build as on Windows and holds none of its pipes.
            RunTool("systemd-run", $"--user --collect --quiet --unit qbrainai-test-ollama \"{ollamaExe}\" serve");
        }

        for (var attempt = 0; attempt < 30; attempt++)
        {
            Thread.Sleep(1000);
            if (TryGetOllamaTags(out _))
            {
                Log.Information("Ollama server is up on localhost:11434.");
                return;
            }
        }

        throw new InvalidOperationException("Ollama server did not answer on localhost:11434 within 30 seconds.");
    }

    static bool TryGetOllamaTags(out string tags)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            tags = http.GetStringAsync(OllamaTagsEndpoint).GetAwaiter().GetResult();
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            tags = string.Empty;
            return false;
        }
    }

    static void EnsureSqlLocalDb()
    {
        var existing = FindSqlLocalDbExe();
        if (existing is not null)
        {
            var newest = GetNewestLocalDbEngineVersion(existing);
            if (newest >= new Version(15, 0))
            {
                Log.Information("SQL Server LocalDB {Version} already installed: {Path}", newest, existing);
                return;
            }

            Log.Information("SQL Server LocalDB {Version} is older than the required 15.0; installing 2022 ...", newest);
        }
        else
        {
            Log.Information("SQL Server LocalDB missing; downloading installer ...");
        }
        WindowsServiceHelper.AssertElevated(nameof(InstallTestDependencies));
        var msiPath = Path.Combine(Path.GetTempPath(), "SqlLocalDB.msi");
        DownloadFile(SqlLocalDbMsiUrl, msiPath);

        Log.Information("Installing SQL Server LocalDB (silent) ...");
        var install = Process.Start(new ProcessStartInfo
        {
            FileName = "msiexec",
            Arguments = $"/i \"{msiPath}\" /qn IACCEPTSQLLOCALDBLICENSETERMS=YES",
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Failed to start msiexec for SqlLocalDB.msi.");
        install.WaitForExit();
        if (install.ExitCode != 0)
            throw new InvalidOperationException($"SqlLocalDB.msi install failed with exit code {install.ExitCode}.");

        _ = FindSqlLocalDbExe()
            ?? throw new InvalidOperationException("SqlLocalDB.exe not found after installation.");
        Log.Information("SQL Server LocalDB installed.");
    }

    static Version GetNewestLocalDbEngineVersion(string sqlLocalDbExe)
    {
        var output = RunToolCapture(sqlLocalDbExe, "versions");
        return System.Text.RegularExpressions.Regex.Matches(output, @"\((\d+)\.(\d+)")
            .Select(m => new Version(
                int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)))
            .DefaultIfEmpty(new Version(0, 0))
            .Max() ?? new Version(0, 0);
    }

    static string? FindSqlLocalDbExe()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft SQL Server");
        if (!Directory.Exists(root))
            return null;
        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
        };
        return Directory
            .EnumerateFiles(root, "SqlLocalDB.exe", enumeration)
            .OrderByDescending(p => p)
            .FirstOrDefault();
    }

    static string EnsurePostgresBinaries()
    {
        // Prefer an existing full installation (any version), newest first.
        var installedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL");
        if (Directory.Exists(installedRoot))
        {
            var installedBin = Directory
                .EnumerateDirectories(installedRoot)
                .OrderByDescending(d => d)
                .Select(d => Path.Combine(d, "bin"))
                .FirstOrDefault(bin => File.Exists(Path.Combine(bin, "initdb.exe")));
            if (installedBin is not null)
            {
                Log.Information("Using installed PostgreSQL binaries: {Bin}", installedBin);
                return installedBin;
            }
        }

        // Portable fallback: EDB binaries zip extracted to a user-scoped tools directory.
        var toolsBin = TestToolsDirectory / "pgsql" / "bin";
        if (File.Exists(toolsBin / "initdb.exe"))
        {
            Log.Information("Using portable PostgreSQL binaries: {Bin}", toolsBin);
            return toolsBin;
        }

        Log.Information("PostgreSQL binaries missing; downloading portable EDB zip ...");
        var zipPath = Path.Combine(Path.GetTempPath(), "postgresql-binaries.zip");
        DownloadFile(PostgresBinariesZipUrl, zipPath);
        TestToolsDirectory.CreateDirectory();
        // The zip contains a single top-level "pgsql" folder.
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, TestToolsDirectory, overwriteFiles: true);
        if (!File.Exists(toolsBin / "initdb.exe"))
            throw new InvalidOperationException($"initdb.exe not found under {toolsBin} after extracting PostgreSQL binaries.");
        Log.Information("Portable PostgreSQL binaries ready: {Bin}", toolsBin);
        return toolsBin;
    }

    static void RunTool(string fileName, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} {arguments.Split(' ')[0]} failed with exit code {process.ExitCode}.");
    }

    static string RunToolCapture(string fileName, string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        var stdout = process.StandardOutput.ReadToEnd();
        _ = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(fileName)} {arguments} failed with exit code {process.ExitCode}.");
        return stdout;
    }

    static void DownloadFile(string url, string destinationPath)
    {
        // TR-MCP-NUKE-016: the portable Ollama releases exceed 1 GB, so the body streams to disk once the headers arrive instead of being
        // buffered in memory under HttpClient's default 100-second limit.
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        using var response = http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using (var source = response.Content.ReadAsStream())
        using (var target = File.Create(destinationPath))
            source.CopyTo(target);
        Log.Information("Downloaded {Url} -> {Path} ({Bytes:N0} bytes)", url, destinationPath, new FileInfo(destinationPath).Length);
    }

}
