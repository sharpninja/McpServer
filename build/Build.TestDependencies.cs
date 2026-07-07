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

    const string OllamaBinariesZipUrl =
        "https://github.com/ollama/ollama/releases/latest/download/ollama-windows-amd64.zip";

    const string OllamaTagsEndpoint = "http://localhost:11434/api/tags";

    /// <summary>
    /// Model pulled for the QuadBrain Ollama tests. The default must be strong enough that the
    /// ArbiterOfTruth slot reliably accepts simple prompts; 1b-class models reject them as
    /// ambiguous and fail the content assertions.
    /// </summary>
    static string RequiredOllamaModel =>
        Environment.GetEnvironmentVariable("MCP_QUADBRAIN_OLLAMA_MODEL") is { Length: > 0 } configured
            ? configured.Trim()
            : "gemma4:e4b";

    static AbsolutePath TestToolsDirectory =>
        (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / "McpServer" / "test-tools";

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
                TestsDirectory / "McpServer.Support.Mcp.Tests" / "McpServer.Support.Mcp.Tests.csproj",
                TestsDirectory / "Build.Tests" / "Build.Tests.csproj",
                TestsDirectory / "AgentPluginCore" / "AgentPluginCore.Tests.csproj",
            };

            foreach (var project in projectsWithIntegrationTests)
            {
                DotNetTest(_ => _
                    .SetProjectFile(project)
                    .SetFilter("Category=Integration")
                    .SetVerbosity(DotNetVerbosity.minimal));
            }
        });

    /// <summary>
    /// Idempotent installer for the local Ollama service used by the QuadBrain Ollama integration
    /// tests (TEST-MCP-QBOLLAMA-001): installs the portable Ollama binaries when absent (existing
    /// installation preferred), starts the server on localhost:11434 when not running, and pulls
    /// the required model (default llama3.2:1b, override with MCP_QUADBRAIN_OLLAMA_MODEL) when it
    /// is not present. Re-running when everything is in place is a no-op.
    /// </summary>
    public Target InstallOllama => _ => _
        .Description("Ensure Ollama is installed, running on localhost:11434, and has the required model (idempotent)")
        .Executes(() =>
        {
            if (TryGetOllamaTags(out var tags) && tags.Contains($"\"{RequiredOllamaModel}\"", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Ollama is running and model {Model} is installed.", RequiredOllamaModel);
                return;
            }

            var ollamaExe = EnsureOllamaBinaries();
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

    static string EnsureOllamaBinaries()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
            (string)(TestToolsDirectory / "ollama" / "ollama.exe"),
        };
        var existing = candidates.FirstOrDefault(File.Exists);
        if (existing is not null)
        {
            Log.Information("Using existing Ollama binaries: {Path}", existing);
            return existing;
        }

        Log.Information("Ollama missing; downloading portable binaries zip ...");
        var zipPath = Path.Combine(Path.GetTempPath(), "ollama-windows-amd64.zip");
        DownloadFile(OllamaBinariesZipUrl, zipPath);
        var targetDirectory = TestToolsDirectory / "ollama";
        targetDirectory.CreateDirectory();
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, targetDirectory, overwriteFiles: true);
        var extracted = targetDirectory / "ollama.exe";
        if (!File.Exists(extracted))
            throw new InvalidOperationException($"ollama.exe not found under {targetDirectory} after extraction.");
        Log.Information("Portable Ollama binaries ready: {Path}", extracted);
        return extracted;
    }

    static void EnsureOllamaServerRunning(string ollamaExe)
    {
        if (TryGetOllamaTags(out _))
            return;

        Log.Information("Starting Ollama server (detached) ...");
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
