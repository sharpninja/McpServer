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

    /// <summary>TR-MCP-NUKE-019 AC-4: the release download folder; the host's asset name comes from <see cref="OllamaInstallLayout"/>.</summary>
    const string OllamaReleaseBaseUrl =
        "https://github.com/ollama/ollama/releases/latest/download/";

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
    /// TR-MCP-NUKE-018 AC-1: test-database env file read on non-Windows hosts when a provider connection
    /// variable is not already set; defaults to ~/.config/qbrain-test/test-db.env.
    /// </summary>
    [Parameter("Test-database env file for Linux provider test provisioning (default ~/.config/qbrain-test/test-db.env)")]
    readonly string TestDbEnvFile = string.Empty;

    /// <summary>
    /// Idempotent installer for the provider integration-test dependencies. On Windows: SQL Server
    /// LocalDB (elevated MSI install when missing) and PostgreSQL server binaries (existing
    /// installation preferred; otherwise the portable EDB binaries zip is downloaded to a
    /// user-scoped tools directory, no elevation required). On other hosts (TR-MCP-NUKE-018): the
    /// PostgreSQL and SQL Server endpoints come from the test-database env file and the dedicated
    /// qbrain-test containers, and must accept connections. Re-running when everything is already
    /// present is a no-op.
    /// </summary>
    public Target InstallTestDependencies => _ => _
        .Description("Ensure provider integration-test servers: LocalDB and PostgreSQL binaries on Windows; qbrain-test containers and env file elsewhere (idempotent)")
        .Executes(() =>
        {
            if (!OperatingSystem.IsWindows())
            {
                ProvisionProviderTestServers();
                return;
            }

            EnsureSqlLocalDb();
            var pgBin = EnsurePostgresBinaries();
            Log.Information("Test dependencies ready. LocalDB: OK; PostgreSQL binaries: {PgBin}", pgBin);
        });

    /// <summary>
    /// TR-MCP-NUKE-018 AC-1 to AC-4: provisions the provider test endpoints through the production
    /// provisioner and the real host boundary, logs each action (names only, never values), and fails
    /// the target with every error when any endpoint is unavailable (no skip).
    /// </summary>
    void ProvisionProviderTestServers()
    {
        var envFile = string.IsNullOrWhiteSpace(TestDbEnvFile)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ProviderTestNames.DefaultEnvFileRelativeToHome)
            : Path.GetFullPath(TestDbEnvFile);
        var result = new LinuxProviderTestProvisioner().Provision(envFile, new SystemProviderTestHost());
        foreach (var action in result.Actions)
            Log.Information("Provider test provisioning: {Action}", action);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                Log.Error("Provider test provisioning: {Error}", error);
            throw new InvalidOperationException("Provider test provisioning failed: " + string.Join("; ", result.Errors));
        }

        Log.Information("Test dependencies ready: {Postgres} and {SqlServer} accept connections.", ProviderTestNames.PostgresVariable, ProviderTestNames.SqlServerVariable);
    }

    /// <summary>
    /// Runs the <c>Category=Integration</c> tests living inside the unit-test projects, including
    /// the provider migration tests (SQLite in-memory, PostgreSQL, SQL Server LocalDB). The
    /// PostgreSQL tests boot their own ephemeral cluster with generated credentials on a free port
    /// via their xUnit class fixture (or honor <c>MCP_TEST_POSTGRES_CONNECTION</c>); SQL Server
    /// tests use an ad-hoc LocalDB database (auto-started) on Windows. On other hosts both
    /// providers use the endpoints InstallTestDependencies provisions (TR-MCP-NUKE-018 AC-6), and a
    /// provisioning failure fails this target before any test runs. Integration tests run only through
    /// this target: the default <c>Test</c> target excludes the category. The dedicated
    /// <c>*.IntegrationTests</c> projects remain separate (their classes carry the same trait).
    /// TR-MCP-NUKE-019: the selection is host-scoped through <see cref="IntegrationLaneFilter"/>; cases tagged
    /// <c>Platform=Linux</c> run only on Linux hosts, <c>Platform=Windows</c> only on Windows hosts, untagged on both.
    /// TR-MCP-NUKE-019 AC-4: the lane also provisions Ollama and the QuadBrain test model on both hosts
    /// (<see cref="InstallOllama"/>) before any test runs.
    /// </summary>
    public Target MigrationIntegrationTests => _ => _
        .Description("Run Category=Integration tests (provider migrations, process spawns) with provisioned dependencies")
        .DependsOn(InstallTestDependencies, InstallOllama)
        .Executes(() =>
        {
            // Only projects that contain Category=Integration tests: an empty filter match
            // makes the test runner fail the target.
            // TR-MCP-NUKE-019 AC-2: every project that holds Category=Integration cases, so the two hosts
            // together run every integration case. Repl.Core.Tests and QBAgent.Tests hold the tests that read the
            // repository checkout through the shared TestRepositoryRoot (TR-MCP-USECASE-018 AC-006).
            var projectsWithIntegrationTests = new[]
            {
                TestsDirectory / "QBrainAi.Support.Mcp.Tests" / "QBrainAi.Support.Mcp.Tests.csproj",
                TestsDirectory / "QBrainAi.McpAgent.Tests" / "QBrainAi.McpAgent.Tests.csproj",
                TestsDirectory / "QBrainAi.Repl.Core.Tests" / "QBrainAi.Repl.Core.Tests.csproj",
                TestsDirectory / "QBrainAi.QBAgent.Tests" / "QBrainAi.QBAgent.Tests.csproj",
                TestsDirectory / "Build.Tests" / "Build.Tests.csproj",
            };

            // TR-MCP-NUKE-019 AC-5: on Windows the lane runs only from an interactive desktop session; refuse
            // before any test runs, naming the missing session.
            var readiness = new InteractiveSessionPrecondition().Evaluate(IntegrationLaneFilter.CurrentHost(), Environment.UserInteractive);
            if (!readiness.CanRun)
            {
                Log.Error("Integration lane precondition: {Message}", readiness.FailureMessage);
                throw new InvalidOperationException(readiness.FailureMessage);
            }

            // TR-MCP-NUKE-019 AC-1 and AC-3: select this host's integration cases and name the filter in the log.
            var laneFilter = new IntegrationLaneFilter().Compose(IntegrationLaneFilter.CurrentHost());
            Log.Information("Integration lane filter: {Filter}", laneFilter);

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
                            .SetFilter(laneFilter)
                            .SetVerbosity(DotNetVerbosity.minimal));
                    }
                    else
                    {
                        var directory = SessionLifeUnitGateReports.LaneProjectDirectory(RootDirectory, runId, "provider", projectName);
                        Directory.CreateDirectory(directory);
                        DotNetTest(_ => _
                            .SetProjectFile(project)
                            .SetFilter(laneFilter)
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
    /// Idempotent installer for the local Ollama service used by the QuadBrain Ollama integration
    /// tests (TEST-MCP-QBOLLAMA-001), on Windows and Linux (TR-MCP-NUKE-019 AC-4): an installed
    /// executable is used when one exists (<see cref="OllamaInstallLayout.Candidates"/>), otherwise the
    /// host's portable release is placed in the user's QBrainAi test-tools folder; the server is started
    /// on localhost:11434 when none answers; the required model (<see cref="RequiredOllamaModel"/>, default
    /// gemma4:e4b) is pulled when the server does not list it. Re-running when everything is in place is a
    /// no-op; a failure fails the target, and the lane that depends on it, before any test runs.
    /// </summary>
    public Target InstallOllama => _ => _
        .Description("Ensure Ollama is installed, running on localhost:11434, and has the required model (idempotent; Windows and Linux)")
        .Executes(() =>
        {
            if (TryGetOllamaTags(out var tags) && tags.Contains($"\"{RequiredOllamaModel}\"", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Ollama is running and model {Model} is installed.", RequiredOllamaModel);
                return;
            }

            var ollamaExe = EnsureOllamaBinaries(IntegrationLaneFilter.CurrentHost());
            EnsureOllamaServerRunning(ollamaExe);

            if (!TryGetOllamaTags(out tags))
                throw new InvalidOperationException("Ollama server did not become reachable on localhost:11434.");

            if (!tags.Contains($"\"{RequiredOllamaModel}\"", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Pulling Ollama model {Model} (this can take a while) ...", RequiredOllamaModel);
                RunTool(ollamaExe, $"pull {RequiredOllamaModel}");
            }

            Log.Information("Ollama ready with model {Model}.", RequiredOllamaModel);
        });

    /// <summary>
    /// TR-MCP-NUKE-019 AC-4: returns an installed Ollama executable (PATH, then the usual install, then the portable
    /// location; <see cref="OllamaInstallLayout"/>), or places the host's portable release in the test-tools folder and
    /// returns its executable: on Windows the zip is extracted in process, on Linux the zstd tarball is extracted with
    /// tar --zstd (GNU tar and zstd on the host).
    /// </summary>
    static string EnsureOllamaBinaries(IntegrationLaneHost host)
    {
        var layout = new OllamaInstallLayout();
        var pathDirectories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var existing = layout.Candidates(host, pathDirectories, localApplicationData, (string)TestToolsDirectory).FirstOrDefault(File.Exists);
        if (existing is not null)
        {
            Log.Information("Using existing Ollama binaries: {Path}", existing);
            return existing;
        }

        var asset = layout.ReleaseAsset(host);
        Log.Information("Ollama missing; downloading the portable release {Asset} ...", asset);
        var archivePath = Path.Combine(Path.GetTempPath(), asset);
        DownloadFile(OllamaReleaseBaseUrl + asset, archivePath);
        var targetDirectory = TestToolsDirectory / "ollama";
        targetDirectory.CreateDirectory();
        if (host == IntegrationLaneHost.Windows)
            System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, targetDirectory, overwriteFiles: true);
        else
            RunTool("tar", $"--zstd -xf \"{archivePath}\" -C \"{targetDirectory}\"");

        var extracted = layout.PortableExecutable(host, (string)TestToolsDirectory);
        if (!File.Exists(extracted))
            throw new InvalidOperationException($"The Ollama executable {extracted} was not found after placing {asset} under {targetDirectory}.");
        Log.Information("Portable Ollama binaries ready: {Path}", extracted);
        return extracted;
    }

    static void EnsureOllamaServerRunning(string ollamaExe)
    {
        if (TryGetOllamaTags(out _))
            return;

        Log.Information("Starting Ollama server (detached) ...");
        // On Windows, UseShellExecute detaches the child from this process's console pipes; with handle
        // inheritance the long-lived server would keep the build's stdout pipe open and hang any caller
        // that captures build output. On Linux shell execution cannot start a program directly, so the
        // server inherits the build's output handles (the lane's runners write them to a log file, not a
        // pipe). The server intentionally outlives the build.
        var started = Process.Start(new ProcessStartInfo
        {
            FileName = ollamaExe,
            Arguments = "serve",
            UseShellExecute = OperatingSystem.IsWindows(),
            WindowStyle = ProcessWindowStyle.Hidden,
        }) ?? throw new InvalidOperationException("Failed to start the Ollama server process.");
        _ = started;

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
        using var http = new HttpClient();
        using var response = http.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var target = File.Create(destinationPath);
        response.Content.CopyToAsync(target).GetAwaiter().GetResult();
        Log.Information("Downloaded {Url} -> {Path} ({Bytes:N0} bytes)", url, destinationPath, target.Length);
    }

}
